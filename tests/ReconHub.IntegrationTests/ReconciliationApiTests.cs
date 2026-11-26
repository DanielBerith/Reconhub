using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ReconHub.Application.Common;
using ReconHub.Application.Events;
using ReconHub.Application.Exceptions;
using ReconHub.Domain.Auditing;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Transactions;
using ReconHub.Infrastructure.Persistence.Seeding;

namespace ReconHub.IntegrationTests;

/// <summary>End-to-end: events go in through the API, the detection cycle runs, exceptions come out through the API.</summary>
public sealed class ReconciliationApiTests(ReconHubApiFactory factory) : IClassFixture<ReconHubApiFactory>
{
    private static readonly Guid TenantId = DevelopmentDataSeeder.DemoTenantId;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Quantity_difference_between_ShipConfirm_and_DeliveryPosted_raises_a_Mismatch_exception()
    {
        var businessKey = NewBusinessKey();
        var now = factory.Clock.GetUtcNow();
        using var wms = factory.CreateSourceSystemClient(ReconHubApiFactory.WmsApiKey);
        using var erp = factory.CreateSourceSystemClient(ReconHubApiFactory.ErpApiKey);

        var shipped = await wms.PostAsJsonAsync("/api/events", Event("ShipConfirm", businessKey, now, quantity: 10), Ct);
        var delivered = await erp.PostAsJsonAsync("/api/events", Event("DeliveryPosted", businessKey, now.AddMinutes(5), quantity: 8), Ct);
        shipped.StatusCode.Should().Be(HttpStatusCode.Created);
        delivered.StatusCode.Should().Be(HttpStatusCode.Created);

        await factory.RunDetectionAsync(Ct);

        var exceptions = await ExceptionsForAsync(businessKey);
        var exception = exceptions.Should().ContainSingle().Subject;
        exception.Type.Should().Be(ExceptionType.Mismatch);
        exception.Status.Should().Be(ExceptionStatus.Open);
        exception.Detail.Should().Contain("quantity: ShipConfirm=10, DeliveryPosted=8");
        exception.FlowName.Should().Be(DevelopmentDataSeeder.FlowName);
        exception.TransactionState.Should().Be(TransactionState.Exception);
    }

    [Fact]
    public async Task Missing_DeliveryPosted_raises_a_Missing_exception_once_the_window_has_passed()
    {
        var businessKey = NewBusinessKey();
        using var wms = factory.CreateSourceSystemClient(ReconHubApiFactory.WmsApiKey);
        (await wms.PostAsJsonAsync("/api/events", Event("ShipConfirm", businessKey, factory.Clock.GetUtcNow(), quantity: 5), Ct))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        await factory.RunDetectionAsync(Ct);
        (await ExceptionsForAsync(businessKey)).Should().BeEmpty("the 15 minute window is still open");

        factory.Clock.Advance(TimeSpan.FromMinutes(16));
        await factory.RunDetectionAsync(Ct);

        var exception = (await ExceptionsForAsync(businessKey)).Should().ContainSingle().Subject;
        exception.Type.Should().Be(ExceptionType.Missing);
        exception.Detail.Should().Contain("DeliveryPosted was not received within 15m");
    }

    [Fact]
    public async Task Reposting_with_the_same_idempotency_key_returns_the_original_event_and_is_not_a_duplicate()
    {
        var businessKey = NewBusinessKey();
        var request = Event("ShipConfirm", businessKey, factory.Clock.GetUtcNow(), quantity: 3);
        using var wms = factory.CreateSourceSystemClient(ReconHubApiFactory.WmsApiKey);

        var first = await wms.PostAsJsonAsync("/api/events", request, Ct);
        var replay = await wms.PostAsJsonAsync("/api/events", request, Ct);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        replay.StatusCode.Should().Be(HttpStatusCode.OK);
        var original = await first.Content.ReadFromJsonAsync<IngestEventResponse>(Json, Ct);
        var replayed = await replay.Content.ReadFromJsonAsync<IngestEventResponse>(Json, Ct);
        replayed!.EventId.Should().Be(original!.EventId);
        replayed.Replayed.Should().BeTrue();
        replayed.TransactionIds.Should().BeEquivalentTo(original.TransactionIds);

        await factory.RunDetectionAsync(Ct);
        (await ExceptionsForAsync(businessKey)).Should().NotContain(e => e.Type == ExceptionType.Duplicate);
    }

    [Fact]
    public async Task Resolving_an_exception_requires_a_note_and_writes_an_audit_entry()
    {
        var businessKey = NewBusinessKey();
        using var wms = factory.CreateSourceSystemClient(ReconHubApiFactory.WmsApiKey);
        await wms.PostAsJsonAsync("/api/events", Event("ShipConfirm", businessKey, factory.Clock.GetUtcNow(), quantity: 1, status: "Failed"), Ct);
        await factory.RunDetectionAsync(Ct);
        var failed = (await ExceptionsForAsync(businessKey)).Should().ContainSingle(e => e.Type == ExceptionType.Failed).Subject;
        using var operatorClient = factory.CreateOperatorClient(TenantId);
        factory.Clock.Advance(TimeSpan.FromMinutes(5)); // support resolves it later, so audit timestamps differ

        var withoutNote = await operatorClient.PostAsJsonAsync($"/api/exceptions/{failed.Id}/resolve", new { note = "  " }, Ct);
        var resolved = await operatorClient.PostAsJsonAsync($"/api/exceptions/{failed.Id}/resolve", new { note = "Re-sent from WMS." }, Ct);
        var again = await operatorClient.PostAsJsonAsync($"/api/exceptions/{failed.Id}/resolve", new { note = "Again." }, Ct);

        withoutNote.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resolved.StatusCode.Should().Be(HttpStatusCode.OK);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var dto = await resolved.Content.ReadFromJsonAsync<ExceptionDto>(Json, Ct);
        dto!.Status.Should().Be(ExceptionStatus.Resolved);
        dto.ResolvedBy.Should().Be(ReconHubApiFactory.OperatorName);
        dto.ResolutionNote.Should().Be("Re-sent from WMS.");

        var audit = await factory.QueryAsync(TenantId, db => db.AuditEntries
            .Where(a => a.ReconExceptionId == failed.Id)
            .OrderBy(a => a.OccurredAt)
            .ToListAsync(Ct));
        audit.Select(a => a.Action).Should().Equal(AuditAction.ExceptionRaised, AuditAction.ExceptionResolved);
        audit[1].Actor.Should().Be(ReconHubApiFactory.OperatorName);
        audit[1].Details.Should().Be("Re-sent from WMS.");
    }

    [Fact]
    public async Task Requests_without_valid_credentials_are_rejected()
    {
        var request = Event("ShipConfirm", NewBusinessKey(), factory.Clock.GetUtcNow(), quantity: 1);
        using var anonymous = factory.CreateClient();
        using var wrongKey = factory.CreateSourceSystemClient("not-a-real-key");
        using var otherTenant = factory.CreateOperatorClient(Guid.NewGuid());

        (await anonymous.PostAsJsonAsync("/api/events", request, Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrongKey.PostAsJsonAsync("/api/events", request, Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/exceptions", Ct)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // A valid operator of another tenant is authenticated but sees none of this tenant's data.
        var page = await otherTenant.GetFromJsonAsync<PagedResult<ExceptionDto>>("/api/exceptions", Json, Ct);
        page!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Invalid_events_are_rejected_with_field_level_errors()
    {
        using var wms = factory.CreateSourceSystemClient(ReconHubApiFactory.WmsApiKey);

        var response = await wms.PostAsJsonAsync(
            "/api/events",
            new { eventType = "ShipConfirm", occurredAt = factory.Clock.GetUtcNow().AddDays(1), status = "Success", payload = "not-an-object" },
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json, Ct);
        var errors = problem.GetProperty("errors");
        errors.TryGetProperty("businessKey", out _).Should().BeTrue();
        errors.TryGetProperty("idempotencyKey", out _).Should().BeTrue();
        errors.TryGetProperty("occurredAt", out _).Should().BeTrue();
        errors.TryGetProperty("payload", out _).Should().BeTrue();
    }

    private static object Event(string eventType, string businessKey, DateTimeOffset occurredAt, int quantity, string status = "Success") => new
    {
        eventType,
        businessKey,
        occurredAt,
        status,
        payload = new { quantity },
        idempotencyKey = $"{eventType}-{businessKey}-{Guid.NewGuid():N}",
    };

    private static string NewBusinessKey() => $"SO-{Guid.NewGuid():N}"[..20];

    private async Task<IReadOnlyList<ExceptionDto>> ExceptionsForAsync(string businessKey)
    {
        using var operatorClient = factory.CreateOperatorClient(TenantId);
        var page = await operatorClient.GetFromJsonAsync<PagedResult<ExceptionDto>>(
            $"/api/exceptions?pageSize={ListExceptionsQuery.MaxPageSize}",
            Json,
            Ct);
        return [.. page!.Items.Where(e => e.BusinessKey == businessKey)];
    }
}

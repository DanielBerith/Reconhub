using ReconHub.Domain.Detection;
using ReconHub.Domain.Events;
using ReconHub.Domain.Flows;
using ReconHub.Domain.Transactions;

namespace ReconHub.UnitTests.Detection;

/// <summary>Builders for the reference flow: ShipConfirm (WMS) -> DeliveryPosted (ERP) within 15 minutes, quantity must match.</summary>
internal static class TestData
{
    public const string BusinessKey = "SO-1001";
    public const string QuantityTen = """{"quantity":10}""";

    public static readonly Guid TenantId = Guid.Parse("0195f0c0-0000-7000-8000-000000000001");
    public static readonly Guid WmsId = Guid.Parse("0195f0c0-0000-7000-8000-0000000000a1");
    public static readonly Guid ErpId = Guid.Parse("0195f0c0-0000-7000-8000-0000000000b1");
    public static readonly DateTimeOffset T0 = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public static FlowDefinition Flow(params string[] matchFields) => new(
        TenantId,
        "ShipConfirm -> DeliveryPosted",
        new FlowStep(WmsId, "ShipConfirm"),
        new FlowStep(ErpId, "DeliveryPosted"),
        Window,
        matchFields.Length == 0 ? ["quantity"] : matchFields,
        T0);

    public static IntegrationEvent ShipConfirm(
        DateTimeOffset occurredAt,
        string payload = QuantityTen,
        EventStatus status = EventStatus.Success,
        string? idempotencyKey = null,
        string eventType = "ShipConfirm") =>
        Event(WmsId, eventType, occurredAt, payload, status, idempotencyKey);

    public static IntegrationEvent DeliveryPosted(
        DateTimeOffset occurredAt,
        string payload = QuantityTen,
        EventStatus status = EventStatus.Success,
        string? idempotencyKey = null) =>
        Event(ErpId, "DeliveryPosted", occurredAt, payload, status, idempotencyKey);

    public static DetectionContext Context(FlowDefinition flow, DateTimeOffset now, params IntegrationEvent[] events) =>
        new(flow, events, now);

    public static Transaction Transaction(FlowDefinition flow, params IntegrationEvent[] events)
    {
        var transaction = new Transaction(TenantId, flow.Id, BusinessKey, T0);
        foreach (var e in events)
        {
            transaction.AttachEvent(e, flow, e.OccurredAt);
        }

        return transaction;
    }

    private static IntegrationEvent Event(
        Guid sourceSystemId,
        string eventType,
        DateTimeOffset occurredAt,
        string payload,
        EventStatus status,
        string? idempotencyKey) =>
        new(
            TenantId,
            sourceSystemId,
            eventType,
            BusinessKey,
            occurredAt,
            status,
            payload,
            idempotencyKey ?? Guid.NewGuid().ToString("N"),
            occurredAt);
}

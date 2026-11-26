using ReconHub.Domain.Detection;
using ReconHub.Domain.Exceptions;
using static ReconHub.UnitTests.Detection.TestData;

namespace ReconHub.UnitTests.Detection;

public sealed class DuplicateEventRuleTests
{
    private readonly DuplicateEventRule _rule = new();
    private readonly Domain.Flows.FlowDefinition _flow = Flow();

    [Fact]
    public void Raises_duplicate_for_the_second_occurrence_of_an_event_type()
    {
        var original = ShipConfirm(T0, idempotencyKey: "wms-1");
        var duplicate = ShipConfirm(T0.AddMinutes(2), idempotencyKey: "wms-2");

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(3), duplicate, original)).ToList();

        findings.Should().ContainSingle();
        findings[0].Type.Should().Be(ExceptionType.Duplicate);
        findings[0].EventId.Should().Be(duplicate.Id);
        findings[0].Detail.Should().Contain("'wms-2'").And.Contain("'wms-1'");
    }

    [Fact]
    public void Returns_nothing_when_each_event_type_occurs_once()
    {
        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(6), ShipConfirm(T0), DeliveryPosted(T0.AddMinutes(5))));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Raises_one_finding_per_extra_occurrence()
    {
        var original = ShipConfirm(T0);
        var second = ShipConfirm(T0.AddMinutes(1));
        var third = ShipConfirm(T0.AddMinutes(2));

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(3), original, second, third));

        findings.Select(f => f.EventId).Should().BeEquivalentTo([second.Id, third.Id]);
    }

    [Fact]
    public void Compares_event_types_case_insensitively()
    {
        var original = ShipConfirm(T0);
        var duplicate = ShipConfirm(T0.AddMinutes(1), eventType: "SHIPCONFIRM");

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(2), original, duplicate));

        findings.Should().ContainSingle().Which.EventId.Should().Be(duplicate.Id);
    }
}

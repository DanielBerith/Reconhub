using ReconHub.Domain.Detection;
using ReconHub.Domain.Exceptions;
using static ReconHub.UnitTests.Detection.TestData;

namespace ReconHub.UnitTests.Detection;

public sealed class MissingFollowUpRuleTests
{
    private readonly MissingFollowUpRule _rule = new();
    private readonly Domain.Flows.FlowDefinition _flow = Flow();

    [Fact]
    public void Returns_nothing_while_the_deadline_is_ahead()
    {
        var trigger = ShipConfirm(T0);

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(14), trigger));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Raises_missing_when_the_deadline_passes_without_a_follow_up()
    {
        var trigger = ShipConfirm(T0);

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(16), trigger)).ToList();

        findings.Should().ContainSingle();
        findings[0].Type.Should().Be(ExceptionType.Missing);
        findings[0].EventId.Should().Be(trigger.Id);
        findings[0].Detail.Should().Contain("DeliveryPosted was not received within 15m of ShipConfirm");
    }

    [Fact]
    public void Raises_missing_once_the_deadline_is_reached()
    {
        var findings = _rule.Evaluate(Context(_flow, T0 + Window, ShipConfirm(T0)));

        findings.Should().ContainSingle();
    }

    [Fact]
    public void Returns_nothing_when_the_follow_up_occurred_in_time()
    {
        var findings = _rule.Evaluate(Context(_flow, T0.AddHours(2), ShipConfirm(T0), DeliveryPosted(T0.AddMinutes(10))));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Raises_missing_when_the_follow_up_occurred_after_the_deadline()
    {
        var trigger = ShipConfirm(T0);
        var lateFollowUp = DeliveryPosted(T0.AddMinutes(20));

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(21), trigger, lateFollowUp)).ToList();

        findings.Should().ContainSingle();
        findings[0].EventId.Should().Be(trigger.Id);
        findings[0].Detail.Should().Contain("arrived late").And.Contain("5m after the deadline");
    }

    [Fact]
    public void Returns_nothing_without_a_trigger()
    {
        var findings = _rule.Evaluate(Context(_flow, T0.AddDays(1), DeliveryPosted(T0)));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Measures_the_deadline_from_the_earliest_trigger()
    {
        var first = ShipConfirm(T0);
        var second = ShipConfirm(T0.AddMinutes(10));

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(16), second, first)).ToList();

        findings.Should().ContainSingle().Which.EventId.Should().Be(first.Id);
    }
}

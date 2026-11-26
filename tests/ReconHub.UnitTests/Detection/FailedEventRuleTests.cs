using ReconHub.Domain.Detection;
using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;
using static ReconHub.UnitTests.Detection.TestData;

namespace ReconHub.UnitTests.Detection;

public sealed class FailedEventRuleTests
{
    private readonly FailedEventRule _rule = new();
    private readonly Domain.Flows.FlowDefinition _flow = Flow();

    [Fact]
    public void Raises_failed_for_an_event_reported_as_failed()
    {
        var failed = DeliveryPosted(T0.AddMinutes(5), status: EventStatus.Failed);

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(6), ShipConfirm(T0), failed)).ToList();

        findings.Should().ContainSingle();
        findings[0].Type.Should().Be(ExceptionType.Failed);
        findings[0].EventId.Should().Be(failed.Id);
        findings[0].Detail.Should().Contain("DeliveryPosted was reported as Failed");
    }

    [Fact]
    public void Returns_nothing_when_every_event_succeeded()
    {
        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(6), ShipConfirm(T0), DeliveryPosted(T0.AddMinutes(5))));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Raises_one_finding_per_failed_event()
    {
        var failedTrigger = ShipConfirm(T0, status: EventStatus.Failed);
        var failedFollowUp = DeliveryPosted(T0.AddMinutes(5), status: EventStatus.Failed);

        var findings = _rule.Evaluate(Context(_flow, T0.AddMinutes(6), failedTrigger, failedFollowUp));

        findings.Select(f => f.EventId).Should().BeEquivalentTo([failedTrigger.Id, failedFollowUp.Id]);
    }
}

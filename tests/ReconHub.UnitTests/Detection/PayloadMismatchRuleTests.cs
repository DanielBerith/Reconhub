using ReconHub.Domain.Detection;
using ReconHub.Domain.Events;
using ReconHub.Domain.Exceptions;
using static ReconHub.UnitTests.Detection.TestData;

namespace ReconHub.UnitTests.Detection;

public sealed class PayloadMismatchRuleTests
{
    private readonly PayloadMismatchRule _rule = new();

    [Fact]
    public void Returns_nothing_when_match_fields_are_equal()
    {
        var findings = Evaluate("""{"quantity":10,"sku":"A"}""", """{"quantity":10,"sku":"B"}""");

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Raises_mismatch_when_quantity_differs()
    {
        var flow = Flow("quantity");
        var trigger = ShipConfirm(T0, """{"quantity":10}""");
        var followUp = DeliveryPosted(T0.AddMinutes(5), """{"quantity":8}""");

        var findings = _rule.Evaluate(Context(flow, T0.AddMinutes(6), trigger, followUp)).ToList();

        findings.Should().ContainSingle();
        findings[0].Type.Should().Be(ExceptionType.Mismatch);
        findings[0].EventId.Should().Be(followUp.Id);
        findings[0].Detail.Should().Contain("quantity: ShipConfirm=10, DeliveryPosted=8");
    }

    [Theory]
    [InlineData("10", "10.0")]
    [InlineData("10", "\"10\"")]
    [InlineData("1e1", "10")]
    public void Treats_numerically_equal_values_as_equal(string left, string right)
    {
        var findings = Evaluate($$"""{"quantity":{{left}}}""", $$"""{"quantity":{{right}}}""");

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Compares_two_strings_as_text()
    {
        var findings = Evaluate("""{"quantity":"007"}""", """{"quantity":"7"}""");

        findings.Should().ContainSingle();
    }

    [Fact]
    public void Raises_mismatch_when_a_field_is_missing_on_one_side()
    {
        var findings = Evaluate("""{"quantity":10}""", """{"qty":10}""").ToList();

        findings.Should().ContainSingle().Which.Detail.Should().Contain(PayloadFieldComparer.MissingValue);
    }

    [Fact]
    public void Compares_nested_fields_by_dotted_path()
    {
        var flow = Flow("line.qty");

        var equal = Evaluate("""{"line":{"qty":3}}""", """{"line":{"qty":3}}""", flow);
        var different = Evaluate("""{"line":{"qty":3}}""", """{"line":{"qty":4}}""", flow);

        equal.Should().BeEmpty();
        different.Should().ContainSingle();
    }

    [Fact]
    public void Matches_property_names_case_insensitively()
    {
        var findings = Evaluate("""{"Quantity":10}""", """{"quantity":10}""");

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Skips_the_comparison_when_an_event_failed()
    {
        var flow = Flow();
        var trigger = ShipConfirm(T0, """{"quantity":10}""");
        var failedFollowUp = DeliveryPosted(T0.AddMinutes(5), """{"error":"timeout"}""", EventStatus.Failed);

        var findings = _rule.Evaluate(Context(flow, T0.AddMinutes(6), trigger, failedFollowUp));

        findings.Should().BeEmpty();
    }

    [Fact]
    public void Returns_nothing_until_the_follow_up_arrives()
    {
        var findings = _rule.Evaluate(Context(Flow(), T0.AddMinutes(6), ShipConfirm(T0)));

        findings.Should().BeEmpty();
    }

    private IEnumerable<DetectionFinding> Evaluate(string triggerPayload, string followUpPayload, Domain.Flows.FlowDefinition? flow = null) =>
        _rule.Evaluate(Context(
            flow ?? Flow(),
            T0.AddMinutes(6),
            ShipConfirm(T0, triggerPayload),
            DeliveryPosted(T0.AddMinutes(5), followUpPayload)));
}

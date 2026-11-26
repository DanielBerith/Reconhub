using ReconHub.Domain.Detection;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Transactions;
using static ReconHub.UnitTests.Detection.TestData;

namespace ReconHub.UnitTests.Detection;

public sealed class TransactionEvaluatorTests
{
    private readonly TransactionEvaluator _evaluator = TransactionEvaluator.Default;
    private readonly Domain.Flows.FlowDefinition _flow = Flow();

    [Fact]
    public void Completes_when_both_events_arrive_cleanly()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0), DeliveryPosted(T0.AddMinutes(5)));

        var raised = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(6));

        raised.Should().BeEmpty();
        transaction.State.Should().Be(TransactionState.Completed);
        transaction.EvaluationDueAt.Should().BeNull();
    }

    [Fact]
    public void Schedules_the_next_evaluation_at_the_deadline_while_waiting()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0));

        _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(1));

        transaction.State.Should().Be(TransactionState.Pending);
        transaction.EvaluationDueAt.Should().Be(T0 + Window);
        transaction.DueAt.Should().Be(T0 + Window);
    }

    [Fact]
    public void Never_raises_the_same_exception_twice()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0));

        var first = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(16));
        var second = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(17));

        first.Should().ContainSingle().Which.Type.Should().Be(ExceptionType.Missing);
        second.Should().BeEmpty();
        transaction.Exceptions.Should().ContainSingle();
        transaction.State.Should().Be(TransactionState.Exception);
    }

    [Fact]
    public void Returns_to_completed_when_the_last_open_exception_is_resolved()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0, """{"quantity":10}"""), DeliveryPosted(T0.AddMinutes(5), """{"quantity":8}"""));
        var raised = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(6));
        raised.Should().ContainSingle().Which.Type.Should().Be(ExceptionType.Mismatch);

        var resolved = transaction.ResolveException(raised[0].Id, "Corrected in ERP.", "alice", T0.AddMinutes(30));

        resolved.Status.Should().Be(ExceptionStatus.Resolved);
        resolved.ResolvedBy.Should().Be("alice");
        transaction.State.Should().Be(TransactionState.Completed);
    }

    [Fact]
    public void Resolving_missing_closes_the_transaction_without_a_follow_up()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0));
        var missing = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(16)).Single();

        transaction.ResolveException(missing.Id, "Posted manually in ERP.", "bob", T0.AddMinutes(40));

        transaction.State.Should().Be(TransactionState.Completed);
    }

    [Fact]
    public void Resolving_twice_is_rejected()
    {
        var transaction = Transaction(_flow, ShipConfirm(T0));
        var missing = _evaluator.Evaluate(transaction, _flow, T0.AddMinutes(16)).Single();
        transaction.ResolveException(missing.Id, "Done.", "bob", T0.AddMinutes(40));

        var act = () => transaction.ResolveException(missing.Id, "Again.", "bob", T0.AddMinutes(41));

        act.Should().Throw<Domain.Common.DomainException>();
    }
}

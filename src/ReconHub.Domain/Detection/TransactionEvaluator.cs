using ReconHub.Domain.Common;
using ReconHub.Domain.Exceptions;
using ReconHub.Domain.Flows;
using ReconHub.Domain.Transactions;

namespace ReconHub.Domain.Detection;

/// <summary>
/// Runs the detection rules against a transaction and records the outcome on it.
/// Pure apart from mutating the transaction passed in: no clock, no I/O.
/// </summary>
public sealed class TransactionEvaluator
{
    private readonly IReadOnlyList<IDetectionRule> _rules;

    public TransactionEvaluator(IEnumerable<IDetectionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = [.. rules];
    }

    /// <summary>The standard rule set: Missing, Failed, Duplicate, Mismatch.</summary>
    public static TransactionEvaluator Default { get; } = new(
    [
        new MissingFollowUpRule(),
        new FailedEventRule(),
        new DuplicateEventRule(),
        new PayloadMismatchRule(),
    ]);

    public IReadOnlyList<IDetectionRule> Rules => _rules;

    /// <summary>Evaluates the transaction and returns the exceptions newly raised by this evaluation.</summary>
    public IReadOnlyList<ReconException> Evaluate(Transaction transaction, FlowDefinition flow, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(flow);

        if (flow.Id != transaction.FlowDefinitionId)
        {
            throw new DomainException($"Flow {flow.Id} does not govern transaction {transaction.Id}.");
        }

        var context = new DetectionContext(flow, transaction.Events, now);
        var findings = _rules.SelectMany(rule => rule.Evaluate(context)).ToList();
        return transaction.RecordEvaluation(findings, NextEvaluationAt(context), now);
    }

    /// <summary>
    /// Missing is the only time-driven rule. While the follow-up is outstanding and the deadline is still ahead,
    /// look again at the deadline. Otherwise only a new event can change the outcome.
    /// </summary>
    private static DateTimeOffset? NextEvaluationAt(DetectionContext context) =>
        context.FollowUp is null && context.Deadline is { } deadline && deadline > context.Now ? deadline : null;
}

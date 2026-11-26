using ReconHub.Domain.Common;
using ReconHub.Domain.Events;

namespace ReconHub.Domain.Flows;

/// <summary>
/// An expected sequence: a trigger event must be followed by a follow-up event within a time window,
/// and the listed payload fields must match between the two.
/// </summary>
public sealed class FlowDefinition : Entity, ITenantScoped
{
    public const int NameMaxLength = 200;
    public const int MatchFieldMaxLength = 200;

    // Not readonly: EF Core materializes this primitive collection by assigning the field.
    private List<string> _matchFields = [];

    public FlowDefinition(
        Guid tenantId,
        string name,
        FlowStep trigger,
        FlowStep followUp,
        TimeSpan completionWindow,
        IEnumerable<string> matchFields,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(followUp);
        ArgumentNullException.ThrowIfNull(matchFields);

        if (trigger.Matches(followUp.SourceSystemId, followUp.EventType))
        {
            throw new DomainException("Trigger and follow-up steps must differ.");
        }

        if (completionWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(completionWindow), "Completion window must be positive.");
        }

        TenantId = Guard.NotEmpty(tenantId, nameof(tenantId));
        Name = Guard.Text(name, NameMaxLength, nameof(name));
        TriggerSourceSystemId = Guard.NotEmpty(trigger.SourceSystemId, nameof(trigger));
        TriggerEventType = Guard.Text(trigger.EventType, IntegrationEvent.EventTypeMaxLength, nameof(trigger));
        FollowUpSourceSystemId = Guard.NotEmpty(followUp.SourceSystemId, nameof(followUp));
        FollowUpEventType = Guard.Text(followUp.EventType, IntegrationEvent.EventTypeMaxLength, nameof(followUp));
        CompletionWindow = completionWindow;
        _matchFields.AddRange(matchFields
            .Select(f => Guard.Text(f, MatchFieldMaxLength, nameof(matchFields)))
            .Distinct(StringComparer.Ordinal));
        CreatedAt = createdAt;
        IsActive = true;
    }

    private FlowDefinition()
    {
        // EF Core
        Name = null!;
        TriggerEventType = null!;
        FollowUpEventType = null!;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public Guid TriggerSourceSystemId { get; private set; }

    public string TriggerEventType { get; private set; }

    public Guid FollowUpSourceSystemId { get; private set; }

    public string FollowUpEventType { get; private set; }

    /// <summary>Maximum time allowed between the trigger and the follow-up.</summary>
    public TimeSpan CompletionWindow { get; private set; }

    /// <summary>Payload field paths (dot-separated) whose values must be equal in both events.</summary>
    public IReadOnlyList<string> MatchFields => _matchFields;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public FlowStep Trigger => new(TriggerSourceSystemId, TriggerEventType);

    public FlowStep FollowUp => new(FollowUpSourceSystemId, FollowUpEventType);

    /// <summary>Returns the role an event plays in this flow, or null if the flow does not involve it.</summary>
    public FlowRole? RoleOf(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        return RoleOf(integrationEvent.SourceSystemId, integrationEvent.EventType);
    }

    public FlowRole? RoleOf(Guid sourceSystemId, string eventType)
    {
        if (Trigger.Matches(sourceSystemId, eventType))
        {
            return FlowRole.Trigger;
        }

        return FollowUp.Matches(sourceSystemId, eventType) ? FlowRole.FollowUp : null;
    }

    public void Deactivate() => IsActive = false;
}

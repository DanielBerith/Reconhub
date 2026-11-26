using System.Text.Json;
using FluentValidation;
using ReconHub.Domain.Events;

namespace ReconHub.Application.Events;

public sealed class IngestEventRequestValidator : AbstractValidator<IngestEventRequest>
{
    /// <summary>How far in the future an event may claim to have occurred, to tolerate clock drift between systems.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    public const int MaxPayloadLength = 64 * 1024;

    public IngestEventRequestValidator(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        RuleFor(x => x.EventType)
            .NotEmpty()
            .MaximumLength(IntegrationEvent.EventTypeMaxLength)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._:-]*$")
            .WithMessage("'Event Type' may contain only letters, digits and . _ : -");

        RuleFor(x => x.BusinessKey)
            .NotEmpty()
            .MaximumLength(IntegrationEvent.BusinessKeyMaxLength);

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(IntegrationEvent.IdempotencyKeyMaxLength);

        RuleFor(x => x.OccurredAt)
            .NotEqual(default(DateTimeOffset))
            .WithMessage("'Occurred At' is required.")
            .Must(occurredAt => occurredAt <= clock.GetUtcNow() + MaxClockSkew)
            .WithMessage($"'Occurred At' must not be more than {MaxClockSkew.TotalMinutes} minutes in the future.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("'Status' must be Success or Failed.");

        RuleFor(x => x.Payload)
            .Must(payload => payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.Object)
            .WithMessage("'Payload' must be a JSON object.")
            .Must(payload => payload.ValueKind != JsonValueKind.Object || payload.GetRawText().Length <= MaxPayloadLength)
            .WithMessage($"'Payload' must be at most {MaxPayloadLength} characters.");
    }
}

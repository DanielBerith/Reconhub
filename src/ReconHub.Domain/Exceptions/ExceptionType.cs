namespace ReconHub.Domain.Exceptions;

public enum ExceptionType
{
    /// <summary>The follow-up event was not received in time.</summary>
    Missing = 1,

    /// <summary>A source system reported the step as failed.</summary>
    Failed = 2,

    /// <summary>The same event type was reported more than once for one business key.</summary>
    Duplicate = 3,

    /// <summary>Configured payload fields differ between trigger and follow-up.</summary>
    Mismatch = 4,
}

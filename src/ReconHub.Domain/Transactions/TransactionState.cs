namespace ReconHub.Domain.Transactions;

public enum TransactionState
{
    /// <summary>Waiting for events, or not evaluated yet.</summary>
    Pending = 1,

    /// <summary>Both steps were received with no open exceptions, or support closed it.</summary>
    Completed = 2,

    /// <summary>At least one exception is open.</summary>
    Exception = 3,
}

using System.Diagnostics.CodeAnalysis;

namespace ReconHub.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}

/// <summary>An expected failure of a use case. Unexpected failures are exceptions.</summary>
public sealed record Error(ErrorType Type, string Code, string Message)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoValidationErrors = new Dictionary<string, string[]>();

    /// <summary>Field name (camelCase) to messages; empty unless <see cref="Type"/> is <see cref="ErrorType.Validation"/>.</summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } = NoValidationErrors;

    public static Error NotFound(string code, string message) => new(ErrorType.NotFound, code, message);

    public static Error Conflict(string code, string message) => new(ErrorType.Conflict, code, message);

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorType.Validation, "validation_failed", "One or more validation errors occurred.") { ValidationErrors = errors };
}

/// <summary>The outcome of a use case: a value or an <see cref="Common.Error"/>.</summary>
public sealed class Result<T>
{
    internal Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    internal Result(Error error) => Error = error;

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }
}

public static class Result
{
    public static Result<T> Success<T>(T value) => new(value);

    public static Result<T> Failure<T>(Error error) => new(error);
}

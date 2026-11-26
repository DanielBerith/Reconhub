using System.Text.Json;
using FluentValidation.Results;

namespace ReconHub.Application.Common;

internal static class ValidationExtensions
{
    /// <summary>Converts FluentValidation failures to an <see cref="Error"/> keyed by camelCase JSON property names.</summary>
    public static Error ToError(this ValidationResult result) =>
        Error.Validation(result.Errors
            .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).Distinct().ToArray()));
}

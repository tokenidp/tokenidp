using System.ComponentModel.DataAnnotations;

namespace Admin.Core;

internal sealed class EndpointValidationFilter : IEndpointFilter
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppLogger<EndpointValidationFilter> _logger;

    public EndpointValidationFilter(
        ICurrentUserService currentUserService,
        IAppLogger<EndpointValidationFilter> logger)
    {
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = ValidateArguments(context.Arguments);

        if (errors.Count > 0)
        {
            var correlationId = _currentUserService.CorrelationId;
            _logger.LogWarning("Validation failed. CorrelationId: {CorrelationId}", correlationId);

            return Results.BadRequest(ApiResult<ApiError>.Failure(
                ApiError.Failure(errors, correlationId)));
        }

        return await next(context);
    }

    private static Dictionary<string, string> ValidateArguments(IList<object?> arguments)
    {
        Dictionary<string, string> errors = new(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in arguments)
        {
            if (argument == null || ShouldSkipValidation(argument))
            {
                continue;
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);

            if (!Validator.TryValidateObject(argument, validationContext, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    var members = validationResult.MemberNames?.Any() == true
                        ? validationResult.MemberNames
                        : new[] { argument.GetType().Name };

                    foreach (var member in members)
                    {
                        AddError(errors, member, validationResult.ErrorMessage);
                    }
                }
            }
        }

        return errors;
    }

    private static bool ShouldSkipValidation(object argument)
    {
        var type = argument.GetType();

        if (type.IsPrimitive || type.IsEnum)
        {
            return true;
        }

        if (type == typeof(string) || type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return true;
        }

        return false;
    }

    private static void AddError(Dictionary<string, string> errors, string member, string? message)
    {
        var errorMessage = string.IsNullOrWhiteSpace(message)
            ? "Validation error."
            : message;

        if (errors.TryGetValue(member, out var existing))
        {
            errors[member] = $"{existing} {errorMessage}";
            return;
        }

        errors[member] = errorMessage;
    }
}

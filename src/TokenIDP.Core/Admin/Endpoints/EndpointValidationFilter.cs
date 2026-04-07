using FluentValidation;
using FluentValidation.Results;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Core.Admin.Endpoints;

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
        var errors = await ValidateArgumentsAsync(
            context.Arguments,
            context.HttpContext.RequestServices,
            context.HttpContext.RequestAborted);

        if (errors.Count > 0)
        {
            var correlationId = _currentUserService.CorrelationId;
            _logger.LogWarning("Validation failed. CorrelationId: {CorrelationId}", correlationId);

            return Results.BadRequest(ApiResult<ApiError>.Failure(
                ApiError.Failure(errors, correlationId.ToString())));
        }

        return await next(context);
    }

    private static async Task<Dictionary<string, string>> ValidateArgumentsAsync(
        IList<object?> arguments,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> errors = new(StringComparer.OrdinalIgnoreCase);

        foreach (var argument in arguments)
        {
            if (argument == null || ShouldSkipValidation(argument))
            {
                continue;
            }

            var usedFluentValidation = await AddFluentValidationErrorsAsync(
                argument,
                serviceProvider,
                cancellationToken,
                errors);

            if (!usedFluentValidation)
            {
                AddDataAnnotationsErrors(argument, errors);
            }
        }

        return errors;
    }

    private static async Task<bool> AddFluentValidationErrorsAsync(
        object argument,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        Dictionary<string, string> errors)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(validatorType);

        if (serviceProvider.GetService(enumerableType) is not IEnumerable validators)
        {
            return false;
        }

        var validationContextType = typeof(ValidationContext<>).MakeGenericType(argument.GetType());
        var validationContext = Activator.CreateInstance(validationContextType, argument) as IValidationContext
            ?? throw new InvalidOperationException($"Unable to create validation context for {argument.GetType().Name}.");

        var usedValidators = false;

        foreach (var validator in validators)
        {
            if (validator is not IValidator fluentValidator)
            {
                continue;
            }

            usedValidators = true;

            var result = await fluentValidator.ValidateAsync(validationContext, cancellationToken);

            foreach (var failure in result.Errors)
            {
                AddError(errors, failure.PropertyName, failure.ErrorMessage);
            }
        }

        return usedValidators;
    }

    private static void AddDataAnnotationsErrors(
        object argument,
        Dictionary<string, string> errors)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new ValidationContext(argument);

        if (Validator.TryValidateObject(argument, validationContext, validationResults, true))
        {
            return;
        }

        foreach (var validationResult in validationResults)
        {
            var members = validationResult.MemberNames?.Any() == true
                ? validationResult.MemberNames
                : [argument.GetType().Name];

            foreach (var member in members)
            {
                AddError(errors, member, validationResult.ErrorMessage);
            }
        }
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


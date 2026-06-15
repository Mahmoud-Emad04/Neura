namespace Neura.Api.Infrastructure;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetService<ILogger<ValidationFilter>>();

        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var argumentType = argument.GetType();

            // Skip primitive types, strings, and standard value types
            if (argumentType.IsPrimitive || argumentType == typeof(string) || argumentType.IsValueType)
                continue;

            // Resolve the generic validator type
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator is not null)
            {
                logger?.LogInformation("Running FluentValidation for {ArgumentType}", argumentType.Name);

                // Use the non-generic ValidationContext
                var validationContext = new ValidationContext<object>(argument);

                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
                if (!validationResult.IsValid)
                {
                    logger?.LogWarning("Validation failed for {ArgumentType}: {Errors}",
                        argumentType.Name, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

                    var errors = validationResult.Errors
                        .GroupBy(x => x.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.ErrorMessage).ToArray()
                        );

                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}

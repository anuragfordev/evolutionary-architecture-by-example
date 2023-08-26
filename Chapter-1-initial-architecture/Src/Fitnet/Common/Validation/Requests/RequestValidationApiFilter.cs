namespace EvolutionaryArchitecture.Fitnet.Common.Validation.Requests;

using System.Net;
using FluentValidation;
using Logging;

internal sealed class RequestValidationApiFilter<TRequestToValidate> : IEndpointFilter where TRequestToValidate : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<IRequestValidationLogger>();
        var requestToValidate = context.Arguments.FirstOrDefault(argument => argument?.GetType() == typeof(TRequestToValidate)) as TRequestToValidate;
        var validator = context.HttpContext.RequestServices.GetService<IValidator<TRequestToValidate>>();

        if (validator is null)
        {
            return await next.Invoke(context);
        }

        var validationResult = await validator.ValidateAsync(requestToValidate!);
        if (validationResult.IsValid)
        {
            return await next.Invoke(context);
        }

        var errors = validationResult.ToDictionary();

        logger.LogValidationErrors(errors);

        return Results.ValidationProblem(errors,
            statusCode: (int)HttpStatusCode.BadRequest);
    }
}
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BimCollab.ClashDetection.Api.Middleware;

internal sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        HttpValidationProblemDetails problemDetails;

        switch (exception)
        {
            case ValidationException validationException:
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());

                problemDetails = new HttpValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc9457"
                };
                break;

            case JsonException jsonException:
                problemDetails = new HttpValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["RequestBody"] = [$"Invalid JSON: {jsonException.Message}"]
                    })
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request body is invalid.",
                    Type = "https://tools.ietf.org/html/rfc9457"
                };
                break;

            case BadHttpRequestException badRequestException:
                problemDetails = new HttpValidationProblemDetails(
                    new Dictionary<string, string[]>
                    {
                        ["RequestBody"] = [badRequestException.Message]
                    })
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "The request body is invalid.",
                    Type = "https://tools.ietf.org/html/rfc9457"
                };
                break;

            default:
                return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

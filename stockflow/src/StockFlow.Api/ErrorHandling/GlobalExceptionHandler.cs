using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Exceptions;
using StockFlow.Domain.Exceptions;

namespace StockFlow.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", "not-found"),
            InsufficientStockException => (StatusCodes.Status409Conflict, "Insufficient stock", "insufficient-stock"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", "conflict"),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", "rule-violation"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", "unauthorized"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "unexpected")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Request refused with {Status}: {Message}", status, exception.Message);
        }

        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://stockflow.local/errors/{code}",
                Detail = status < 500 ? exception.Message : null
            }
        });
    }
}
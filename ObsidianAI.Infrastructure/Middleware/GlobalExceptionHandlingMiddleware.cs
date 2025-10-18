using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObsidianAI.Domain.Exceptions;

namespace ObsidianAI.Infrastructure.Middleware;

/// <summary>
/// Middleware for handling exceptions globally and returning structured error responses.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="environment">The hosting environment.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                CreateValidationProblemDetails(context, validationEx)
            ),
            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                CreateProblemDetails(context, notFoundEx, HttpStatusCode.NotFound)
            ),
            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                CreateProblemDetails(context, conflictEx, HttpStatusCode.Conflict)
            ),
            ExternalServiceException serviceEx => (
                HttpStatusCode.BadGateway,
                CreateProblemDetails(context, serviceEx, HttpStatusCode.BadGateway)
            ),
            DomainException domainEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(context, domainEx, HttpStatusCode.BadRequest)
            ),
            InvalidOperationException invalidOpEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(context, invalidOpEx, HttpStatusCode.BadRequest)
            ),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                CreateProblemDetails(context, argEx, HttpStatusCode.BadRequest)
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblemDetails(context, exception, HttpStatusCode.InternalServerError)
            )
        };

        // Log the exception with appropriate level
        LogException(exception, statusCode);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateProblemDetails(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode)
    {
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitle(statusCode),
            Detail = GetDetail(exception, statusCode),
            Instance = context.Request.Path,
            Type = GetTypeUrl(statusCode)
        };

        // Add error code for domain exceptions
        if (exception is DomainException domainException)
        {
            problemDetails.Extensions["errorCode"] = domainException.ErrorCode;
        }

        // Include stack trace in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    message = exception.InnerException.Message,
                    type = exception.InnerException.GetType().Name
                };
            }
        }

        // Add request metadata
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        return problemDetails;
    }

    private ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext context,
        ValidationException validationException)
    {
        var problemDetails = new ValidationProblemDetails(validationException.Errors)
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "One or more validation errors occurred",
            Detail = validationException.Message,
            Instance = context.Request.Path,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
        };

        problemDetails.Extensions["errorCode"] = validationException.ErrorCode;
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        // Include stack trace in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["stackTrace"] = validationException.StackTrace;
        }

        return problemDetails;
    }

    private void LogException(Exception exception, HttpStatusCode statusCode)
    {
        var logLevel = statusCode switch
        {
            HttpStatusCode.BadRequest => LogLevel.Warning,
            HttpStatusCode.NotFound => LogLevel.Information,
            HttpStatusCode.Conflict => LogLevel.Warning,
            HttpStatusCode.BadGateway => LogLevel.Error,
            HttpStatusCode.InternalServerError => LogLevel.Critical,
            _ => LogLevel.Error
        };

        _logger.Log(
            logLevel,
            exception,
            "Unhandled exception: {ExceptionType} - {Message}",
            exception.GetType().Name,
            exception.Message);
    }

    private static string GetTitle(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Bad Request",
        HttpStatusCode.NotFound => "Resource Not Found",
        HttpStatusCode.Conflict => "Conflict",
        HttpStatusCode.BadGateway => "External Service Error",
        HttpStatusCode.InternalServerError => "Internal Server Error",
        _ => "An error occurred"
    };

    private string GetDetail(Exception exception, HttpStatusCode statusCode)
    {
        // Return the actual exception message for domain exceptions
        if (exception is DomainException)
        {
            return exception.Message;
        }

        // For other exceptions, be careful about exposing details in production
        if (_environment.IsDevelopment())
        {
            return exception.Message;
        }

        return statusCode switch
        {
            HttpStatusCode.InternalServerError => "An unexpected error occurred. Please try again later.",
            HttpStatusCode.BadGateway => "An error occurred while communicating with an external service.",
            _ => exception.Message
        };
    }

    private static string GetTypeUrl(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        HttpStatusCode.BadGateway => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => "https://tools.ietf.org/html/rfc9110"
    };
}

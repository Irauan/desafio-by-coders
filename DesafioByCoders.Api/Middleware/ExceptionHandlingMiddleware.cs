using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DesafioByCoders.Api.Middleware;

/// <summary>
/// Middleware for centralized exception handling and logging.
/// </summary>
/// <remarks>
/// <para>
/// This middleware catches all unhandled exceptions in the request pipeline,
/// logs them with appropriate context, and returns standardized error responses to clients.
/// </para>
/// <para>
/// <b>Features:</b>
/// </para>
/// <list type="bullet">
/// <item><description>Catches all unhandled exceptions</description></item>
/// <item><description>Logs exceptions with request context (method, path, user)</description></item>
/// <item><description>Returns RFC 7807 ProblemDetails responses</description></item>
/// <item><description>Hides sensitive error details in production</description></item>
/// <item><description>Includes stack traces in development environment</description></item>
/// </list>
/// </remarks>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recording exception details.</param>
    /// <param name="environment">Host environment information to determine error detail level.</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware to handle the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// If an exception occurs during request processing, this method:
    /// </para>
    /// <list type="number">
    /// <item><description>Logs the exception with full context</description></item>
    /// <item><description>Determines the appropriate HTTP status code</description></item>
    /// <item><description>Creates a ProblemDetails response</description></item>
    /// <item><description>Returns the error response to the client</description></item>
    /// </list>
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unhandled exception occurred while processing request {Method} {Path}. User: {User}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.User.Identity?.Name ?? "Anonymous",
                context.TraceIdentifier
            );

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles the exception by creating and returning an appropriate error response.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <remarks>
    /// <para>
    /// The response format follows RFC 7807 (Problem Details for HTTP APIs).
    /// </para>
    /// <para>
    /// <b>Status Code Mapping:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="OperationCanceledException"/> → 499 Client Closed Request</description></item>
    /// <item><description><see cref="UnauthorizedAccessException"/> → 403 Forbidden</description></item>
    /// <item><description><see cref="ArgumentException"/> → 400 Bad Request</description></item>
    /// <item><description>All other exceptions → 500 Internal Server Error</description></item>
    /// </list>
    /// <para>
    /// In production, error details are sanitized to prevent information disclosure.
    /// In development, full exception details including stack traces are included.
    /// </para>
    /// </remarks>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title) = GetStatusCodeAndTitle(exception);
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Instance = context.Request.Path,
            Detail = GetErrorDetail(exception)
        };

        // Add trace identifier for debugging
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        // Include exception details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = new
                {
                    type = exception.InnerException.GetType().FullName,
                    message = exception.InnerException.Message
                };
            }
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(problemDetails, options);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Determines the appropriate HTTP status code and title based on the exception type.
    /// </summary>
    /// <param name="exception">The exception to analyze.</param>
    /// <returns>A tuple containing the HTTP status code and title.</returns>
    private static (HttpStatusCode StatusCode, string Title) GetStatusCodeAndTitle(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => (
                (HttpStatusCode)499, // Client Closed Request
                "Request was cancelled"
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Forbidden,
                "Access forbidden"
            ),
            ArgumentException or ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Invalid request"
            ),
            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                "Invalid operation"
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                "An error occurred while processing your request"
            )
        };
    }

    /// <summary>
    /// Gets the error detail message to include in the response.
    /// </summary>
    /// <param name="exception">The exception to extract details from.</param>
    /// <returns>An appropriate error message for the client.</returns>
    /// <remarks>
    /// In production, generic messages are returned to prevent information disclosure.
    /// In development, the actual exception message is included for debugging.
    /// </remarks>
    private string GetErrorDetail(Exception exception)
    {
        if (_environment.IsDevelopment())
        {
            return exception.Message;
        }

        // Return generic messages in production to avoid information disclosure
        return exception switch
        {
            OperationCanceledException => "The request was cancelled by the client.",
            UnauthorizedAccessException => "You do not have permission to perform this action.",
            ArgumentException => "The request contains invalid data.",
            _ => "An unexpected error occurred. Please try again later or contact support if the problem persists."
        };
    }
}

using System.Net;
using System.Text.Json;
using DesafioByCoders.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace DesafioByCoders.Api.Tests.Units.Middleware;

/// <summary>
/// Unit tests for the ExceptionHandlingMiddleware.
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<IHostEnvironment> _environmentMock;

    public ExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _environmentMock = new Mock<IHostEnvironment>();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExceptionOccurs_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (_) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new InvalidOperationException("Test exception");
        RequestDelegate next = (_) => throw exception;

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenOperationCanceledException_Returns499StatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new OperationCanceledException("Request cancelled");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessException_Returns403StatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new UnauthorizedAccessException("Access denied");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_Returns400StatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new ArgumentException("Invalid argument");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenGenericException_Returns500StatusCode()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new Exception("Generic error");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InProduction_ReturnsProblemDetailsWithoutExceptionDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new InvalidOperationException("Sensitive error details");

        // Setup environment as Production (IsDevelopment will return false)
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal((int)HttpStatusCode.BadRequest, problemDetails.Status);
        Assert.DoesNotContain("Sensitive error details", problemDetails.Detail ?? "");
        
        // Should not contain exception details in production
        Assert.False(problemDetails.Extensions.ContainsKey("exceptionType"));
        Assert.False(problemDetails.Extensions.ContainsKey("stackTrace"));
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_ReturnsProblemDetailsWithExceptionDetails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var exception = new InvalidOperationException("Detailed error message");
        RequestDelegate next = (_) => throw exception;

        // Setup environment as Development (IsDevelopment will return true)
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Contains("Detailed error message", problemDetails.Detail ?? "");
        
        // Should contain exception details in development
        Assert.True(problemDetails.Extensions.ContainsKey("exceptionType"));
        Assert.True(problemDetails.Extensions.ContainsKey("stackTrace"));
    }

    [Fact]
    public async Task InvokeAsync_WithInnerException_IncludesInnerExceptionInDevelopment()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);
        RequestDelegate next = (_) => throw outerException;

        // Setup environment as Development (IsDevelopment will return true)
        _environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Extensions.ContainsKey("innerException"));
    }

    [Fact]
    public async Task InvokeAsync_AlwaysIncludesTraceId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.TraceIdentifier = "test-trace-id";
        
        RequestDelegate next = (_) => throw new Exception("Test error");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Extensions.ContainsKey("traceId"));
        Assert.Equal("test-trace-id", problemDetails.Extensions["traceId"]?.ToString());
    }

    [Fact]
    public async Task InvokeAsync_SetsCorrectContentType()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new Exception("Test error");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_SetsProblemDetailsInstanceToRequestPath()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/test";
        context.Response.Body = new MemoryStream();
        
        RequestDelegate next = (_) => throw new Exception("Test error");

        _environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        var middleware = new ExceptionHandlingMiddleware(next, _loggerMock.Object, _environmentMock.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problemDetails);
        Assert.Equal("/api/v1/test", problemDetails.Instance);
    }
}

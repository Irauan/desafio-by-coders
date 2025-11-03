using System.Diagnostics;
using DesafioByCoders.Api.Handlers;

namespace DesafioByCoders.Api.Features;

/// <summary>
/// Decorator that adds logging capabilities to handlers without response.
/// Logs the start of request handling, completion time, and any exceptions that occur.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled. Must implement <see cref="IRequest"/>.</typeparam>
/// <remarks>
/// This decorator wraps around the actual handler implementation and provides:
/// <list type="bullet">
///   <item><description>Information logging when a request starts being handled</description></item>
///   <item><description>Performance tracking using <see cref="Stopwatch"/> to measure execution time</description></item>
///   <item><description>Success logging with elapsed time when request completes successfully</description></item>
///   <item><description>Error logging with elapsed time and exception details when request fails</description></item>
/// </list>
/// The decorator is automatically applied to all handlers via Scrutor decoration in the dependency injection configuration.
/// </remarks>
internal class LoggingHandlerDecorator<TRequest> : IHandler<TRequest> where TRequest : IRequest
{
    private readonly IHandler<TRequest> _inner;
    private readonly ILogger<LoggingHandlerDecorator<TRequest>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingHandlerDecorator{TRequest}"/> class.
    /// </summary>
    /// <param name="inner">The actual handler implementation to be decorated.</param>
    /// <param name="logger">The logger instance used to log request handling information.</param>
    public LoggingHandlerDecorator(IHandler<TRequest> inner, ILogger<LoggingHandlerDecorator<TRequest>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request asynchronously with logging capabilities.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// The method performs the following steps:
    /// <list type="number">
    ///   <item><description>Logs the start of request handling with the request type name</description></item>
    ///   <item><description>Starts a stopwatch to measure execution time</description></item>
    ///   <item><description>Delegates to the inner handler to perform the actual work</description></item>
    ///   <item><description>Logs successful completion with elapsed time in milliseconds</description></item>
    ///   <item><description>If an exception occurs, logs the error with elapsed time and rethrows the exception</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Rethrows any exception thrown by the inner handler after logging it.</exception>
    public async Task HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Handling {RequestName}", requestName);
            
            await _inner.HandleAsync(request, cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("Completed {RequestName} in {ElapsedMilliseconds}ms", 
                requestName, 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMilliseconds}ms", 
                requestName, 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Decorator that adds logging capabilities to handlers with response.
/// Logs the start of request handling, completion time, and any exceptions that occur.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled. Must implement <see cref="IRequest"/>.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// This decorator wraps around the actual handler implementation and provides:
/// <list type="bullet">
///   <item><description>Information logging when a request starts being handled</description></item>
///   <item><description>Performance tracking using <see cref="Stopwatch"/> to measure execution time</description></item>
///   <item><description>Success logging with elapsed time when request completes successfully</description></item>
///   <item><description>Error logging with elapsed time and exception details when request fails</description></item>
/// </list>
/// The decorator is automatically applied to all handlers via Scrutor decoration in the dependency injection configuration.
/// Unlike <see cref="LoggingHandlerDecorator{TRequest}"/>, this version handles requests that return a response.
/// </remarks>
internal class LoggingHandlerDecorator<TRequest, TResponse> : IHandler<TRequest, TResponse> where TRequest : IRequest
{
    private readonly IHandler<TRequest, TResponse> _inner;
    private readonly ILogger<LoggingHandlerDecorator<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingHandlerDecorator{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="inner">The actual handler implementation to be decorated.</param>
    /// <param name="logger">The logger instance used to log request handling information.</param>
    public LoggingHandlerDecorator(IHandler<TRequest, TResponse> inner, ILogger<LoggingHandlerDecorator<TRequest, TResponse>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request asynchronously with logging capabilities and returns a response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation that contains the response from the inner handler.</returns>
    /// <remarks>
    /// The method performs the following steps:
    /// <list type="number">
    ///   <item><description>Logs the start of request handling with the request type name</description></item>
    ///   <item><description>Starts a stopwatch to measure execution time</description></item>
    ///   <item><description>Delegates to the inner handler to perform the actual work and get the response</description></item>
    ///   <item><description>Logs successful completion with elapsed time in milliseconds</description></item>
    ///   <item><description>Returns the response from the inner handler</description></item>
    ///   <item><description>If an exception occurs, logs the error with elapsed time and rethrows the exception</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Rethrows any exception thrown by the inner handler after logging it.</exception>
    public async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Handling {RequestName}", requestName);
            
            var response = await _inner.HandleAsync(request, cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("Completed {RequestName} in {ElapsedMilliseconds}ms", 
                requestName, 
                stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error handling {RequestName} after {ElapsedMilliseconds}ms", 
                requestName, 
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

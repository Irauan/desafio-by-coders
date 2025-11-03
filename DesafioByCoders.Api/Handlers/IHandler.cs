namespace DesafioByCoders.Api.Handlers;

/// <summary>
/// Defines a handler for processing requests without returning a response.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle. Must implement <see cref="IRequest"/>.</typeparam>
/// <remarks>
/// This interface is used for command-style operations where no response is expected.
/// Handlers implementing this interface are automatically registered via Scrutor scanning
/// and decorated with logging capabilities through <see cref="Features.LoggingHandlerDecorator{TRequest}"/>.
/// </remarks>
public interface IHandler<in TRequest> where TRequest : IRequest
{
    /// <summary>
    /// Handles the specified request asynchronously.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// Implementations should handle the business logic for the given request type.
    /// Exceptions thrown by this method will be logged by the logging decorator before being propagated.
    /// </remarks>
    Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for processing requests and returning a response.
/// </summary>
/// <typeparam name="TRequest">The type of request to handle. Must implement <see cref="IRequest"/>.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// This interface is used for query-style operations where a response is expected.
/// Handlers implementing this interface are automatically registered via Scrutor scanning
/// and decorated with logging capabilities through <see cref="Features.LoggingHandlerDecorator{TRequest, TResponse}"/>.
/// </remarks>
public interface IHandler<in TRequest, TResponse> where TRequest : IRequest
{
    /// <summary>
    /// Handles the specified request asynchronously and returns a response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Default is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous operation and contains the response.</returns>
    /// <remarks>
    /// Implementations should handle the business logic for the given request type and return the appropriate response.
    /// Exceptions thrown by this method will be logged by the logging decorator before being propagated.
    /// </remarks>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
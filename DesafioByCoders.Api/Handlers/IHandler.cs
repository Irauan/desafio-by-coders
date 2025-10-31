namespace DesafioByCoders.Api.Handlers;

public interface IHandler<in TRequest> where TRequest : IRequest
{
    Task HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default
    );
}

public interface IHandler<in TRequest, TResponse> where TRequest : IRequest
{
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken = default
    );
}
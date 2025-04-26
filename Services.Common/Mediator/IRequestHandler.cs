namespace Services.Common.Mediator;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken ct);
}
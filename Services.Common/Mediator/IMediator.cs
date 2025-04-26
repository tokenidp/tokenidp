namespace Services.Common.Mediator;

public interface IMediator
{
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken ct = default)
      where TRequest : IRequest<TResponse>;
}

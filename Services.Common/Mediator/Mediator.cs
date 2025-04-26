using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Services.Common.Mediator;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, RequestHandlerBase> _handlerCache;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlerCache = new ConcurrentDictionary<Type, RequestHandlerBase>();
    }

    public async Task<TResponse> Send<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = GetHandler<TRequest, TResponse>();

        // Get behaviors once per TRequest/TResponse combo
        var behaviors = _serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse();

        RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(request, ct);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.Handle(request, next, ct);
        }

        return await pipeline();
    }

    private IRequestHandler<TRequest, TResponse> GetHandler<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        var requestType = typeof(TRequest);

        if (!_handlerCache.TryGetValue(requestType, out var handler))
        {
            handler = (RequestHandlerBase)_serviceProvider
                .GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            _handlerCache.TryAdd(requestType, handler);
        }

        return (IRequestHandler<TRequest, TResponse>)handler;
    }

    private abstract class RequestHandlerBase { }
}
using System.Collections.Concurrent;

namespace Services.Common.Mediator;

public class Mediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, Func<object, Task<object>>> _handlerCache = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    //public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    //{
    //    // Resolve or cache handler delegate
    //    var handlerDelegate = ResolveHandlerDelegate(request);

    //    // Resolve pipeline behaviors
    //    var behaviors = ResolvePipelineBehaviors<IRequest<TResponse>, TResponse>();

    //    // Build the pipeline
    //    RequestHandlerDelegate<TResponse> next = () => handlerDelegate(request);

    //    foreach (var behavior in behaviors.Reverse())
    //    {
    //        var currentBehavior = behavior;
    //        var currentNext = next;
    //        next = () => currentBehavior.Handle(request, currentNext);
    //    }

    //    // Execute the pipeline
    //    return await next();
    //}

    //private RequestHandlerDelegate<TResponse> ResolveHandlerDelegate<TResponse>(IRequest<TResponse> request)
    //{
    //    // Resolve handler delegate (cached for performance)
    //    var handlerDelegate = _handlerCache.GetOrAdd(request.GetType(), requestType =>
    //    {
    //        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
    //        var method = handlerType.GetMethod("Handle");
    //        var handlerInstance = _serviceProvider.GetService(handlerType);

    //        if (handlerInstance == null)
    //            throw new InvalidOperationException($"No handler found for {requestType}");

    //        // Create a strongly typed delegate for handler execution
    //        var response = new Task<TResponse>(() =>
    //        {
    //            var tResponse = (Task<TResponse>)method.Invoke(handlerInstance, new[] { request });
    //            return tResponse;
    //        });
    //    });

    //    return handlerDelegate;
    //}



    //private IEnumerable<IPipelineBehavior<TRequest, TResponse>> ResolvePipelineBehaviors<TRequest, TResponse>()
    //where TRequest : IRequest<TResponse>
    //{
    //    // Resolve all pipeline behaviors for the current request/response types
    //    return _serviceProvider
    //        .GetServices<IPipelineBehavior<TRequest, TResponse>>()
    //        .Reverse()
    //        .ToList();
    //}
}

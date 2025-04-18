namespace Services.Common.Mediator;

public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

//Example 

//using System;
//using System.Threading.Tasks;

//public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
//    where TRequest : IRequest<TResponse>
//{
//    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next)
//    {
//        Console.WriteLine($"[LOG] Handling {typeof(TRequest).Name}");
//        var response = await next();
//        Console.WriteLine($"[LOG] Handled {typeof(TRequest).Name}");
//        return response;
//    }
//}
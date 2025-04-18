using MediatR.Pipeline;

namespace Identity.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>
{
    private readonly IAppLogger<LoggingBehaviour<TRequest>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehaviour(IAppLogger<LoggingBehaviour<TRequest>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;

        _logger.LogInfo("Admin Portal Request: {Name} {@UserId} {@Request}",
            requestName, userId, request);

        return Task.CompletedTask;
    }
}

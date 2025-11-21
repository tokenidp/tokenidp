using Microsoft.Data.SqlClient;
using System.Net;
using System.Text;

namespace IDP.Core.Middlewares;

internal class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JsonHelper _jsonHelper;
    private readonly IAppLogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next,
        JsonHelper jsonHelper,
        IAppLogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _jsonHelper = jsonHelper;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
        var requestData = await FormatRequest(context.Request);

        switch (exception)
        {
            case DbUpdateConcurrencyException dbEx:
                _logger.LogError(dbEx, "DbUpdateConcurrencyException with correlation ID: {CorrelationId}." +
                    " Request: {@RequestData}", correlationId, requestData);
                await WriteErrorResponseAsync(context, "Conflict detected due to version mismatch.",
                    HttpStatusCode.Conflict, correlationId);
                break;

            case SqlException sqlEx:
                LogSqlError(sqlEx, correlationId);
                await WriteErrorResponseAsync(context, "A database error occurred. Please try again later.",
                    HttpStatusCode.InternalServerError, correlationId);
                break;

            default:
                _logger.LogError(exception, "Unhandled Exception with correlation ID: {CorrelationId}. " +
                    "Request: {@RequestData}", correlationId, requestData);
                await WriteErrorResponseAsync(context, "An error has occurred, please contact support.",
                    HttpStatusCode.InternalServerError, correlationId);
                break;
        }
    }

    private void LogSqlError(SqlException sqlError, string correlationId)
    {
        if (sqlError.Number is 2 or 53)
        {
            _logger.LogFatal(sqlError, "SQL network error (Code: {Code}) on server {Server} with " +
                "correlation ID: {CorrelationId}.", sqlError.Number, sqlError.Server, correlationId);
        }
        else
        {
            _logger.LogError(sqlError, "SQL error (Code: {Code}) on server {Server} with " +
                "correlation ID: {CorrelationId}.", sqlError.Number, sqlError.Server, correlationId);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context,
        string message,
        HttpStatusCode statusCode,
        string correlationId)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiError.Failure(message, correlationId);
        var result = _jsonHelper.SerializeObject(ApiResult<ApiError>.Failure(response));

        await context.Response.WriteAsync(result);
    }

    private async Task<object> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        string body = string.Empty;
        if (request.Body.CanSeek && request.ContentLength > 0)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        return new
        {
            request.Scheme,
            Host = request.Host.ToString(),
            Path = request.Path.ToString(),
            QueryString = request.QueryString.ToString(),
            Body = body.Truncate(500) // optional: avoid dumping huge body
        };
    }
}
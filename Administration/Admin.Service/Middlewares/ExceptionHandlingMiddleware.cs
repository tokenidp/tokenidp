using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Common;
using Services.Common.Model;
using System.IO;
using System.Net;
using System.Text;

namespace Identity.Service.Middlewares;

public class ExceptionHandlingMiddleware
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
        catch (DbUpdateConcurrencyException ex)
        {
            var requestData = await FormatRequest(context.Request);

            _logger.LogError(requestData, "Conflict: Resource update failed due to a version mismatch. Request data: {RequestData}");

            _logger.LogError(ex, "Conflict: Resource update failed due to a version mismatch.");

            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var message = "An error has occurred, Please try again and if the error persists. " +
               "Contact the system administrator for assistance.";

        string correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";

        if (!LogSQLError(context, exception))
        {
            _logger.LogError(exception, "An unhandled exception occurred with correlation ID: {CorrelationId}.",
                correlationId);
        }

        var statusCode = HttpStatusCode.InternalServerError;

        var response = ApiError.Failure(message, correlationId);

        var result = _jsonHelper.SerializeObject(response);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsync(result);
    }

    private bool LogSQLError(HttpContext context, Exception exception)
    {
        if (exception.GetType() != typeof(SqlException))
        {
            return false;
        }

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";

        var sqlError = (SqlException)exception;

        if (sqlError.Number == 2 || sqlError.Number == 53)
        {
            var internalMessage = $"A SQL server '{sqlError.Server}' network connection error occurred, please " +
                "look into it on priority with correlation ID: {CorrelationId}.";

            _logger.LogFatal(sqlError, internalMessage, correlationId);
        }
        else
        {
            var internalMessage = $"The following error occurred in the infrastructure layer for the SQL server '{sqlError.Server}'" +
                " with correlation ID: {CorrelationId}.";

            _logger.LogError(sqlError, internalMessage, correlationId);
        }

        return true;
    }

    private async Task<object> FormatRequest(HttpRequest request)
    {
        // Allow reading the request body multiple times
        request.EnableBuffering();

        var body = "";
        if (request.Body.CanSeek && request.ContentLength > 0)
        {
            // Go to the beginning of the body to read it
            request.Body.Position = 0;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
                // Reset position after reading
                request.Body.Position = 0;
            }
        }

        var requestData = new
        {
            request.Scheme,
            Host = request.Host.ToString(),
            Path = request.Path.ToString(),
            QueryString = request.QueryString.ToString(),
            Body = body
        };

        return requestData;
    }
}

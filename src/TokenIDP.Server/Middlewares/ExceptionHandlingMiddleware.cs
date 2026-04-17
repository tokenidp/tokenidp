using TokenIDP.Core.Foundation;
using TokenIDP.Core.Foundation.Contracts;
using TokenIDP.Core.Foundation.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using TokenIDP.Core.Abstractions;

namespace TokenIDP.Server.Middlewares;

public class ExceptionHandlingMiddleware
{
    private const int MaxLoggedValueLength = 100;
    private const long MaxParsedBodyLength = 4096;
    private const int MaxLoggedCollectionItems = 20;
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "newpassword",
        "confirmpassword",
        "token",
        "access_token",
        "refresh_token",
        "id_token",
        "client_secret",
        "clientsecret",
        "secret",
        "code",
        "code_verifier",
        "authorization",
        "assertion"
    };

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

        var response = ApiError.Failure(statusCode.ToString(), message, correlationId);
        var result = _jsonHelper.SerializeObject(ApiResult<ApiError>.Failure(response));

        await context.Response.WriteAsync(result);
    }

    private async Task<object> FormatRequest(HttpRequest request)
    {
        request.EnableBuffering();

        return new
        {
            request.Method,
            request.Scheme,
            Host = request.Host.ToString(),
            Path = request.Path.ToString(),
            Query = FormatQuery(request),
            Body = await FormatBodyAsync(request)
        };
    }

    private static Dictionary<string, string>? FormatQuery(HttpRequest request)
    {
        if (!request.Query.Any())
        {
            return null;
        }

        return request.Query.ToDictionary(
            entry => entry.Key,
            entry => SanitizeValue(entry.Key, string.Join(",", entry.Value.ToArray())),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<object?> FormatBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
        {
            return null;
        }

        if (request.ContentLength > MaxParsedBodyLength)
        {
            return new
            {
                request.ContentType,
                request.ContentLength,
                Preview = "<omitted: body too large>"
            };
        }

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            ResetBodyPosition(request);

            return new
            {
                request.ContentType,
                request.ContentLength,
                Form = form.ToDictionary(
                    entry => entry.Key,
                    entry => SanitizeValue(entry.Key, string.Join(",", entry.Value.ToArray())),
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        if (!request.Body.CanSeek)
        {
            return new
            {
                request.ContentType,
                request.ContentLength,
                Preview = "<omitted: non-seekable body>"
            };
        }

        var body = await ReadBodyAsync(request);
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        if (IsJsonContentType(request.ContentType))
        {
            return new
            {
                request.ContentType,
                request.ContentLength,
                Json = SummarizeJsonBody(body)
            };
        }

        return new
        {
            request.ContentType,
            request.ContentLength,
            Preview = "<omitted>"
        };
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        return body;
    }

    private static object SummarizeJsonBody(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            return SummarizeJsonElement(document.RootElement) ?? "<null>";
        }
        catch (JsonException)
        {
            return "<unparsed json>";
        }
    }

    private static object? SummarizeJsonElement(JsonElement element, string? propertyName = null)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property => SummarizeJsonElement(property.Value, property.Name),
                    StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray()
                .Take(MaxLoggedCollectionItems)
                .Select(item => SummarizeJsonElement(item))
                .ToList(),
            JsonValueKind.String => SanitizeValue(propertyName, element.GetString()),
            JsonValueKind.Number => IsSensitiveKey(propertyName)
                ? "<redacted>"
                : SanitizeString(element.ToString()),
            JsonValueKind.True => IsSensitiveKey(propertyName) ? "<redacted>" : true,
            JsonValueKind.False => IsSensitiveKey(propertyName) ? "<redacted>" : false,
            JsonValueKind.Null => null,
            _ => SanitizeString(element.ToString())
        };
    }

    private static string SanitizeValue(string? key, string? value)
    {
        return IsSensitiveKey(key)
            ? "<redacted>"
            : SanitizeString(value);
    }

    private static bool IsSensitiveKey(string? key)
    {
        return !string.IsNullOrWhiteSpace(key) && SensitiveKeys.Contains(key);
    }

    private static string SanitizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("<", "\\u003c", StringComparison.Ordinal)
            .Replace(">", "\\u003e", StringComparison.Ordinal)
            .Truncate(MaxLoggedValueLength);
    }

    private static bool IsJsonContentType(string? contentType)
    {
        return !string.IsNullOrWhiteSpace(contentType) &&
               (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("+json", StringComparison.OrdinalIgnoreCase));
    }

    private static void ResetBodyPosition(HttpRequest request)
    {
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }
    }
}

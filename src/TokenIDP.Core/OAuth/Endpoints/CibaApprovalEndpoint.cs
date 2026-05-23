using Microsoft.AspNetCore.Antiforgery;
using System.Net;
using TokenIDP.Core.OAuth.UseCases;
using TokenIDP.Domain;

namespace TokenIDP.Core.OAuth.Endpoints;

internal sealed class CibaApprovalEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/ciba/approve", HandleApprovePageAsync)
            .AllowAnonymous()
            .WithName("CibaApprovalPage")
            .WithTags("CibaApproval");

        app.MapPost("/ciba/approve", HandleApproveAsync)
            .AllowAnonymous()
            .WithName("CibaApprove")
            .WithTags("CibaApproval");

    }

    private static async Task<IResult> HandleApprovePageAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        CibaApprovalUseCase useCase)
    {
        if (!TryReadRequest(httpContext, out var publicRequestId, out var approvalToken))
        {
            return Results.BadRequest("Invalid CIBA approval link.");
        }

        try
        {
            var challenge = await useCase.GetApprovalChallengeAsync(
                publicRequestId,
                approvalToken,
                recordPageOpened: true,
                httpContext.RequestAborted);

            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return Results.Content(RenderApprovalPage(challenge, approvalToken, tokens.RequestToken ?? string.Empty), "text/html");
        }
        catch (Exception ex) when (ex is NotFoundException or DomainException)
        {
            return Results.Content(RenderErrorPage(ex.Message), "text/html", statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> HandleApproveAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        CibaApprovalUseCase useCase)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Invalid or missing antiforgery token.");
        }

        var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
        if (!Guid.TryParse(form["requestId"].ToString(), out var publicRequestId))
        {
            return Results.BadRequest("Invalid CIBA request.");
        }

        var approvalToken = form["token"].ToString();
        var decision = form["decision"].ToString();
        var approve = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase);
        var reject = string.Equals(decision, "reject", StringComparison.OrdinalIgnoreCase);
        if (!approve && !reject)
        {
            return Results.BadRequest("Invalid CIBA approval decision.");
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        try
        {
            if (approve)
            {
                await useCase.ApproveWithTokenAsync(
                    publicRequestId,
                    approvalToken,
                    ipAddress,
                    userAgent,
                    httpContext.RequestAborted);
            }
            else
            {
                await useCase.RejectWithTokenAsync(
                    publicRequestId,
                    approvalToken,
                    ipAddress,
                    userAgent,
                    httpContext.RequestAborted);
            }

            return Results.Content(RenderDecisionComplete(approve), "text/html");
        }
        catch (Exception ex) when (ex is NotFoundException or DomainException)
        {
            return Results.Content(RenderErrorPage(ex.Message), "text/html", statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static bool TryReadRequest(HttpContext httpContext, out Guid publicRequestId, out string approvalToken)
    {
        publicRequestId = Guid.Empty;
        approvalToken = httpContext.Request.Query["token"].ToString();
        return Guid.TryParse(httpContext.Request.Query["requestId"].ToString(), out publicRequestId) &&
            !string.IsNullOrWhiteSpace(approvalToken);
    }

    private static string RenderApprovalPage(CibaApprovalChallenge challenge, string approvalToken, string antiforgeryToken)
    {
        var scopes = string.Join(", ", challenge.RequestedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return "<!doctype html><html><head><title>CIBA approval</title></head><body>" +
            "<main style=\"max-width:560px;margin:48px auto;font-family:Arial,sans-serif;line-height:1.5\">" +
            "<h1>Approve sign-in request</h1>" +
            $"<p><strong>Client:</strong> {Encode(challenge.ClientName)}</p>" +
            $"<p><strong>Binding message:</strong> {Encode(challenge.BindingMessage)}</p>" +
            $"<p><strong>Requested scopes:</strong> {Encode(scopes)}</p>" +
            $"<p><strong>Expires:</strong> {Encode(challenge.ExpiresAtUtc.ToString("u"))}</p>" +
            "<p>Only approve this request if you initiated it.</p>" +
            RenderDecisionForm("approve", "Approve", challenge.PublicRequestId, approvalToken, antiforgeryToken) +
            RenderDecisionForm("reject", "Reject", challenge.PublicRequestId, approvalToken, antiforgeryToken) +
            "<p style=\"margin-top:24px;color:#667085\">TODO: require MFA/step-up for sensitive CIBA approvals and support push/biometric approval later.</p>" +
            "</main></body></html>";
    }

    private static string RenderDecisionForm(
        string decision,
        string label,
        Guid publicRequestId,
        string approvalToken,
        string antiforgeryToken)
        => "<form method=\"post\" action=\"/ciba/approve\" style=\"display:inline-block;margin-right:12px\">" +
            $"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{Encode(antiforgeryToken)}\" />" +
            $"<input type=\"hidden\" name=\"requestId\" value=\"{publicRequestId:D}\" />" +
            $"<input type=\"hidden\" name=\"token\" value=\"{Encode(approvalToken)}\" />" +
            $"<input type=\"hidden\" name=\"decision\" value=\"{Encode(decision)}\" />" +
            $"<button type=\"submit\">{label}</button></form>";

    private static string RenderDecisionComplete(bool approved)
        => "<!doctype html><html><head><title>CIBA approval</title></head><body>" +
            "<main style=\"max-width:560px;margin:48px auto;font-family:Arial,sans-serif;line-height:1.5\">" +
            $"<h1>Request {(approved ? "approved" : "rejected")}</h1>" +
            "<p>You can close this window. The requesting client will receive the result through polling.</p>" +
            "</main></body></html>";

    private static string RenderErrorPage(string message)
        => "<!doctype html><html><head><title>CIBA approval</title></head><body>" +
            "<main style=\"max-width:560px;margin:48px auto;font-family:Arial,sans-serif;line-height:1.5\">" +
            "<h1>Approval link unavailable</h1>" +
            $"<p>{Encode(message)}</p>" +
            "</main></body></html>";

    private static string Encode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}

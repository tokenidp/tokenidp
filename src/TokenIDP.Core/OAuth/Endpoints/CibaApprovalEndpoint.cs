using Microsoft.AspNetCore.Antiforgery;
using System.Globalization;
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
        var expires = challenge.ExpiresAtUtc.ToUniversalTime().ToString("MMM dd, yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);

        return RenderPage(
            "Approve sign-in request",
            "<div class=\"page-header\">" +
                "<div>" +
                    "<div class=\"eyebrow\">CIBA approval</div>" +
                    "<h1>Approve sign-in request</h1>" +
                "</div>" +
                "<span class=\"status-pill\">Pending</span>" +
            "</div>" +
            "<section class=\"card-surface\">" +
                "<div class=\"request-card\">" +
                    "<div class=\"request-main\">" +
                        $"<h2>{Encode(challenge.ClientName)}</h2>" +
                        "<p class=\"request-copy\">A client is requesting permission to complete a sign-in for your account.</p>" +
                        "<div class=\"meta-grid\">" +
                            RenderMeta("Binding Message", challenge.BindingMessage) +
                            RenderMeta("Requested Scopes", scopes) +
                            RenderMeta("Expires", expires) +
                        "</div>" +
                    "</div>" +
                    "<div class=\"action-panel\">" +
                        "<p>Only approve this request if you initiated it.</p>" +
                        "<div class=\"action-row\">" +
                            RenderDecisionForm("approve", "Approve", "btn btn-success", challenge.PublicRequestId, approvalToken, antiforgeryToken) +
                            RenderDecisionForm("reject", "Reject", "btn btn-outline-danger", challenge.PublicRequestId, approvalToken, antiforgeryToken) +
                        "</div>" +
                    "</div>" +
                "</div>" +
            "</section>");
    }

    private static string RenderDecisionForm(
        string decision,
        string label,
        string buttonClass,
        Guid publicRequestId,
        string approvalToken,
        string antiforgeryToken)
        => "<form method=\"post\" action=\"/ciba/approve\">" +
            $"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{Encode(antiforgeryToken)}\" />" +
            $"<input type=\"hidden\" name=\"requestId\" value=\"{publicRequestId:D}\" />" +
            $"<input type=\"hidden\" name=\"token\" value=\"{Encode(approvalToken)}\" />" +
            $"<input type=\"hidden\" name=\"decision\" value=\"{Encode(decision)}\" />" +
            $"<button class=\"{Encode(buttonClass)}\" type=\"submit\">{Encode(label)}</button></form>";

    private static string RenderDecisionComplete(bool approved)
        => RenderPage(
            "CIBA approval",
            "<section class=\"card-surface centered-state\">" +
                $"<div class=\"state-icon {(approved ? "state-success" : "state-danger")}\">{(approved ? "✓" : "!")}</div>" +
                $"<h1>Request {(approved ? "approved" : "rejected")}</h1>" +
                "<p>The requesting client will receive the result through polling. You can close this window.</p>" +
            "</section>");

    private static string RenderErrorPage(string message)
        => RenderPage(
            "CIBA approval",
            "<section class=\"card-surface centered-state\">" +
                "<div class=\"state-icon state-danger\">!</div>" +
                "<h1>Approval link unavailable</h1>" +
                $"<p>{Encode(message)}</p>" +
            "</section>");

    private static string RenderMeta(string label, string? value)
        => "<div>" +
            $"<div class=\"meta-label\">{Encode(label)}</div>" +
            $"<div class=\"meta-value\">{Encode(string.IsNullOrWhiteSpace(value) ? "-" : value)}</div>" +
            "</div>";

    private static string RenderPage(string title, string content)
        => "<!doctype html><html lang=\"en\"><head>" +
            "<meta charset=\"utf-8\" />" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />" +
            $"<title>{Encode(title)}</title>" +
            "<style>" + PageStyles + "</style>" +
            "</head><body><main class=\"approval-shell\">" +
            content +
            "</main></body></html>";

    private const string PageStyles = """
        :root {
          --primary: #00a9ff;
          --success: #15803d;
          --danger: #c10007;
          --bg: #f8fafc;
          --surface: #ffffff;
          --surface-soft: #f7f9fc;
          --surface-border: #e5e7eb;
          --border: #e2e8f0;
          --text: #0f172a;
          --muted: #334155;
          --muted-soft: #64748b;
          --shadow: 0 14px 28px rgba(15, 23, 42, 0.06);
        }

        * {
          box-sizing: border-box;
        }

        body {
          margin: 0;
          min-height: 100vh;
          background: var(--bg);
          color: var(--text);
          font-family: "Manrope", "DM Sans", "Segoe UI", -apple-system, BlinkMacSystemFont, sans-serif;
          line-height: 1.5;
        }

        .approval-shell {
          width: min(920px, calc(100% - 32px));
          margin: 0 auto;
          padding: 42px 0;
        }

        .page-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 16px;
          margin-bottom: 16px;
        }

        .eyebrow {
          margin-bottom: 4px;
          color: var(--muted-soft);
          font-size: 13px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.06em;
        }

        h1 {
          margin: 0;
          color: var(--text);
          font-size: 22px;
          font-weight: 700;
          letter-spacing: 0;
        }

        h2 {
          margin: 0 0 4px;
          color: var(--text);
          font-size: 18px;
          font-weight: 700;
          letter-spacing: 0;
        }

        p {
          margin: 0;
          color: var(--muted);
        }

        .card-surface {
          background: var(--surface);
          border: 1px solid var(--surface-border);
          border-radius: 18px;
          box-shadow: var(--shadow);
        }

        .request-card {
          display: flex;
          justify-content: space-between;
          gap: 24px;
          padding: 24px;
        }

        .request-main {
          flex: 1;
          min-width: 0;
        }

        .request-copy {
          margin-bottom: 20px;
        }

        .meta-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 16px 20px;
        }

        .meta-label {
          margin-bottom: 6px;
          color: var(--muted-soft);
          font-size: 12px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.06em;
        }

        .meta-value {
          color: var(--text);
          font-size: 15px;
          overflow-wrap: anywhere;
        }

        .status-pill {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          min-height: 28px;
          padding: 4px 12px;
          border: 1px solid #fcd34d;
          border-radius: 999px;
          background: #fef3c7;
          color: #b45309;
          font-size: 12px;
          font-weight: 700;
        }

        .action-panel {
          display: flex;
          width: 240px;
          flex-direction: column;
          justify-content: center;
          gap: 14px;
          padding-left: 24px;
          border-left: 1px solid var(--border);
        }

        .action-row {
          display: grid;
          gap: 10px;
        }

        .action-row form {
          margin: 0;
        }

        .btn {
          display: inline-flex;
          width: 100%;
          min-height: 40px;
          align-items: center;
          justify-content: center;
          border-radius: 10px;
          padding: 8px 16px;
          font: inherit;
          font-weight: 600;
          cursor: pointer;
          transition: background-color 0.15s ease, border-color 0.15s ease, color 0.15s ease, box-shadow 0.15s ease;
        }

        .btn:focus {
          outline: none;
          box-shadow: 0 0 0 4px rgba(0, 169, 255, 0.25);
        }

        .btn-success {
          border: 1px solid var(--success);
          background: var(--success);
          color: #ffffff;
        }

        .btn-success:hover {
          border-color: #166534;
          background: #166534;
        }

        .btn-outline-danger {
          border: 1px solid var(--danger);
          background: transparent;
          color: var(--danger);
        }

        .btn-outline-danger:hover {
          background: var(--danger);
          color: #ffffff;
        }

        .centered-state {
          max-width: 560px;
          margin: 80px auto 0;
          padding: 34px 28px;
          text-align: center;
        }

        .centered-state h1 {
          margin-top: 14px;
          margin-bottom: 8px;
        }

        .state-icon {
          display: inline-flex;
          width: 48px;
          height: 48px;
          align-items: center;
          justify-content: center;
          border-radius: 999px;
          color: #ffffff;
          font-size: 24px;
          font-weight: 800;
        }

        .state-success {
          background: var(--success);
        }

        .state-danger {
          background: var(--danger);
        }

        @media (max-width: 720px) {
          .approval-shell {
            width: min(100% - 24px, 920px);
            padding: 24px 0;
          }

          .page-header {
            align-items: flex-start;
            flex-direction: column;
          }

          .request-card {
            flex-direction: column;
            padding: 20px;
          }

          .meta-grid {
            grid-template-columns: 1fr;
          }

          .action-panel {
            width: 100%;
            padding-top: 18px;
            padding-left: 0;
            border-top: 1px solid var(--border);
            border-left: 0;
          }
        }
        """;

    private static string Encode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}

using TokenIDP.Core.Admin.Permissions;
using TokenIDP.Core.Admin.Permissions.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace TokenIDP.Core.Admin.Endpoints;

internal class PermissionEndpoint : IEndpointDefinition
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/admin/permission")
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            })
            .AddEndpointFilter<EndpointValidationFilter>();

        authGroup.MapGet("assign", async (PermissionQueryUseCase permissionUseCases) =>
        {
            var response = await permissionUseCases.GetPermissions();

            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.edit"
         })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "roles.add"
         })
        .WithName("Permissions")
        .WithTags("Permissions");

        authGroup.MapPost("list", async (SearchData data,
            PermissionQueryUseCase permissionUseCases) =>
        {
            var response = await permissionUseCases.GetPermissions(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "permissions.view"
         })
        .WithName("PagedPermissions")
        .WithTags("Permissions");

        authGroup.MapPost("/", async (CreateUpdatePermission request,
            PermissionCommandUseCase permissionUseCases) =>
        {
            var response = await permissionUseCases.CreatePermission(request);

            var location = response.IsSuccess
                ? $"permissions/{response.Value}"
                : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "permissions.add"
         })
        .WithName("CreatePermission")
        .WithTags("Permissions");

        authGroup.MapPut("/{id}", async (int id, CreateUpdatePermission request,
            PermissionCommandUseCase permissionUseCases) =>
        {
            if (id != request.Id)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Ids didn't match.")));
            }

            var response = await permissionUseCases.UpdatePermission(id, request);

            return EndpointResultMapper.ToNoContentOrError(response);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "permissions.edit"
         })
        .WithName("UpdatePermission")
        .WithTags("Permissions");

        authGroup.MapGet("/{id}", async (int id,
            PermissionQueryUseCase permissionUseCases) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(ApiResult<ApiError>.Failure(
                    ApiError.Failure("Record Id should be greater than zero.")));
            }

            var response = await permissionUseCases.GetPermissionById(id);

            var location = response.IsSuccess
                ? $"admin/permission/{response.Value?.Id}"
                : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
         .RequireAuthorization(new AuthorizeAttribute
         {
             Policy = "permissions.view"
         })
        .WithName("PermissionById")
        .WithTags("Permissions");

        authGroup.MapGet("lookups", async (
            PermissionLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetPermissionLookups(
                httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("PermissionLookups")
        .WithTags("Permissions");
    }
}


using Admin.Core.Permissions;
using Admin.Core.Permissions.UseCases;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Admin.Core.Endpoints;

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
        .WithName("Permissions")
        .WithTags("Permissions");

        authGroup.MapPost("list", async (SearchData data,
            PermissionQueryUseCase permissionUseCases) =>
        {
            var response = await permissionUseCases.GetPermissions(data);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("PagedPermissions")
        .WithTags("PagedPermissions");

        authGroup.MapPost("/", async (CreateUpdatePermission request,
            PermissionCommandUseCase permissionUseCases) =>
        {
            var response = await permissionUseCases.CreatePermission(request);

            var location = response.IsSuccess
                ? $"permissions/{response.Value}"
                : string.Empty;

            return EndpointResultMapper.ToCreatedOrError(response, location);
        })
        .WithName("CreatePermission")
        .WithTags("CreatePermission");

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
        .WithName("UpdatePermission")
        .WithTags("UpdatePermission");

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
        .WithName("PermissionbyId")
        .WithTags("PermissionbyId");

        authGroup.MapGet("lookups", async (
            PermissionLookupsUseCase useCase,
            HttpContext httpContext) =>
        {
            var response = await useCase.GetPermissionLookups(
                httpContext.RequestAborted);

            return EndpointResultMapper.ToOkOrError(response);
        })
        .WithName("PermissionLookups")
        .WithTags("PermissionLookups");
    }
}

using Microsoft.AspNetCore.Authorization;

namespace Neura.Services.Authentication.Filters;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
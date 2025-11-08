using Microsoft.AspNetCore.Authorization;

namespace Neura.Core.Authentication.Filters;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
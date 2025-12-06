using Microsoft.AspNetCore.Authorization;

namespace Neura.Services.Authentication.Filters;

public class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
}
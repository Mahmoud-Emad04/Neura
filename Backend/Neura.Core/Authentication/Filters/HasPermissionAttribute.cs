using Microsoft.AspNetCore.Authorization;

namespace Neura.Core.Authentication.Filters;

public class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
}
using Microsoft.AspNetCore.Authorization;

namespace Neura.Services.Filters;

public class HasPermissionAttribute(string permission) : AuthorizeAttribute(permission)
{
}
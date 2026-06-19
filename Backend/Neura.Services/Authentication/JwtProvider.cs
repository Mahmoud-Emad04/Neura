using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Neura.Core.Authentication;
using Neura.Services.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Neura.Services.Authentication;

public class JwtProvider(IOptions<JwtOptions> options, IServiceHelpers _serviceHelpers) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, int ExpiresIn) GenerateToken(ApplicationUser applicationUser, IEnumerable<string> roles,
        IEnumerable<string> permissions)
    {
        string baseUrl = _serviceHelpers.GetBaseUrl();

        Claim[] calims =
        [
            new(JwtRegisteredClaimNames.Sub, applicationUser.Id),
            new(JwtRegisteredClaimNames.Email, applicationUser.Email!),
            new(JwtRegisteredClaimNames.GivenName, applicationUser.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, applicationUser.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(nameof(applicationUser.ImageUrl), applicationUser.ImageUrl!.StartsWith("Images") ? $"{baseUrl}/{applicationUser.ImageUrl}" : applicationUser.ImageUrl),
            new(nameof(roles), JsonSerializer.Serialize(roles), JsonClaimValueTypes.JsonArray),
            new(nameof(permissions), JsonSerializer.Serialize(permissions), JsonClaimValueTypes.JsonArray)
        ];

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            calims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: signingCredentials
        );

        return (Token: new JwtSecurityTokenHandler().WriteToken(token), ExpiresIn: _options.ExpiryMinutes * 60);
    }

    public string? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var summetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = summetricSecurityKey,
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            },
                out var validatedToken
            );
            var jwtToken = (JwtSecurityToken)validatedToken;
            return jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
        }
        catch
        {
            return null;
        }
    }

    public string? GetUserIdFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var tokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = symmetricSecurityKey,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = false, // Matches your existing ValidateToken config
            ValidateAudience = false, // Matches your existing ValidateToken config
            ValidateLifetime = false // <--- CRITICAL: Allows reading expired tokens
        };

        try
        {
            // 1. Validate signature (but ignore expiry date)
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            // 2. Security Check: Prevent "None" algorithm attacks
            // We ensure the token actually used HmacSha256
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                return null;

            // 3. Extract the User ID (Subject)
            // Note: We access the raw jwtSecurityToken claims to ensure we find "sub" 
            // even if the ClaimsPrincipal mapped it to something else.
            return ((JwtSecurityToken)securityToken).Claims
                .FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch
        {
            return null;
        }
    }
}
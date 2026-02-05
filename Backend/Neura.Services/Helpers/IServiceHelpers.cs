namespace Neura.Services.Helpers;

public interface IServiceHelpers
{
    string GetBaseUrl();
    int[] DecodeHash(string encoded);
    string? GetCurrentUserId();
    bool IsUserInRole(string role);
}
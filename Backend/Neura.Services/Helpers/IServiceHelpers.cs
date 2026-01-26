namespace Neura.Services.Helpers;

public interface IServiceHelpers
{
    string GetBaseUrl();
    int[] DecodeHash(string encoded);
}

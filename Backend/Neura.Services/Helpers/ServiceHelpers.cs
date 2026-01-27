namespace Neura.Services.Helpers;

public class ServiceHelpers(IHttpContextAccessor httpContextAccessor, IHashids hashids) : IServiceHelpers
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IHashids _hashids = hashids;

    public string GetBaseUrl()
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null) return string.Empty;
        return $"{request.Scheme}://{request.Host}";
    }

    public int[] DecodeHash(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded)) return Array.Empty<int>();
        return _hashids.Decode(encoded);
    }
}

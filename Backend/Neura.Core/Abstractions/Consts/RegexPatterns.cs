namespace Neura.Core.Abstractions.Consts;

public static class RegexPatterns
{
    public const string Password =
        "(?=(.*[0-9]))(?=.*[\\!@#$%^&*()\\\\[\\]{}\\-_+=~`|:;\"'<>,./?])(?=.*[a-z])(?=(.*[A-Z]))(?=(.*)).{8,}";

    public const string UserName = "^[a-zA-Z0-9\\-._@+]+$";
    public const string youtubeMatch = @"(?:youtube\\.com\\/watch\\?v=|youtu\\.be\\/)([^& \\n]+)";
}
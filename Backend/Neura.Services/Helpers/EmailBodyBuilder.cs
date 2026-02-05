namespace Neura.Services.Helpers;

public static class EmailBodyBuilder
{
    public static string GenerateEmailBody(string template, Dictionary<string, string> templateModel)
    {
        var solutionDir = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        var coreDir = Path.Combine(solutionDir, "Neura.Core");
        var templatePath = $"{coreDir}/Templates/{template}.html";
        var streamReader = new StreamReader(templatePath);
        var body = streamReader.ReadToEnd();
        streamReader.Close();

        foreach (var item in templateModel)
            body = body.Replace(item.Key, item.Value);

        return body;
    }
}
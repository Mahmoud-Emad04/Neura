namespace Neura.Services.Helpers;

public static class EmailBodyBuilder
{
    public static string GenerateEmailBody(string template, Dictionary<string, string> templateModel)
    {
        //var solutionDir = Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;
        //var coreDir = Path.Combine(solutionDir, "Neura.Core");
        //var templatePath = $"{coreDir}/Templates/{template}.html";
        //var streamReader = new StreamReader(templatePath);
        //var body = streamReader.ReadToEnd();
        //streamReader.Close();

        //foreach (var item in templateModel)
        //    body = body.Replace(item.Key, item.Value);

        // The assembly where the templates are embedded
        var assembly = typeof(Neura.Core.Abstractions.Error).Assembly;
        // ↑ Replace with any class that lives in Neura.Core

        var resourceName = $"Neura.Core.Templates.{template}.html";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                   $"Embedded template '{resourceName}' not found. " +
                   $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        var body = reader.ReadToEnd();

        foreach (var item in templateModel)
            body = body.Replace(item.Key, item.Value);


        return body;
    }
}
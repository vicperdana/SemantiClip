using System.Reflection;
using System.Text;

namespace SemanticClip.Services.Utils;

public static class EmbeddedResource
{
    public static string Read(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

        if (resourcePath == null)
        {
            throw new ArgumentException($"Resource '{resourceName}' not found in assembly {assembly.FullName}");
        }

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not open stream for resource '{resourcePath}'");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
} 
using System.Reflection;

namespace AISmarteasy.Core.Planning;

internal sealed class EmbeddedResource
{
    private static readonly string? Namespace = typeof(EmbeddedResource).Namespace;

    internal static string Read(string name)
    {
        var assembly = typeof(EmbeddedResource).GetTypeInfo().Assembly;
        if (assembly == null) { throw new SKException($"[{Namespace}] {name} assembly not found"); }

        using Stream? resource = assembly.GetManifestResourceStream($"{Namespace}." + name);
        if (resource == null) { throw new SKException($"[{Namespace}] {name} resource not found"); }

        using var reader = new StreamReader(resource);
        return reader.ReadToEnd();
    }
}

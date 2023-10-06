using System.ComponentModel;
using System.Text;
using AISmarteasy.Core.Function;

namespace Plugins.Native.Skills;

public sealed class FileIOSkill
{
    [SKFunction, Description("Read a file")]
    public async Task<string> ReadAsync([Description("Source file")] string path)
    {
        using var reader = File.OpenText(path);
        return await reader.ReadToEndAsync().ConfigureAwait(false);
    }

    [SKFunction, Description("Write a file")]
    public async Task WriteAsync(
        [Description("Destination file")] string path,
        [Description("File content")] string content)
    {
        byte[] text = Encoding.UTF8.GetBytes(content);
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
        {
            throw new UnauthorizedAccessException($"File is read-only: {path}");
        }

        var writer = File.OpenWrite(path);
        await writer.WriteAsync(text, 0, text.Length).ConfigureAwait(false);
    }
}

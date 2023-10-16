using AISmarteasy.Core.Connector;

namespace AISmarteasy.Core.Service;

public interface IImageGeneration : IAIService
{
    Task<string?> GenerateImageAsync(string description, int width, int height, CancellationToken cancellationToken = default);
}

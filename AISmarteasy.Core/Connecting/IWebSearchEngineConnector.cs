namespace AISmarteasy.Core.Connecting;


public interface IWebSearchEngineConnector
{
    Task<IEnumerable<string>> SearchAsync(string query, int count = 1, int offset = 0, CancellationToken cancellationToken = default);
}

using AISmarteasy.Core.Web;

namespace AISmarteasy.Core.Connector;

public static class HttpContentExtensions
{
    public static async Task<string> ReadAsStringWithExceptionMappingAsync(this HttpContent httpContent)
    {
        try
        {
            return await httpContent.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpOperationException(message: ex.Message, innerException: ex);
        }
    }
    
    public static async Task<Stream> ReadAsStreamAndTranslateExceptionAsync(this HttpContent httpContent)
    {
        try
        {
            return await httpContent.ReadAsStreamAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpOperationException(message: ex.Message, innerException: ex);
        }
    }

    public static async Task<byte[]> ReadAsByteArrayAndTranslateExceptionAsync(this HttpContent httpContent)
    {
        try
        {
            return await httpContent.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpOperationException(message: ex.Message, innerException: ex);
        }
    }
}

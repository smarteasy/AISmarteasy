namespace AISmarteasy.Core.Connecting;

internal sealed class NonDisposableHttpClientHandler : HttpClientHandler
{
    private NonDisposableHttpClientHandler()
    {
        this.CheckCertificateRevocationList = true;
    }

    public static NonDisposableHttpClientHandler Instance { get; } = new();

    protected override void Dispose(bool disposing)
    {
    }
}

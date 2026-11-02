namespace UnitTest;

internal interface IPlaylistHttpHandler
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

internal sealed class SubstituteHttpMessageHandler : HttpMessageHandler
{
    private readonly IPlaylistHttpHandler _handler;

    public SubstituteHttpMessageHandler(IPlaylistHttpHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => _handler.SendAsync(request, cancellationToken);
}

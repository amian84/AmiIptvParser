using Microsoft.Extensions.DependencyInjection;

namespace UnitTest;

internal static class TestClients
{
    public static ServiceProvider CreateProvider(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services
            .AddHttpClient(nameof(AmiIptvParser.PlaylistClient))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    public static HttpClient CreateHttpClient(IServiceProvider provider)
        => provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AmiIptvParser.PlaylistClient));
}

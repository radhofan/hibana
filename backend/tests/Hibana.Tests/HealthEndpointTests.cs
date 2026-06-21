using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hibana.Tests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_confirm_the_application_is_running(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        Assert.True(response.IsSuccessStatusCode);
    }
}

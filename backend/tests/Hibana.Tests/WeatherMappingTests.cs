using Hibana.Api;
using Xunit;

namespace Hibana.Tests;

public sealed class WeatherMappingTests
{
    [Theory]
    [InlineData(0, "clear")]
    [InlineData(61, "rain")]
    [InlineData(71, "snow")]
    [InlineData(95, "thunderstorm")]
    public void Condition_maps_open_meteo_codes_to_stable_hibana_codes(int providerCode, string expectedCode)
    {
        Assert.Equal(expectedCode, WeatherService.Condition(providerCode).Code);
    }

    [Theory]
    [InlineData(0, "N")]
    [InlineData(90, "E")]
    [InlineData(225, "SW")]
    [InlineData(360, "N")]
    public void Wind_direction_maps_degrees_to_cardinal_direction(double degrees, string expectedDirection)
    {
        Assert.Equal(expectedDirection, WeatherService.WindDirection(degrees));
    }
}

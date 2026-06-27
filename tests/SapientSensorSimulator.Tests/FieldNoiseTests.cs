using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class FieldNoiseTests
{
    [Theory]
    [InlineData("East:Gaussian:2.5", "East", NoiseKind.Gaussian, 2.5)]
    [InlineData("Altitude:uniform:1", "Altitude", NoiseKind.Uniform, 1)]
    [InlineData("UpRate:SYSTEMATIC:0.5", "UpRate", NoiseKind.Systematic, 0.5)]
    public void TryParse_ValidSpec_ParsesAllParts(string spec, string field, NoiseKind kind, double magnitude)
    {
        var noise = FieldNoise.TryParse(spec);

        Assert.NotNull(noise);
        Assert.Equal(field, noise!.Field);
        Assert.Equal(kind, noise.Kind);
        Assert.Equal(magnitude, noise.Magnitude);
    }

    [Theory]
    [InlineData("East:Gaussian")] // missing magnitude
    [InlineData("East:NotAKind:2.5")] // unknown kind
    [InlineData("East:Gaussian:notanumber")] // unparsable magnitude
    [InlineData("justonepart")]
    public void TryParse_InvalidSpec_ReturnsNull(string spec)
    {
        Assert.Null(FieldNoise.TryParse(spec));
    }
}

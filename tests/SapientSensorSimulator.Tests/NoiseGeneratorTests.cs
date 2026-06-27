using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class NoiseGeneratorTests
{
    [Fact]
    public void Apply_NoNoise_ReturnsValueUnchanged()
    {
        var result = NoiseGenerator.Apply(42, noise: null, absoluteSeconds: 0);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Apply_NoneKind_ReturnsValueUnchanged()
    {
        var noise = new FieldNoise("East", NoiseKind.None, 5.0);
        var result = NoiseGenerator.Apply(42, noise, absoluteSeconds: 0);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Apply_Systematic_IsDeterministicForTheSameTime()
    {
        var noise = new FieldNoise("East", NoiseKind.Systematic, 3.0);

        var a = NoiseGenerator.Apply(0, noise, absoluteSeconds: 12.5);
        var b = NoiseGenerator.Apply(0, noise, absoluteSeconds: 12.5);

        Assert.Equal(a, b); // same wall-clock time -> same bias, not random.
    }

    [Fact]
    public void Apply_Systematic_StaysWithinMagnitudeBounds()
    {
        var noise = new FieldNoise("East", NoiseKind.Systematic, 3.0);

        for (var t = 0.0; t < 200; t += 1.0)
        {
            var result = NoiseGenerator.Apply(0, noise, t);
            Assert.InRange(result, -3.0, 3.0);
        }
    }

    [Fact]
    public void Apply_Uniform_StaysWithinMagnitudeBounds()
    {
        var noise = new FieldNoise("East", NoiseKind.Uniform, 5.0);

        for (var i = 0; i < 200; i++)
        {
            var result = NoiseGenerator.Apply(10, noise, absoluteSeconds: 0);
            Assert.InRange(result, 10 - 5.0, 10 + 5.0);
        }
    }

    [Fact]
    public void Apply_Gaussian_IsCenteredOnOriginalValue()
    {
        var noise = new FieldNoise("East", NoiseKind.Gaussian, 2.0);
        var samples = Enumerable.Range(0, 5000).Select(_ => NoiseGenerator.Apply(0, noise, 0)).ToList();

        var mean = samples.Average();
        Assert.InRange(mean, -0.2, 0.2); // should average out close to 0 over many samples
    }
}

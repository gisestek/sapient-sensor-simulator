namespace SapientSensorSimulator;

/// <summary>
/// Applies per-field error to an otherwise-clean trajectory value. <see cref="NoiseKind.Systematic"/>
/// is driven by <paramref name="absoluteSeconds"/> (wall-clock-derived, like the trajectory itself)
/// so it stays deterministic and reproducible rather than depending on process-local random state —
/// consistent with this simulator's "same wall clock -> same data" design.
/// </summary>
public static class NoiseGenerator
{
    private static readonly Random Random = new();

    public static double Apply(double value, FieldNoise? noise, double absoluteSeconds)
    {
        if (noise is null || noise.Kind == NoiseKind.None || noise.Magnitude == 0)
        {
            return value;
        }

        return noise.Kind switch
        {
            NoiseKind.Gaussian => value + SampleGaussian(noise.Magnitude),
            NoiseKind.Uniform => value + (Random.NextDouble() * 2 - 1) * noise.Magnitude,
            // A slow (~100s period) sine wave: a "regular" bias rather than randomness, e.g. a
            // sensor with consistent calibration drift instead of pure measurement noise.
            NoiseKind.Systematic => value + noise.Magnitude * Math.Sin(absoluteSeconds * (2 * Math.PI / 100.0)),
            _ => value
        };
    }

    private static double SampleGaussian(double standardDeviation)
    {
        // Box-Muller transform.
        var u1 = 1.0 - Random.NextDouble();
        var u2 = Random.NextDouble();
        var standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return standardNormal * standardDeviation;
    }
}

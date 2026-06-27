namespace SapientSensorSimulator;

/// <summary>One "--noise field:kind:magnitude" specification — see SimulatorOptions.PrintUsage for the field names this simulator recognises.</summary>
public sealed record FieldNoise(string Field, NoiseKind Kind, double Magnitude)
{
    public static FieldNoise? TryParse(string spec)
    {
        var parts = spec.Split(':');
        if (parts.Length != 3)
        {
            return null;
        }

        if (!Enum.TryParse<NoiseKind>(parts[1], ignoreCase: true, out var kind))
        {
            return null;
        }

        if (!double.TryParse(parts[2], System.Globalization.CultureInfo.InvariantCulture, out var magnitude))
        {
            return null;
        }

        return new FieldNoise(parts[0], kind, magnitude);
    }
}

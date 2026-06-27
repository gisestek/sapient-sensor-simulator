namespace SapientSensorSimulator;

public sealed class SimulatorOptions
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public int TargetCount { get; init; } = 2;
    public int IntervalMs { get; init; } = 1000;
    public double OriginLat { get; init; } = 60.1699;
    public double OriginLon { get; init; } = 24.9384;
    public string NodeName { get; init; } = "Sapient Sensor Simulator";

    /// <summary>Per-field noise specs from repeated "--noise field:kind:magnitude" args, keyed
    /// by field name (case-insensitive). Recognised fields: East, North, Altitude, EastRate,
    /// NorthRate, UpRate (all metres / metres-per-second, applied before the lat/lon conversion).</summary>
    public Dictionary<string, FieldNoise> NoiseSpecs { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static SimulatorOptions? Parse(string[] args)
    {
        string? host = null;
        int? port = null;
        var targetCount = 2;
        var intervalMs = 1000;
        var originLat = 60.1699;
        var originLon = 24.9384;
        var nodeName = "Sapient Sensor Simulator";
        var noiseSpecs = new Dictionary<string, FieldNoise>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host" when i + 1 < args.Length:
                    host = args[++i];
                    break;
                case "--port" when i + 1 < args.Length:
                    port = int.Parse(args[++i]);
                    break;
                case "--targets" when i + 1 < args.Length:
                    targetCount = int.Parse(args[++i]);
                    break;
                case "--interval-ms" when i + 1 < args.Length:
                    intervalMs = int.Parse(args[++i]);
                    break;
                case "--origin-lat" when i + 1 < args.Length:
                    originLat = double.Parse(args[++i]);
                    break;
                case "--origin-lon" when i + 1 < args.Length:
                    originLon = double.Parse(args[++i]);
                    break;
                case "--name" when i + 1 < args.Length:
                    nodeName = args[++i];
                    break;
                case "--noise" when i + 1 < args.Length:
                    var spec = FieldNoise.TryParse(args[++i]);
                    if (spec is null)
                    {
                        Console.WriteLine($"Ignoring malformed --noise value (expected field:kind:magnitude): {args[i]}");
                    }
                    else
                    {
                        noiseSpecs[spec.Field] = spec;
                    }

                    break;
                case "--help":
                case "-h":
                    return null;
            }
        }

        if (port is null)
        {
            return null;
        }

        return new SimulatorOptions
        {
            Host = host ?? "127.0.0.1",
            Port = port.Value,
            TargetCount = targetCount,
            IntervalMs = intervalMs,
            OriginLat = originLat,
            OriginLon = originLon,
            NodeName = nodeName,
            NoiseSpecs = noiseSpecs
        };
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
            Usage: SapientSensorSimulator --port <port> [options]

            Connects out to a SAPIENT Fusion Node / Apex-SAPIENT-Middleware over TCP and
            streams a Registration message followed by periodic DetectionReports for
            simulated moving targets (BSI Flex 335 v2.0, Apex wire framing).

            Target motion is a pure function of wall-clock (Unix) time, not time since this
            process started — run two instances with the same target parameters and synchronised
            clocks (even on different machines) and they describe the exact same trajectory at
            the exact same moment, so a Fusion Node can genuinely associate them as one target.

            Options:
              --host <host>        Target host (default: 127.0.0.1)
              --port <port>        Target port (required)
              --targets <n>        Number of simulated targets (default: 2)
              --interval-ms <ms>   Time between detection report batches (default: 1000)
              --origin-lat <deg>   Origin latitude for simulated targets (default: 60.1699)
              --origin-lon <deg>   Origin longitude for simulated targets (default: 24.9384)
              --name <name>        Registration "name" field (default: Sapient Sensor Simulator)
              --noise field:kind:magnitude
                                    Adds error to one field before it's sent. Repeatable.
                                    Fields: East, North, Altitude, EastRate, NorthRate, UpRate
                                            (metres / metres-per-second)
                                    Kinds:  Gaussian (magnitude = std. deviation)
                                            Uniform  (magnitude = +/- range)
                                            Systematic (magnitude = deterministic sine-wave bias
                                                        amplitude, driven by wall-clock time —
                                                        "säännönmukaista", not random)
                                    Example: --noise East:Gaussian:2.5 --noise Altitude:Systematic:1.0
            """);
    }
}

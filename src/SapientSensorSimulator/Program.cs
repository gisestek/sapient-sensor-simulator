using System.Net.Sockets;
using SapientSensorSimulator;
using Task = System.Threading.Tasks.Task;

var options = SimulatorOptions.Parse(args);
if (options is null)
{
    SimulatorOptions.PrintUsage();
    return 1;
}

var nodeId = Guid.NewGuid().ToString();
var targets = Enumerable.Range(1, options.TargetCount)
    .Select(i => new SimulatedTarget
    {
        ObjectId = Guid.NewGuid().ToString(),
        RadiusMetres = 100 + i * 50,
        AngularSpeedRadPerSec = 0.05 * i,
        AltitudeMetres = 30 + i * 10,
        PhaseOffsetRad = i * Math.PI / 3
    })
    .ToList();

Console.WriteLine($"Sapient Sensor Simulator — connecting to {options.Host}:{options.Port} as node {nodeId}");
Console.WriteLine($"Simulating {targets.Count} target(s) around origin ({options.OriginLat}, {options.OriginLon})");
if (options.NoiseSpecs.Count > 0)
{
    Console.WriteLine($"Noise: {string.Join(", ", options.NoiseSpecs.Values.Select(n => $"{n.Field}={n.Kind}({n.Magnitude})"))}");
}

using var client = new TcpClient();
await client.ConnectAsync(options.Host, options.Port);
var stream = client.GetStream();

await SendAsync(stream, MessageFactory.BuildRegistration(nodeId, options.NodeName));
Console.WriteLine("Registration sent.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
    // Wall-clock (Unix) time, not "seconds since this process started" — see SimulatedTarget's
    // docs for why that's what lets independent simulator instances describe the same target.
    var absoluteSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

    foreach (var target in targets)
    {
        var (east, north, alt, eastRate, northRate, upRate) = target.GetLocalState(absoluteSeconds);

        east = NoiseGenerator.Apply(east, options.NoiseSpecs.GetValueOrDefault("East"), absoluteSeconds);
        north = NoiseGenerator.Apply(north, options.NoiseSpecs.GetValueOrDefault("North"), absoluteSeconds);
        alt = NoiseGenerator.Apply(alt, options.NoiseSpecs.GetValueOrDefault("Altitude"), absoluteSeconds);
        eastRate = NoiseGenerator.Apply(eastRate, options.NoiseSpecs.GetValueOrDefault("EastRate"), absoluteSeconds);
        northRate = NoiseGenerator.Apply(northRate, options.NoiseSpecs.GetValueOrDefault("NorthRate"), absoluteSeconds);
        upRate = NoiseGenerator.Apply(upRate, options.NoiseSpecs.GetValueOrDefault("UpRate"), absoluteSeconds);

        var (lat, lon) = SimulatedTarget.ToLatLon(options.OriginLat, options.OriginLon, east, north);
        var message = MessageFactory.BuildDetectionReport(nodeId, target.ObjectId, lat, lon, alt, eastRate, northRate, upRate);
        await SendAsync(stream, message);
    }

    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Sent {targets.Count} detection report(s).");

    try
    {
        await Task.Delay(options.IntervalMs, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine("Stopped.");
return 0;

static async Task SendAsync(NetworkStream stream, Google.Protobuf.IMessage message)
{
    var frame = SapientWireCodec.Encode(message);
    await stream.WriteAsync(frame);
}

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

using var client = new TcpClient();
await client.ConnectAsync(options.Host, options.Port);
var stream = client.GetStream();

await SendAsync(stream, MessageFactory.BuildRegistration(nodeId, options.NodeName));
Console.WriteLine("Registration sent.");

var startedAt = DateTime.UtcNow;
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
    var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;

    foreach (var target in targets)
    {
        var (lat, lon, alt, eastRate, northRate) = target.GetState(options.OriginLat, options.OriginLon, elapsed);
        var message = MessageFactory.BuildDetectionReport(nodeId, target.ObjectId, lat, lon, alt, eastRate, northRate);
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

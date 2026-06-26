using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using SapientMsg.BsiFlex335V20;
using SapientSensorSimulator;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace SapientSensorSimulator.Tests;

/// <summary>
/// Sends a Registration and a DetectionReport over a real TCP loopback connection using
/// SapientWireCodec, then reads them back with a minimal independent frame reader — proving
/// the simulator's wire output is decodable exactly as a Fusion Node / Apex would see it.
/// </summary>
public class WireProtocolTests
{
    [Fact]
    public async Task MessagesSentOverTcp_DecodeBackToOriginalContent()
    {
        var port = GetFreeTcpPort();
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        var acceptTask = listener.AcceptTcpClientAsync();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var clientStream = client.GetStream();

        var nodeId = Guid.NewGuid().ToString();
        await clientStream.WriteAsync(SapientWireCodec.Encode(MessageFactory.BuildRegistration(nodeId, "Test Sensor")));
        await clientStream.WriteAsync(SapientWireCodec.Encode(
            MessageFactory.BuildDetectionReport(nodeId, "obj-1", lat: 60.2, lon: 24.9, alt: 5, eastRate: 1, northRate: 1)));

        using var serverClient = await acceptTask;
        var serverStream = serverClient.GetStream();

        var first = await ReadFrameAsync(serverStream);
        var second = await ReadFrameAsync(serverStream);

        var registrationMessage = SapientMessage.Parser.ParseFrom(first);
        var detectionMessage = SapientMessage.Parser.ParseFrom(second);

        Assert.Equal(SapientMessage.ContentOneofCase.Registration, registrationMessage.ContentCase);
        Assert.Equal(nodeId, registrationMessage.NodeId);

        Assert.Equal(SapientMessage.ContentOneofCase.DetectionReport, detectionMessage.ContentCase);
        Assert.Equal("obj-1", detectionMessage.DetectionReport.ObjectId);

        listener.Stop();
    }

    private static async Task<byte[]> ReadFrameAsync(NetworkStream stream)
    {
        var lengthBuffer = new byte[4];
        await ReadExactAsync(stream, lengthBuffer);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(lengthBuffer);

        var payload = new byte[length];
        await ReadExactAsync(stream, payload);
        return payload;
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset));
            if (read == 0)
            {
                throw new IOException("Connection closed before frame was fully read.");
            }

            offset += read;
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

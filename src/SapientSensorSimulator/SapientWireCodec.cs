using System.Buffers.Binary;
using Google.Protobuf;

namespace SapientSensorSimulator;

/// <summary>
/// Frames SAPIENT protobuf messages exactly as Apex-SAPIENT-Middleware does on the wire:
/// a 4-byte little-endian length prefix followed by the raw protobuf payload
/// (see Apex's sapient_apex_server/message_io.py: struct.pack("&lt;I", len) + bytes).
/// Kept identical to sapient-fusion-node's SapientWireCodec so the two interoperate.
/// </summary>
public static class SapientWireCodec
{
    public const int LengthPrefixSize = 4;

    public static byte[] Encode(IMessage message)
    {
        var payload = message.ToByteArray();
        var frame = new byte[LengthPrefixSize + payload.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(frame, (uint)payload.Length);
        payload.CopyTo(frame, LengthPrefixSize);
        return frame;
    }
}

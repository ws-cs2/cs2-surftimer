using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace WST;

public class VectorConverter : JsonConverter<Vector>
{
    public override Vector Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader, options);
        return new Vector(obj["x"], obj["y"], obj["z"]);
    }

    public override void Write(Utf8JsonWriter writer, Vector value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}

public class QAngleConverter : JsonConverter<QAngle>
{
    public override QAngle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader, options);
        return new QAngle(obj["x"], obj["y"], obj["z"]);
    }

    public override void Write(Utf8JsonWriter writer, QAngle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}

public class ReplayPlayback
{
    public ReplayPlayback(Replay replay)
    {
        // convert to native
        foreach (var frame in replay.Frames)
        {
            var frameNative = new FrameTNative
            {
                Pos = frame.Pos.ToNativeVector(),
                Ang = frame.Ang.ToNativeQAngle(),
                Buttons = frame.Buttons,
                Flags = frame.Flags,
                MoveType = frame.MoveType
            };
            Frames.Add(frameNative);
        }
    }
    
    public class FrameTNative
    {
        public Vector Pos { get; set; }
        public QAngle Ang { get; set; }
        public ulong Buttons { get; set; }
        public uint Flags { get; set; }
        public MoveType_t MoveType { get; set; }
    }
    
    public List<FrameTNative> Frames { get; set; } = new List<FrameTNative>();
}

public class Replay
{
    public List<FrameT> Frames { get; set; } = new List<FrameT>();

    public byte[] Serialize()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VectorConverter(), new QAngleConverter() }
        };
        return JsonSerializer.SerializeToUtf8Bytes(this, options);
    }
    
    public string SerializeString()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VectorConverter(), new QAngleConverter() }
        };
        return JsonSerializer.Serialize(this, options);
    }

    public static Replay Deserialize(byte[] data)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VectorConverter(), new QAngleConverter() }
        };
        return JsonSerializer.Deserialize<Replay>(data, options);
    }
    
    public static Replay DeserializeString(string data)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new VectorConverter(), new QAngleConverter() }
        };
        return JsonSerializer.Deserialize<Replay>(data, options);
    }

    public class FrameT
    {
        public ZoneVector Pos { get; set; }
        public ZoneVector Ang { get; set; }
        public ulong Buttons { get; set; }
        public uint Flags { get; set; }
        public MoveType_t MoveType { get; set; }
    }
    
    public Replay DeepCopy()
    {
        var copy = new Replay
        {
            Frames = this.Frames.Select(f => new FrameT
            {
                Pos = new ZoneVector(f.Pos.x, f.Pos.y, f.Pos.z),
                Ang = new ZoneVector(f.Ang.x, f.Ang.y, f.Ang.z),
                Buttons = f.Buttons,
                Flags = f.Flags,
                MoveType = f.MoveType
            }).ToList()
        };

        return copy;
    }
}


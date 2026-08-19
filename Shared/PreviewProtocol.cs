namespace Cerneala.Preview;

using System;
using System.IO;
using System.Text;

internal enum PreviewRequestKind : byte
{
    Render = 1,
    Capture = 2,
    Click = 3,
    Text = 4,
    Key = 5,
    Shutdown = 6,
    PointerMove = 7,
    PointerButton = 8,
    PointerWheel = 9,
    PointerLeave = 10,
    KeyState = 11,
    ResetInput = 12
}

internal enum PreviewResponseKind : byte
{
    Frame = 1,
    Error = 2,
    Acknowledged = 3
}

internal sealed class PreviewRequest
{
    public PreviewRequestKind Kind { get; set; }

    public int RequestId { get; set; }

    public string DocumentPath { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public int Width { get; set; }

    public int Height { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public string Text { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int Modifiers { get; set; }

    public string Button { get; set; } = string.Empty;

    public bool IsDown { get; set; }

    public int WheelDelta { get; set; }
}

internal sealed class PreviewResponse
{
    public PreviewResponseKind Kind { get; set; }

    public int RequestId { get; set; }

    public byte[] Image { get; set; } = Array.Empty<byte>();

    public int Width { get; set; }

    public int Height { get; set; }

    public int Stride { get; set; }

    public double CompileMilliseconds { get; set; }

    public double RenderMilliseconds { get; set; }

    public string Error { get; set; } = string.Empty;
}

internal static class PreviewProtocol
{
    private const int MaximumFrameLength = 128 * 1024 * 1024;

    public static void WriteRequest(Stream stream, PreviewRequest request)
    {
        WriteFrame(stream, writer =>
        {
            writer.Write((byte)request.Kind);
            writer.Write(request.RequestId);
            switch (request.Kind)
            {
                case PreviewRequestKind.Render:
                    writer.Write(request.DocumentPath);
                    writer.Write(request.SourceText);
                    writer.Write(request.Width);
                    writer.Write(request.Height);
                    break;
                case PreviewRequestKind.Click:
                case PreviewRequestKind.PointerMove:
                    writer.Write(request.X);
                    writer.Write(request.Y);
                    break;
                case PreviewRequestKind.PointerButton:
                    writer.Write(request.X);
                    writer.Write(request.Y);
                    writer.Write(request.Button);
                    writer.Write(request.IsDown);
                    break;
                case PreviewRequestKind.PointerWheel:
                    writer.Write(request.X);
                    writer.Write(request.Y);
                    writer.Write(request.WheelDelta);
                    break;
                case PreviewRequestKind.Text:
                    writer.Write(request.Text);
                    break;
                case PreviewRequestKind.Key:
                    writer.Write(request.Key);
                    writer.Write(request.Modifiers);
                    break;
                case PreviewRequestKind.KeyState:
                    writer.Write(request.Key);
                    writer.Write(request.IsDown);
                    break;
            }
        });
    }

    public static PreviewRequest? ReadRequest(Stream stream)
    {
        BinaryReader? reader = ReadFrame(stream);
        if (reader is null)
        {
            return null;
        }

        using (reader)
        {
            PreviewRequest request = new()
            {
                Kind = (PreviewRequestKind)reader.ReadByte(),
                RequestId = reader.ReadInt32()
            };
            switch (request.Kind)
            {
                case PreviewRequestKind.Render:
                    request.DocumentPath = reader.ReadString();
                    request.SourceText = reader.ReadString();
                    request.Width = reader.ReadInt32();
                    request.Height = reader.ReadInt32();
                    break;
                case PreviewRequestKind.Click:
                case PreviewRequestKind.PointerMove:
                    request.X = reader.ReadDouble();
                    request.Y = reader.ReadDouble();
                    break;
                case PreviewRequestKind.PointerButton:
                    request.X = reader.ReadDouble();
                    request.Y = reader.ReadDouble();
                    request.Button = reader.ReadString();
                    request.IsDown = reader.ReadBoolean();
                    break;
                case PreviewRequestKind.PointerWheel:
                    request.X = reader.ReadDouble();
                    request.Y = reader.ReadDouble();
                    request.WheelDelta = reader.ReadInt32();
                    break;
                case PreviewRequestKind.Text:
                    request.Text = reader.ReadString();
                    break;
                case PreviewRequestKind.Key:
                    request.Key = reader.ReadString();
                    request.Modifiers = reader.ReadInt32();
                    break;
                case PreviewRequestKind.KeyState:
                    request.Key = reader.ReadString();
                    request.IsDown = reader.ReadBoolean();
                    break;
            }

            return request;
        }
    }

    public static void WriteResponse(Stream stream, PreviewResponse response)
    {
        if (response.Kind == PreviewResponseKind.Frame)
        {
            int payloadLength = checked(37 + response.Image.Length);
            using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(payloadLength);
            writer.Write((byte)response.Kind);
            writer.Write(response.RequestId);
            writer.Write(response.CompileMilliseconds);
            writer.Write(response.RenderMilliseconds);
            writer.Write(response.Width);
            writer.Write(response.Height);
            writer.Write(response.Stride);
            writer.Write(response.Image.Length);
            writer.Write(response.Image);
            writer.Flush();
            return;
        }

        WriteFrame(stream, writer =>
        {
            writer.Write((byte)response.Kind);
            writer.Write(response.RequestId);
            if (response.Kind == PreviewResponseKind.Error)
            {
                writer.Write(response.Error);
            }
        });
    }

    public static PreviewResponse? ReadResponse(Stream stream) => ReadResponse(stream, reusableImage: null);

    public static PreviewResponse? ReadResponse(Stream stream, byte[]? reusableImage)
    {
        int? frameLength = ReadFrameLength(stream);
        if (frameLength is null)
        {
            return null;
        }

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
        PreviewResponse response = new()
        {
            Kind = (PreviewResponseKind)reader.ReadByte(),
            RequestId = reader.ReadInt32()
        };
        if (response.Kind == PreviewResponseKind.Frame)
        {
            response.CompileMilliseconds = reader.ReadDouble();
            response.RenderMilliseconds = reader.ReadDouble();
            response.Width = reader.ReadInt32();
            response.Height = reader.ReadInt32();
            response.Stride = reader.ReadInt32();
            int imageLength = reader.ReadInt32();
            if (response.Width <= 0 || response.Height <= 0 ||
                response.Stride != checked(response.Width * 4) ||
                imageLength != checked(response.Stride * response.Height) ||
                imageLength > MaximumFrameLength ||
                frameLength != checked(37 + imageLength))
            {
                throw new InvalidDataException("The preview frame dimensions or image length are invalid.");
            }

            byte[] image = reusableImage is not null && reusableImage.Length == imageLength
                ? reusableImage
                : new byte[imageLength];
            ReadExactly(stream, image, 0, imageLength);
            response.Image = image;
        }
        else if (response.Kind == PreviewResponseKind.Error)
        {
            response.Error = reader.ReadString();
        }

        return response;
    }

    private static void WriteFrame(Stream stream, Action<BinaryWriter> writePayload)
    {
        using MemoryStream payload = new();
        using (BinaryWriter writer = new(payload, Encoding.UTF8, leaveOpen: true))
        {
            writePayload(writer);
            writer.Flush();
        }

        using BinaryWriter frame = new(stream, Encoding.UTF8, leaveOpen: true);
        frame.Write(checked((int)payload.Length));
        payload.Position = 0;
        payload.CopyTo(stream);
        stream.Flush();
    }

    private static BinaryReader? ReadFrame(Stream stream)
    {
        int? length = ReadFrameLength(stream);
        if (length is null)
        {
            return null;
        }

        byte[] payload = new byte[length.Value];
        ReadExactly(stream, payload, 0, payload.Length);
        return new BinaryReader(new MemoryStream(payload, writable: false), Encoding.UTF8);
    }

    private static int? ReadFrameLength(Stream stream)
    {
        byte[] lengthBytes = new byte[sizeof(int)];
        int first = stream.ReadByte();
        if (first < 0)
        {
            return null;
        }

        lengthBytes[0] = (byte)first;
        ReadExactly(stream, lengthBytes, 1, lengthBytes.Length - 1);
        int length = BitConverter.ToInt32(lengthBytes, 0);
        if (length <= 0 || length > MaximumFrameLength)
        {
            throw new InvalidDataException("The preview protocol frame length is invalid.");
        }

        return length;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int read = stream.Read(buffer, offset, count);
            if (read == 0)
            {
                throw new EndOfStreamException("The preview protocol frame was truncated.");
            }

            offset += read;
            count -= read;
        }
    }
}

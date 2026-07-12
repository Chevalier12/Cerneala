using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ri.Mcp;

public sealed class ContinuationTokenCodec
{
    private readonly byte[] key;

    public ContinuationTokenCodec()
        : this(RandomNumberGenerator.GetBytes(32))
    {
    }

    internal ContinuationTokenCodec(byte[] key)
        => this.key = key.ToArray();

    public string Encode(string tool, string generationId, int offset)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(new TokenPayload(tool, generationId, offset));
        var signature = HMACSHA256.HashData(key, payload);
        return Base64Url(payload) + "." + Base64Url(signature);
    }

    public int Decode(string token, string expectedTool, string expectedGenerationId)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4096)
        {
            throw new ContinuationTokenException("Continuation token is malformed.");
        }
        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            throw new ContinuationTokenException("Continuation token is malformed.");
        }

        try
        {
            var payloadBytes = FromBase64Url(parts[0]);
            var signature = FromBase64Url(parts[1]);
            var expectedSignature = HMACSHA256.HashData(key, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
            {
                throw new ContinuationTokenException("Continuation token signature is invalid.");
            }

            var payload = JsonSerializer.Deserialize<TokenPayload>(payloadBytes)
                ?? throw new ContinuationTokenException("Continuation token payload is invalid.");
            if (!string.Equals(payload.Tool, expectedTool, StringComparison.Ordinal)
                || !string.Equals(payload.GenerationId, expectedGenerationId, StringComparison.Ordinal)
                || payload.Offset < 0)
            {
                throw new ContinuationTokenException("Continuation token does not match the tool or active index generation.");
            }

            return payload.Offset;
        }
        catch (ContinuationTokenException)
        {
            throw;
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new ContinuationTokenException("Continuation token is malformed.");
        }
    }

    private static string Base64Url(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
        return Convert.FromBase64String(normalized);
    }

    private sealed record TokenPayload(string Tool, string GenerationId, int Offset);
}

public sealed class ContinuationTokenException : Exception
{
    public ContinuationTokenException(string message) : base(message)
    {
    }
}

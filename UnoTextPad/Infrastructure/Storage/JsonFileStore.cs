using System.Text.Json;

namespace UnoTextPad.Infrastructure.Storage;

/// <inheritdoc cref="IJsonFileStore"/>
public sealed class JsonFileStore : IJsonFileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        TypeInfoResolver = AppJsonSerializerContext.Default,

        // These files are small and occasionally worth reading or repairing by hand.
        WriteIndented = true
    };

    public async Task<TValue?> ReadAsync<TValue>(
        string filePath,
        CancellationToken cancellationToken = default) where TValue : class
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer
                .DeserializeAsync<TValue>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            // A corrupt or unreadable state file must never stop the app from starting;
            // the caller falls back to defaults.
            return null;
        }
    }

    public async Task WriteAsync<TValue>(
        string filePath,
        TValue value,
        CancellationToken cancellationToken = default) where TValue : class
    {
        var temporaryFilePath = filePath + ".tmp";

        await using (var stream = File.Create(temporaryFilePath))
        {
            await JsonSerializer
                .SerializeAsync(stream, value, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        File.Move(temporaryFilePath, filePath, overwrite: true);
    }
}

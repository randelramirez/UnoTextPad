namespace UnoTextPad.Infrastructure.Storage;

/// <summary>
/// Reads and writes small JSON documents. Writes are atomic so an interrupted save can
/// never leave a half-written settings or session file behind.
/// </summary>
public interface IJsonFileStore
{
    /// <summary>
    /// Returns the deserialized contents of <paramref name="filePath"/>, or <c>null</c> when the
    /// file is missing, empty or unreadable.
    /// </summary>
    Task<TValue?> ReadAsync<TValue>(string filePath, CancellationToken cancellationToken = default)
        where TValue : class;

    Task WriteAsync<TValue>(string filePath, TValue value, CancellationToken cancellationToken = default)
        where TValue : class;
}

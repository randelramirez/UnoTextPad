namespace UnoTextPad.Features.Session;

/// <summary>
/// The persisted set of open tabs, restored the next time the app starts.
/// </summary>
public sealed class SessionSnapshot
{
    public int ActiveDocumentIndex { get; set; }

    public List<DocumentSnapshot> Documents { get; set; } = [];
}

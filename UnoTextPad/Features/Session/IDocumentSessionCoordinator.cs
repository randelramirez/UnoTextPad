using UnoTextPad.Features.Documents;
using UnoTextPad.Features.Settings;

namespace UnoTextPad.Features.Session;

/// <summary>The tabs restored from the previous run.</summary>
/// <param name="Documents">The documents that could be restored, in tab order.</param>
/// <param name="ActiveDocumentIndex">The tab that was selected, or -1 when there is none.</param>
public sealed record RestoredSession(IReadOnlyList<DocumentViewModel> Documents, int ActiveDocumentIndex);

/// <summary>
/// Translates between the open tabs and the persisted <see cref="SessionSnapshot"/>, so that
/// the main view model only has to say "restore" or "save".
/// </summary>
public interface IDocumentSessionCoordinator
{
    Task<RestoredSession> RestoreAsync(
        EditorSettingsViewModel settings,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        IReadOnlyList<DocumentViewModel> documents,
        int activeDocumentIndex,
        CancellationToken cancellationToken = default);
}

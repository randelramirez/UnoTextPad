using UnoTextPad.Features.Documents;
using UnoTextPad.Features.Session;
using UnoTextPad.Features.Settings;
using UnoTextPad.Infrastructure.Storage;
using UnoTextPad.Tests.TestInfrastructure;
using Xunit;
using static UnoTextPad.Tests.TestInfrastructure.TestCancellation;

namespace UnoTextPad.Tests.Features.Session;

/// <summary>
/// Exercises the behaviour that matters most to the user: the tabs that were open when the
/// app closed come back, including work that was never saved to a file.
/// </summary>
public class DocumentSessionCoordinatorTests : IDisposable
{
    private readonly TemporaryAppDataPathProvider _pathProvider = new();
    private readonly SessionRepository _sessionRepository;
    private readonly DocumentSessionCoordinator _coordinator;
    private readonly EditorSettingsViewModel _settings;
    private readonly string _workingFolder;

    public DocumentSessionCoordinatorTests()
    {
        _sessionRepository = new SessionRepository(_pathProvider, new JsonFileStore());
        _coordinator = new DocumentSessionCoordinator(_sessionRepository, new TextFileService());
        _settings = new EditorSettingsViewModel(
            new InMemorySettingsRepository(),
            new StubSystemFontProvider(),
            new RecordingThemeService());

        _workingFolder = Path.Combine(Path.GetTempPath(), $"unotextpad-docs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workingFolder);
    }

    public void Dispose()
    {
        _pathProvider.Dispose();
        Directory.Delete(_workingFolder, recursive: true);
    }

    [Fact]
    public async Task RestoreAsync_OnFirstRun_ReturnsNoDocuments()
    {
        var session = await _coordinator.RestoreAsync(_settings, Token);

        Assert.Empty(session.Documents);
        Assert.Equal(-1, session.ActiveDocumentIndex);
    }

    [Fact]
    public async Task RestoreAsync_ForASavedFile_ReadsTheTextBackFromDisk()
    {
        var filePath = CreateFile("notes.txt", "line one\r\nline two");
        await SaveSessionAsync(activeIndex: 0, new DocumentSnapshot
        {
            Id = "saved",
            FilePath = filePath,
            DisplayName = "notes.txt",
            CaretPosition = 4
        });

        var session = await _coordinator.RestoreAsync(_settings, Token);

        var document = Assert.Single(session.Documents);
        Assert.Equal("notes.txt", document.FileName);
        Assert.Equal("line one\nline two", document.Content);
        Assert.Equal(LineEndingStyle.CrLf, document.LineEnding);
        Assert.False(document.HasUnsavedChanges);
        Assert.Equal(4, document.CaretPosition);
    }

    [Fact]
    public async Task RestoreAsync_ForAnUnsavedBuffer_ReadsTheTextBackFromItsBackup()
    {
        await _sessionRepository.SaveBackupAsync("draft", "text that was never saved", Token);
        await SaveSessionAsync(activeIndex: 0, new DocumentSnapshot
        {
            Id = "draft",
            FilePath = null,
            DisplayName = "Untitled 3",
            HasUnsavedChanges = true
        });

        var session = await _coordinator.RestoreAsync(_settings, Token);

        var document = Assert.Single(session.Documents);
        Assert.Equal("Untitled 3", document.FileName);
        Assert.Equal("text that was never saved", document.Content);
        Assert.True(document.HasUnsavedChanges);
        Assert.Null(document.FilePath);
    }

    [Fact]
    public async Task RestoreAsync_ForAnEditedFile_PrefersTheBackupOverTheFileOnDisk()
    {
        var filePath = CreateFile("edited.txt", "what is on disk");
        await _sessionRepository.SaveBackupAsync("edited", "what the user was typing", Token);
        await SaveSessionAsync(activeIndex: 0, new DocumentSnapshot
        {
            Id = "edited",
            FilePath = filePath,
            DisplayName = "edited.txt",
            HasUnsavedChanges = true
        });

        var session = await _coordinator.RestoreAsync(_settings, Token);

        var document = Assert.Single(session.Documents);
        Assert.Equal("what the user was typing", document.Content);
        Assert.True(document.HasUnsavedChanges);
        Assert.Equal(filePath, document.FilePath);
    }

    [Fact]
    public async Task RestoreAsync_WhenAFileWasDeleted_DropsThatTabAndKeepsTheActiveOne()
    {
        var survivingFilePath = CreateFile("survivor.txt", "still here");
        await SaveSessionAsync(
            activeIndex: 1,
            new DocumentSnapshot { Id = "gone", FilePath = Path.Combine(_workingFolder, "gone.txt") },
            new DocumentSnapshot { Id = "kept", FilePath = survivingFilePath, DisplayName = "survivor.txt" });

        var session = await _coordinator.RestoreAsync(_settings, Token);

        Assert.Equal("survivor.txt", Assert.Single(session.Documents).FileName);
        Assert.Equal(0, session.ActiveDocumentIndex);
    }

    [Fact]
    public async Task SaveAsync_BacksUpOnlyUnsavedWork()
    {
        var cleanDocument = new DocumentViewModel("clean", "clean.txt", _settings);
        cleanDocument.AttachToFile(CreateFile("clean.txt", "saved"), TextEncodingKind.Utf8, LineEndingStyle.Lf);
        cleanDocument.LoadContent("saved");

        var dirtyDocument = new DocumentViewModel("dirty", "Untitled 1", _settings)
        {
            Content = "in progress"
        };

        await _coordinator.SaveAsync([cleanDocument, dirtyDocument], activeDocumentIndex: 1, Token);

        Assert.Null(await _sessionRepository.LoadBackupAsync("clean", Token));
        Assert.Equal("in progress", await _sessionRepository.LoadBackupAsync("dirty", Token));
    }

    [Fact]
    public async Task SaveAsync_RemovesBackupsOfTabsThatAreNoLongerOpen()
    {
        await _sessionRepository.SaveBackupAsync("closed-tab", "orphaned", Token);
        var openDocument = new DocumentViewModel("open-tab", "Untitled 1", _settings)
        {
            Content = "still open"
        };

        await _coordinator.SaveAsync([openDocument], activeDocumentIndex: 0, Token);

        Assert.Null(await _sessionRepository.LoadBackupAsync("closed-tab", Token));
        Assert.Equal("still open", await _sessionRepository.LoadBackupAsync("open-tab", Token));
    }

    [Fact]
    public async Task SaveAsync_ThenRestoreAsync_ReproducesTheWholeWorkspace()
    {
        var savedDocument = new DocumentViewModel("saved", "report.txt", _settings);
        savedDocument.AttachToFile(CreateFile("report.txt", "final"), TextEncodingKind.Utf8, LineEndingStyle.Lf);
        savedDocument.LoadContent("final");
        savedDocument.CaretPosition = 3;

        var draftDocument = new DocumentViewModel("draft", "Untitled 2", _settings)
        {
            Content = "half written"
        };

        await _coordinator.SaveAsync([savedDocument, draftDocument], activeDocumentIndex: 1, Token);
        var session = await _coordinator.RestoreAsync(_settings, Token);

        Assert.Equal(1, session.ActiveDocumentIndex);
        Assert.Equal(["report.txt", "Untitled 2"], session.Documents.Select(document => document.FileName));
        Assert.Equal("final", session.Documents[0].Content);
        Assert.Equal(3, session.Documents[0].CaretPosition);
        Assert.Equal("half written", session.Documents[1].Content);
        Assert.True(session.Documents[1].HasUnsavedChanges);
    }

    private string CreateFile(string fileName, string text)
    {
        var filePath = Path.Combine(_workingFolder, fileName);
        File.WriteAllText(filePath, text);
        return filePath;
    }

    private Task SaveSessionAsync(int activeIndex, params DocumentSnapshot[] documents)
        => _sessionRepository.SaveAsync(new SessionSnapshot
        {
            ActiveDocumentIndex = activeIndex,
            Documents = [.. documents]
        }, Token);
}

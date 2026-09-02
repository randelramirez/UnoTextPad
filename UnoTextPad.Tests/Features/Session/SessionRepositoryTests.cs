namespace UnoTextPad.Tests.Features.Session;

public class SessionRepositoryTests : IDisposable
{
    private readonly TemporaryAppDataPathProvider _pathProvider = new();
    private readonly SessionRepository _repository;

    public SessionRepositoryTests()
        => _repository = new SessionRepository(_pathProvider, new JsonFileStore());

    public void Dispose() => _pathProvider.Dispose();

    [Fact]
    public async Task LoadAsync_OnFirstRun_ReturnsNull()
        => Assert.Null(await _repository.LoadAsync(Token));

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RestoresEveryOpenTab()
    {
        var snapshot = new SessionSnapshot
        {
            ActiveDocumentIndex = 1,
            Documents =
            [
                new DocumentSnapshot { Id = "a", FilePath = "/tmp/one.txt", DisplayName = "one.txt" },
                new DocumentSnapshot
                {
                    Id = "b",
                    DisplayName = "Untitled 1",
                    HasUnsavedChanges = true,
                    CaretPosition = 42,
                    Encoding = TextEncodingKind.Utf8Bom,
                    LineEnding = LineEndingStyle.CrLf
                }
            ]
        };

        await _repository.SaveAsync(snapshot, Token);
        var reloaded = await _repository.LoadAsync(Token);

        Assert.Equivalent(snapshot, reloaded, strict: true);
    }

    [Fact]
    public async Task SaveBackupAsync_ThenLoadBackupAsync_ReturnsTheSameText()
    {
        await _repository.SaveBackupAsync("document-a", "unsaved work\nsecond line", Token);

        Assert.Equal("unsaved work\nsecond line", await _repository.LoadBackupAsync("document-a", Token));
    }

    [Fact]
    public async Task LoadBackupAsync_WhenThereIsNoBackup_ReturnsNull()
        => Assert.Null(await _repository.LoadBackupAsync("missing", Token));

    [Fact]
    public async Task DeleteBackup_RemovesOnlyThatBackup()
    {
        await _repository.SaveBackupAsync("keep", "kept", Token);
        await _repository.SaveBackupAsync("remove", "removed", Token);

        _repository.DeleteBackup("remove");

        Assert.Null(await _repository.LoadBackupAsync("remove", Token));
        Assert.Equal("kept", await _repository.LoadBackupAsync("keep", Token));
    }

    [Fact]
    public void DeleteBackup_WhenThereIsNothingToDelete_DoesNotThrow()
        => Assert.Null(Record.Exception(() => _repository.DeleteBackup("missing")));

    [Fact]
    public async Task DeleteBackupsExcept_RemovesBackupsOfClosedTabs()
    {
        await _repository.SaveBackupAsync("still-open", "text", Token);
        await _repository.SaveBackupAsync("was-closed", "text", Token);

        _repository.DeleteBackupsExcept(["still-open"]);

        Assert.Equal("text", await _repository.LoadBackupAsync("still-open", Token));
        Assert.Null(await _repository.LoadBackupAsync("was-closed", Token));
    }

    [Fact]
    public async Task SaveBackupAsync_PreservesUnicodeText()
    {
        await _repository.SaveBackupAsync("unicode", "grüße 🌍 世界", Token);

        Assert.Equal("grüße 🌍 世界", await _repository.LoadBackupAsync("unicode", Token));
    }
}

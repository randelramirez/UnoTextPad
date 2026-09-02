using System.Text.Json.Serialization;
using UnoTextPad.Features.Session;
using UnoTextPad.Features.Settings;

namespace UnoTextPad.Infrastructure.Storage;

/// <summary>
/// Compile-time JSON metadata for the types UnoTextPad persists. Using the source generator
/// avoids reflection at startup, which keeps session restore fast.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(EditorSettings))]
[JsonSerializable(typeof(SessionSnapshot))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;

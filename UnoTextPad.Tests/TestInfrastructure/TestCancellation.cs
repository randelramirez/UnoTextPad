using Xunit;

namespace UnoTextPad.Tests.TestInfrastructure;

/// <summary>
/// Exposes the cancellation token of the currently running test, so that an asynchronous call
/// that hangs is cancelled together with the test instead of stalling the whole run.
/// </summary>
internal static class TestCancellation
{
    public static CancellationToken Token => TestContext.Current.CancellationToken;
}

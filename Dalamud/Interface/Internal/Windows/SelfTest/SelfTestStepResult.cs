namespace Dalamud.Interface.Internal.Windows.SelfTest;

/// <summary>
/// Enum declaring result states of tests.
/// </summary>
internal enum SelfTestStepResult
{
    /// <summary>
    /// Test was not ran.
    /// </summary>
    NotRan,

    /// <summary>
    /// Test is waiting for completion.
    /// </summary>
    Waiting,

    /// <summary>
    /// Test has failed.
    /// </summary>
    Fail,

    /// <summary>
    /// Test has passed.
    /// </summary>
    Pass,
}

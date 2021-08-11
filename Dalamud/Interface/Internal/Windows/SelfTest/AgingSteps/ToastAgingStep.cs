namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Test setup for toasts.
    /// </summary>
    internal class ToastAgingStep : IAgingStep
    {
        /// <inheritdoc/>
        public string Name => "Test Toasts";

        /// <inheritdoc/>
        public SelfTestStepResult RunStep(Dalamud dalamud)
        {
            dalamud.Framework.Gui.Toast.ShowNormal("Normal Toast");
            dalamud.Framework.Gui.Toast.ShowError("Error Toast");

            return SelfTestStepResult.Pass;
        }

        /// <inheritdoc/>
        public void CleanUp(Dalamud dalamud)
        {
            // ignored
        }
    }
}

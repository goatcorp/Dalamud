namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps
{
    /// <summary>
    /// Interface for test implementations.
    /// </summary>
    internal interface IAgingStep
    {
        /// <summary>
        /// Gets the name of the test.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Run the test step, once per frame it is active.
        /// </summary>
        /// <param name="dalamud">Dalamud instance to act on.</param>
        /// <returns>The result of this frame, test is discarded once a result other than <see cref="SelfTestStepResult.Waiting"/> is returned.</returns>
        public SelfTestStepResult RunStep(Dalamud dalamud);

        /// <summary>
        /// Clean up this test.
        /// </summary>
        /// <param name="dalamud">Dalamud instance to act on.</param>
        public void CleanUp(Dalamud dalamud);
    }
}

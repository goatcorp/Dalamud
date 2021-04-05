namespace Dalamud.Interface.Components
{
    /// <summary>
    /// Base interface implementing a modular interface component.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Draw the component via ImGui.
        /// </summary>
        public void Draw();
    }
}

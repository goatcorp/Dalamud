using System.Collections.Generic;
using System.IO;

namespace Dalamud.Interface.Internal.Scratchpad
{
    /// <summary>
    /// A file watcher for <see cref="ScratchpadDocument"/> classes.
    /// </summary>
    internal class ScratchFileWatcher
    {
        private FileSystemWatcher watcher = new();

        /// <summary>
        /// Gets or sets the list of tracked ScratchPad documents.
        /// </summary>
        public List<ScratchpadDocument> TrackedScratches { get; set; } = new List<ScratchpadDocument>();

        /// <summary>
        /// Load a new ScratchPadDocument from disk.
        /// </summary>
        /// <param name="path">The filepath to load.</param>
        public void Load(string path)
        {
            this.TrackedScratches.Add(new ScratchpadDocument
            {
                Title = Path.GetFileName(path),
                Content = File.ReadAllText(path),
            });

            this.watcher.Path = Path.GetDirectoryName(path);
            this.watcher.Filter = Path.GetFileName(path);
            this.watcher.EnableRaisingEvents = true;
            this.watcher.Changed += (sender, args) => this.TrackedScratches[0].Content = File.ReadAllText(args.FullPath);
            this.watcher.BeginInit();
        }
    }
}

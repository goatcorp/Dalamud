using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Interface.Scratchpad
{
    class ScratchFileWatcher
    {
        public List<ScratchpadDocument> TrackedScratches { get; set; } = new List<ScratchpadDocument>();

        private FileSystemWatcher watcher = new FileSystemWatcher();

        public void Load(string path)
        {
            TrackedScratches.Add(new ScratchpadDocument
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

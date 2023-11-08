#pragma warning disable

// ReSharper disable all

using System;

namespace Dalamud.CorePlugin.MyFonts;
#if !STBSHARP_INTERNAL
public
#else
	internal
#endif
    static unsafe partial class StbRectPack
{
    public struct stbrp_context: IDisposable
    {
        public int width;
        public int height;
        public int align;
        public int init_mode;
        public int heuristic;
        public int num_nodes;
        public stbrp_node* active_head;
        public stbrp_node* free_head;
        public stbrp_node* extra;
        public stbrp_node* all_nodes;


        public stbrp_context(int nodesCount)
        {
            if (nodesCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodesCount));
            }

            this.width = this.height = this.align = this.init_mode = this.heuristic = this.num_nodes = 0;
            this.active_head = this.free_head = null;

            // Allocate nodes
            this.all_nodes = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * nodesCount);

            // Allocate extras
            this.extra = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * 2);
        }

        public void Dispose()
        {
            if (this.all_nodes != null)
            {
                CRuntime.free(this.all_nodes);
                this.all_nodes = null;
            }

            if (this.extra != null)
            {
                CRuntime.free(this.extra);
                this.extra = null;
            }
        }
    }
}

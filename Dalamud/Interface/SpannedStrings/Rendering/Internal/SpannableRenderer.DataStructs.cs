using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering.Internal;

/// <summary>A custom text renderer implementation.</summary>
internal sealed partial class SpannableRenderer
{
    private struct LinkRangeToRenderCoordinates
    {
        public int RecordIndex;
        public Vector2 LeftTop;
        public Vector2 RightBottom;
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct ItemStateStruct
    {
        [FieldOffset(0)]
        public int LinkRecordIndex;

        [FieldOffset(4)]
        public uint Flags;

        public bool IsMouseButtonDownHandled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (this.Flags & 1) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~1u) | (value ? 1u : 0u);
        }

        public ImGuiMouseButton FirstMouseButton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (ImGuiMouseButton)((this.Flags >> 1) & 3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~(3u << 1)) | ((uint)value << 1);
        }
    }

    private ref struct StateInfo
    {
        public float HorizontalOffsetWrtLine;
        public float VerticalOffsetWrtLine;

        private readonly float wrapWidth;
        private readonly ref RenderState state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateInfo(float wrapWidth, ref RenderState state)
        {
            this.wrapWidth = wrapWidth;
            this.state = ref state;
        }

        public void Update(in SpanStyleFontData fontInfo)
        {
            var lineAscentDescent = this.state.LastMeasurement.BBoxVertical;
            this.VerticalOffsetWrtLine = (fontInfo.BBoxVertical.Y - fontInfo.BBoxVertical.X) *
                                         this.state.LastStyle.VerticalOffset;
            switch (this.state.LastStyle.VerticalAlignment)
            {
                case VerticalAlignment.Baseline:
                    this.VerticalOffsetWrtLine -= lineAscentDescent.X + (fontInfo.Font.Ascent * fontInfo.Scale);
                    break;
                case VerticalAlignment.Middle:
                    this.VerticalOffsetWrtLine +=
                        (lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    this.VerticalOffsetWrtLine += lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize;
                    break;
                case VerticalAlignment.Top:
                default:
                    break;
            }

            this.VerticalOffsetWrtLine = MathF.Round(this.VerticalOffsetWrtLine);

            switch (this.state.LastStyle.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    this.HorizontalOffsetWrtLine = this.wrapWidth - this.state.LastMeasurement.Width;
                    break;

                case HorizontalAlignment.Center:
                    this.HorizontalOffsetWrtLine =
                        MathF.Round((this.wrapWidth - this.state.LastMeasurement.Width) / 2);
                    break;

                case HorizontalAlignment.Left:
                case var _ when this.wrapWidth is <= 0 or >= float.MaxValue or float.NaN:
                default:
                    this.HorizontalOffsetWrtLine = 0;
                    break;
            }
        }
    }
}

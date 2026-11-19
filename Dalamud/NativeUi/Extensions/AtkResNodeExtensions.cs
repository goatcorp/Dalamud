using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace Dalamud.NativeUi.Extensions;

/// <summary>
/// Extension methods for AtkResNode.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Stylecop doesn't understand Extension Blocks, you cant prefix with 'this'.")]
internal static unsafe class AtkResNodeExtensions
{
    extension(ref AtkResNode node) {
        /// <summary>
        /// Gets or sets the nodes position, if setting will update the nodes state.
        /// </summary>
        public Vector2 Position
        {
            get => new(node.X, node.Y);
            set => node.SetPositionFloat(value.X, value.Y);
        }

        /// <summary>
        /// Gets the nodes screen position.
        /// </summary>
        public Vector2 ScreenPosition
            => new(node.ScreenX, node.ScreenY);

        /// <summary>
        /// Gets or sets the nodes size, if setting will update the nodes state.
        /// </summary>
        public Vector2 Size
        {
            get => new(node.GetWidth(), node.GetHeight());
            set
            {
                node.SetWidth((ushort)value.X);
                node.SetHeight((ushort)value.Y);
            }
        }

        /// <summary>
        /// Gets the nodes <see cref="Bounds"/>.
        /// </summary>
        public Bounds Bounds => new()
        {
            TopLeft = node.Position,
            BottomRight = node.Position + node.Size,
        };

        /// <summary>
        /// Gets the nodes center position.
        /// </summary>
        public Vector2 Center
            => node.Position + (node.Size / 2.0f);

        /// <summary>
        /// Gets or sets the nodes scale, if setting will update the nodes state.
        /// </summary>
        public Vector2 Scale
        {
            get => new(node.GetScaleX(), node.GetScaleY());
            set => node.SetScale(value.X, value.Y);
        }

        /// <summary>
        /// Gets or sets the nodes rotation angle in degrees, if setting will update the nodes state.
        /// </summary>
        public float RotationDegrees
        {
            get => node.GetRotationDegrees();
            set => node.SetRotationDegrees(value - (int)(value / 360.0f) * 360.0f);
        }

        /// <summary>
        /// Gets or sets the nodes origin, if setting will update the nodes state.
        /// </summary>
        public Vector2 Origin
        {
            get => new(node.OriginX, node.OriginY);
            set => node.SetOrigin(value.X, value.Y);
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets if the node should be visible, if setting will update the nodes state.
        /// </summary>
        public bool Visible
        {
            get => node.IsVisible();
            set => node.ToggleVisibility(value);
        }

        /// <summary>
        /// Gets or sets the nodes Color field.
        /// </summary>
        public Vector4 ColorVector
        {
            get => node.Color.ToVector4();
            set => node.Color = value.ToByteColor();
        }

        /// <summary>
        /// Gets or sets the nodes color as HSVA.
        /// </summary>
        /// <remarks>
        /// This will be converted back and forth to RGBA color, there may be conversion error.
        /// </remarks>
        public ColorHelpers.HsvaColor ColorHsva
        {
            get => ColorHelpers.RgbaToHsv(node.ColorVector);
            set => node.Color = ColorHelpers.HsvToRgb(value).ToByteColor();
        }

        /// <summary>
        /// Gets or sets the nodes AddColor field.
        /// </summary>
        /// <remarks>
        /// Expects values between 0.0f and 1.0f.
        /// </remarks>
        public Vector3 AddColor
        {
            get => new Vector3(node.AddRed, node.AddGreen, node.AddBlue) / 255.0f;
            set
            {
                node.AddRed = (short)(value.X * 255);
                node.AddGreen = (short)(value.Y * 255);
                node.AddBlue = (short)(value.Z * 255);
            }
        }

        /// <summary>
        /// Gets or sets the AddColor as a HSVA value.
        /// </summary>
        /// <remarks>
        /// This will be converted back and forth to RGBA color, there may be conversion error.
        /// </remarks>
        public ColorHelpers.HsvaColor AddColorHsva
        {
            get => ColorHelpers.RgbaToHsv(node.AddColor.AsVector4());
            set => node.AddColor = ColorHelpers.HsvToRgb(value).AsVector3();
        }

        /// <summary>
        /// Gets or sets the nodes MultipleColorField.
        /// </summary>
        /// <remarks>
        /// Expects a value between 0.0f and 1.0f.
        /// </remarks>
        public Vector3 MultiplyColor
        {
            get => new Vector3(node.MultiplyRed, node.MultiplyGreen, node.MultiplyBlue) / 100.0f;
            set
            {
                node.MultiplyRed = (byte)(value.X * 100.0f);
                node.MultiplyGreen = (byte)(value.Y * 100.0f);
                node.MultiplyBlue = (byte)(value.Z * 100.0f);
            }
        }

        /// <summary>
        /// Gets or sets the nodes MultiplyColor as a HSVA color.
        /// </summary>
        /// <remarks>
        /// This will be converted back and forth to RGBA color, there may be conversion error.
        /// </remarks>
        public ColorHelpers.HsvaColor MultiplyColorHsva
        {
            get => ColorHelpers.RgbaToHsv(node.MultiplyColor.AsVector4());
            set => node.MultiplyColor = ColorHelpers.HsvToRgb(value).AsVector3();
        }

        /// <summary>
        /// Adds the provided flags to the current node flags.
        /// </summary>
        public void AddNodeFlag(params NodeFlags[] flags)
        {
            foreach (var flag in flags)
            {
                node.NodeFlags |= flag;
            }
        }

        /// <summary>
        /// Removes the provided flags from the current node flags.
        /// </summary>
        public void RemoveNodeFlag(params NodeFlags[] flags)
        {
            foreach (var flag in flags)
            {
                node.NodeFlags &= ~flag;
            }
        }

        /// <summary>
        /// Adds the provided draw flags to the current draw flags.
        /// </summary>
        public void AddDrawFlag(params DrawFlags[] flags)
        {
            foreach (var flag in flags)
            {
                node.DrawFlags |= (uint)flag;
            }
        }

        /// <summary>
        /// Removes the provided draw flags from the current draw flags.
        /// </summary>
        public void RemoveDrawFlag(params DrawFlags[] flags)
        {
            foreach (var flag in flags)
            {
                node.DrawFlags &= (uint)flag;
            }
        }

        /// <summary>
        /// Check collision with this node using short-value coords.
        /// </summary>
        public bool CheckCollision(short x, short y, bool inclusive = true)
            => node.CheckCollisionAtCoords(x, y, inclusive);

        /// <summary>
        /// Check collision with this node using vector-valued coords.
        /// </summary>
        public bool CheckCollision(Vector2 position, bool inclusive = true)
            => node.CheckCollisionAtCoords((short)position.X, (short)position.Y, inclusive);

        /// <summary>
        /// Check collision with this node using coords read from a AtkEventData object.
        /// </summary>
        public bool CheckCollision(AtkEventData* eventData, bool inclusive = true)
            => node.CheckCollisionAtCoords(eventData->MouseData.PosX, eventData->MouseData.PosY, inclusive);

        /// <summary>
        /// Gets a value indicating whether gets if this node is actually visible by checking each parent node to see if they are all visible.
        /// </summary>
        public bool IsActuallyVisible
        {
            get
            {
                if (!node.Visible) return false;

                var targetNode = node.ParentNode;
                while (targetNode is not null)
                {
                    if (!targetNode->Visible) return false;
                    targetNode = targetNode->ParentNode;
                }

                return true;
            }
        }
    }
}

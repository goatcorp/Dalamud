// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HexaGen.Runtime;
using System.Numerics;

namespace Dalamud.Bindings.ImGui
{
	/// <summary>
	/// Helper: ImRect (2D axis aligned bounding-box)<br/>
	/// NB: we can't rely on ImVec2 math operators being available here!<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImRect
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 Min;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 Max;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImRect(Vector2 min = default, Vector2 max = default)
		{
			Min = min;
			Max = max;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImRect* @this = &this)
			{
				ImGui.DestroyNative(@this);
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImRectPtr : IEquatable<ImRectPtr>
	{
		public ImRectPtr(ImRect* handle) { Handle = handle; }

		public ImRect* Handle;

		public bool IsNull => Handle == null;

		public static ImRectPtr Null => new ImRectPtr(null);

		public ImRect this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImRectPtr(ImRect* handle) => new ImRectPtr(handle);

		public static implicit operator ImRect*(ImRectPtr handle) => handle.Handle;

		public static bool operator ==(ImRectPtr left, ImRectPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImRectPtr left, ImRectPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImRectPtr left, ImRect* right) => left.Handle == right;

		public static bool operator !=(ImRectPtr left, ImRect* right) => left.Handle != right;

		public bool Equals(ImRectPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImRectPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImRectPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 Min => ref Unsafe.AsRef<Vector2>(&Handle->Min);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 Max => ref Unsafe.AsRef<Vector2>(&Handle->Max);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

	}

}

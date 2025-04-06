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
using Dalamud.Bindings.ImGui;

namespace Dalamud.Bindings.ImPlot
{
	/// <summary>
	/// To be documented.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImPlotRect
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public ImPlotRange X;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImPlotRange Y;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImPlotRect(ImPlotRange x = default, ImPlotRange y = default)
		{
			X = x;
			Y = y;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool Contains(ImPlotPoint p)
		{
			fixed (ImPlotRect* @this = &this)
			{
				byte ret = ImPlot.ContainsNative(@this, p);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool Contains(double x, double y)
		{
			fixed (ImPlotRect* @this = &this)
			{
				byte ret = ImPlot.ContainsNative(@this, x, y);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImPlotRect* @this = &this)
			{
				ImPlot.DestroyNative(@this);
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImPlotRectPtr : IEquatable<ImPlotRectPtr>
	{
		public ImPlotRectPtr(ImPlotRect* handle) { Handle = handle; }

		public ImPlotRect* Handle;

		public bool IsNull => Handle == null;

		public static ImPlotRectPtr Null => new ImPlotRectPtr(null);

		public ImPlotRect this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImPlotRectPtr(ImPlotRect* handle) => new ImPlotRectPtr(handle);

		public static implicit operator ImPlotRect*(ImPlotRectPtr handle) => handle.Handle;

		public static bool operator ==(ImPlotRectPtr left, ImPlotRectPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImPlotRectPtr left, ImPlotRectPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImPlotRectPtr left, ImPlotRect* right) => left.Handle == right;

		public static bool operator !=(ImPlotRectPtr left, ImPlotRect* right) => left.Handle != right;

		public bool Equals(ImPlotRectPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImPlotRectPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImPlotRectPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImPlotRange X => ref Unsafe.AsRef<ImPlotRange>(&Handle->X);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImPlotRange Y => ref Unsafe.AsRef<ImPlotRange>(&Handle->Y);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool Contains(ImPlotPoint p)
		{
			byte ret = ImPlot.ContainsNative(Handle, p);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool Contains(double x, double y)
		{
			byte ret = ImPlot.ContainsNative(Handle, x, y);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImPlot.DestroyNative(Handle);
		}

	}

}

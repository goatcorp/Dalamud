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
	/// Temporary clipper data, buffers sharedreused between instances<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiListClipperData
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiListClipper* ListClipper;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float LossynessOffset;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int StepNo;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int ItemsFrozen;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImVector<ImGuiListClipperRange> Ranges;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiListClipperData(ImGuiListClipper* listClipper = default, float lossynessOffset = default, int stepNo = default, int itemsFrozen = default, ImVector<ImGuiListClipperRange> ranges = default)
		{
			ListClipper = listClipper;
			LossynessOffset = lossynessOffset;
			StepNo = stepNo;
			ItemsFrozen = itemsFrozen;
			Ranges = ranges;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiListClipperData* @this = &this)
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
	public unsafe struct ImGuiListClipperDataPtr : IEquatable<ImGuiListClipperDataPtr>
	{
		public ImGuiListClipperDataPtr(ImGuiListClipperData* handle) { Handle = handle; }

		public ImGuiListClipperData* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiListClipperDataPtr Null => new ImGuiListClipperDataPtr(null);

		public ImGuiListClipperData this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiListClipperDataPtr(ImGuiListClipperData* handle) => new ImGuiListClipperDataPtr(handle);

		public static implicit operator ImGuiListClipperData*(ImGuiListClipperDataPtr handle) => handle.Handle;

		public static bool operator ==(ImGuiListClipperDataPtr left, ImGuiListClipperDataPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiListClipperDataPtr left, ImGuiListClipperDataPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiListClipperDataPtr left, ImGuiListClipperData* right) => left.Handle == right;

		public static bool operator !=(ImGuiListClipperDataPtr left, ImGuiListClipperData* right) => left.Handle != right;

		public bool Equals(ImGuiListClipperDataPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiListClipperDataPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiListClipperDataPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImGuiListClipperPtr ListClipper => ref Unsafe.AsRef<ImGuiListClipperPtr>(&Handle->ListClipper);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float LossynessOffset => ref Unsafe.AsRef<float>(&Handle->LossynessOffset);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int StepNo => ref Unsafe.AsRef<int>(&Handle->StepNo);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int ItemsFrozen => ref Unsafe.AsRef<int>(&Handle->ItemsFrozen);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImVector<ImGuiListClipperRange> Ranges => ref Unsafe.AsRef<ImVector<ImGuiListClipperRange>>(&Handle->Ranges);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

	}

}

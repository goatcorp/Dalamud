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
	/// Internal state of the currently focusededited text input box<br/>
	/// For a given item ID, access with ImGui::GetInputTextState()<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiInputTextState
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public uint ID;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int CurLenW;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int CurLenA;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImVector<ushort> TextW;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImVector<byte> TextA;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImVector<byte> InitialTextA;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte TextAIsValid;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int BufCapacityA;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ScrollX;

		/// <summary>
		/// To be documented.
		/// </summary>
		public STBTexteditState Stb;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float CursorAnim;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte CursorFollow;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte SelectedAllMouseLock;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte Edited;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImGuiInputTextFlags Flags;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiInputTextState(uint id = default, int curLenW = default, int curLenA = default, ImVector<ushort> textW = default, ImVector<byte> textA = default, ImVector<byte> initialTextA = default, bool textAIsValid = default, int bufCapacityA = default, float scrollX = default, STBTexteditState stb = default, float cursorAnim = default, bool cursorFollow = default, bool selectedAllMouseLock = default, bool edited = default, ImGuiInputTextFlags flags = default)
		{
			ID = id;
			CurLenW = curLenW;
			CurLenA = curLenA;
			TextW = textW;
			TextA = textA;
			InitialTextA = initialTextA;
			TextAIsValid = textAIsValid ? (byte)1 : (byte)0;
			BufCapacityA = bufCapacityA;
			ScrollX = scrollX;
			Stb = stb;
			CursorAnim = cursorAnim;
			CursorFollow = cursorFollow ? (byte)1 : (byte)0;
			SelectedAllMouseLock = selectedAllMouseLock ? (byte)1 : (byte)0;
			Edited = edited ? (byte)1 : (byte)0;
			Flags = flags;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiInputTextState* @this = &this)
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
	public unsafe struct ImGuiInputTextStatePtr : IEquatable<ImGuiInputTextStatePtr>
	{
		public ImGuiInputTextStatePtr(ImGuiInputTextState* handle) { Handle = handle; }

		public ImGuiInputTextState* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiInputTextStatePtr Null => new ImGuiInputTextStatePtr(null);

		public ImGuiInputTextState this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiInputTextStatePtr(ImGuiInputTextState* handle) => new ImGuiInputTextStatePtr(handle);

		public static implicit operator ImGuiInputTextState*(ImGuiInputTextStatePtr handle) => handle.Handle;

		public static bool operator ==(ImGuiInputTextStatePtr left, ImGuiInputTextStatePtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiInputTextStatePtr left, ImGuiInputTextStatePtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiInputTextStatePtr left, ImGuiInputTextState* right) => left.Handle == right;

		public static bool operator !=(ImGuiInputTextStatePtr left, ImGuiInputTextState* right) => left.Handle != right;

		public bool Equals(ImGuiInputTextStatePtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiInputTextStatePtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiInputTextStatePtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint ID => ref Unsafe.AsRef<uint>(&Handle->ID);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int CurLenW => ref Unsafe.AsRef<int>(&Handle->CurLenW);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int CurLenA => ref Unsafe.AsRef<int>(&Handle->CurLenA);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImVector<ushort> TextW => ref Unsafe.AsRef<ImVector<ushort>>(&Handle->TextW);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImVector<byte> TextA => ref Unsafe.AsRef<ImVector<byte>>(&Handle->TextA);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImVector<byte> InitialTextA => ref Unsafe.AsRef<ImVector<byte>>(&Handle->InitialTextA);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool TextAIsValid => ref Unsafe.AsRef<bool>(&Handle->TextAIsValid);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int BufCapacityA => ref Unsafe.AsRef<int>(&Handle->BufCapacityA);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ScrollX => ref Unsafe.AsRef<float>(&Handle->ScrollX);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref STBTexteditState Stb => ref Unsafe.AsRef<STBTexteditState>(&Handle->Stb);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float CursorAnim => ref Unsafe.AsRef<float>(&Handle->CursorAnim);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool CursorFollow => ref Unsafe.AsRef<bool>(&Handle->CursorFollow);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool SelectedAllMouseLock => ref Unsafe.AsRef<bool>(&Handle->SelectedAllMouseLock);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool Edited => ref Unsafe.AsRef<bool>(&Handle->Edited);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImGuiInputTextFlags Flags => ref Unsafe.AsRef<ImGuiInputTextFlags>(&Handle->Flags);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

	}

}

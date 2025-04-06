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
	/// To be documented.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiStyle
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public float Alpha;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float DisabledAlpha;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 WindowPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float WindowRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float WindowBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 WindowMinSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 WindowTitleAlign;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImGuiDir WindowMenuButtonPosition;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ChildRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ChildBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float PopupRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float PopupBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 FramePadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float FrameRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float FrameBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 ItemSpacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 ItemInnerSpacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 CellPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 TouchExtraPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float IndentSpacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ColumnsMinSpacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ScrollbarSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ScrollbarRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float GrabMinSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float GrabRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float LogSliderDeadzone;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float TabRounding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float TabBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float TabMinWidthForCloseButton;

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImGuiDir ColorButtonPosition;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 ButtonTextAlign;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 SelectableTextAlign;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 DisplayWindowPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 DisplaySafeAreaPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float MouseCursorScale;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte AntiAliasedLines;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte AntiAliasedLinesUseTex;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte AntiAliasedFill;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float CurveTessellationTol;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float CircleTessellationMaxError;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector4 Colors_0;
		public Vector4 Colors_1;
		public Vector4 Colors_2;
		public Vector4 Colors_3;
		public Vector4 Colors_4;
		public Vector4 Colors_5;
		public Vector4 Colors_6;
		public Vector4 Colors_7;
		public Vector4 Colors_8;
		public Vector4 Colors_9;
		public Vector4 Colors_10;
		public Vector4 Colors_11;
		public Vector4 Colors_12;
		public Vector4 Colors_13;
		public Vector4 Colors_14;
		public Vector4 Colors_15;
		public Vector4 Colors_16;
		public Vector4 Colors_17;
		public Vector4 Colors_18;
		public Vector4 Colors_19;
		public Vector4 Colors_20;
		public Vector4 Colors_21;
		public Vector4 Colors_22;
		public Vector4 Colors_23;
		public Vector4 Colors_24;
		public Vector4 Colors_25;
		public Vector4 Colors_26;
		public Vector4 Colors_27;
		public Vector4 Colors_28;
		public Vector4 Colors_29;
		public Vector4 Colors_30;
		public Vector4 Colors_31;
		public Vector4 Colors_32;
		public Vector4 Colors_33;
		public Vector4 Colors_34;
		public Vector4 Colors_35;
		public Vector4 Colors_36;
		public Vector4 Colors_37;
		public Vector4 Colors_38;
		public Vector4 Colors_39;
		public Vector4 Colors_40;
		public Vector4 Colors_41;
		public Vector4 Colors_42;
		public Vector4 Colors_43;
		public Vector4 Colors_44;
		public Vector4 Colors_45;
		public Vector4 Colors_46;
		public Vector4 Colors_47;
		public Vector4 Colors_48;
		public Vector4 Colors_49;
		public Vector4 Colors_50;
		public Vector4 Colors_51;
		public Vector4 Colors_52;
		public Vector4 Colors_53;
		public Vector4 Colors_54;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiStyle(float alpha = default, float disabledAlpha = default, Vector2 windowPadding = default, float windowRounding = default, float windowBorderSize = default, Vector2 windowMinSize = default, Vector2 windowTitleAlign = default, ImGuiDir windowMenuButtonPosition = default, float childRounding = default, float childBorderSize = default, float popupRounding = default, float popupBorderSize = default, Vector2 framePadding = default, float frameRounding = default, float frameBorderSize = default, Vector2 itemSpacing = default, Vector2 itemInnerSpacing = default, Vector2 cellPadding = default, Vector2 touchExtraPadding = default, float indentSpacing = default, float columnsMinSpacing = default, float scrollbarSize = default, float scrollbarRounding = default, float grabMinSize = default, float grabRounding = default, float logSliderDeadzone = default, float tabRounding = default, float tabBorderSize = default, float tabMinWidthForCloseButton = default, ImGuiDir colorButtonPosition = default, Vector2 buttonTextAlign = default, Vector2 selectableTextAlign = default, Vector2 displayWindowPadding = default, Vector2 displaySafeAreaPadding = default, float mouseCursorScale = default, bool antiAliasedLines = default, bool antiAliasedLinesUseTex = default, bool antiAliasedFill = default, float curveTessellationTol = default, float circleTessellationMaxError = default, Vector4* colors = default)
		{
			Alpha = alpha;
			DisabledAlpha = disabledAlpha;
			WindowPadding = windowPadding;
			WindowRounding = windowRounding;
			WindowBorderSize = windowBorderSize;
			WindowMinSize = windowMinSize;
			WindowTitleAlign = windowTitleAlign;
			WindowMenuButtonPosition = windowMenuButtonPosition;
			ChildRounding = childRounding;
			ChildBorderSize = childBorderSize;
			PopupRounding = popupRounding;
			PopupBorderSize = popupBorderSize;
			FramePadding = framePadding;
			FrameRounding = frameRounding;
			FrameBorderSize = frameBorderSize;
			ItemSpacing = itemSpacing;
			ItemInnerSpacing = itemInnerSpacing;
			CellPadding = cellPadding;
			TouchExtraPadding = touchExtraPadding;
			IndentSpacing = indentSpacing;
			ColumnsMinSpacing = columnsMinSpacing;
			ScrollbarSize = scrollbarSize;
			ScrollbarRounding = scrollbarRounding;
			GrabMinSize = grabMinSize;
			GrabRounding = grabRounding;
			LogSliderDeadzone = logSliderDeadzone;
			TabRounding = tabRounding;
			TabBorderSize = tabBorderSize;
			TabMinWidthForCloseButton = tabMinWidthForCloseButton;
			ColorButtonPosition = colorButtonPosition;
			ButtonTextAlign = buttonTextAlign;
			SelectableTextAlign = selectableTextAlign;
			DisplayWindowPadding = displayWindowPadding;
			DisplaySafeAreaPadding = displaySafeAreaPadding;
			MouseCursorScale = mouseCursorScale;
			AntiAliasedLines = antiAliasedLines ? (byte)1 : (byte)0;
			AntiAliasedLinesUseTex = antiAliasedLinesUseTex ? (byte)1 : (byte)0;
			AntiAliasedFill = antiAliasedFill ? (byte)1 : (byte)0;
			CurveTessellationTol = curveTessellationTol;
			CircleTessellationMaxError = circleTessellationMaxError;
			if (colors != default(Vector4*))
			{
				Colors_0 = colors[0];
				Colors_1 = colors[1];
				Colors_2 = colors[2];
				Colors_3 = colors[3];
				Colors_4 = colors[4];
				Colors_5 = colors[5];
				Colors_6 = colors[6];
				Colors_7 = colors[7];
				Colors_8 = colors[8];
				Colors_9 = colors[9];
				Colors_10 = colors[10];
				Colors_11 = colors[11];
				Colors_12 = colors[12];
				Colors_13 = colors[13];
				Colors_14 = colors[14];
				Colors_15 = colors[15];
				Colors_16 = colors[16];
				Colors_17 = colors[17];
				Colors_18 = colors[18];
				Colors_19 = colors[19];
				Colors_20 = colors[20];
				Colors_21 = colors[21];
				Colors_22 = colors[22];
				Colors_23 = colors[23];
				Colors_24 = colors[24];
				Colors_25 = colors[25];
				Colors_26 = colors[26];
				Colors_27 = colors[27];
				Colors_28 = colors[28];
				Colors_29 = colors[29];
				Colors_30 = colors[30];
				Colors_31 = colors[31];
				Colors_32 = colors[32];
				Colors_33 = colors[33];
				Colors_34 = colors[34];
				Colors_35 = colors[35];
				Colors_36 = colors[36];
				Colors_37 = colors[37];
				Colors_38 = colors[38];
				Colors_39 = colors[39];
				Colors_40 = colors[40];
				Colors_41 = colors[41];
				Colors_42 = colors[42];
				Colors_43 = colors[43];
				Colors_44 = colors[44];
				Colors_45 = colors[45];
				Colors_46 = colors[46];
				Colors_47 = colors[47];
				Colors_48 = colors[48];
				Colors_49 = colors[49];
				Colors_50 = colors[50];
				Colors_51 = colors[51];
				Colors_52 = colors[52];
				Colors_53 = colors[53];
				Colors_54 = colors[54];
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiStyle(float alpha = default, float disabledAlpha = default, Vector2 windowPadding = default, float windowRounding = default, float windowBorderSize = default, Vector2 windowMinSize = default, Vector2 windowTitleAlign = default, ImGuiDir windowMenuButtonPosition = default, float childRounding = default, float childBorderSize = default, float popupRounding = default, float popupBorderSize = default, Vector2 framePadding = default, float frameRounding = default, float frameBorderSize = default, Vector2 itemSpacing = default, Vector2 itemInnerSpacing = default, Vector2 cellPadding = default, Vector2 touchExtraPadding = default, float indentSpacing = default, float columnsMinSpacing = default, float scrollbarSize = default, float scrollbarRounding = default, float grabMinSize = default, float grabRounding = default, float logSliderDeadzone = default, float tabRounding = default, float tabBorderSize = default, float tabMinWidthForCloseButton = default, ImGuiDir colorButtonPosition = default, Vector2 buttonTextAlign = default, Vector2 selectableTextAlign = default, Vector2 displayWindowPadding = default, Vector2 displaySafeAreaPadding = default, float mouseCursorScale = default, bool antiAliasedLines = default, bool antiAliasedLinesUseTex = default, bool antiAliasedFill = default, float curveTessellationTol = default, float circleTessellationMaxError = default, Span<Vector4> colors = default)
		{
			Alpha = alpha;
			DisabledAlpha = disabledAlpha;
			WindowPadding = windowPadding;
			WindowRounding = windowRounding;
			WindowBorderSize = windowBorderSize;
			WindowMinSize = windowMinSize;
			WindowTitleAlign = windowTitleAlign;
			WindowMenuButtonPosition = windowMenuButtonPosition;
			ChildRounding = childRounding;
			ChildBorderSize = childBorderSize;
			PopupRounding = popupRounding;
			PopupBorderSize = popupBorderSize;
			FramePadding = framePadding;
			FrameRounding = frameRounding;
			FrameBorderSize = frameBorderSize;
			ItemSpacing = itemSpacing;
			ItemInnerSpacing = itemInnerSpacing;
			CellPadding = cellPadding;
			TouchExtraPadding = touchExtraPadding;
			IndentSpacing = indentSpacing;
			ColumnsMinSpacing = columnsMinSpacing;
			ScrollbarSize = scrollbarSize;
			ScrollbarRounding = scrollbarRounding;
			GrabMinSize = grabMinSize;
			GrabRounding = grabRounding;
			LogSliderDeadzone = logSliderDeadzone;
			TabRounding = tabRounding;
			TabBorderSize = tabBorderSize;
			TabMinWidthForCloseButton = tabMinWidthForCloseButton;
			ColorButtonPosition = colorButtonPosition;
			ButtonTextAlign = buttonTextAlign;
			SelectableTextAlign = selectableTextAlign;
			DisplayWindowPadding = displayWindowPadding;
			DisplaySafeAreaPadding = displaySafeAreaPadding;
			MouseCursorScale = mouseCursorScale;
			AntiAliasedLines = antiAliasedLines ? (byte)1 : (byte)0;
			AntiAliasedLinesUseTex = antiAliasedLinesUseTex ? (byte)1 : (byte)0;
			AntiAliasedFill = antiAliasedFill ? (byte)1 : (byte)0;
			CurveTessellationTol = curveTessellationTol;
			CircleTessellationMaxError = circleTessellationMaxError;
			if (colors != default(Span<Vector4>))
			{
				Colors_0 = colors[0];
				Colors_1 = colors[1];
				Colors_2 = colors[2];
				Colors_3 = colors[3];
				Colors_4 = colors[4];
				Colors_5 = colors[5];
				Colors_6 = colors[6];
				Colors_7 = colors[7];
				Colors_8 = colors[8];
				Colors_9 = colors[9];
				Colors_10 = colors[10];
				Colors_11 = colors[11];
				Colors_12 = colors[12];
				Colors_13 = colors[13];
				Colors_14 = colors[14];
				Colors_15 = colors[15];
				Colors_16 = colors[16];
				Colors_17 = colors[17];
				Colors_18 = colors[18];
				Colors_19 = colors[19];
				Colors_20 = colors[20];
				Colors_21 = colors[21];
				Colors_22 = colors[22];
				Colors_23 = colors[23];
				Colors_24 = colors[24];
				Colors_25 = colors[25];
				Colors_26 = colors[26];
				Colors_27 = colors[27];
				Colors_28 = colors[28];
				Colors_29 = colors[29];
				Colors_30 = colors[30];
				Colors_31 = colors[31];
				Colors_32 = colors[32];
				Colors_33 = colors[33];
				Colors_34 = colors[34];
				Colors_35 = colors[35];
				Colors_36 = colors[36];
				Colors_37 = colors[37];
				Colors_38 = colors[38];
				Colors_39 = colors[39];
				Colors_40 = colors[40];
				Colors_41 = colors[41];
				Colors_42 = colors[42];
				Colors_43 = colors[43];
				Colors_44 = colors[44];
				Colors_45 = colors[45];
				Colors_46 = colors[46];
				Colors_47 = colors[47];
				Colors_48 = colors[48];
				Colors_49 = colors[49];
				Colors_50 = colors[50];
				Colors_51 = colors[51];
				Colors_52 = colors[52];
				Colors_53 = colors[53];
				Colors_54 = colors[54];
			}
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe Span<Vector4> Colors
		
		{
			get
			{
				fixed (Vector4* p = &this.Colors_0)
				{
					return new Span<Vector4>(p, 55);
				}
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiStyle* @this = &this)
			{
				ImGui.DestroyNative(@this);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void ScaleAllSizes(float scaleFactor)
		{
			fixed (ImGuiStyle* @this = &this)
			{
				ImGui.ScaleAllSizesNative(@this, scaleFactor);
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImGuiStylePtr : IEquatable<ImGuiStylePtr>
	{
		public ImGuiStylePtr(ImGuiStyle* handle) { Handle = handle; }

		public ImGuiStyle* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiStylePtr Null => new ImGuiStylePtr(null);

		public ImGuiStyle this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiStylePtr(ImGuiStyle* handle) => new ImGuiStylePtr(handle);

		public static implicit operator ImGuiStyle*(ImGuiStylePtr handle) => handle.Handle;

		public static bool operator ==(ImGuiStylePtr left, ImGuiStylePtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiStylePtr left, ImGuiStylePtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiStylePtr left, ImGuiStyle* right) => left.Handle == right;

		public static bool operator !=(ImGuiStylePtr left, ImGuiStyle* right) => left.Handle != right;

		public bool Equals(ImGuiStylePtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiStylePtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiStylePtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float Alpha => ref Unsafe.AsRef<float>(&Handle->Alpha);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float DisabledAlpha => ref Unsafe.AsRef<float>(&Handle->DisabledAlpha);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 WindowPadding => ref Unsafe.AsRef<Vector2>(&Handle->WindowPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float WindowRounding => ref Unsafe.AsRef<float>(&Handle->WindowRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float WindowBorderSize => ref Unsafe.AsRef<float>(&Handle->WindowBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 WindowMinSize => ref Unsafe.AsRef<Vector2>(&Handle->WindowMinSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 WindowTitleAlign => ref Unsafe.AsRef<Vector2>(&Handle->WindowTitleAlign);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImGuiDir WindowMenuButtonPosition => ref Unsafe.AsRef<ImGuiDir>(&Handle->WindowMenuButtonPosition);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ChildRounding => ref Unsafe.AsRef<float>(&Handle->ChildRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ChildBorderSize => ref Unsafe.AsRef<float>(&Handle->ChildBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float PopupRounding => ref Unsafe.AsRef<float>(&Handle->PopupRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float PopupBorderSize => ref Unsafe.AsRef<float>(&Handle->PopupBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 FramePadding => ref Unsafe.AsRef<Vector2>(&Handle->FramePadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float FrameRounding => ref Unsafe.AsRef<float>(&Handle->FrameRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float FrameBorderSize => ref Unsafe.AsRef<float>(&Handle->FrameBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 ItemSpacing => ref Unsafe.AsRef<Vector2>(&Handle->ItemSpacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 ItemInnerSpacing => ref Unsafe.AsRef<Vector2>(&Handle->ItemInnerSpacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 CellPadding => ref Unsafe.AsRef<Vector2>(&Handle->CellPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 TouchExtraPadding => ref Unsafe.AsRef<Vector2>(&Handle->TouchExtraPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float IndentSpacing => ref Unsafe.AsRef<float>(&Handle->IndentSpacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ColumnsMinSpacing => ref Unsafe.AsRef<float>(&Handle->ColumnsMinSpacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ScrollbarSize => ref Unsafe.AsRef<float>(&Handle->ScrollbarSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ScrollbarRounding => ref Unsafe.AsRef<float>(&Handle->ScrollbarRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float GrabMinSize => ref Unsafe.AsRef<float>(&Handle->GrabMinSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float GrabRounding => ref Unsafe.AsRef<float>(&Handle->GrabRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float LogSliderDeadzone => ref Unsafe.AsRef<float>(&Handle->LogSliderDeadzone);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float TabRounding => ref Unsafe.AsRef<float>(&Handle->TabRounding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float TabBorderSize => ref Unsafe.AsRef<float>(&Handle->TabBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float TabMinWidthForCloseButton => ref Unsafe.AsRef<float>(&Handle->TabMinWidthForCloseButton);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImGuiDir ColorButtonPosition => ref Unsafe.AsRef<ImGuiDir>(&Handle->ColorButtonPosition);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 ButtonTextAlign => ref Unsafe.AsRef<Vector2>(&Handle->ButtonTextAlign);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 SelectableTextAlign => ref Unsafe.AsRef<Vector2>(&Handle->SelectableTextAlign);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 DisplayWindowPadding => ref Unsafe.AsRef<Vector2>(&Handle->DisplayWindowPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 DisplaySafeAreaPadding => ref Unsafe.AsRef<Vector2>(&Handle->DisplaySafeAreaPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float MouseCursorScale => ref Unsafe.AsRef<float>(&Handle->MouseCursorScale);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool AntiAliasedLines => ref Unsafe.AsRef<bool>(&Handle->AntiAliasedLines);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool AntiAliasedLinesUseTex => ref Unsafe.AsRef<bool>(&Handle->AntiAliasedLinesUseTex);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool AntiAliasedFill => ref Unsafe.AsRef<bool>(&Handle->AntiAliasedFill);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float CurveTessellationTol => ref Unsafe.AsRef<float>(&Handle->CurveTessellationTol);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float CircleTessellationMaxError => ref Unsafe.AsRef<float>(&Handle->CircleTessellationMaxError);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe Span<Vector4> Colors
		
		{
			get
			{
				return new Span<Vector4>(&Handle->Colors_0, 55);
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImGui.DestroyNative(Handle);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void ScaleAllSizes(float scaleFactor)
		{
			ImGui.ScaleAllSizesNative(Handle, scaleFactor);
		}

	}

}

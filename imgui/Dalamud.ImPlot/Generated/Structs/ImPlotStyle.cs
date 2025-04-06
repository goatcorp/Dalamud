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
	public partial struct ImPlotStyle
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public float LineWeight;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int Marker;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float MarkerSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float MarkerWeight;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float FillAlpha;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ErrorBarSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float ErrorBarWeight;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float DigitalBitHeight;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float DigitalBitGap;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float PlotBorderSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public float MinorAlpha;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MajorTickLen;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MinorTickLen;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MajorTickSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MinorTickSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MajorGridSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MinorGridSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 PlotPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 LabelPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 LegendPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 LegendInnerPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 LegendSpacing;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 MousePosPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 AnnotationPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 FitPadding;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 PlotDefaultSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public Vector2 PlotMinSize;

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

		/// <summary>
		/// To be documented.
		/// </summary>
		public ImPlotColormap Colormap;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte UseLocalTime;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte UseISO8601;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte Use24HourClock;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImPlotStyle(float lineWeight = default, int marker = default, float markerSize = default, float markerWeight = default, float fillAlpha = default, float errorBarSize = default, float errorBarWeight = default, float digitalBitHeight = default, float digitalBitGap = default, float plotBorderSize = default, float minorAlpha = default, Vector2 majorTickLen = default, Vector2 minorTickLen = default, Vector2 majorTickSize = default, Vector2 minorTickSize = default, Vector2 majorGridSize = default, Vector2 minorGridSize = default, Vector2 plotPadding = default, Vector2 labelPadding = default, Vector2 legendPadding = default, Vector2 legendInnerPadding = default, Vector2 legendSpacing = default, Vector2 mousePosPadding = default, Vector2 annotationPadding = default, Vector2 fitPadding = default, Vector2 plotDefaultSize = default, Vector2 plotMinSize = default, Vector4* colors = default, ImPlotColormap colormap = default, bool useLocalTime = default, bool useIso8601 = default, bool use24HourClock = default)
		{
			LineWeight = lineWeight;
			Marker = marker;
			MarkerSize = markerSize;
			MarkerWeight = markerWeight;
			FillAlpha = fillAlpha;
			ErrorBarSize = errorBarSize;
			ErrorBarWeight = errorBarWeight;
			DigitalBitHeight = digitalBitHeight;
			DigitalBitGap = digitalBitGap;
			PlotBorderSize = plotBorderSize;
			MinorAlpha = minorAlpha;
			MajorTickLen = majorTickLen;
			MinorTickLen = minorTickLen;
			MajorTickSize = majorTickSize;
			MinorTickSize = minorTickSize;
			MajorGridSize = majorGridSize;
			MinorGridSize = minorGridSize;
			PlotPadding = plotPadding;
			LabelPadding = labelPadding;
			LegendPadding = legendPadding;
			LegendInnerPadding = legendInnerPadding;
			LegendSpacing = legendSpacing;
			MousePosPadding = mousePosPadding;
			AnnotationPadding = annotationPadding;
			FitPadding = fitPadding;
			PlotDefaultSize = plotDefaultSize;
			PlotMinSize = plotMinSize;
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
			}
			Colormap = colormap;
			UseLocalTime = useLocalTime ? (byte)1 : (byte)0;
			UseISO8601 = useIso8601 ? (byte)1 : (byte)0;
			Use24HourClock = use24HourClock ? (byte)1 : (byte)0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImPlotStyle(float lineWeight = default, int marker = default, float markerSize = default, float markerWeight = default, float fillAlpha = default, float errorBarSize = default, float errorBarWeight = default, float digitalBitHeight = default, float digitalBitGap = default, float plotBorderSize = default, float minorAlpha = default, Vector2 majorTickLen = default, Vector2 minorTickLen = default, Vector2 majorTickSize = default, Vector2 minorTickSize = default, Vector2 majorGridSize = default, Vector2 minorGridSize = default, Vector2 plotPadding = default, Vector2 labelPadding = default, Vector2 legendPadding = default, Vector2 legendInnerPadding = default, Vector2 legendSpacing = default, Vector2 mousePosPadding = default, Vector2 annotationPadding = default, Vector2 fitPadding = default, Vector2 plotDefaultSize = default, Vector2 plotMinSize = default, Span<Vector4> colors = default, ImPlotColormap colormap = default, bool useLocalTime = default, bool useIso8601 = default, bool use24HourClock = default)
		{
			LineWeight = lineWeight;
			Marker = marker;
			MarkerSize = markerSize;
			MarkerWeight = markerWeight;
			FillAlpha = fillAlpha;
			ErrorBarSize = errorBarSize;
			ErrorBarWeight = errorBarWeight;
			DigitalBitHeight = digitalBitHeight;
			DigitalBitGap = digitalBitGap;
			PlotBorderSize = plotBorderSize;
			MinorAlpha = minorAlpha;
			MajorTickLen = majorTickLen;
			MinorTickLen = minorTickLen;
			MajorTickSize = majorTickSize;
			MinorTickSize = minorTickSize;
			MajorGridSize = majorGridSize;
			MinorGridSize = minorGridSize;
			PlotPadding = plotPadding;
			LabelPadding = labelPadding;
			LegendPadding = legendPadding;
			LegendInnerPadding = legendInnerPadding;
			LegendSpacing = legendSpacing;
			MousePosPadding = mousePosPadding;
			AnnotationPadding = annotationPadding;
			FitPadding = fitPadding;
			PlotDefaultSize = plotDefaultSize;
			PlotMinSize = plotMinSize;
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
			}
			Colormap = colormap;
			UseLocalTime = useLocalTime ? (byte)1 : (byte)0;
			UseISO8601 = useIso8601 ? (byte)1 : (byte)0;
			Use24HourClock = use24HourClock ? (byte)1 : (byte)0;
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
					return new Span<Vector4>(p, 21);
				}
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImPlotStyle* @this = &this)
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
	public unsafe struct ImPlotStylePtr : IEquatable<ImPlotStylePtr>
	{
		public ImPlotStylePtr(ImPlotStyle* handle) { Handle = handle; }

		public ImPlotStyle* Handle;

		public bool IsNull => Handle == null;

		public static ImPlotStylePtr Null => new ImPlotStylePtr(null);

		public ImPlotStyle this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImPlotStylePtr(ImPlotStyle* handle) => new ImPlotStylePtr(handle);

		public static implicit operator ImPlotStyle*(ImPlotStylePtr handle) => handle.Handle;

		public static bool operator ==(ImPlotStylePtr left, ImPlotStylePtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImPlotStylePtr left, ImPlotStylePtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImPlotStylePtr left, ImPlotStyle* right) => left.Handle == right;

		public static bool operator !=(ImPlotStylePtr left, ImPlotStyle* right) => left.Handle != right;

		public bool Equals(ImPlotStylePtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImPlotStylePtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImPlotStylePtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float LineWeight => ref Unsafe.AsRef<float>(&Handle->LineWeight);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int Marker => ref Unsafe.AsRef<int>(&Handle->Marker);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float MarkerSize => ref Unsafe.AsRef<float>(&Handle->MarkerSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float MarkerWeight => ref Unsafe.AsRef<float>(&Handle->MarkerWeight);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float FillAlpha => ref Unsafe.AsRef<float>(&Handle->FillAlpha);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ErrorBarSize => ref Unsafe.AsRef<float>(&Handle->ErrorBarSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float ErrorBarWeight => ref Unsafe.AsRef<float>(&Handle->ErrorBarWeight);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float DigitalBitHeight => ref Unsafe.AsRef<float>(&Handle->DigitalBitHeight);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float DigitalBitGap => ref Unsafe.AsRef<float>(&Handle->DigitalBitGap);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float PlotBorderSize => ref Unsafe.AsRef<float>(&Handle->PlotBorderSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref float MinorAlpha => ref Unsafe.AsRef<float>(&Handle->MinorAlpha);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MajorTickLen => ref Unsafe.AsRef<Vector2>(&Handle->MajorTickLen);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MinorTickLen => ref Unsafe.AsRef<Vector2>(&Handle->MinorTickLen);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MajorTickSize => ref Unsafe.AsRef<Vector2>(&Handle->MajorTickSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MinorTickSize => ref Unsafe.AsRef<Vector2>(&Handle->MinorTickSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MajorGridSize => ref Unsafe.AsRef<Vector2>(&Handle->MajorGridSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MinorGridSize => ref Unsafe.AsRef<Vector2>(&Handle->MinorGridSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 PlotPadding => ref Unsafe.AsRef<Vector2>(&Handle->PlotPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 LabelPadding => ref Unsafe.AsRef<Vector2>(&Handle->LabelPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 LegendPadding => ref Unsafe.AsRef<Vector2>(&Handle->LegendPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 LegendInnerPadding => ref Unsafe.AsRef<Vector2>(&Handle->LegendInnerPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 LegendSpacing => ref Unsafe.AsRef<Vector2>(&Handle->LegendSpacing);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 MousePosPadding => ref Unsafe.AsRef<Vector2>(&Handle->MousePosPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 AnnotationPadding => ref Unsafe.AsRef<Vector2>(&Handle->AnnotationPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 FitPadding => ref Unsafe.AsRef<Vector2>(&Handle->FitPadding);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 PlotDefaultSize => ref Unsafe.AsRef<Vector2>(&Handle->PlotDefaultSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref Vector2 PlotMinSize => ref Unsafe.AsRef<Vector2>(&Handle->PlotMinSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe Span<Vector4> Colors
		
		{
			get
			{
				return new Span<Vector4>(&Handle->Colors_0, 21);
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImPlotColormap Colormap => ref Unsafe.AsRef<ImPlotColormap>(&Handle->Colormap);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool UseLocalTime => ref Unsafe.AsRef<bool>(&Handle->UseLocalTime);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool UseISO8601 => ref Unsafe.AsRef<bool>(&Handle->UseISO8601);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool Use24HourClock => ref Unsafe.AsRef<bool>(&Handle->Use24HourClock);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			ImPlot.DestroyNative(Handle);
		}

	}

}

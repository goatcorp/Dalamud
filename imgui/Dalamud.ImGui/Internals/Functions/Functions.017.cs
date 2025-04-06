// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HexaGen.Runtime;
using System.Numerics;

namespace Dalamud.Bindings.ImGui
{
	public unsafe partial class ImGuiP
	{

		/// <summary>
		/// To be documented.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ImFontAtlasBuildRender8bppRectFromStringNative(ImFontAtlas* atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			#if NET5_0_OR_GREATER
			((delegate* unmanaged[Cdecl]<ImFontAtlas*, int, int, int, int, int, byte*, byte, byte, void>)funcTable[1261])(atlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
			#else
			((delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, nint, byte, byte, void>)funcTable[1261])((nint)atlas, textureIndex, x, y, w, h, (nint)inStr, inMarkerChar, inMarkerPixelValue);
			#endif
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			ImFontAtlasBuildRender8bppRectFromStringNative(atlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				ImFontAtlasBuildRender8bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ref byte inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (byte* pinStr = &inStr)
			{
				ImFontAtlasBuildRender8bppRectFromStringNative(atlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (byte* pinStr = inStr)
			{
				ImFontAtlasBuildRender8bppRectFromStringNative(atlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, string inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (inStr != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(inStr);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(inStr, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			ImFontAtlasBuildRender8bppRectFromStringNative(atlas, textureIndex, x, y, w, h, pStr0, inMarkerChar, inMarkerPixelValue);
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, ref byte inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				fixed (byte* pinStr = &inStr)
				{
					ImFontAtlasBuildRender8bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				fixed (byte* pinStr = inStr)
				{
					ImFontAtlasBuildRender8bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender8bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, string inStr, byte inMarkerChar, byte inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (inStr != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(inStr);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(inStr, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImFontAtlasBuildRender8bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, pStr0, inMarkerChar, inMarkerPixelValue);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ImFontAtlasBuildRender32bppRectFromStringNative(ImFontAtlas* atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			#if NET5_0_OR_GREATER
			((delegate* unmanaged[Cdecl]<ImFontAtlas*, int, int, int, int, int, byte*, byte, uint, void>)funcTable[1262])(atlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
			#else
			((delegate* unmanaged[Cdecl]<nint, int, int, int, int, int, nint, byte, uint, void>)funcTable[1262])((nint)atlas, textureIndex, x, y, w, h, (nint)inStr, inMarkerChar, inMarkerPixelValue);
			#endif
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			ImFontAtlasBuildRender32bppRectFromStringNative(atlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, byte* inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				ImFontAtlasBuildRender32bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, inStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ref byte inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (byte* pinStr = &inStr)
			{
				ImFontAtlasBuildRender32bppRectFromStringNative(atlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (byte* pinStr = inStr)
			{
				ImFontAtlasBuildRender32bppRectFromStringNative(atlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ImFontAtlasPtr atlas, int textureIndex, int x, int y, int w, int h, string inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (inStr != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(inStr);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(inStr, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			ImFontAtlasBuildRender32bppRectFromStringNative(atlas, textureIndex, x, y, w, h, pStr0, inMarkerChar, inMarkerPixelValue);
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, ref byte inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				fixed (byte* pinStr = &inStr)
				{
					ImFontAtlasBuildRender32bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, ReadOnlySpan<byte> inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				fixed (byte* pinStr = inStr)
				{
					ImFontAtlasBuildRender32bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, (byte*)pinStr, inMarkerChar, inMarkerPixelValue);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildRender32bppRectFromString(ref ImFontAtlas atlas, int textureIndex, int x, int y, int w, int h, string inStr, byte inMarkerChar, uint inMarkerPixelValue)
		{
			fixed (ImFontAtlas* patlas = &atlas)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (inStr != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(inStr);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(inStr, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImFontAtlasBuildRender32bppRectFromStringNative((ImFontAtlas*)patlas, textureIndex, x, y, w, h, pStr0, inMarkerChar, inMarkerPixelValue);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ImFontAtlasBuildMultiplyCalcLookupTableNative(byte* outTable, float inMultiplyFactor, float gammaFactor)
		{
			#if NET5_0_OR_GREATER
			((delegate* unmanaged[Cdecl]<byte*, float, float, void>)funcTable[1263])(outTable, inMultiplyFactor, gammaFactor);
			#else
			((delegate* unmanaged[Cdecl]<nint, float, float, void>)funcTable[1263])((nint)outTable, inMultiplyFactor, gammaFactor);
			#endif
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyCalcLookupTable(byte* outTable, float inMultiplyFactor, float gammaFactor)
		{
			ImFontAtlasBuildMultiplyCalcLookupTableNative(outTable, inMultiplyFactor, gammaFactor);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyCalcLookupTable(ref byte outTable, float inMultiplyFactor, float gammaFactor)
		{
			fixed (byte* poutTable = &outTable)
			{
				ImFontAtlasBuildMultiplyCalcLookupTableNative((byte*)poutTable, inMultiplyFactor, gammaFactor);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyCalcLookupTable(ReadOnlySpan<byte> outTable, float inMultiplyFactor, float gammaFactor)
		{
			fixed (byte* poutTable = outTable)
			{
				ImFontAtlasBuildMultiplyCalcLookupTableNative((byte*)poutTable, inMultiplyFactor, gammaFactor);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ImFontAtlasBuildMultiplyRectAlpha8Native(byte* table, byte* pixels, int x, int y, int w, int h, int stride)
		{
			#if NET5_0_OR_GREATER
			((delegate* unmanaged[Cdecl]<byte*, byte*, int, int, int, int, int, void>)funcTable[1264])(table, pixels, x, y, w, h, stride);
			#else
			((delegate* unmanaged[Cdecl]<nint, nint, int, int, int, int, int, void>)funcTable[1264])((nint)table, (nint)pixels, x, y, w, h, stride);
			#endif
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(byte* table, byte* pixels, int x, int y, int w, int h, int stride)
		{
			ImFontAtlasBuildMultiplyRectAlpha8Native(table, pixels, x, y, w, h, stride);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(ref byte table, byte* pixels, int x, int y, int w, int h, int stride)
		{
			fixed (byte* ptable = &table)
			{
				ImFontAtlasBuildMultiplyRectAlpha8Native((byte*)ptable, pixels, x, y, w, h, stride);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(ReadOnlySpan<byte> table, byte* pixels, int x, int y, int w, int h, int stride)
		{
			fixed (byte* ptable = table)
			{
				ImFontAtlasBuildMultiplyRectAlpha8Native((byte*)ptable, pixels, x, y, w, h, stride);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(byte* table, ref byte pixels, int x, int y, int w, int h, int stride)
		{
			fixed (byte* ppixels = &pixels)
			{
				ImFontAtlasBuildMultiplyRectAlpha8Native(table, (byte*)ppixels, x, y, w, h, stride);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(ref byte table, ref byte pixels, int x, int y, int w, int h, int stride)
		{
			fixed (byte* ptable = &table)
			{
				fixed (byte* ppixels = &pixels)
				{
					ImFontAtlasBuildMultiplyRectAlpha8Native((byte*)ptable, (byte*)ppixels, x, y, w, h, stride);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public static void ImFontAtlasBuildMultiplyRectAlpha8(ReadOnlySpan<byte> table, ref byte pixels, int x, int y, int w, int h, int stride)
		{
			fixed (byte* ptable = table)
			{
				fixed (byte* ppixels = &pixels)
				{
					ImFontAtlasBuildMultiplyRectAlpha8Native((byte*)ptable, (byte*)ppixels, x, y, w, h, stride);
				}
			}
		}

	}
}

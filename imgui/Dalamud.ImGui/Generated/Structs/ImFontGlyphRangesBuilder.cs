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
	/// Helper to build glyph ranges from textstring data. Feed your application stringscharacters to it then call BuildRanges().<br/>
	/// This is essentially a tightly packed of vector of 64k booleans = 8KB storage.<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImFontGlyphRangesBuilder
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public ImVector<uint> UsedChars;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImFontGlyphRangesBuilder(ImVector<uint> usedChars = default)
		{
			UsedChars = usedChars;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddChar(ushort c)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.AddCharNative(@this, c);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddRanges(ushort* ranges)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.AddRangesNative(@this, ranges);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddRanges(ref ushort ranges)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (ushort* pranges = &ranges)
				{
					ImGui.AddRangesNative(@this, (ushort*)pranges);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, byte* textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.AddTextNative(@this, text, textEnd);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.AddTextNative(@this, text, (byte*)(default));
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, byte* textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = &text)
				{
					ImGui.AddTextNative(@this, (byte*)ptext, textEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = &text)
				{
					ImGui.AddTextNative(@this, (byte*)ptext, (byte*)(default));
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, byte* textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = text)
				{
					ImGui.AddTextNative(@this, (byte*)ptext, textEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = text)
				{
					ImGui.AddTextNative(@this, (byte*)ptext, (byte*)(default));
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, byte* textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (text != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(text);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImGui.AddTextNative(@this, pStr0, textEnd);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (text != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(text);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImGui.AddTextNative(@this, pStr0, (byte*)(default));
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, ref byte textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptextEnd = &textEnd)
				{
					ImGui.AddTextNative(@this, text, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, ReadOnlySpan<byte> textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptextEnd = textEnd)
				{
					ImGui.AddTextNative(@this, text, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, string textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (textEnd != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(textEnd);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImGui.AddTextNative(@this, text, pStr0);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, ref byte textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = &text)
				{
					fixed (byte* ptextEnd = &textEnd)
					{
						ImGui.AddTextNative(@this, (byte*)ptext, (byte*)ptextEnd);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, ReadOnlySpan<byte> textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = text)
				{
					fixed (byte* ptextEnd = textEnd)
					{
						ImGui.AddTextNative(@this, (byte*)ptext, (byte*)ptextEnd);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, string textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (text != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(text);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				byte* pStr1 = null;
				int pStrSize1 = 0;
				if (textEnd != null)
				{
					pStrSize1 = Utils.GetByteCountUTF8(textEnd);
					if (pStrSize1 >= Utils.MaxStackallocSize)
					{
						pStr1 = Utils.Alloc<byte>(pStrSize1 + 1);
					}
					else
					{
						byte* pStrStack1 = stackalloc byte[pStrSize1 + 1];
						pStr1 = pStrStack1;
					}
					int pStrOffset1 = Utils.EncodeStringUTF8(textEnd, pStr1, pStrSize1);
					pStr1[pStrOffset1] = 0;
				}
				ImGui.AddTextNative(@this, pStr0, pStr1);
				if (pStrSize1 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr1);
				}
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, ReadOnlySpan<byte> textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = &text)
				{
					fixed (byte* ptextEnd = textEnd)
					{
						ImGui.AddTextNative(@this, (byte*)ptext, (byte*)ptextEnd);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, string textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = &text)
				{
					byte* pStr0 = null;
					int pStrSize0 = 0;
					if (textEnd != null)
					{
						pStrSize0 = Utils.GetByteCountUTF8(textEnd);
						if (pStrSize0 >= Utils.MaxStackallocSize)
						{
							pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
						}
						else
						{
							byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
							pStr0 = pStrStack0;
						}
						int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
						pStr0[pStrOffset0] = 0;
					}
					ImGui.AddTextNative(@this, (byte*)ptext, pStr0);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						Utils.Free(pStr0);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, ref byte textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = text)
				{
					fixed (byte* ptextEnd = &textEnd)
					{
						ImGui.AddTextNative(@this, (byte*)ptext, (byte*)ptextEnd);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, string textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (byte* ptext = text)
				{
					byte* pStr0 = null;
					int pStrSize0 = 0;
					if (textEnd != null)
					{
						pStrSize0 = Utils.GetByteCountUTF8(textEnd);
						if (pStrSize0 >= Utils.MaxStackallocSize)
						{
							pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
						}
						else
						{
							byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
							pStr0 = pStrStack0;
						}
						int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
						pStr0[pStrOffset0] = 0;
					}
					ImGui.AddTextNative(@this, (byte*)ptext, pStr0);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						Utils.Free(pStr0);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, ref byte textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (text != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(text);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				fixed (byte* ptextEnd = &textEnd)
				{
					ImGui.AddTextNative(@this, pStr0, (byte*)ptextEnd);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						Utils.Free(pStr0);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, ReadOnlySpan<byte> textEnd)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (text != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(text);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				fixed (byte* ptextEnd = textEnd)
				{
					ImGui.AddTextNative(@this, pStr0, (byte*)ptextEnd);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						Utils.Free(pStr0);
					}
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void BuildRanges(ImVector<ushort>* outRanges)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.BuildRangesNative(@this, outRanges);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void BuildRanges(ref ImVector<ushort> outRanges)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				fixed (ImVector<ushort>* poutRanges = &outRanges)
				{
					ImGui.BuildRangesNative(@this, (ImVector<ushort>*)poutRanges);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Clear()
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.ClearNative(@this);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.DestroyNative(@this);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool GetBit(ulong n)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte ret = ImGui.GetBitNative(@this, n);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool GetBit(nuint n)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				byte ret = ImGui.GetBitNative(@this, n);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void SetBit(ulong n)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.SetBitNative(@this, n);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void SetBit(nuint n)
		{
			fixed (ImFontGlyphRangesBuilder* @this = &this)
			{
				ImGui.SetBitNative(@this, n);
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImFontGlyphRangesBuilderPtr : IEquatable<ImFontGlyphRangesBuilderPtr>
	{
		public ImFontGlyphRangesBuilderPtr(ImFontGlyphRangesBuilder* handle) { Handle = handle; }

		public ImFontGlyphRangesBuilder* Handle;

		public bool IsNull => Handle == null;

		public static ImFontGlyphRangesBuilderPtr Null => new ImFontGlyphRangesBuilderPtr(null);

		public ImFontGlyphRangesBuilder this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImFontGlyphRangesBuilderPtr(ImFontGlyphRangesBuilder* handle) => new ImFontGlyphRangesBuilderPtr(handle);

		public static implicit operator ImFontGlyphRangesBuilder*(ImFontGlyphRangesBuilderPtr handle) => handle.Handle;

		public static bool operator ==(ImFontGlyphRangesBuilderPtr left, ImFontGlyphRangesBuilderPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImFontGlyphRangesBuilderPtr left, ImFontGlyphRangesBuilderPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImFontGlyphRangesBuilderPtr left, ImFontGlyphRangesBuilder* right) => left.Handle == right;

		public static bool operator !=(ImFontGlyphRangesBuilderPtr left, ImFontGlyphRangesBuilder* right) => left.Handle != right;

		public bool Equals(ImFontGlyphRangesBuilderPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImFontGlyphRangesBuilderPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImFontGlyphRangesBuilderPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImVector<uint> UsedChars => ref Unsafe.AsRef<ImVector<uint>>(&Handle->UsedChars);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddChar(ushort c)
		{
			ImGui.AddCharNative(Handle, c);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddRanges(ushort* ranges)
		{
			ImGui.AddRangesNative(Handle, ranges);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddRanges(ref ushort ranges)
		{
			fixed (ushort* pranges = &ranges)
			{
				ImGui.AddRangesNative(Handle, (ushort*)pranges);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, byte* textEnd)
		{
			ImGui.AddTextNative(Handle, text, textEnd);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text)
		{
			ImGui.AddTextNative(Handle, text, (byte*)(default));
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, byte* textEnd)
		{
			fixed (byte* ptext = &text)
			{
				ImGui.AddTextNative(Handle, (byte*)ptext, textEnd);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text)
		{
			fixed (byte* ptext = &text)
			{
				ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)(default));
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, byte* textEnd)
		{
			fixed (byte* ptext = text)
			{
				ImGui.AddTextNative(Handle, (byte*)ptext, textEnd);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text)
		{
			fixed (byte* ptext = text)
			{
				ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)(default));
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, byte* textEnd)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (text != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(text);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			ImGui.AddTextNative(Handle, pStr0, textEnd);
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (text != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(text);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			ImGui.AddTextNative(Handle, pStr0, (byte*)(default));
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, ref byte textEnd)
		{
			fixed (byte* ptextEnd = &textEnd)
			{
				ImGui.AddTextNative(Handle, text, (byte*)ptextEnd);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, ReadOnlySpan<byte> textEnd)
		{
			fixed (byte* ptextEnd = textEnd)
			{
				ImGui.AddTextNative(Handle, text, (byte*)ptextEnd);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(byte* text, string textEnd)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (textEnd != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(textEnd);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			ImGui.AddTextNative(Handle, text, pStr0);
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, ref byte textEnd)
		{
			fixed (byte* ptext = &text)
			{
				fixed (byte* ptextEnd = &textEnd)
				{
					ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, ReadOnlySpan<byte> textEnd)
		{
			fixed (byte* ptext = text)
			{
				fixed (byte* ptextEnd = textEnd)
				{
					ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, string textEnd)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (text != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(text);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			byte* pStr1 = null;
			int pStrSize1 = 0;
			if (textEnd != null)
			{
				pStrSize1 = Utils.GetByteCountUTF8(textEnd);
				if (pStrSize1 >= Utils.MaxStackallocSize)
				{
					pStr1 = Utils.Alloc<byte>(pStrSize1 + 1);
				}
				else
				{
					byte* pStrStack1 = stackalloc byte[pStrSize1 + 1];
					pStr1 = pStrStack1;
				}
				int pStrOffset1 = Utils.EncodeStringUTF8(textEnd, pStr1, pStrSize1);
				pStr1[pStrOffset1] = 0;
			}
			ImGui.AddTextNative(Handle, pStr0, pStr1);
			if (pStrSize1 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr1);
			}
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, ReadOnlySpan<byte> textEnd)
		{
			fixed (byte* ptext = &text)
			{
				fixed (byte* ptextEnd = textEnd)
				{
					ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ref byte text, string textEnd)
		{
			fixed (byte* ptext = &text)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (textEnd != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(textEnd);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImGui.AddTextNative(Handle, (byte*)ptext, pStr0);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, ref byte textEnd)
		{
			fixed (byte* ptext = text)
			{
				fixed (byte* ptextEnd = &textEnd)
				{
					ImGui.AddTextNative(Handle, (byte*)ptext, (byte*)ptextEnd);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(ReadOnlySpan<byte> text, string textEnd)
		{
			fixed (byte* ptext = text)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (textEnd != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(textEnd);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(textEnd, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				ImGui.AddTextNative(Handle, (byte*)ptext, pStr0);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, ref byte textEnd)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (text != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(text);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			fixed (byte* ptextEnd = &textEnd)
			{
				ImGui.AddTextNative(Handle, pStr0, (byte*)ptextEnd);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void AddText(string text, ReadOnlySpan<byte> textEnd)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (text != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(text);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(text, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			fixed (byte* ptextEnd = textEnd)
			{
				ImGui.AddTextNative(Handle, pStr0, (byte*)ptextEnd);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void BuildRanges(ImVector<ushort>* outRanges)
		{
			ImGui.BuildRangesNative(Handle, outRanges);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void BuildRanges(ref ImVector<ushort> outRanges)
		{
			fixed (ImVector<ushort>* poutRanges = &outRanges)
			{
				ImGui.BuildRangesNative(Handle, (ImVector<ushort>*)poutRanges);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Clear()
		{
			ImGui.ClearNative(Handle);
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
		public unsafe bool GetBit(ulong n)
		{
			byte ret = ImGui.GetBitNative(Handle, n);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool GetBit(nuint n)
		{
			byte ret = ImGui.GetBitNative(Handle, n);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void SetBit(ulong n)
		{
			ImGui.SetBitNative(Handle, n);
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void SetBit(nuint n)
		{
			ImGui.SetBitNative(Handle, n);
		}

	}

}

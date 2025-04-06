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
	public partial struct ImFontAtlasTexture
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public ImTextureID TexID;

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe byte* TexPixelsAlpha8;

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe uint* TexPixelsRGBA32;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImFontAtlasTexture(ImTextureID texId = default, byte* texPixelsAlpha8 = default, uint* texPixelsRgba32 = default)
		{
			TexID = texId;
			TexPixelsAlpha8 = texPixelsAlpha8;
			TexPixelsRGBA32 = texPixelsRgba32;
		}


	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImFontAtlasTexturePtr : IEquatable<ImFontAtlasTexturePtr>
	{
		public ImFontAtlasTexturePtr(ImFontAtlasTexture* handle) { Handle = handle; }

		public ImFontAtlasTexture* Handle;

		public bool IsNull => Handle == null;

		public static ImFontAtlasTexturePtr Null => new ImFontAtlasTexturePtr(null);

		public ImFontAtlasTexture this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImFontAtlasTexturePtr(ImFontAtlasTexture* handle) => new ImFontAtlasTexturePtr(handle);

		public static implicit operator ImFontAtlasTexture*(ImFontAtlasTexturePtr handle) => handle.Handle;

		public static bool operator ==(ImFontAtlasTexturePtr left, ImFontAtlasTexturePtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImFontAtlasTexturePtr left, ImFontAtlasTexturePtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImFontAtlasTexturePtr left, ImFontAtlasTexture* right) => left.Handle == right;

		public static bool operator !=(ImFontAtlasTexturePtr left, ImFontAtlasTexture* right) => left.Handle != right;

		public bool Equals(ImFontAtlasTexturePtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImFontAtlasTexturePtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImFontAtlasTexturePtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref ImTextureID TexID => ref Unsafe.AsRef<ImTextureID>(&Handle->TexID);
		/// <summary>
		/// To be documented.
		/// </summary>
		public byte* TexPixelsAlpha8 { get => Handle->TexPixelsAlpha8; set => Handle->TexPixelsAlpha8 = value; }
		/// <summary>
		/// To be documented.
		/// </summary>
		public uint* TexPixelsRGBA32 { get => Handle->TexPixelsRGBA32; set => Handle->TexPixelsRGBA32 = value; }
	}

}

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
	/// Data payload for Drag and Drop operations: AcceptDragDropPayload(), GetDragDropPayload()<br/>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public partial struct ImGuiPayload
	{
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void* Data;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int DataSize;

		/// <summary>
		/// To be documented.
		/// </summary>
		public uint SourceId;

		/// <summary>
		/// To be documented.
		/// </summary>
		public uint SourceParentId;

		/// <summary>
		/// To be documented.
		/// </summary>
		public int DataFrameCount;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte DataType_0;
		public byte DataType_1;
		public byte DataType_2;
		public byte DataType_3;
		public byte DataType_4;
		public byte DataType_5;
		public byte DataType_6;
		public byte DataType_7;
		public byte DataType_8;
		public byte DataType_9;
		public byte DataType_10;
		public byte DataType_11;
		public byte DataType_12;
		public byte DataType_13;
		public byte DataType_14;
		public byte DataType_15;
		public byte DataType_16;
		public byte DataType_17;
		public byte DataType_18;
		public byte DataType_19;
		public byte DataType_20;
		public byte DataType_21;
		public byte DataType_22;
		public byte DataType_23;
		public byte DataType_24;
		public byte DataType_25;
		public byte DataType_26;
		public byte DataType_27;
		public byte DataType_28;
		public byte DataType_29;
		public byte DataType_30;
		public byte DataType_31;
		public byte DataType_32;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte Preview;

		/// <summary>
		/// To be documented.
		/// </summary>
		public byte Delivery;


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiPayload(void* data = default, int dataSize = default, uint sourceId = default, uint sourceParentId = default, int dataFrameCount = default, byte* dataType = default, bool preview = default, bool delivery = default)
		{
			Data = data;
			DataSize = dataSize;
			SourceId = sourceId;
			SourceParentId = sourceParentId;
			DataFrameCount = dataFrameCount;
			if (dataType != default(byte*))
			{
				DataType_0 = dataType[0];
				DataType_1 = dataType[1];
				DataType_2 = dataType[2];
				DataType_3 = dataType[3];
				DataType_4 = dataType[4];
				DataType_5 = dataType[5];
				DataType_6 = dataType[6];
				DataType_7 = dataType[7];
				DataType_8 = dataType[8];
				DataType_9 = dataType[9];
				DataType_10 = dataType[10];
				DataType_11 = dataType[11];
				DataType_12 = dataType[12];
				DataType_13 = dataType[13];
				DataType_14 = dataType[14];
				DataType_15 = dataType[15];
				DataType_16 = dataType[16];
				DataType_17 = dataType[17];
				DataType_18 = dataType[18];
				DataType_19 = dataType[19];
				DataType_20 = dataType[20];
				DataType_21 = dataType[21];
				DataType_22 = dataType[22];
				DataType_23 = dataType[23];
				DataType_24 = dataType[24];
				DataType_25 = dataType[25];
				DataType_26 = dataType[26];
				DataType_27 = dataType[27];
				DataType_28 = dataType[28];
				DataType_29 = dataType[29];
				DataType_30 = dataType[30];
				DataType_31 = dataType[31];
				DataType_32 = dataType[32];
			}
			Preview = preview ? (byte)1 : (byte)0;
			Delivery = delivery ? (byte)1 : (byte)0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe ImGuiPayload(void* data = default, int dataSize = default, uint sourceId = default, uint sourceParentId = default, int dataFrameCount = default, Span<byte> dataType = default, bool preview = default, bool delivery = default)
		{
			Data = data;
			DataSize = dataSize;
			SourceId = sourceId;
			SourceParentId = sourceParentId;
			DataFrameCount = dataFrameCount;
			if (dataType != default(Span<byte>))
			{
				DataType_0 = dataType[0];
				DataType_1 = dataType[1];
				DataType_2 = dataType[2];
				DataType_3 = dataType[3];
				DataType_4 = dataType[4];
				DataType_5 = dataType[5];
				DataType_6 = dataType[6];
				DataType_7 = dataType[7];
				DataType_8 = dataType[8];
				DataType_9 = dataType[9];
				DataType_10 = dataType[10];
				DataType_11 = dataType[11];
				DataType_12 = dataType[12];
				DataType_13 = dataType[13];
				DataType_14 = dataType[14];
				DataType_15 = dataType[15];
				DataType_16 = dataType[16];
				DataType_17 = dataType[17];
				DataType_18 = dataType[18];
				DataType_19 = dataType[19];
				DataType_20 = dataType[20];
				DataType_21 = dataType[21];
				DataType_22 = dataType[22];
				DataType_23 = dataType[23];
				DataType_24 = dataType[24];
				DataType_25 = dataType[25];
				DataType_26 = dataType[26];
				DataType_27 = dataType[27];
				DataType_28 = dataType[28];
				DataType_29 = dataType[29];
				DataType_30 = dataType[30];
				DataType_31 = dataType[31];
				DataType_32 = dataType[32];
			}
			Preview = preview ? (byte)1 : (byte)0;
			Delivery = delivery ? (byte)1 : (byte)0;
		}


		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Clear()
		{
			fixed (ImGuiPayload* @this = &this)
			{
				ImGui.ClearNative(@this);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe void Destroy()
		{
			fixed (ImGuiPayload* @this = &this)
			{
				ImGui.DestroyNative(@this);
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(byte* type)
		{
			fixed (ImGuiPayload* @this = &this)
			{
				byte ret = ImGui.IsDataTypeNative(@this, type);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(ref byte type)
		{
			fixed (ImGuiPayload* @this = &this)
			{
				fixed (byte* ptype = &type)
				{
					byte ret = ImGui.IsDataTypeNative(@this, (byte*)ptype);
					return ret != 0;
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(ReadOnlySpan<byte> type)
		{
			fixed (ImGuiPayload* @this = &this)
			{
				fixed (byte* ptype = type)
				{
					byte ret = ImGui.IsDataTypeNative(@this, (byte*)ptype);
					return ret != 0;
				}
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(string type)
		{
			fixed (ImGuiPayload* @this = &this)
			{
				byte* pStr0 = null;
				int pStrSize0 = 0;
				if (type != null)
				{
					pStrSize0 = Utils.GetByteCountUTF8(type);
					if (pStrSize0 >= Utils.MaxStackallocSize)
					{
						pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
					}
					else
					{
						byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
						pStr0 = pStrStack0;
					}
					int pStrOffset0 = Utils.EncodeStringUTF8(type, pStr0, pStrSize0);
					pStr0[pStrOffset0] = 0;
				}
				byte ret = ImGui.IsDataTypeNative(@this, pStr0);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					Utils.Free(pStr0);
				}
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDelivery()
		{
			fixed (ImGuiPayload* @this = &this)
			{
				byte ret = ImGui.IsDeliveryNative(@this);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsPreview()
		{
			fixed (ImGuiPayload* @this = &this)
			{
				byte ret = ImGui.IsPreviewNative(@this);
				return ret != 0;
			}
		}

	}

	/// <summary>
	/// To be documented.
	/// </summary>
	#if NET5_0_OR_GREATER
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	#endif
	public unsafe struct ImGuiPayloadPtr : IEquatable<ImGuiPayloadPtr>
	{
		public ImGuiPayloadPtr(ImGuiPayload* handle) { Handle = handle; }

		public ImGuiPayload* Handle;

		public bool IsNull => Handle == null;

		public static ImGuiPayloadPtr Null => new ImGuiPayloadPtr(null);

		public ImGuiPayload this[int index] { get => Handle[index]; set => Handle[index] = value; }

		public static implicit operator ImGuiPayloadPtr(ImGuiPayload* handle) => new ImGuiPayloadPtr(handle);

		public static implicit operator ImGuiPayload*(ImGuiPayloadPtr handle) => handle.Handle;

		public static bool operator ==(ImGuiPayloadPtr left, ImGuiPayloadPtr right) => left.Handle == right.Handle;

		public static bool operator !=(ImGuiPayloadPtr left, ImGuiPayloadPtr right) => left.Handle != right.Handle;

		public static bool operator ==(ImGuiPayloadPtr left, ImGuiPayload* right) => left.Handle == right;

		public static bool operator !=(ImGuiPayloadPtr left, ImGuiPayload* right) => left.Handle != right;

		public bool Equals(ImGuiPayloadPtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is ImGuiPayloadPtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		#if NET5_0_OR_GREATER
		private string DebuggerDisplay => string.Format("ImGuiPayloadPtr [0x{0}]", ((nuint)Handle).ToString("X"));
		#endif
		/// <summary>
		/// To be documented.
		/// </summary>
		public void* Data { get => Handle->Data; set => Handle->Data = value; }
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int DataSize => ref Unsafe.AsRef<int>(&Handle->DataSize);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint SourceId => ref Unsafe.AsRef<uint>(&Handle->SourceId);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref uint SourceParentId => ref Unsafe.AsRef<uint>(&Handle->SourceParentId);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref int DataFrameCount => ref Unsafe.AsRef<int>(&Handle->DataFrameCount);
		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe Span<byte> DataType
		
		{
			get
			{
				return new Span<byte>(&Handle->DataType_0, 33);
			}
		}
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool Preview => ref Unsafe.AsRef<bool>(&Handle->Preview);
		/// <summary>
		/// To be documented.
		/// </summary>
		public ref bool Delivery => ref Unsafe.AsRef<bool>(&Handle->Delivery);
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
		public unsafe bool IsDataType(byte* type)
		{
			byte ret = ImGui.IsDataTypeNative(Handle, type);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(ref byte type)
		{
			fixed (byte* ptype = &type)
			{
				byte ret = ImGui.IsDataTypeNative(Handle, (byte*)ptype);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(ReadOnlySpan<byte> type)
		{
			fixed (byte* ptype = type)
			{
				byte ret = ImGui.IsDataTypeNative(Handle, (byte*)ptype);
				return ret != 0;
			}
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDataType(string type)
		{
			byte* pStr0 = null;
			int pStrSize0 = 0;
			if (type != null)
			{
				pStrSize0 = Utils.GetByteCountUTF8(type);
				if (pStrSize0 >= Utils.MaxStackallocSize)
				{
					pStr0 = Utils.Alloc<byte>(pStrSize0 + 1);
				}
				else
				{
					byte* pStrStack0 = stackalloc byte[pStrSize0 + 1];
					pStr0 = pStrStack0;
				}
				int pStrOffset0 = Utils.EncodeStringUTF8(type, pStr0, pStrSize0);
				pStr0[pStrOffset0] = 0;
			}
			byte ret = ImGui.IsDataTypeNative(Handle, pStr0);
			if (pStrSize0 >= Utils.MaxStackallocSize)
			{
				Utils.Free(pStr0);
			}
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsDelivery()
		{
			byte ret = ImGui.IsDeliveryNative(Handle);
			return ret != 0;
		}

		/// <summary>
		/// To be documented.
		/// </summary>
		public unsafe bool IsPreview()
		{
			byte ret = ImGui.IsPreviewNative(Handle);
			return ret != 0;
		}

	}

}

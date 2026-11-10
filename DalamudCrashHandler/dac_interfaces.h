#pragma once

// See clrdata.idl, xclrdata.idl, sospriv.idl

#include <objbase.h>

typedef ULONG64 CLRDATA_ADDRESS;
typedef ULONG64 CLRDATA_ENUM;

// Taken from cor.h
typedef UINT32  mdToken;
typedef mdToken mdTypeDef;
typedef mdToken mdMethodDef;
typedef mdToken mdFieldDef;

interface ICLRDataTarget;
interface ICLRDataTarget2;
interface IXCLRDataProcess;
interface IXCLRDataTask;
interface IXCLRDataStackWalk;
interface IXCLRDataFrame;
interface IXCLRDataMethodInstance;
interface IXCLRDataMethodDefinition;
interface IXCLRDataTypeInstance;
interface IXCLRDataTypeDefinition;
interface IXCLRDataAppDomain;
interface IXCLRDataAssembly;
interface IXCLRDataModule;
interface IXCLRDataValue;
interface IXCLRDataExceptionState;
interface IXCLRDataExceptionNotification;
interface IXCLRDataDisplay;
interface IXCLRLibrarySupport;
interface IXCLRDisassemblySupport;

typedef struct
{
    CLRDATA_ADDRESS startAddress;
    CLRDATA_ADDRESS endAddress;
} CLRDATA_ADDRESS_RANGE;

typedef struct
{
    ULONG64 Data[8];
} CLRDATA_FOLLOW_STUB_BUFFER;

typedef enum
{
    CLRDATA_ADDRESS_UNRECOGNIZED,
    CLRDATA_ADDRESS_MANAGED_METHOD,
    CLRDATA_ADDRESS_RUNTIME_MANAGED_CODE,
    CLRDATA_ADDRESS_RUNTIME_UNMANAGED_CODE,
    CLRDATA_ADDRESS_GC_DATA,
    CLRDATA_ADDRESS_RUNTIME_MANAGED_STUB,
    CLRDATA_ADDRESS_RUNTIME_UNMANAGED_STUB,
} CLRDataAddressType;

typedef enum
{
    CLRDATA_SIMPFRAME_UNRECOGNIZED          = 0x1,
    CLRDATA_SIMPFRAME_MANAGED_METHOD        = 0x2,
    CLRDATA_SIMPFRAME_RUNTIME_MANAGED_CODE  = 0x4,
    CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE = 0x8,
} CLRDataSimpleFrameType;

typedef enum
{
    CLRDATA_DETFRAME_UNRECOGNIZED,
    CLRDATA_DETFRAME_UNKNOWN_STUB,
    CLRDATA_DETFRAME_CLASS_INIT,
    CLRDATA_DETFRAME_EXCEPTION_FILTER,
    CLRDATA_DETFRAME_SECURITY,
    CLRDATA_DETFRAME_CONTEXT_POLICY,
    CLRDATA_DETFRAME_INTERCEPTION,
    CLRDATA_DETFRAME_PROCESS_START,
    CLRDATA_DETFRAME_THREAD_START,
    CLRDATA_DETFRAME_TRANSITION_TO_MANAGED,
    CLRDATA_DETFRAME_TRANSITION_TO_UNMANAGED,
    CLRDATA_DETFRAME_COM_INTEROP_STUB,
    CLRDATA_DETFRAME_DEBUGGER_EVAL,
    CLRDATA_DETFRAME_CONTEXT_SWITCH,
    CLRDATA_DETFRAME_FUNC_EVAL,
    CLRDATA_DETFRAME_FINALLY,
} CLRDataDetailedFrameType;

typedef enum
{
    CLRDATA_STACK_SET_UNWIND_CONTEXT  = 0x00000000,
    CLRDATA_STACK_SET_CURRENT_CONTEXT = 0x00000001,
} CLRDataStackSetContextFlag;

typedef HRESULT (STDAPICALLTYPE* PFN_CLRDataCreateInstance)(REFIID iid, ICLRDataTarget* target, void** iface);

MIDL_INTERFACE("3E11CCEE-D08B-43e5-AF01-32717A64DA03")
ICLRDataTarget : public IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE GetMachineType(
        /* [out] */ ULONG32* machineType) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetPointerSize(
        /* [out] */ ULONG32* pointerSize) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetImageBase(
        /* [in]  */ LPCWSTR imagePath,
        /* [out] */ CLRDATA_ADDRESS* baseAddress) = 0;

    virtual HRESULT STDMETHODCALLTYPE ReadVirtual(
        /* [in]  */ CLRDATA_ADDRESS address,
        /* [out] */ BYTE* buffer,
        /* [in]  */ ULONG32 bytesRequested,
        /* [out] */ ULONG32* bytesRead) = 0;

    virtual HRESULT STDMETHODCALLTYPE WriteVirtual(
        /* [in]  */ CLRDATA_ADDRESS address,
        /* [in]  */ BYTE* buffer,
        /* [in]  */ ULONG32 bytesRequested,
        /* [out] */ ULONG32* bytesWritten) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetTLSValue(
        /* [in]  */ ULONG32 threadID,
        /* [in]  */ ULONG32 index,
        /* [out] */ CLRDATA_ADDRESS* value) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetTLSValue(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 index,
        /* [in] */ CLRDATA_ADDRESS value) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetCurrentThreadID(
        /* [out] */ ULONG32* threadID) = 0;

    virtual HRESULT STDMETHODCALLTYPE GetThreadContext(
        /* [in]  */ ULONG32 threadID,
        /* [in]  */ ULONG32 contextFlags,
        /* [in]  */ ULONG32 contextSize,
        /* [out] */ BYTE* context) = 0;

    virtual HRESULT STDMETHODCALLTYPE SetThreadContext(
        /* [in] */ ULONG32 threadID,
        /* [in] */ ULONG32 contextSize,
        /* [in] */ BYTE* context) = 0;

    virtual HRESULT STDMETHODCALLTYPE Request(
        /* [in]  */ ULONG32 reqCode,
        /* [in]  */ ULONG32 inBufferSize,
        /* [in]  */ BYTE* inBuffer,
        /* [in]  */ ULONG32 outBufferSize,
        /* [out] */ BYTE* outBuffer) = 0;
};

MIDL_INTERFACE("6d05fae3-189c-4630-a6dc-1c251e1c01ab")
ICLRDataTarget2 : public ICLRDataTarget
{
    virtual HRESULT STDMETHODCALLTYPE AllocVirtual(
        /* [in]  */ CLRDATA_ADDRESS addr,
        /* [in]  */ ULONG32 size,
        /* [in]  */ ULONG32 typeFlags,
        /* [in]  */ ULONG32 protectFlags,
        /* [out] */ CLRDATA_ADDRESS* virt) = 0;

    virtual HRESULT STDMETHODCALLTYPE FreeVirtual(
        /* [in] */ CLRDATA_ADDRESS addr,
        /* [in] */ ULONG32 size,
        /* [in] */ ULONG32 typeFlags) = 0;
};

#define DAC_UNUSED_SLOT(n) virtual HRESULT STDMETHODCALLTYPE __unused_xclrdata_##n() = 0

MIDL_INTERFACE("5c552ab6-fc09-4cb3-8e36-22fa03c798b7")
IXCLRDataProcess : public IUnknown
{
    // Slot 0: Flush cached data, all sub-interfaces obtained are now invalid
    virtual HRESULT STDMETHODCALLTYPE Flush() = 0;
    // Slot 1
    DAC_UNUSED_SLOT(1);  // StartEnumTasks
    // Slot 2
    DAC_UNUSED_SLOT(2);  // EnumTask
    // Slot 3
    DAC_UNUSED_SLOT(3);  // EndEnumTasks
    // Slot 4 - get the managed task for a given OS thread ID
    virtual HRESULT STDMETHODCALLTYPE GetTaskByOSThreadID(
        /* [in]  */ ULONG32 osThreadID,
        /* [out] */ IXCLRDataTask** task) = 0;

    // Slot 5
    DAC_UNUSED_SLOT(5);  // GetTaskByUniqueID
    // Slot 6
    DAC_UNUSED_SLOT(6);  // GetFlags
    // Slot 7
    DAC_UNUSED_SLOT(7);  // IsSameObject
    // Slot 8
    DAC_UNUSED_SLOT(8);  // GetManagedObject
    // Slot 9
    DAC_UNUSED_SLOT(9);  // GetDesiredExecutionState
    // Slot 10
    DAC_UNUSED_SLOT(10); // SetDesiredExecutionState

    // Slot 11: used to classify addresses
    virtual HRESULT STDMETHODCALLTYPE GetAddressType(
        /* [in]  */ CLRDATA_ADDRESS address,
        /* [out] */ CLRDataAddressType* type) = 0;

    // Slot 12: used for CLR-internal runtime names (JIT helpers, stubs, etc.)
    virtual HRESULT STDMETHODCALLTYPE GetRuntimeNameByAddress(
        /* [in]  */ CLRDATA_ADDRESS address,
        /* [in]  */ ULONG32 flags,
        /* [in]  */ ULONG32 bufLen,
        /* [out] */ ULONG32* nameLen,
        /* [out] */ WCHAR nameBuf[],
        /* [out] */ CLRDATA_ADDRESS* displacement) = 0;

    // Slot 13
    DAC_UNUSED_SLOT(13); // StartEnumAppDomains
    // Slot 14
    DAC_UNUSED_SLOT(14); // EnumAppDomain
    // Slot 15
    DAC_UNUSED_SLOT(15); // EndEnumAppDomains
    // Slot 16
    DAC_UNUSED_SLOT(16); // GetAppDomainByUniqueID
    // Slot 17
    DAC_UNUSED_SLOT(17); // StartEnumAssemblies
    // Slot 18
    DAC_UNUSED_SLOT(18); // EnumAssembly
    // Slot 19
    DAC_UNUSED_SLOT(19); // EndEnumAssemblies
    // Slot 20
    DAC_UNUSED_SLOT(20); // StartEnumModules
    // Slot 21
    DAC_UNUSED_SLOT(21); // EnumModule
    // Slot 22
    DAC_UNUSED_SLOT(22); // EndEnumModules
    // Slot 23
    DAC_UNUSED_SLOT(23); // GetModuleByAddress

    // Slots 24-26: look up managed method instances by native code address
    virtual HRESULT STDMETHODCALLTYPE StartEnumMethodInstancesByAddress(
        /* [in]  */ CLRDATA_ADDRESS address,
        /* [in]  */ IXCLRDataAppDomain* appDomain,
        /* [out] */ CLRDATA_ENUM* handle) = 0;

    virtual HRESULT STDMETHODCALLTYPE EnumMethodInstanceByAddress(
        /* [in, out] */ CLRDATA_ENUM* handle,
        /* [out]     */ IXCLRDataMethodInstance** method) = 0;

    virtual HRESULT STDMETHODCALLTYPE EndEnumMethodInstancesByAddress(
        /* [in] */ CLRDATA_ENUM handle) = 0;

    // Slot 27
    DAC_UNUSED_SLOT(27); // GetDataByAddress
    // Slot 28
    DAC_UNUSED_SLOT(28); // GetExceptionStateByExceptionRecord
    // Slot 29
    DAC_UNUSED_SLOT(29); // TranslateExceptionRecordToNotification
    // Slot 30
    DAC_UNUSED_SLOT(30); // Request
    // Slot 31
    DAC_UNUSED_SLOT(31); // CreateMemoryValue
    // Slot 32
    DAC_UNUSED_SLOT(32); // SetAllTypeNotifications
    // Slot 33
    DAC_UNUSED_SLOT(33); // SetAllCodeNotifications
    // Slot 34
    DAC_UNUSED_SLOT(34); // GetTypeNotifications
    // Slot 35
    DAC_UNUSED_SLOT(35); // SetTypeNotifications
    // Slot 36
    DAC_UNUSED_SLOT(36); // GetCodeNotifications
    // Slot 37
    DAC_UNUSED_SLOT(37); // SetCodeNotifications
    // Slot 38
    DAC_UNUSED_SLOT(38); // GetOtherNotificationFlags
    // Slot 39
    DAC_UNUSED_SLOT(39); // SetOtherNotificationFlags
    // Slot 40
    DAC_UNUSED_SLOT(40); // StartEnumMethodDefinitionsByAddress
    // Slot 41
    DAC_UNUSED_SLOT(41); // EnumMethodDefinitionByAddress
    // Slot 42
    DAC_UNUSED_SLOT(42); // EndEnumMethodDefinitionsByAddress
    // Slot 43
    DAC_UNUSED_SLOT(43); // FollowStub
    // Slot 44
    DAC_UNUSED_SLOT(44); // FollowStub2
    // Slot 45
    DAC_UNUSED_SLOT(45); // DumpNativeImage
};

#undef DAC_UNUSED_SLOT

#define DAC_UNUSED_SLOT(n) virtual HRESULT STDMETHODCALLTYPE __unused_methdinst_##n() = 0

MIDL_INTERFACE("ECD73800-22CA-4b0d-AB55-E9BA7E6318A5")
IXCLRDataMethodInstance : public IUnknown
{
    // Slot 0
    DAC_UNUSED_SLOT(0);  // GetTypeInstance
    // Slot 1
    DAC_UNUSED_SLOT(1);  // GetDefinition
    // Slot 2
    DAC_UNUSED_SLOT(2);  // GetTokenAndScope

    // Slot 3: returns the fully qualified name of this managed method
    virtual HRESULT STDMETHODCALLTYPE GetName(
        /* [in]  */ ULONG32 flags,
        /* [in]  */ ULONG32 bufLen,
        /* [out] */ ULONG32* nameLen,
        /* [out] */ WCHAR nameBuf[]) = 0;

    // methods we never call
    DAC_UNUSED_SLOT(4);  // GetFlags
    DAC_UNUSED_SLOT(5);  // IsSameObject
    DAC_UNUSED_SLOT(6);  // GetEnCVersion
    DAC_UNUSED_SLOT(7);  // GetNumTypeArguments
    DAC_UNUSED_SLOT(8);  // GetTypeArgumentByIndex
    DAC_UNUSED_SLOT(9);  // GetILOffsetsByAddress
    DAC_UNUSED_SLOT(10); // GetAddressRangesByILOffset
    DAC_UNUSED_SLOT(11); // GetILAddressMap
    DAC_UNUSED_SLOT(12); // StartEnumExtents
    DAC_UNUSED_SLOT(13); // EnumExtent
    DAC_UNUSED_SLOT(14); // EndEnumExtents
    DAC_UNUSED_SLOT(15); // Request
    DAC_UNUSED_SLOT(16); // GetRepresentativeEntryAddress
};

#undef DAC_UNUSED_SLOT

#define SOS_UNUSED_SLOT(n) virtual HRESULT STDMETHODCALLTYPE __unused_sos_##n() = 0

// Must match DacpCodeHeaderData in dacprivate.h
enum JITTypes { TYPE_UNKNOWN = 0, TYPE_JIT = 1, TYPE_PJIT = 2 };

struct DacpCodeHeaderData
{
    CLRDATA_ADDRESS GCInfo          = 0;  // +0x00  (8)
    JITTypes        JITType         = TYPE_UNKNOWN; // +0x08 (4)
    DWORD           _pad0           = 0;  // +0x0C  (4) alignment padding
    CLRDATA_ADDRESS MethodDescPtr   = 0;  // +0x10  (8)
    CLRDATA_ADDRESS MethodStart     = 0;  // +0x18  (8)
    DWORD           MethodSize      = 0;  // +0x20  (4)
    DWORD           _pad1           = 0;  // +0x24  (4) alignment padding
    CLRDATA_ADDRESS ColdRegionStart = 0;  // +0x28  (8)
    DWORD           ColdRegionSize  = 0;  // +0x30  (4)
    DWORD           HotRegionSize   = 0;  // +0x34  (4)
    // Total: 0x38 = 56 bytes
};

MIDL_INTERFACE("436f00f2-b42a-4b9f-870c-e73db66ae930")
ISOSDacInterface : public IUnknown
{
    // methods we never call
    SOS_UNUSED_SLOT(0);   // GetThreadStoreData
    SOS_UNUSED_SLOT(1);   // GetAppDomainStoreData
    SOS_UNUSED_SLOT(2);   // GetAppDomainList
    SOS_UNUSED_SLOT(3);   // GetAppDomainData
    SOS_UNUSED_SLOT(4);   // GetAppDomainName
    SOS_UNUSED_SLOT(5);   // GetDomainFromContext
    SOS_UNUSED_SLOT(6);   // GetAssemblyList
    SOS_UNUSED_SLOT(7);   // GetAssemblyData
    SOS_UNUSED_SLOT(8);   // GetAssemblyName
    SOS_UNUSED_SLOT(9);   // GetModule
    SOS_UNUSED_SLOT(10);  // GetModuleData
    SOS_UNUSED_SLOT(11);  // TraverseModuleMap
    SOS_UNUSED_SLOT(12);  // GetAssemblyModuleList
    SOS_UNUSED_SLOT(13);  // GetILForModule
    SOS_UNUSED_SLOT(14);  // GetThreadData
    SOS_UNUSED_SLOT(15);  // GetThreadFromThinlockID
    SOS_UNUSED_SLOT(16);  // GetStackLimits
    SOS_UNUSED_SLOT(17);  // GetMethodDescData
    SOS_UNUSED_SLOT(18);  // GetMethodDescPtrFromIP
    SOS_UNUSED_SLOT(19);  // GetMethodDescName
    SOS_UNUSED_SLOT(20);  // GetMethodDescPtrFromFrame
    SOS_UNUSED_SLOT(21);  // GetMethodDescFromToken
    SOS_UNUSED_SLOT(22);  // GetMethodDescTransparencyData

    // Slot 23
    // Returns information about the JIT-compiled method at ip
    virtual HRESULT STDMETHODCALLTYPE GetCodeHeaderData(
        /* [in]  */ CLRDATA_ADDRESS          ip,
        /* [out] */ struct DacpCodeHeaderData* data) = 0;
};

#undef SOS_UNUSED_SLOT

#define DAC_UNUSED_SLOT(n) virtual HRESULT STDMETHODCALLTYPE __unused_task_##n() = 0

MIDL_INTERFACE("A5B0BEEA-EC62-4618-8012-A24FFC23934C")
IXCLRDataTask : public IUnknown
{
    DAC_UNUSED_SLOT(0);  // GetProcess
    DAC_UNUSED_SLOT(1);  // GetCurrentAppDomain
    DAC_UNUSED_SLOT(2);  // GetUniqueID
    DAC_UNUSED_SLOT(3);  // GetFlags
    DAC_UNUSED_SLOT(4);  // IsSameObject
    DAC_UNUSED_SLOT(5);  // GetManagedObject
    DAC_UNUSED_SLOT(6);  // GetDesiredExecutionState
    DAC_UNUSED_SLOT(7);  // SetDesiredExecutionState

    // Slot 8: create a stack walker for this task
    // flags is a bitmask of CLRDataSimpleFrameType values indicating which frame types to visit
    virtual HRESULT STDMETHODCALLTYPE CreateStackWalk(
        /* [in]  */ ULONG32 flags,
        /* [out] */ IXCLRDataStackWalk** stackWalk) = 0;

    DAC_UNUSED_SLOT(9);  // GetOSThreadID
    DAC_UNUSED_SLOT(10); // GetContext
    DAC_UNUSED_SLOT(11); // SetContext
    DAC_UNUSED_SLOT(12); // GetCurrentExceptionState
    DAC_UNUSED_SLOT(13); // Request
    DAC_UNUSED_SLOT(14); // GetName
    DAC_UNUSED_SLOT(15); // GetLastExceptionState
};

#undef DAC_UNUSED_SLOT

#define DAC_UNUSED_SLOT(n) virtual HRESULT STDMETHODCALLTYPE __unused_stackwalk_##n() = 0

MIDL_INTERFACE("E59D8D22-ADA7-49a2-89B5-A415AFCFC95F")
IXCLRDataStackWalk : public IUnknown
{
    // Slot 0: get the context (registers) at the current frame position.
    virtual HRESULT STDMETHODCALLTYPE GetContext(
        /* [in]  */ ULONG32 contextFlags,
        /* [in]  */ ULONG32 contextBufSize,
        /* [out] */ ULONG32* contextSize,
        /* [out] */ BYTE contextBuf[]) = 0;

    DAC_UNUSED_SLOT(1);  // SetContext (obsolete, use SetContext2)

    // Slot 2: advance to the next frame.
    // Returns S_OK on success, S_FALSE when there are no more frames.
    // For CLRDATA_SIMPFRAME_UNRECOGNIZED frames the DAC may not be able to advance,
    // in that case we have to manually unwind and call SetContext2
    virtual HRESULT STDMETHODCALLTYPE Next() = 0;

    DAC_UNUSED_SLOT(3);  // GetStackSizeSkipped

    // Slot 4: classify the current frame.
    virtual HRESULT STDMETHODCALLTYPE GetFrameType(
        /* [out] */ CLRDataSimpleFrameType* simpleType,
        /* [out] */ CLRDataDetailedFrameType* detailedType) = 0;

    DAC_UNUSED_SLOT(5);  // GetFrame
    DAC_UNUSED_SLOT(6);  // Request

    // Slot 7: replace the current context (used to start with a manually-unwound native context)
    // flags: CLRDATA_STACK_SET_UNWIND_CONTEXT  - context is the result of manual unwinding
    //        CLRDATA_STACK_SET_CURRENT_CONTEXT - context is the initial/current thread context
    virtual HRESULT STDMETHODCALLTYPE SetContext2(
        /* [in] */ ULONG32 flags,
        /* [in] */ ULONG32 contextSize,
        /* [in] */ BYTE context[]) = 0;
};

#undef DAC_UNUSED_SLOT

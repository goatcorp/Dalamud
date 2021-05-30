using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Dalamud.Injector
{
    /// <summary>
    /// Native functions.
    /// </summary>
    internal static class NativeFunctions
    {
        /// <summary>
        /// MEM_* from memoryapi.
        /// </summary>
        [Flags]
        public enum AllocationType
        {
            /// <summary>
            /// To coalesce two adjacent placeholders, specify MEM_RELEASE | MEM_COALESCE_PLACEHOLDERS. When you coalesce
            /// placeholders, lpAddress and dwSize must exactly match those of the placeholder.
            /// </summary>
            CoalescePlaceholders = 0x00000001,

            /// <summary>
            /// Frees an allocation back to a placeholder (after you've replaced a placeholder with a private allocation using
            /// VirtualAlloc2 or Virtual2AllocFromApp). To split a placeholder into two placeholders, specify
            /// MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER.
            /// </summary>
            PreservePlaceholder = 0x00000002,

            /// <summary>
            /// Allocates memory charges (from the overall size of memory and the paging files on disk) for the specified reserved
            /// memory pages. The function also guarantees that when the caller later initially accesses the memory, the contents
            /// will be zero. Actual physical pages are not allocated unless/until the virtual addresses are actually accessed.
            /// To reserve and commit pages in one step, call VirtualAllocEx with MEM_COMMIT | MEM_RESERVE. Attempting to commit
            /// a specific address range by specifying MEM_COMMIT without MEM_RESERVE and a non-NULL lpAddress fails unless the
            /// entire range has already been reserved. The resulting error code is ERROR_INVALID_ADDRESS. An attempt to commit
            /// a page that is already committed does not cause the function to fail. This means that you can commit pages without
            /// first determining the current commitment state of each page. If lpAddress specifies an address within an enclave,
            /// flAllocationType must be MEM_COMMIT.
            /// </summary>
            Commit = 0x1000,

            /// <summary>
            /// Reserves a range of the process's virtual address space without allocating any actual physical storage in memory
            /// or in the paging file on disk. You commit reserved pages by calling VirtualAllocEx again with MEM_COMMIT. To
            /// reserve and commit pages in one step, call VirtualAllocEx with MEM_COMMIT | MEM_RESERVE. Other memory allocation
            /// functions, such as malloc and LocalAlloc, cannot use reserved memory until it has been released.
            /// </summary>
            Reserve = 0x2000,

            /// <summary>
            /// Decommits the specified region of committed pages. After the operation, the pages are in the reserved state.
            /// The function does not fail if you attempt to decommit an uncommitted page. This means that you can decommit
            /// a range of pages without first determining the current commitment state. The MEM_DECOMMIT value is not supported
            /// when the lpAddress parameter provides the base address for an enclave.
            /// </summary>
            Decommit = 0x4000,

            /// <summary>
            /// Releases the specified region of pages, or placeholder (for a placeholder, the address space is released and
            /// available for other allocations). After this operation, the pages are in the free state. If you specify this
            /// value, dwSize must be 0 (zero), and lpAddress must point to the base address returned by the VirtualAlloc function
            /// when the region is reserved. The function fails if either of these conditions is not met. If any pages in the
            /// region are committed currently, the function first decommits, and then releases them. The function does not
            /// fail if you attempt to release pages that are in different states, some reserved and some committed. This means
            /// that you can release a range of pages without first determining the current commitment state.
            /// </summary>
            Release = 0x8000,

            /// <summary>
            /// Indicates that data in the memory range specified by lpAddress and dwSize is no longer of interest. The pages
            /// should not be read from or written to the paging file. However, the memory block will be used again later, so
            /// it should not be decommitted. This value cannot be used with any other value. Using this value does not guarantee
            /// that the range operated on with MEM_RESET will contain zeros. If you want the range to contain zeros, decommit
            /// the memory and then recommit it. When you use MEM_RESET, the VirtualAllocEx function ignores the value of fProtect.
            /// However, you must still set fProtect to a valid protection value, such as PAGE_NOACCESS. VirtualAllocEx returns
            /// an error if you use MEM_RESET and the range of memory is mapped to a file. A shared view is only acceptable
            /// if it is mapped to a paging file.
            /// </summary>
            Reset = 0x80000,

            /// <summary>
            /// MEM_RESET_UNDO should only be called on an address range to which MEM_RESET was successfully applied earlier.
            /// It indicates that the data in the specified memory range specified by lpAddress and dwSize is of interest to
            /// the caller and attempts to reverse the effects of MEM_RESET. If the function succeeds, that means all data in
            /// the specified address range is intact. If the function fails, at least some of the data in the address range
            /// has been replaced with zeroes. This value cannot be used with any other value. If MEM_RESET_UNDO is called on
            /// an address range which was not MEM_RESET earlier, the behavior is undefined. When you specify MEM_RESET, the
            /// VirtualAllocEx function ignores the value of flProtect. However, you must still set flProtect to a valid 
            /// protection value, such as PAGE_NOACCESS.
            /// </summary>
            ResetUndo = 0x1000000,

            /// <summary>
            /// Reserves an address range that can be used to map Address Windowing Extensions (AWE) pages. This value must
            /// be used with MEM_RESERVE and no other values.
            /// </summary>
            Physical = 0x400000,

            /// <summary>
            /// Allocates memory at the highest possible address. This can be slower than regular allocations, especially when
            /// there are many allocations.
            /// </summary>
            TopDown = 0x100000,

            /// <summary>
            /// Causes the system to track pages that are written to in the allocated region. If you specify this value, you
            /// must also specify MEM_RESERVE. To retrieve the addresses of the pages that have been written to since the region
            /// was allocated or the write-tracking state was reset, call the GetWriteWatch function. To reset the write-tracking
            /// state, call GetWriteWatch or ResetWriteWatch. The write-tracking feature remains enabled for the memory region
            /// until the region is freed.
            /// </summary>
            WriteWatch = 0x200000,

            /// <summary>
            /// Allocates memory using large page support. The size and alignment must be a multiple of the large-page minimum.
            /// To obtain this value, use the GetLargePageMinimum function. If you specify this value, you must also specify
            /// MEM_RESERVE and MEM_COMMIT.
            /// </summary>
            LargePages = 0x20000000,
        }

        /// <summary>
        /// PAGE_* from memoryapi.
        /// </summary>
        [Flags]
        public enum MemoryProtection
        {
            /// <summary>
            /// Enables execute access to the committed region of pages. An attempt to write to the committed region results
            /// in an access violation. This flag is not supported by the CreateFileMapping function.
            /// </summary>
            Execute = 0x10,

            /// <summary>
            /// Enables execute or read-only access to the committed region of pages. An attempt to write to the committed region
            /// results in an access violation.
            /// </summary>
            ExecuteRead = 0x20,

            /// <summary>
            /// Enables execute, read-only, or read/write access to the committed region of pages.
            /// </summary>
            ExecuteReadWrite = 0x40,

            /// <summary>
            /// Enables execute, read-only, or copy-on-write access to a mapped view of a file mapping object. An attempt to
            /// write to a committed copy-on-write page results in a private copy of the page being made for the process. The
            /// private page is marked as PAGE_EXECUTE_READWRITE, and the change is written to the new page. This flag is not
            /// supported by the VirtualAlloc or VirtualAllocEx functions.
            /// </summary>
            ExecuteWriteCopy = 0x80,

            /// <summary>
            /// Disables all access to the committed region of pages. An attempt to read from, write to, or execute the committed
            /// region results in an access violation. This flag is not supported by the CreateFileMapping function.
            /// </summary>
            NoAccess = 0x01,

            /// <summary>
            /// Enables read-only access to the committed region of pages. An attempt to write to the committed region results
            /// in an access violation. If Data Execution Prevention is enabled, an attempt to execute code in the committed
            /// region results in an access violation.
            /// </summary>
            ReadOnly = 0x02,

            /// <summary>
            /// Enables read-only or read/write access to the committed region of pages. If Data Execution Prevention is enabled,
            /// attempting to execute code in the committed region results in an access violation.
            /// </summary>
            ReadWrite = 0x04,

            /// <summary>
            /// Enables read-only or copy-on-write access to a mapped view of a file mapping object. An attempt to write to
            /// a committed copy-on-write page results in a private copy of the page being made for the process. The private
            /// page is marked as PAGE_READWRITE, and the change is written to the new page. If Data Execution Prevention is
            /// enabled, attempting to execute code in the committed region results in an access violation. This flag is not
            /// supported by the VirtualAlloc or VirtualAllocEx functions.
            /// </summary>
            WriteCopy = 0x08,

            /// <summary>
            /// Sets all locations in the pages as invalid targets for CFG. Used along with any execute page protection like
            /// PAGE_EXECUTE, PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE and PAGE_EXECUTE_WRITECOPY. Any indirect call to locations
            /// in those pages will fail CFG checks and the process will be terminated. The default behavior for executable
            /// pages allocated is to be marked valid call targets for CFG. This flag is not supported by the VirtualProtect
            /// or CreateFileMapping functions.
            /// </summary>
            TargetsInvalid = 0x40000000,

            /// <summary>
            /// Pages in the region will not have their CFG information updated while the protection changes for VirtualProtect.
            /// For example, if the pages in the region was allocated using PAGE_TARGETS_INVALID, then the invalid information
            /// will be maintained while the page protection changes. This flag is only valid when the protection changes to
            /// an executable type like PAGE_EXECUTE, PAGE_EXECUTE_READ, PAGE_EXECUTE_READWRITE and PAGE_EXECUTE_WRITECOPY.
            /// The default behavior for VirtualProtect protection change to executable is to mark all locations as valid call
            /// targets for CFG.
            /// </summary>
            TargetsNoUpdate = 0x40000000,

            /// <summary>
            /// Pages in the region become guard pages. Any attempt to access a guard page causes the system to raise a
            /// STATUS_GUARD_PAGE_VIOLATION exception and turn off the guard page status. Guard pages thus act as a one-time
            /// access alarm. For more information, see Creating Guard Pages. When an access attempt leads the system to turn
            /// off guard page status, the underlying page protection takes over. If a guard page exception occurs during a
            /// system service, the service typically returns a failure status indicator. This value cannot be used with
            /// PAGE_NOACCESS. This flag is not supported by the CreateFileMapping function.
            /// </summary>
            Guard = 0x100,

            /// <summary>
            /// Sets all pages to be non-cachable. Applications should not use this attribute except when explicitly required
            /// for a device. Using the interlocked functions with memory that is mapped with SEC_NOCACHE can result in an
            /// EXCEPTION_ILLEGAL_INSTRUCTION exception. The PAGE_NOCACHE flag cannot be used with the PAGE_GUARD, PAGE_NOACCESS,
            /// or PAGE_WRITECOMBINE flags. The PAGE_NOCACHE flag can be used only when allocating private memory with the
            /// VirtualAlloc, VirtualAllocEx, or VirtualAllocExNuma functions. To enable non-cached memory access for shared
            /// memory, specify the SEC_NOCACHE flag when calling the CreateFileMapping function.
            /// </summary>
            NoCache = 0x200,

            /// <summary>
            /// Sets all pages to be write-combined. Applications should not use this attribute except when explicitly required
            /// for a device. Using the interlocked functions with memory that is mapped as write-combined can result in an
            /// EXCEPTION_ILLEGAL_INSTRUCTION exception. The PAGE_WRITECOMBINE flag cannot be specified with the PAGE_NOACCESS,
            /// PAGE_GUARD, and PAGE_NOCACHE flags. The PAGE_WRITECOMBINE flag can be used only when allocating private memory
            /// with the VirtualAlloc, VirtualAllocEx, or VirtualAllocExNuma functions. To enable write-combined memory access
            /// for shared memory, specify the SEC_WRITECOMBINE flag when calling the CreateFileMapping function.
            /// </summary>
            WriteCombine = 0x400,
        }

        /// <summary>
        /// PROCESS_* from processthreadsapi.
        /// </summary>
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            /// <summary>
            /// All possible access rights for a process object.
            /// </summary>
            AllAccess = 0x001F0FFF,

            /// <summary>
            /// Required to create a process.
            /// </summary>
            CreateProcess = 0x0080,

            /// <summary>
            /// Required to create a thread.
            /// </summary>
            CreateThread = 0x0002,

            /// <summary>
            /// Required to duplicate a handle using DuplicateHandle.
            /// </summary>
            DupHandle = 0x0040,

            /// <summary>
            /// Required to retrieve certain information about a process, such as its token, exit code,
            /// and priority class (see OpenProcessToken).
            /// </summary>
            QueryInformation = 0x0400,

            /// <summary>
            /// Required to retrieve certain information about a process(see GetExitCodeProcess, GetPriorityClass, IsProcessInJob,
            /// QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted
            /// PROCESS_QUERY_LIMITED_INFORMATION.
            /// </summary>
            QueryLimitedInformation = 0x1000,

            /// <summary>
            /// Required to set certain information about a process, such as its priority class (see SetPriorityClass).
            /// </summary>
            SetInformation = 0x0200,

            /// <summary>
            /// Required to set memory limits using SetProcessWorkingSetSize.
            /// </summary>
            SetQuote = 0x0100,

            /// <summary>
            /// Required to suspend or resume a process.
            /// </summary>
            SuspendResume = 0x0800,

            /// <summary>
            /// Required to terminate a process using TerminateProcess.
            /// </summary>
            Terminate = 0x0001,

            /// <summary>
            /// Required to perform an operation on the address space of a process(see VirtualProtectEx and WriteProcessMemory).
            /// </summary>
            VmOperation = 0x0008,

            /// <summary>
            /// Required to read memory in a process using ReadProcessMemory.
            /// </summary>
            VmRead = 0x0010,

            /// <summary>
            /// Required to write to memory in a process using WriteProcessMemory.
            /// </summary>
            VmWrite = 0x0020,

            /// <summary>
            /// Required to wait for the process to terminate using the wait functions.
            /// </summary>
            Synchronize = 0x00100000,
        }

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">
        /// A valid handle to an open object.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.To get extended
        /// error information, call GetLastError. If the application is running under a debugger, the function will throw an
        /// exception if it receives either a handle value that is not valid or a pseudo-handle value. This can happen if you
        /// close a handle twice, or if you call CloseHandle on a handle returned by the FindFirstFile function instead of calling
        /// the FindClose function.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Creates a thread that runs in the virtual address space of another process. Use the CreateRemoteThreadEx function
        /// to create a thread that runs in the virtual address space of another process and optionally specify extended attributes.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process in which the thread is to be created. The handle must have the PROCESS_CREATE_THREAD,
        /// PROCESS_QUERY_INFORMATION, PROCESS_VM_OPERATION, PROCESS_VM_WRITE, and PROCESS_VM_READ access rights, and may fail
        /// without these rights on certain platforms. For more information, see Process Security and Access Rights.
        /// </param>
        /// <param name="lpThreadAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread and determines
        /// whether child processes can inherit the returned handle. If lpThreadAttributes is NULL, the thread gets a default
        /// security descriptor and the handle cannot be inherited. The access control lists (ACL) in the default security descriptor
        /// for a thread come from the primary token of the creator.
        /// </param>
        /// <param name="dwStackSize">
        /// The initial size of the stack, in bytes. The system rounds this value to the nearest page. If this parameter is
        /// 0 (zero), the new thread uses the default size for the executable. For more information, see Thread Stack Size.
        /// </param>
        /// <param name="lpStartAddress">
        /// A pointer to the application-defined function of type LPTHREAD_START_ROUTINE to be executed by the thread and
        /// represents the starting address of the thread in the remote process. The function must exist in the remote process.
        /// For more information, see ThreadProc.
        /// </param>
        /// <param name="lpParameter">
        /// A pointer to a variable to be passed to the thread function.
        /// </param>
        /// <param name="dwCreationFlags">
        /// The flags that control the creation of the thread.
        /// </param>
        /// <param name="lpThreadId">
        /// A pointer to a variable that receives the thread identifier. If this parameter is NULL, the thread identifier is
        /// not returned.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the new thread. If the function fails, the return value
        /// is NULL.To get extended error information, call GetLastError. Note that CreateRemoteThread may succeed even if
        /// lpStartAddress points to data, code, or is not accessible. If the start address is invalid when the thread runs,
        /// an exception occurs, and the thread terminates. Thread termination due to a invalid start address is handled as
        /// an error exit for the thread's process. This behavior is similar to the asynchronous nature of CreateProcess, where
        /// the process is created even if it refers to invalid or missing dynamic-link libraries (DLL).
        /// </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            IntPtr lpThreadId);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulehandlew.
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process. To
        /// avoid the race conditions described in the Remarks section, use the GetModuleHandleEx function.
        /// </summary>
        /// <param name="lpModuleName">
        /// The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default library
        /// extension .dll is appended. The file name string can include a trailing point character (.) to indicate that the
        /// module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use
        /// backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently
        /// mapped into the address space of the calling process. If this parameter is NULL, GetModuleHandle returns a handle
        /// to the file used to create the calling process (.exe file). The GetModuleHandle function does not retrieve handles
        /// for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.For more information, see LoadLibraryEx.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the specified module. If the function fails, the return
        /// value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
        /// </summary>
        /// <param name="hModule">
        /// A handle to the DLL module that contains the function or variable. The LoadLibrary, LoadLibraryEx, LoadPackagedLibrary,
        /// or GetModuleHandle function returns this handle. The GetProcAddress function does not retrieve addresses from modules
        /// that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.For more information, see LoadLibraryEx.
        /// </param>
        /// <param name="procName">
        /// The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be
        /// in the low-order word; the high-order word must be zero.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the address of the exported function or variable. If the function
        /// fails, the return value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess.
        /// Opens an existing local process object.
        /// </summary>
        /// <param name="processAccess">
        /// The access to the process object. This access right is checked against the security descriptor for the process.
        /// This parameter can be one or more of the process access rights. If the caller has enabled the SeDebugPrivilege
        /// privilege, the requested access is granted regardless of the contents of the security descriptor.
        /// </param>
        /// <param name="bInheritHandle">
        /// If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do
        /// not inherit this handle.
        /// </param>
        /// <param name="processId">
        /// The identifier of the local process to be opened.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is an open handle to the specified process. If the function fails, the
        /// return value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags processAccess,
            bool bInheritHandle,
            int processId);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualallocex.
        /// Reserves, commits, or changes the state of a region of memory within the virtual address space of a specified process.
        /// The function initializes the memory it allocates to zero. To specify the NUMA node for the physical memory, see
        /// VirtualAllocExNuma.
        /// </summary>
        /// <param name="hProcess">
        /// The handle to a process. The function allocates memory within the virtual address space of this process. The handle
        /// must have the PROCESS_VM_OPERATION access right. For more information, see Process Security and Access Rights.
        /// </param>
        /// <param name="lpAddress">
        /// The pointer that specifies a desired starting address for the region of pages that you want to allocate. If you
        /// are reserving memory, the function rounds this address down to the nearest multiple of the allocation granularity.
        /// If you are committing memory that is already reserved, the function rounds this address down to the nearest page
        /// boundary. To determine the size of a page and the allocation granularity on the host computer, use the GetSystemInfo
        /// function. If lpAddress is NULL, the function determines where to allocate the region. If this address is within
        /// an enclave that you have not initialized by calling InitializeEnclave, VirtualAllocEx allocates a page of zeros
        /// for the enclave at that address. The page must be previously uncommitted, and will not be measured with the EEXTEND
        /// instruction of the Intel Software Guard Extensions programming model. If the address in within an enclave that you
        /// initialized, then the allocation operation fails with the ERROR_INVALID_ADDRESS error.
        /// </param>
        /// <param name="dwSize">
        /// The size of the region of memory to allocate, in bytes. If lpAddress is NULL, the function rounds dwSize up to the
        /// next page boundary. If lpAddress is not NULL, the function allocates all pages that contain one or more bytes in
        /// the range from lpAddress to lpAddress+dwSize. This means, for example, that a 2-byte range that straddles a page
        /// boundary causes the function to allocate both pages.
        /// </param>
        /// <param name="flAllocationType">
        /// The type of memory allocation. This parameter must contain one of the MEM_* enum values.
        /// </param>
        /// <param name="flProtect">
        /// The memory protection for the region of pages to be allocated. If the pages are being committed, you can specify
        /// any one of the memory protection constants.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the base address of the allocated region of pages. If the function
        /// fails, the return value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualfreeex.
        /// Releases, decommits, or releases and decommits a region of memory within the virtual address space of a specified
        /// process.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to a process. The function frees memory within the virtual address space of the process. The handle must
        /// have the PROCESS_VM_OPERATION access right.For more information, see Process Security and Access Rights.
        /// </param>
        /// <param name="lpAddress">
        /// A pointer to the starting address of the region of memory to be freed. If the dwFreeType parameter is MEM_RELEASE,
        /// lpAddress must be the base address returned by the VirtualAllocEx function when the region is reserved.
        /// </param>
        /// <param name="dwSize">
        /// The size of the region of memory to free, in bytes. If the dwFreeType parameter is MEM_RELEASE, dwSize must be 0
        /// (zero). The function frees the entire region that is reserved in the initial allocation call to VirtualAllocEx.
        /// If dwFreeType is MEM_DECOMMIT, the function decommits all memory pages that contain one or more bytes in the range
        /// from the lpAddress parameter to (lpAddress+dwSize). This means, for example, that a 2-byte region of memory that
        /// straddles a page boundary causes both pages to be decommitted. If lpAddress is the base address returned by
        /// VirtualAllocEx and dwSize is 0 (zero), the function decommits the entire region that is allocated by VirtualAllocEx.
        /// After that, the entire region is in the reserved state.
        /// </param>
        /// <param name="dwFreeType">
        /// The type of free operation. This parameter must be one of the MEM_* enum values.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a nonzero value. If the function fails, the return value is 0 (zero).
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            int dwSize,
            AllocationType dwFreeType);

        /// <summary>
        /// Writes data to an area of memory in a specified process. The entire area to be written to must be accessible or
        /// the operation fails.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process memory to be modified. The handle must have PROCESS_VM_WRITE and PROCESS_VM_OPERATION access
        /// to the process.
        /// </param>
        /// <param name="lpBaseAddress">
        /// A pointer to the base address in the specified process to which data is written. Before data transfer occurs, the
        /// system verifies that all data in the base address and memory of the specified size is accessible for write access,
        /// and if it is not accessible, the function fails.
        /// </param>
        /// <param name="lpBuffer">
        /// A pointer to the buffer that contains data to be written in the address space of the specified process.
        /// </param>
        /// <param name="dwSize">
        /// The number of bytes to be written to the specified process.
        /// </param>
        /// <param name="lpNumberOfBytesWritten">
        /// A pointer to a variable that receives the number of bytes transferred into the specified process. This parameter
        /// is optional. If lpNumberOfBytesWritten is NULL, the parameter is ignored.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
        /// extended error information, call GetLastError.The function fails if the requested write operation crosses into an
        /// area of the process that is inaccessible.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesWritten);
    }
}

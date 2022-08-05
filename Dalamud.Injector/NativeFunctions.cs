using System;
using System.Runtime.InteropServices;

namespace Dalamud.Injector
{
    /// <summary>
    /// Native user32 functions.
    /// </summary>
    internal static partial class NativeFunctions
    {
        /// <summary>
        /// MB_* from winuser.
        /// </summary>
        public enum MessageBoxType : uint
        {
            /// <summary>
            /// The default value for any of the various subtypes.
            /// </summary>
            DefaultValue = 0x0,

            // To indicate the buttons displayed in the message box, specify one of the following values.

            /// <summary>
            /// The message box contains three push buttons: Abort, Retry, and Ignore.
            /// </summary>
            AbortRetryIgnore = 0x2,

            /// <summary>
            /// The message box contains three push buttons: Cancel, Try Again, Continue. Use this message box type instead
            /// of MB_ABORTRETRYIGNORE.
            /// </summary>
            CancelTryContinue = 0x6,

            /// <summary>
            /// Adds a Help button to the message box. When the user clicks the Help button or presses F1, the system sends
            /// a WM_HELP message to the owner.
            /// </summary>
            Help = 0x4000,

            /// <summary>
            /// The message box contains one push button: OK. This is the default.
            /// </summary>
            Ok = DefaultValue,

            /// <summary>
            /// The message box contains two push buttons: OK and Cancel.
            /// </summary>
            OkCancel = 0x1,

            /// <summary>
            /// The message box contains two push buttons: Retry and Cancel.
            /// </summary>
            RetryCancel = 0x5,

            /// <summary>
            /// The message box contains two push buttons: Yes and No.
            /// </summary>
            YesNo = 0x4,

            /// <summary>
            /// The message box contains three push buttons: Yes, No, and Cancel.
            /// </summary>
            YesNoCancel = 0x3,

            // To display an icon in the message box, specify one of the following values.

            /// <summary>
            /// An exclamation-point icon appears in the message box.
            /// </summary>
            IconExclamation = 0x30,

            /// <summary>
            /// An exclamation-point icon appears in the message box.
            /// </summary>
            IconWarning = IconExclamation,

            /// <summary>
            /// An icon consisting of a lowercase letter i in a circle appears in the message box.
            /// </summary>
            IconInformation = 0x40,

            /// <summary>
            /// An icon consisting of a lowercase letter i in a circle appears in the message box.
            /// </summary>
            IconAsterisk = IconInformation,

            /// <summary>
            /// A question-mark icon appears in the message box.
            /// The question-mark message icon is no longer recommended because it does not clearly represent a specific type
            /// of message and because the phrasing of a message as a question could apply to any message type. In addition,
            /// users can confuse the message symbol question mark with Help information. Therefore, do not use this question
            /// mark message symbol in your message boxes. The system continues to support its inclusion only for backward
            /// compatibility.
            /// </summary>
            IconQuestion = 0x20,

            /// <summary>
            /// A stop-sign icon appears in the message box.
            /// </summary>
            IconStop = 0x10,

            /// <summary>
            /// A stop-sign icon appears in the message box.
            /// </summary>
            IconError = IconStop,

            /// <summary>
            /// A stop-sign icon appears in the message box.
            /// </summary>
            IconHand = IconStop,

            // To indicate the default button, specify one of the following values.

            /// <summary>
            /// The first button is the default button.
            /// MB_DEFBUTTON1 is the default unless MB_DEFBUTTON2, MB_DEFBUTTON3, or MB_DEFBUTTON4 is specified.
            /// </summary>
            DefButton1 = DefaultValue,

            /// <summary>
            /// The second button is the default button.
            /// </summary>
            DefButton2 = 0x100,

            /// <summary>
            /// The third button is the default button.
            /// </summary>
            DefButton3 = 0x200,

            /// <summary>
            /// The fourth button is the default button.
            /// </summary>
            DefButton4 = 0x300,

            // To indicate the modality of the dialog box, specify one of the following values.

            /// <summary>
            /// The user must respond to the message box before continuing work in the window identified by the hWnd parameter.
            /// However, the user can move to the windows of other threads and work in those windows. Depending on the hierarchy
            /// of windows in the application, the user may be able to move to other windows within the thread. All child windows
            /// of the parent of the message box are automatically disabled, but pop-up windows are not. MB_APPLMODAL is the
            /// default if neither MB_SYSTEMMODAL nor MB_TASKMODAL is specified.
            /// </summary>
            ApplModal = DefaultValue,

            /// <summary>
            /// Same as MB_APPLMODAL except that the message box has the WS_EX_TOPMOST style.
            /// Use system-modal message boxes to notify the user of serious, potentially damaging errors that require immediate
            /// attention (for example, running out of memory). This flag has no effect on the user's ability to interact with
            /// windows other than those associated with hWnd.
            /// </summary>
            SystemModal = 0x1000,

            /// <summary>
            /// Same as MB_APPLMODAL except that all the top-level windows belonging to the current thread are disabled if the
            /// hWnd parameter is NULL. Use this flag when the calling application or library does not have a window handle
            /// available but still needs to prevent input to other windows in the calling thread without suspending other threads.
            /// </summary>
            TaskModal = 0x2000,

            // To specify other options, use one or more of the following values.

            /// <summary>
            /// Same as desktop of the interactive window station. For more information, see Window Stations. If the current
            /// input desktop is not the default desktop, MessageBox does not return until the user switches to the default
            /// desktop.
            /// </summary>
            DefaultDesktopOnly = 0x20000,

            /// <summary>
            /// The text is right-justified.
            /// </summary>
            Right = 0x80000,

            /// <summary>
            /// Displays message and caption text using right-to-left reading order on Hebrew and Arabic systems.
            /// </summary>
            RtlReading = 0x100000,

            /// <summary>
            /// The message box becomes the foreground window. Internally, the system calls the SetForegroundWindow function
            /// for the message box.
            /// </summary>
            SetForeground = 0x10000,

            /// <summary>
            /// The message box is created with the WS_EX_TOPMOST window style.
            /// </summary>
            Topmost = 0x40000,

            /// <summary>
            /// The caller is a service notifying the user of an event. The function displays a message box on the current active
            /// desktop, even if there is no user logged on to the computer.
            /// </summary>
            ServiceNotification = 0x200000,
        }

        /// <summary>
        /// Displays a modal dialog box that contains a system icon, a set of buttons, and a brief application-specific message,
        /// such as status or error information. The message box returns an integer value that indicates which button the user
        /// clicked.
        /// </summary>
        /// <param name="hWnd">
        /// A handle to the owner window of the message box to be created. If this parameter is NULL, the message box has no
        /// owner window.
        /// </param>
        /// <param name="text">
        /// The message to be displayed. If the string consists of more than one line, you can separate the lines using a carriage
        /// return and/or linefeed character between each line.
        /// </param>
        /// <param name="caption">
        /// The dialog box title. If this parameter is NULL, the default title is Error.</param>
        /// <param name="type">
        /// The contents and behavior of the dialog box. This parameter can be a combination of flags from the following groups
        /// of flags.
        /// </param>
        /// <returns>
        /// If a message box has a Cancel button, the function returns the IDCANCEL value if either the ESC key is pressed or
        /// the Cancel button is selected. If the message box has no Cancel button, pressing ESC will no effect - unless an
        /// MB_OK button is present. If an MB_OK button is displayed and the user presses ESC, the return value will be IDOK.
        /// If the function fails, the return value is zero.To get extended error information, call GetLastError. If the function
        /// succeeds, the return value is one of the ID* enum values.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(IntPtr hWnd, string text, string caption, MessageBoxType type);
    }

    /// <summary>
    /// Native kernel32 functions.
    /// </summary>
    internal static partial class NativeFunctions
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
            CoalescePlaceholders = 0x1,

            /// <summary>
            /// Frees an allocation back to a placeholder (after you've replaced a placeholder with a private allocation using
            /// VirtualAlloc2 or Virtual2AllocFromApp). To split a placeholder into two placeholders, specify
            /// MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER.
            /// </summary>
            PreservePlaceholder = 0x2,

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
        /// Unprefixed flags from CreateRemoteThread.
        /// </summary>
        [Flags]
        public enum CreateThreadFlags
        {
            /// <summary>
            /// The thread runs immediately after creation.
            /// </summary>
            RunImmediately = 0x0,

            /// <summary>
            /// The thread is created in a suspended state, and does not run until the ResumeThread function is called.
            /// </summary>
            CreateSuspended = 0x4,

            /// <summary>
            /// The dwStackSize parameter specifies the initial reserve size of the stack. If this flag is not specified, dwStackSize specifies the commit size.
            /// </summary>
            StackSizeParamIsReservation = 0x10000,
        }

        /// <summary>
        /// DUPLICATE_* values for DuplicateHandle's dwDesiredAccess.
        /// </summary>
        [Flags]
        public enum DuplicateOptions : uint
        {
            /// <summary>
            /// Closes the source handle. This occurs regardless of any error status returned.
            /// </summary>
            CloseSource = 0x00000001,

            /// <summary>
            /// Ignores the dwDesiredAccess parameter. The duplicate handle has the same access as the source handle.
            /// </summary>
            SameAccess = 0x00000002,
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
            TargetsNoUpdate = TargetsInvalid,

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
        /// WAIT_* from synchapi.
        /// </summary>
        public enum WaitResult
        {
            /// <summary>
            /// The specified object is a mutex object that was not released by the thread that owned the mutex object
            /// before the owning thread terminated.Ownership of the mutex object is granted to the calling thread and
            /// the mutex state is set to nonsignaled. If the mutex was protecting persistent state information, you
            /// should check it for consistency.
            /// </summary>
            Abandoned = 0x80,

            /// <summary>
            /// The state of the specified object is signaled.
            /// </summary>
            Object0 = 0x0,

            /// <summary>
            /// The time-out interval elapsed, and the object's state is nonsignaled.
            /// </summary>
            Timeout = 0x102,

            /// <summary>
            /// The function has failed. To get extended error information, call GetLastError.
            /// </summary>
            WAIT_FAILED = 0xFFFFFFF,
        }

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">
        /// A valid handle to an open object.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error
        /// information, call GetLastError. If the application is running under a debugger, the function will throw an exception if it receives
        /// either a handle value that is not valid or a pseudo-handle value. This can happen if you close a handle twice, or if you call
        /// CloseHandle on a handle returned by the FindFirstFile function instead of calling the FindClose function.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Creates a thread that runs in the virtual address space of another process. Use the CreateRemoteThreadEx function
        /// to create a thread that runs in the virtual address space of another process and optionally specify extended attributes.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process in which the thread is to be created. The handle must have the PROCESS_CREATE_THREAD,
        /// PROCESS_QUERY_INFORMATION, PROCESS_VM_OPERATION, PROCESS_VM_WRITE, and PROCESS_VM_READ access rights, and may fail without
        /// these rights on certain platforms. For more information, see Process Security and Access Rights.
        /// </param>
        /// <param name="lpThreadAttributes">
        /// A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread and determines whether
        /// child processes can inherit the returned handle. If lpThreadAttributes is NULL, the thread gets a default security descriptor
        /// and the handle cannot be inherited. The access control lists (ACL) in the default security descriptor for a thread come from
        /// the primary token of the creator.
        /// </param>
        /// <param name="dwStackSize">
        /// The initial size of the stack, in bytes. The system rounds this value to the nearest page. If this parameter is 0 (zero), the
        /// new thread uses the default size for the executable. For more information, see Thread Stack Size.
        /// </param>
        /// <param name="lpStartAddress">
        /// A pointer to the application-defined function of type LPTHREAD_START_ROUTINE to be executed by the thread and represents the
        /// starting address of the thread in the remote process. The function must exist in the remote process. For more information,
        /// see ThreadProc.
        /// </param>
        /// <param name="lpParameter">
        /// A pointer to a variable to be passed to the thread function.
        /// </param>
        /// <param name="dwCreationFlags">
        /// The flags that control the creation of the thread.
        /// </param>
        /// <param name="lpThreadId">
        /// A pointer to a variable that receives the thread identifier. If this parameter is NULL, the thread identifier is not returned.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the new thread. If the function fails, the return value is
        /// NULL.To get extended error information, call GetLastError. Note that CreateRemoteThread may succeed even if lpStartAddress
        /// points to data, code, or is not accessible. If the start address is invalid when the thread runs, an exception occurs, and
        /// the thread terminates. Thread termination due to a invalid start address is handled as an error exit for the thread's process.
        /// This behavior is similar to the asynchronous nature of CreateProcess, where the process is created even if it refers to
        /// invalid or missing dynamic-link libraries (DLL).
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            UIntPtr dwStackSize,
            nuint lpStartAddress,
            nuint lpParameter,
            CreateThreadFlags dwCreationFlags,
            out uint lpThreadId);

        /// <summary>
        /// Retrieves the termination status of the specified thread.
        /// </summary>
        /// <param name="hThread">
        /// A handle to the thread. The handle must have the THREAD_QUERY_INFORMATION or THREAD_QUERY_LIMITED_INFORMATION
        /// access right.For more information, see Thread Security and Access Rights.
        /// </param>
        /// <param name="lpExitCode">
        /// A pointer to a variable to receive the thread termination status.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get
        /// extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        /// <summary>
        /// Opens an existing local process object.
        /// </summary>
        /// <param name="dwDesiredAccess">
        /// The access to the process object. This access right is checked against the security descriptor for the process. This parameter can be one or
        /// more of the process access rights. If the caller has enabled the SeDebugPrivilege privilege, the requested access is granted regardless of the
        /// contents of the security descriptor.
        /// </param>
        /// <param name="bInheritHandle">
        /// If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.
        /// </param>
        /// <param name="dwProcessId">
        /// The identifier of the local process to be opened. If the specified process is the System Idle Process(0x00000000), the function fails and the
        /// last error code is ERROR_INVALID_PARAMETER.If the specified process is the System process or one of the Client Server Run-Time Subsystem(CSRSS)
        /// processes, this function fails and the last error code is ERROR_ACCESS_DENIED because their access restrictions prevent user-level code from
        /// opening them. If you are using GetCurrentProcessId as an argument to this function, consider using GetCurrentProcess instead of OpenProcess, for
        /// improved performance.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is an open handle to the specified process.
        /// If the function fails, the return value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
            ProcessAccessFlags dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

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
        /// Waits until the specified object is in the signaled state or the time-out interval elapses. To enter an alertable wait
        /// state, use the WaitForSingleObjectEx function.To wait for multiple objects, use WaitForMultipleObjects.
        /// </summary>
        /// <param name="hHandle">
        /// A handle to the object. For a list of the object types whose handles can be specified, see the following Remarks section.
        /// If this handle is closed while the wait is still pending, the function's behavior is undefined. The handle must have the
        /// SYNCHRONIZE access right. For more information, see Standard Access Rights.
        /// </param>
        /// <param name="dwMilliseconds">
        /// The time-out interval, in milliseconds. If a nonzero value is specified, the function waits until the object is signaled
        /// or the interval elapses. If dwMilliseconds is zero, the function does not enter a wait state if the object is not signaled;
        /// it always returns immediately. If dwMilliseconds is INFINITE, the function will return only when the object is signaled.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value indicates the event that caused the function to return.
        /// It can be one of the WaitResult values.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

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

        /// <summary>
        /// Duplicates an object handle.
        /// </summary>
        /// <param name="hSourceProcessHandle">
        /// A handle to the process with the handle to be duplicated.
        ///
        /// The handle must have the PROCESS_DUP_HANDLE access right.
        /// </param>
        /// <param name="hSourceHandle">
        /// The handle to be duplicated. This is an open object handle that is valid in the context of the source process.
        /// For a list of objects whose handles can be duplicated, see the following Remarks section.
        /// </param>
        /// <param name="hTargetProcessHandle">
        /// A handle to the process that is to receive the duplicated handle.
        ///
        /// The handle must have the PROCESS_DUP_HANDLE access right.
        /// </param>
        /// <param name="lpTargetHandle">
        /// A pointer to a variable that receives the duplicate handle. This handle value is valid in the context of the target process.
        ///
        /// If hSourceHandle is a pseudo handle returned by GetCurrentProcess or GetCurrentThread, DuplicateHandle converts it to a real handle to a process or thread, respectively.
        ///
        /// If lpTargetHandle is NULL, the function duplicates the handle, but does not return the duplicate handle value to the caller. This behavior exists only for backward compatibility with previous versions of this function. You should not use this feature, as you will lose system resources until the target process terminates.
        ///
        /// This parameter is ignored if hTargetProcessHandle is NULL.
        /// </param>
        /// <param name="dwDesiredAccess">
        /// The access requested for the new handle. For the flags that can be specified for each object type, see the following Remarks section.
        ///
        /// This parameter is ignored if the dwOptions parameter specifies the DUPLICATE_SAME_ACCESS flag. Otherwise, the flags that can be specified depend on the type of object whose handle is to be duplicated.
        ///
        /// This parameter is ignored if hTargetProcessHandle is NULL.
        /// </param>
        /// <param name="bInheritHandle">
        /// A variable that indicates whether the handle is inheritable. If TRUE, the duplicate handle can be inherited by new processes created by the target process. If FALSE, the new handle cannot be inherited.
        ///
        /// This parameter is ignored if hTargetProcessHandle is NULL.
        /// </param>
        /// <param name="dwOptions">
        /// Optional actions.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        ///
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-duplicatehandle.
        /// </remarks>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcessHandle,
            out IntPtr lpTargetHandle,
            uint dwDesiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            DuplicateOptions dwOptions);
    }
}

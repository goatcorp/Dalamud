using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Dalamud
{
    /// <summary>
    /// Native user32 functions.
    /// </summary>
    internal static partial class NativeFunctions
    {
        /// <summary>
        /// FLASHW_* from winuser.
        /// </summary>
        public enum FlashWindow : uint
        {
            /// <summary>
            /// Stop flashing. The system restores the window to its original state.
            /// </summary>
            Stop = 0,

            /// <summary>
            /// Flash the window caption.
            /// </summary>
            Caption = 1,

            /// <summary>
            /// Flash the taskbar button.
            /// </summary>
            Tray = 2,

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
            /// </summary>
            All = 3,

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            Timer = 4,

            /// <summary>
            /// Flash continuously until the window comes to the foreground.
            /// </summary>
            TimerNoFG = 12,
        }

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
        /// Returns true if the current application has focus, false otherwise.
        /// </summary>
        /// <returns>
        /// If the current application is focused.
        /// </returns>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
                return false; // No window is currently activated

            _ = GetWindowThreadProcessId(activatedHandle, out var activeProcId);
            if (Marshal.GetLastWin32Error() != 0)
                return false;

            return activeProcId == Environment.ProcessId;
        }

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-flashwindowex.
        /// Flashes the specified window. It does not change the active state of the window.
        /// </summary>
        /// <param name="pwfi">
        /// A pointer to a FLASHWINFO structure.
        /// </param>
        /// <returns>
        /// The return value specifies the window's state before the call to the FlashWindowEx function. If the window caption
        /// was drawn as active before the call, the return value is nonzero. Otherwise, the return value is zero.
        /// </returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FlashWindowInfo pwfi);

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working). The system assigns
        /// a slightly higher priority to the thread that creates the foreground window than it does to other threads.
        /// </summary>
        /// <returns>
        /// The return value is a handle to the foreground window. The foreground window can be NULL in certain circumstances,
        /// such as when a window is losing activation.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the
        /// process that created the window.
        /// </summary>
        /// <param name="handle">
        /// A handle to the window.
        /// </param>
        /// <param name="processId">
        /// A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId
        /// copies the identifier of the process to the variable; otherwise, it does not.
        /// </param>
        /// <returns>
        /// The return value is the identifier of the thread that created the window.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

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

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-flashwinfo.
        /// Contains the flash status for a window and the number of times the system should flash the window.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct FlashWindowInfo
        {
            /// <summary>
            /// The size of the structure, in bytes.
            /// </summary>
            public uint Size;

            /// <summary>
            /// A handle to the window to be flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr Hwnd;

            /// <summary>
            /// The flash status. This parameter can be one or more of the FlashWindow enum values.
            /// </summary>
            public FlashWindow Flags;

            /// <summary>
            /// The number of times to flash the window.
            /// </summary>
            public uint Count;

            /// <summary>
            /// The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the
            /// default cursor blink rate.
            /// </summary>
            public uint Timeout;
        }
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
        /// SEM_* from errhandlingapi.
        /// </summary>
        [Flags]
        public enum ErrorModes : uint
        {
            /// <summary>
            /// Use the system default, which is to display all error dialog boxes.
            /// </summary>
            SystemDefault = 0x0,

            /// <summary>
            /// The system does not display the critical-error-handler message box. Instead, the system sends the error to the
            /// calling process. Best practice is that all applications call the process-wide SetErrorMode function with a parameter
            /// of SEM_FAILCRITICALERRORS at startup. This is to prevent error mode dialogs from hanging the application.
            /// </summary>
            FailCriticalErrors = 0x0001,

            /// <summary>
            /// The system automatically fixes memory alignment faults and makes them invisible to the application. It does
            /// this for the calling process and any descendant processes. This feature is only supported by certain processor
            /// architectures. For more information, see the Remarks section. After this value is set for a process, subsequent
            /// attempts to clear the value are ignored.
            /// </summary>
            NoAlignmentFaultExcept = 0x0004,

            /// <summary>
            /// The system does not display the Windows Error Reporting dialog.
            /// </summary>
            NoGpFaultErrorBox = 0x0002,

            /// <summary>
            /// The OpenFile function does not display a message box when it fails to find a file. Instead, the error is returned
            /// to the caller. This error mode overrides the OF_PROMPT flag.
            /// </summary>
            NoOpenFileErrorBox = 0x8000,
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
        /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-freelibrary.
        /// Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count. When the reference
        /// count reaches zero, the module is unloaded from the address space of the calling process and the handle is no longer
        /// valid.
        /// </summary>
        /// <param name="hModule">
        /// A handle to the loaded library module. The LoadLibrary, LoadLibraryEx, GetModuleHandle, or GetModuleHandleEx function
        /// returns this handle.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended
        /// error information, call the GetLastError function.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulefilenamew.
        /// Retrieves the fully qualified path for the file that contains the specified module. The module must have been loaded
        /// by the current process. To locate the file for a module that was loaded by another process, use the GetModuleFileNameEx
        /// function.
        /// </summary>
        /// <param name="hModule">
        /// A handle to the loaded module whose path is being requested. If this parameter is NULL, GetModuleFileName retrieves
        /// the path of the executable file of the current process. The GetModuleFileName function does not retrieve the path
        /// for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag. For more information, see LoadLibraryEx.
        /// </param>
        /// <param name="lpFilename">
        /// A pointer to a buffer that receives the fully qualified path of the module. If the length of the path is less than
        /// the size that the nSize parameter specifies, the function succeeds and the path is returned as a null-terminated
        /// string. If the length of the path exceeds the size that the nSize parameter specifies, the function succeeds and
        /// the string is truncated to nSize characters including the terminating null character.
        /// </param>
        /// <param name="nSize">
        /// The size of the lpFilename buffer, in TCHARs.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is the length of the string that is copied to the buffer, in characters,
        /// not including the terminating null character. If the buffer is too small to hold the module name, the string is
        /// truncated to nSize characters including the terminating null character, the function returns nSize, and the function
        /// sets the last error to ERROR_INSUFFICIENT_BUFFER. If nSize is zero, the return value is zero and the last error
        /// code is ERROR_SUCCESS. If the function fails, the return value is 0 (zero). To get extended error information, call
        /// GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileNameW(
            [In] IntPtr hModule,
            [Out] StringBuilder lpFilename,
            [In][MarshalAs(UnmanagedType.U4)] int nSize);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-getmodulehandlew.
        /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process. To
        /// avoid the race conditions described in the Remarks section, use the GetModuleHandleEx function.
        /// </summary>
        /// <param name="lpModuleName">
        /// The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default
        /// library extension .dll is appended. The file name string can include a trailing point character (.) to indicate
        /// that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure
        /// to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules
        /// currently mapped into the address space of the calling process. If this parameter is NULL, GetModuleHandle returns
        /// a handle to the file used to create the calling process (.exe file). The GetModuleHandle function does not retrieve
        /// handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.For more information, see LoadLibraryEx.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the specified module. If the function fails, the return
        /// value is NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetModuleHandleW(string lpModuleName);

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
        [SuppressMessage("Globalization", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "Ansi only")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryw.
        /// Loads the specified module into the address space of the calling process. The specified module may cause other modules
        /// to be loaded. For additional load options, use the LoadLibraryEx function.
        /// </summary>
        /// <param name="lpFileName">
        /// The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file).
        /// The name specified is the file name of the module and is not related to the name stored in the library module itself,
        /// as specified by the LIBRARY keyword in the module-definition (.def) file. If the string specifies a full path, the
        /// function searches only that path for the module. If the string specifies a relative path or a module name without
        /// a path, the function uses a standard search strategy to find the module; for more information, see the Remarks.
        /// If the function cannot find the module, the function fails.When specifying a path, be sure to use backslashes (\),
        /// not forward slashes(/). For more information about paths, see Naming a File or Directory. If the string specifies
        /// a module name without a path and the file name extension is omitted, the function appends the default library extension
        /// .dll to the module name. To prevent the function from appending .dll to the module name, include a trailing point
        /// character (.) in the module name string.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is a handle to the module. If the function fails, the return value is
        /// NULL.To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        /// <summary>
        /// ReadProcessMemory copies the data in the specified address range from the address space of the specified process
        /// into the specified buffer of the current process. Any process that has a handle with PROCESS_VM_READ access can
        /// call the function. The entire area to be read must be accessible, and if it is not accessible, the function fails.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.
        /// </param>
        /// <param name="lpBaseAddress">
        /// A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the
        /// system verifies that all data in the base address and memory of the specified size is accessible for read access,
        /// and if it is not accessible the function fails.
        /// </param>
        /// <param name="lpBuffer">
        /// A pointer to a buffer that receives the contents from the address space of the specified process.
        /// </param>
        /// <param name="dwSize">
        /// The number of bytes to be read from the specified process.
        /// </param>
        /// <param name="lpNumberOfBytesRead">
        /// A pointer to a variable that receives the number of bytes transferred into the specified buffer. If lpNumberOfBytesRead
        /// is NULL, the parameter is ignored.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
        /// extended error information, call GetLastError. The function fails if the requested read operation crosses into an
        /// area of the process that is inaccessible.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// ReadProcessMemory copies the data in the specified address range from the address space of the specified process
        /// into the specified buffer of the current process. Any process that has a handle with PROCESS_VM_READ access can
        /// call the function. The entire area to be read must be accessible, and if it is not accessible, the function fails.
        /// </summary>
        /// <param name="hProcess">
        /// A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.
        /// </param>
        /// <param name="lpBaseAddress">
        /// A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the
        /// system verifies that all data in the base address and memory of the specified size is accessible for read access,
        /// and if it is not accessible the function fails.
        /// </param>
        /// <param name="lpBuffer">
        /// A pointer to a buffer that receives the contents from the address space of the specified process.
        /// </param>
        /// <param name="dwSize">
        /// The number of bytes to be read from the specified process.
        /// </param>
        /// <param name="lpNumberOfBytesRead">
        /// A pointer to a variable that receives the number of bytes transferred into the specified buffer. If lpNumberOfBytesRead
        /// is NULL, the parameter is ignored.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
        /// extended error information, call GetLastError. The function fails if the requested read operation crosses into an
        /// area of the process that is inaccessible.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-seterrormode.
        /// Controls whether the system will handle the specified types of serious errors or whether the process will handle
        /// them.
        /// </summary>
        /// <param name="uMode">
        /// The process error mode. This parameter can be one or more of the ErrorMode enum values.
        /// </param>
        /// <returns>
        /// The return value is the previous state of the error-mode bit flags.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern ErrorModes SetErrorMode(ErrorModes uMode);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/errhandlingapi/nf-errhandlingapi-setunhandledexceptionfilter.
        /// Enables an application to supersede the top-level exception handler of each thread of a process. After calling this
        /// function, if an exception occurs in a process that is not being debugged, and the exception makes it to the unhandled
        /// exception filter, that filter will call the exception filter function specified by the lpTopLevelExceptionFilter
        /// parameter.
        /// </summary>
        /// <param name="lpTopLevelExceptionFilter">
        /// A pointer to a top-level exception filter function that will be called whenever the UnhandledExceptionFilter function
        /// gets control, and the process is not being debugged. A value of NULL for this parameter specifies default handling
        /// within UnhandledExceptionFilter. The filter function has syntax similar to that of UnhandledExceptionFilter: It
        /// takes a single parameter of type LPEXCEPTION_POINTERS, has a WINAPI calling convention, and returns a value of type
        /// LONG. The filter function should return one of the EXCEPTION_* enum values.
        /// </param>
        /// <returns>
        /// The SetUnhandledExceptionFilter function returns the address of the previous exception filter established with the
        /// function. A NULL return value means that there is no current top-level exception handler.
        /// </returns>
        [DllImport("kernel32.dll")]
        public static extern IntPtr SetUnhandledExceptionFilter(IntPtr lpTopLevelExceptionFilter);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc.
        /// Reserves, commits, or changes the state of a region of pages in the virtual address space of the calling process.
        /// Memory allocated by this function is automatically initialized to zero. To allocate memory in the address space
        /// of another process, use the VirtualAllocEx function.
        /// </summary>
        /// <param name="lpAddress">
        /// The starting address of the region to allocate. If the memory is being reserved, the specified address is rounded
        /// down to the nearest multiple of the allocation granularity. If the memory is already reserved and is being committed,
        /// the address is rounded down to the next page boundary. To determine the size of a page and the allocation granularity
        /// on the host computer, use the GetSystemInfo function. If this parameter is NULL, the system determines where to
        /// allocate the region. If this address is within an enclave that you have not initialized by calling InitializeEnclave,
        /// VirtualAlloc allocates a page of zeros for the enclave at that address. The page must be previously uncommitted,
        /// and will not be measured with the EEXTEND instruction of the Intel Software Guard Extensions programming model.
        /// If the address in within an enclave that you initialized, then the allocation operation fails with the
        /// ERROR_INVALID_ADDRESS error.
        /// </param>
        /// <param name="dwSize">
        /// The size of the region, in bytes. If the lpAddress parameter is NULL, this value is rounded up to the next page
        /// boundary. Otherwise, the allocated pages include all pages containing one or more bytes in the range from lpAddress
        /// to lpAddress+dwSize. This means that a 2-byte range straddling a page boundary causes both pages to be included
        /// in the allocated region.
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
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        /// <inheritdoc cref="VirtualAlloc(IntPtr, UIntPtr, AllocationType, MemoryProtection)"/>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            AllocationType flAllocationType,
            Memory.MemoryProtection flProtect);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualfree.
        /// Releases, decommits, or releases and decommits a region of pages within the virtual address space of the calling
        /// process.
        /// process.
        /// </summary>
        /// <param name="lpAddress">
        /// A pointer to the base address of the region of pages to be freed. If the dwFreeType parameter is MEM_RELEASE, this
        /// parameter must be the base address returned by the VirtualAlloc function when the region of pages is reserved.
        /// </param>
        /// <param name="dwSize">
        /// The size of the region of memory to be freed, in bytes. If the dwFreeType parameter is MEM_RELEASE, this parameter
        /// must be 0 (zero). The function frees the entire region that is reserved in the initial allocation call to VirtualAlloc.
        /// If the dwFreeType parameter is MEM_DECOMMIT, the function decommits all memory pages that contain one or more bytes
        /// in the range from the lpAddress parameter to (lpAddress+dwSize). This means, for example, that a 2-byte region of
        /// memory that straddles a page boundary causes both pages to be decommitted.If lpAddress is the base address returned
        /// by VirtualAlloc and dwSize is 0 (zero), the function decommits the entire region that is allocated by VirtualAlloc.
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
        public static extern bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            AllocationType dwFreeType);

        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualprotect.
        /// Changes the protection on a region of committed pages in the virtual address space of the calling process.
        /// </summary>
        /// <param name="lpAddress">
        /// The address of the starting page of the region of pages whose access protection attributes are to be changed. All
        /// pages in the specified region must be within the same reserved region allocated when calling the VirtualAlloc or
        /// VirtualAllocEx function using MEM_RESERVE. The pages cannot span adjacent reserved regions that were allocated by
        /// separate calls to VirtualAlloc or VirtualAllocEx using MEM_RESERVE.
        /// </param>
        /// <param name="dwSize">
        /// The size of the region whose access protection attributes are to be changed, in bytes. The region of affected pages
        /// includes all pages containing one or more bytes in the range from the lpAddress parameter to (lpAddress+dwSize).
        /// This means that a 2-byte range straddling a page boundary causes the protection attributes of both pages to be changed.
        /// </param>
        /// <param name="flNewProtection">
        /// The memory protection option. This parameter can be one of the memory protection constants. For mapped views, this
        /// value must be compatible with the access protection specified when the view was mapped (see MapViewOfFile,
        /// MapViewOfFileEx, and MapViewOfFileExNuma).
        /// </param>
        /// <param name="lpflOldProtect">
        /// A pointer to a variable that receives the previous access protection value of the first page in the specified region
        /// of pages. If this parameter is NULL or does not point to a valid variable, the function fails.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is zero.
        /// To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualProtect(
            IntPtr lpAddress,
            UIntPtr dwSize,
            MemoryProtection flNewProtection,
            out MemoryProtection lpflOldProtect);

        /// <inheritdoc cref="VirtualAlloc(IntPtr, UIntPtr, AllocationType, MemoryProtection)"/>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualProtect(
            IntPtr lpAddress,
            UIntPtr dwSize,
            Memory.MemoryProtection flNewProtection,
            out Memory.MemoryProtection lpflOldProtect);

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

    /// <summary>
    /// Native ws2_32 functions.
    /// </summary>
    internal static partial class NativeFunctions
    {
        /// <summary>
        /// See https://docs.microsoft.com/en-us/windows/win32/api/winsock/nf-winsock-setsockopt.
        /// The setsockopt function sets a socket option.
        /// </summary>
        /// <param name="socket">
        /// A descriptor that identifies a socket.
        /// </param>
        /// <param name="level">
        /// The level at which the option is defined (for example, SOL_SOCKET).
        /// </param>
        /// <param name="optName">
        /// The socket option for which the value is to be set (for example, SO_BROADCAST). The optname parameter must be a
        /// socket option defined within the specified level, or behavior is undefined.
        /// </param>
        /// <param name="optVal">
        /// A pointer to the buffer in which the value for the requested option is specified.
        /// </param>
        /// <param name="optLen">
        /// The size, in bytes, of the buffer pointed to by the optval parameter.
        /// </param>
        /// <returns>
        /// If no error occurs, setsockopt returns zero. Otherwise, a value of SOCKET_ERROR is returned, and a specific error
        /// code can be retrieved by calling WSAGetLastError.
        /// </returns>
        [DllImport("ws2_32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "setsockopt")]
        public static extern int SetSockOpt(IntPtr socket, SocketOptionLevel level, SocketOptionName optName, ref IntPtr optVal, int optLen);
    }
}

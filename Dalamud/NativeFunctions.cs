using System;
using System.Diagnostics;
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
            Ok = 0x0,

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
            IconWarning = 0x30,

            /// <summary>
            /// An icon consisting of a lowercase letter i in a circle appears in the message box.
            /// </summary>
            IconInformation = 0x40,

            /// <summary>
            /// An icon consisting of a lowercase letter i in a circle appears in the message box.
            /// </summary>
            IconAsterisk = 0x40,

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
            IconError = 0x10,

            /// <summary>
            /// A stop-sign icon appears in the message box.
            /// </summary>
            IconHand = 0x10,

            // To indicate the default button, specify one of the following values.

            /// <summary>
            /// The first button is the default button.
            /// MB_DEFBUTTON1 is the default unless MB_DEFBUTTON2, MB_DEFBUTTON3, or MB_DEFBUTTON4 is specified.
            /// </summary>
            DefButton1 = 0x0,

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
            ApplModal = 0x0,

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
            {
                return false; // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            GetWindowThreadProcessId(activatedHandle, out var activeProcId);

            return activeProcId == procId;
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
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int MessageBox(IntPtr hWnd, string text, string caption, MessageBoxType type);

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
            public uint cbSize;

            /// <summary>
            /// A handle to the window to be flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr hwnd;

            /// <summary>
            /// The flash status. This parameter can be one or more of the FlashWindow enum values.
            /// </summary>
            public FlashWindow dwFlags;

            /// <summary>
            /// The number of times to flash the window.
            /// </summary>
            public uint uCount;

            /// <summary>
            /// The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the
            /// default cursor blink rate.
            /// </summary>
            public uint dwTimeout;
        }
    }

    /// <summary>
    /// Native kernel32 functions.
    /// </summary>
    internal static partial class NativeFunctions
    {
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
        /// See https://docs.microsoft.com/en-us/windows/win32/api/debugapi/nf-debugapi-debugactiveprocess.
        /// Enables a debugger to attach to an active process and debug it.
        /// </summary>
        /// <param name="dwProcessId">
        /// The identifier for the process to be debugged. The debugger is granted debugging access to the process as if it
        /// created the process with the DEBUG_ONLY_THIS_PROCESS flag. For more information, see the Remarks section of this
        /// topic.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. If the function fails, the return value is 0 (zero). To get
        /// extended error information, call GetLastError.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DebugActiveProcess(uint dwProcessId);

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
        [DllImport("kernel32.dll", SetLastError = true)]
        [PreserveSig]
        public static extern uint GetModuleFileName(
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
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

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
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

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
    }

    /// <summary>
    /// Native ws2_32 functions.
    /// </summary>
    internal static partial class NativeFunctions
    {
        /// <summary>
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

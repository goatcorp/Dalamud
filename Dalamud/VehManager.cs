using System.Runtime.InteropServices;

namespace Dalamud
{
    /// <summary>
    /// Class encapsulating a vectored exception handler.
    /// </summary>
    internal unsafe class VehManager
    {
        /// <summary>
        /// Execute the exception handler.
        /// </summary>
        public const int ExceptionExecuteHandler = 1;

        /// <summary>
        /// Continue the search for another handler.
        /// </summary>
        public const int ExceptionContinueSearch = 0;

        /// <summary>
        /// Continue execution after the handler.
        /// </summary>
        public const int ExceptionContinueExecution = -1;

        private VectoredExceptionHandler? myHandler; // to keep a reference, just in case, idk if it's needed

        /// <summary>
        /// Initializes a new instance of the <see cref="VehManager"/> class.
        /// </summary>
        /// <param name="first">Position of this VEH. 0 = Last, 1 = First.</param>
        /// <param name="myHandler">The handler to register.</param>
        public VehManager(uint first, VectoredExceptionHandler myHandler)
        {
            this.myHandler = myHandler;
            this.Handle = AddVectoredExceptionHandler(first, this.myHandler);
        }

        /// <summary>
        /// VEH Delegate.
        /// </summary>
        /// <param name="ex">Exception information.</param>
        /// <returns>Code that determines which action to take.</returns>
        public delegate int VectoredExceptionHandler(ref ExceptionPointers ex);

        /// <summary>
        /// Gets the handle to the VEH.
        /// </summary>
        public nint Handle { get; private set; }

        /// <summary>
        /// Dispose and remove this VEH.
        /// </summary>
        public void Dispose()
        {
            if (this.Handle == 0)
                return;

            if (!RemoveVectoredExceptionHandler(this.Handle))
                return;

            this.Handle = 0;
            this.myHandler = null;
        }

        #region DllImports

        [DllImport("kernel32", SetLastError = true)]
        private static extern nint AddVectoredExceptionHandler(uint first, VectoredExceptionHandler handler);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool RemoveVectoredExceptionHandler(nint handle);

        #endregion

#pragma warning disable SA1600
#pragma warning disable SA1602
#pragma warning disable SA1201

        #region Enums

        public enum ExceptionCode : uint
        {
            AccessViolation = 0xC0000005,
            InPageError = 0xC0000006,
            InvalidHandle = 0xC0000008,
            InvalidParameter = 0xC000000D,
            NoMemory = 0xC0000017,
            IllegalInstruction = 0xC000001D,
            NoncontinuableException = 0xC0000025,
            InvalidDisposition = 0xC0000026,
            ArrayBoundsExceeded = 0xC000008C,
            FloatDenormalOperand = 0xC000008D,
            FloatDivideByZero = 0xC000008E,
            FloatInexactResult = 0xC000008F,
            FloatInvalidOperation = 0xC0000090,
            FloatOverflow = 0xC0000091,
            FloatStackCheck = 0xC0000092,
            FloatUnderflow = 0xC0000093,
            IntegerDivideByZero = 0xC0000094,
            IntegerOverflow = 0xC0000095,
            PrivilegedInstruction = 0xC0000096,
            StackOverflow = 0xC00000FD,
            DllNotFound = 0xC0000135,
            OrdinalNotFound = 0xC0000138,
            EntrypointNotFound = 0xC0000139,
            ControlCExit = 0xC000013A,
            DllInitFailed = 0xC0000142,
            ControlStackViolation = 0xC00001B2,
            FloatMultipleFaults = 0xC00002B4,
            FloatMultipleTraps = 0xC00002B5,
            RegNatConsumption = 0xC00002C9,
            HeapCorruption = 0xC0000374,
            StackBufferOverrun = 0xC0000409,
            InvalidCruntimeParameter = 0xC0000417,
            AssertionFailure = 0xC0000420,
            EnclaveViolation = 0xC00004A2,
            Interrupted = 0xC0000515,
            ThreadNotRunning = 0xC0000516,
            AlreadyRegistered = 0xC0000718,
        }

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct ExceptionPointers
        {
            public ExceptionRecord64* ExceptionRecord;
            public Context* ContextRecord;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ExceptionRecord64
        {
            public uint ExceptionCode;
            public uint ExceptionFlags;
            public nint ExceptionRecord;
            public nint ExceptionAddress;
            public uint NumberParameters;
            public uint UnusedAlignment;
            public fixed ulong ExceptionInformation[15];
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Context
        {
            // Register parameter home addresses.
            //
            // N.B. These fields are for convience - they could be used to extend the
            //      context record in the future.

            public nint P1Home;
            public nint P2Home;
            public nint P3Home;
            public nint P4Home;
            public nint P5Home;
            public nint P6Home;

            // Control flags.

            public uint ContextFlags;
            public uint MxCsr;

            // Segment Registers and processor flags.

            public ushort SegCs;
            public ushort SegDs;
            public ushort SegEs;
            public ushort SegFs;
            public ushort SegGs;
            public ushort SegSs;
            public uint EFlags;

            // Debug registers

            public nint Dr0;
            public nint Dr1;
            public nint Dr2;
            public nint Dr3;
            public nint Dr6;
            public nint Dr7;

            // Integer registers.

            public nint Rax;
            public nint Rcx;
            public nint Rdx;
            public nint Rbx;
            public nint Rsp;
            public nint Rbp;
            public nint Rsi;
            public nint Rdi;
            public nint R8;
            public nint R9;
            public nint R10;
            public nint R11;
            public nint R12;
            public nint R13;
            public nint R14;
            public nint R15;

            // Program counter.

            public nint Rip;

            // Floating point state.

            public XmmRegisters FloatRegisters;

            // Vector registers.

            public VectorRegisters VectorRegister;
            public nint VectorControl;

            // Special debug control registers.

            public nint DebugControl;
            public nint LastBranchToRip;
            public nint LastBranchFromRip;
            public nint LastExceptionToRip;

            public nint LastExceptionFromRip;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct M128A
        {
            public ulong Low;
            public long High;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VectorRegisters
        {
            public M128A V0;
            public M128A V1;
            public M128A V2;
            public M128A V3;
            public M128A V4;
            public M128A V5;
            public M128A V6;
            public M128A V7;
            public M128A V8;
            public M128A V9;
            public M128A V10;
            public M128A V11;
            public M128A V12;
            public M128A V13;
            public M128A V14;
            public M128A V15;
            public M128A V16;
            public M128A V17;
            public M128A V18;
            public M128A V19;
            public M128A V20;
            public M128A V21;
            public M128A V22;
            public M128A V23;
            public M128A V24;
            public M128A V25;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XmmRegisters
        {
            public readonly M128A Header0;
            public readonly M128A Header1;

            public M128A Float0;
            public M128A Float1;
            public M128A Float2;
            public M128A Float3;
            public M128A Float4;
            public M128A Float5;
            public M128A Float6;
            public M128A Float7;

            public M128A Xmm0;
            public M128A Xmm1;
            public M128A Xmm2;
            public M128A Xmm3;
            public M128A Xmm4;
            public M128A Xmm5;
            public M128A Xmm6;
            public M128A Xmm7;
            public M128A Xmm8;
            public M128A Xmm9;
            public M128A Xmm10;
            public M128A Xmm11;
            public M128A Xmm12;
            public M128A Xmm13;
            public M128A Xmm14;
            public M128A Xmm15;
        }

        #endregion

#pragma warning restore SA1600
#pragma warning restore SA1602
#pragma warning restore SA1201
    }
}

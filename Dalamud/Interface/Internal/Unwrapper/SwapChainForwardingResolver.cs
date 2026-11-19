using Iced.Intel;

namespace Dalamud.Interface.Internal.Unwrapper;

/// <summary>Resolves receiver pointer paths in straight-line x64 COM GetDesc forwarders.</summary>
/// <remarks>Unsupported instructions and pointer paths cause rejection.</remarks>
internal static class SwapChainForwardingResolver
{
    /// <summary>The maximum number of function bytes considered during forwarding analysis.</summary>
    internal const int MaximumCodeLength = 128;

    private const int MaximumInstructions = 64;
    private const int MaximumDepth = 4;
    private const int MaximumDisplacement = 0x10000;
    private const int GetDescDisplacement = 12 * sizeof(ulong);

    private enum ValueKind
    {
        Unknown,
        Object,
        VTable,
        Descriptor,
        Stack,
    }

    /// <summary>Derives pointer fields used by a supported forwarding implementation.</summary>
    /// <param name="code">The function's initial bytes.</param>
    /// <param name="offsets">The receiver's pointer-field path when recognition succeeds.</param>
    /// <returns>Whether a supported GetDesc dispatch is reached.</returns>
    internal static bool TryResolve(ReadOnlySpan<byte> code, out int[] offsets)
    {
        offsets = [];
        var reader = new ByteArrayCodeReader(code[..Math.Min(code.Length, MaximumCodeLength)].ToArray());
        var decoder = Decoder.Create(64, reader);
        var infoFactory = new InstructionInfoFactory();

        // Track the x64 inputs: RCX is the receiver, RDX the output descriptor, and RSP the stack.
        var registers = new RegisterValue[16];
        registers[RegisterIndex(Register.RCX)] = new(ValueKind.Object, []);
        registers[RegisterIndex(Register.RDX)] = new(ValueKind.Descriptor);
        registers[RegisterIndex(Register.RSP)] = new(ValueKind.Stack);

        for (var count = 0; count < MaximumInstructions && reader.CanReadByte; count++)
        {
            decoder.Decode(out var instruction);
            if (instruction.Code == Code.INVALID || instruction.HasLockPrefix || instruction.HasRepPrefix ||
                instruction.HasRepnePrefix || instruction.SegmentPrefix != Register.None)
                return false;

            // A GetDesc call or tail jump must preserve the output argument and use the forwarded receiver's vtable.
            if (instruction.FlowControl is FlowControl.IndirectCall or FlowControl.IndirectBranch)
            {
                if (registers[RegisterIndex(Register.RSP)].Kind != ValueKind.Stack ||
                    registers[RegisterIndex(Register.RDX)].Kind != ValueKind.Descriptor ||
                    instruction.Op0Kind != OpKind.Memory || !HasSimpleAddress(in instruction) ||
                    instruction.MemorySize.GetSize() != sizeof(ulong) ||
                    instruction.MemoryDisplacement64 != GetDescDisplacement)
                    return false;

                var table = registers[RegisterIndex(instruction.MemoryBase)];
                var receiver = registers[RegisterIndex(Register.RCX)];
                if (table.Kind != ValueKind.VTable || receiver.Kind != ValueKind.Object ||
                    receiver.Path is not { Length: > 0 } || !receiver.Path.AsSpan().SequenceEqual(table.Path))
                    return false;

                offsets = receiver.Path;
                return true;
            }

            if (instruction.FlowControl != FlowControl.Next || !TryApply(in instruction, infoFactory, registers))
                return false;
        }

        return false;
    }

    private static bool TryApply(in Instruction instruction, InstructionInfoFactory infoFactory, RegisterValue[] registers)
    {
        // Evaluate assignments from the register state before this instruction.
        var destination = -1;
        var value = default(RegisterValue);
        switch (instruction.Mnemonic)
        {
            case Mnemonic.Nop:
                break;

            case Mnemonic.Mov:
                if (instruction.Op0Kind == OpKind.Memory)
                {
                    if (!IsStackMemory(in instruction, registers))
                        return false;
                }
                else if (instruction.Op0Kind == OpKind.Register)
                {
                    destination = RegisterIndex(instruction.Op0Register.GetFullRegister());
                    if (destination < 0)
                        return false;

                    if (instruction.Op1Kind == OpKind.Memory)
                    {
                        if (!HasSimpleAddress(in instruction))
                            return false;

                        if (instruction.Op0Register.GetSize() == sizeof(ulong) && instruction.MemorySize.GetSize() == sizeof(ulong))
                        {
                            var source = registers[RegisterIndex(instruction.MemoryBase)];
                            if (source.Kind == ValueKind.Object)
                            {
                                var displacement = unchecked((long)instruction.MemoryDisplacement64);
                                // Offset zero loads a vtable; positive aligned offsets extend the receiver path.
                                if (displacement == 0)
                                {
                                    value = new(ValueKind.VTable, source.Path);
                                }
                                else
                                {
                                    if (displacement < 0 || displacement > MaximumDisplacement ||
                                        displacement % sizeof(ulong) != 0 || source.Path!.Length >= MaximumDepth)
                                        return false;

                                    var path = new int[source.Path.Length + 1];
                                    source.Path.CopyTo(path, 0);
                                    path[^1] = (int)displacement;
                                    value = new(ValueKind.Object, path);
                                }
                            }
                        }
                    }
                    else if (instruction.Op1Kind == OpKind.Register)
                    {
                        if (RegisterIndex(instruction.Op1Register.GetFullRegister()) < 0)
                            return false;

                        if (instruction.Op0Register.GetSize() == sizeof(ulong) && instruction.Op1Register.GetSize() == sizeof(ulong))
                        {
                            var sourceIndex = RegisterIndex(instruction.Op1Register);
                            if (sourceIndex >= 0)
                                value = registers[sourceIndex];
                        }
                    }
                    else if (!IsImmediate(instruction.Op1Kind))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                break;

            case Mnemonic.Lea:
                if (instruction.Op0Kind != OpKind.Register || instruction.Op0Register.GetSize() != sizeof(ulong) ||
                    instruction.Op1Kind != OpKind.Memory || !IsStackMemory(in instruction, registers))
                    return false;

                destination = RegisterIndex(instruction.Op0Register);
                value = new(ValueKind.Stack);
                break;

            case Mnemonic.Push:
            case Mnemonic.Pop:
                if (instruction.Op0Kind != OpKind.Register || instruction.Op0Register.GetSize() != sizeof(ulong) ||
                    RegisterIndex(instruction.Op0Register) < 0 || instruction.Op0Register == Register.RSP ||
                    registers[RegisterIndex(Register.RSP)].Kind != ValueKind.Stack)
                    return false;

                // Stack contents are not tracked, so popped values remain unknown.
                destination = RegisterIndex(Register.RSP);
                value = new(ValueKind.Stack);
                break;

            case Mnemonic.Add:
            case Mnemonic.Sub:
            case Mnemonic.And:
            case Mnemonic.Or:
            case Mnemonic.Xor:
                if (!HasRegisterOperands(in instruction))
                    return false;

                if (instruction.Op0Register == Register.RSP && (instruction.Mnemonic is Mnemonic.Add or Mnemonic.Sub) &&
                    IsImmediate(instruction.Op1Kind) && instruction.GetImmediate(1) <= MaximumDisplacement &&
                    registers[RegisterIndex(Register.RSP)].Kind == ValueKind.Stack)
                {
                    destination = RegisterIndex(Register.RSP);
                    value = new(ValueKind.Stack);
                }

                break;

            case Mnemonic.Cmp:
            case Mnemonic.Test:
                if (!HasRegisterOperands(in instruction))
                    return false;

                break;

            default:
                return false;
        }

        // Invalidate explicit and implicit writes, then restore only the proven assignment.
        var info = infoFactory.GetInfo(in instruction);
        foreach (var usedRegister in info.GetUsedRegisters())
        {
            if (usedRegister.Access is OpAccess.Write or OpAccess.CondWrite or OpAccess.ReadWrite or OpAccess.ReadCondWrite)
            {
                var index = RegisterIndex(usedRegister.Register.GetFullRegister());
                if (index >= 0)
                    registers[index] = default;
            }
        }

        if (destination >= 0)
            registers[destination] = value;

        // Forwarded calls and tail jumps require a valid stack.
        return registers[RegisterIndex(Register.RSP)].Kind == ValueKind.Stack;
    }

    private static bool HasRegisterOperands(in Instruction instruction) =>
        instruction.OpCount == 2 && instruction.Op0Kind == OpKind.Register &&
        RegisterIndex(instruction.Op0Register.GetFullRegister()) >= 0 &&
        ((instruction.Op1Kind == OpKind.Register && RegisterIndex(instruction.Op1Register.GetFullRegister()) >= 0) ||
         IsImmediate(instruction.Op1Kind));

    private static bool HasSimpleAddress(in Instruction instruction) =>
        instruction.MemoryIndex == Register.None && RegisterIndex(instruction.MemoryBase) >= 0;

    private static bool IsStackMemory(in Instruction instruction, RegisterValue[] registers) =>
        HasSimpleAddress(in instruction) &&
        registers[RegisterIndex(instruction.MemoryBase)].Kind == ValueKind.Stack &&
        unchecked((long)instruction.MemoryDisplacement64) is >= -MaximumDisplacement and <= MaximumDisplacement;

    private static bool IsImmediate(OpKind kind) => kind is
        OpKind.Immediate8 or OpKind.Immediate16 or OpKind.Immediate32 or OpKind.Immediate64 or
        OpKind.Immediate8to16 or OpKind.Immediate8to32 or OpKind.Immediate8to64 or OpKind.Immediate32to64;

    private static int RegisterIndex(Register register) =>
        register >= Register.RAX && register <= Register.R15 ? register - Register.RAX : -1;

    private readonly record struct RegisterValue(ValueKind Kind, int[]? Path = null);
}

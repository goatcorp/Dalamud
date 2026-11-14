using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Interface.Internal.Unwrapper;

using Xunit;

namespace Dalamud.Test.Interface;

public class NvPresentUnwrapperTests
{
    // Synthetic x64 GetDesc forwarders use vtable byte offset 0x60.
    private const string DirectForwarder = "488B4908488B01FF5060";

    public static IEnumerable<object[]> InvalidForwarders()
    {
        yield return Case("empty", string.Empty);
        yield return Case("no contained object", "488B01FF5060");
        yield return Case("wrong slot", "488B4908488B01FF5058");
        yield return Case("register call", "488B4908488B01FFD0");
        yield return Case("object used as vtable", "488B4908FF5160");
        yield return Case("vtable from different object", "488B4118488B00488B4908FF5060");
        yield return Case("receiver changed after loading vtable", "488B4908488B01488B4910FF5060");
        yield return Case("unused matching dispatch register", "488B4108488B10488BC1FF5260");
        yield return Case("output argument used as object origin", "488B4A08488B01FF5060");
        yield return Case("prior direct call", "E800000000" + DirectForwarder);
        yield return Case("prior indirect call", "FFD2" + DirectForwarder);
        yield return Case("conditional branch", "7500" + DirectForwarder);
        yield return Case("direct branch", "EB00" + DirectForwarder);
        yield return Case("return before dispatch", "C3" + DirectForwarder);
        yield return Case("exception before dispatch", "CC" + DirectForwarder);
        yield return Case("loop before dispatch", "E200" + DirectForwarder);
        yield return Case("negative field", "488B49F8488B01FF5060");
        yield return Case("misaligned field", "488B4901488B01FF5060");
        yield return Case("field above bound", "488B8908000100488B01FF5060");
        yield return Case("absolute field address", "488B0C2500100000488B01FF5060");
        yield return Case("instruction-relative field", "488B0D00000000488B01FF5060");
        yield return Case("indexed field", "488B4C1108488B01FF5060");
        yield return Case("address32 field", "67488B4908488B01FF5060");
        yield return Case("GS-relative field", "65488B4908488B01FF5060");
        yield return Case("FS-relative field", "64488B4908488B01FF5060");
        yield return Case("indexed dispatch", "488B4908488B01FF541060");
        yield return Case("address32 dispatch", "488B4908488B0167FF5060");
        yield return Case("GS-relative dispatch", "488B4908488B0165FF5060");
        yield return Case("write through object alias", "488BC1488B4908488B114C894008FF5260");
        yield return Case("write through vtable alias", "488B4908488B014C894060FF5060");
        yield return Case("nonstack memory arithmetic", "488B4908488B014883410801FF5060");
        yield return Case("object disguised as frame pointer", "488BE9488B4D08488B0148897508FF5060");
        yield return Case("object disguised as stack pointer", "488BE1488B4C2408488B014889742408FF5060");
        yield return Case("stack pointer arithmetic using object", "4803E1" + DirectForwarder);
        yield return Case("stack pointer exchange", "4887CC" + DirectForwarder);
        yield return Case("bytes resembling dispatch inside immediate", "48B80000488B01FF5060");
        yield return Case("bytes resembling field load inside immediate", "48B8488B490800000000488B01FF5060");
    }

    public static IEnumerable<object[]> ClobberedForwarders()
    {
        const string receiverAndVtable = "488B4908488B01";

        foreach (var (name, instruction) in new[]
                 {
                     ("ECX write", "B900000000"),
                     ("CX write", "66B90000"),
                     ("CL write", "B100"),
                     ("CH write", "B500"),
                     ("RCX immediate", "48B90000000000000000"),
                     ("RDX immediate", "48BA0000000000000000"),
                     ("EDX write", "BA00000000"),
                     ("DX write", "66BA0000"),
                     ("DL write", "B200"),
                     ("DH write", "B600"),
                     ("output argument zeroed", "33D2"),
                     ("EAX write", "B800000000"),
                     ("AX write", "66B80000"),
                     ("AL write", "B000"),
                     ("AH write", "B400"),
                     ("RCX arithmetic", "4883C108"),
                     ("vtable arithmetic", "4883C008"),
                     ("receiver exchange", "4887C8"),
                     ("implicit multiply", "48F7E3"),
                     ("implicit divide", "48F7F3"),
                     ("CPUID", "0FA2"),
                     ("RDTSC", "0F31"),
                     ("RDTSCP", "0F01F9"),
                     ("pop receiver", "59"),
                     ("pop vtable", "58"),
                 })
        {
            yield return Case(name, receiverAndVtable + instruction + "FF5060");
        }

        // CQO writes RDX implicitly, although it has no explicit register operands.
        yield return Case("implicit RDX write", "488B4908488B114899FF5260");
    }

    [Fact]
    public void SyntheticNestedForwarderResolvesReportedPath()
    {
        // mov rax,[rcx+18h]; mov rcx,[rax+1588h]; mov rax,[rcx]; call [rax+60h]
        AssertOffsets("488B4118488B8888150000488B01FF5060", 0x18, 0x1588);
    }

    [Theory]
    [InlineData("488B4908488B01FF5060", 8)]
    [InlineData("488B4928488B01FF5060", 0x28)]
    [InlineData("488B8900000100488B01FF5060", 0x10000)]
    [InlineData("488B4940488B01FF6060", 0x40)]
    public void ResolvesDifferentFieldOffsetsAndTailDispatch(string code, int expectedOffset)
    {
        AssertOffsets(code, expectedOffset);
    }

    [Fact]
    public void ResolvesDifferentNestedLayoutThroughRegisterAliases()
    {
        // r8 = [rcx+28h]; r9 = [r8+40h]; rcx = r9; rax = [r9]; call [rax+60h]
        AssertOffsets("4C8B41284D8B48404C89C9498B01FF5060", 0x28, 0x40);
    }

    [Fact]
    public void PreservesPointerCapturedBeforeAnotherRegisterAliasIsOverwritten()
    {
        // r8 = [rcx+8]; ecx = 1; rcx = r8; rax = [rcx]; call [rax+60h]
        AssertOffsets("4C8B4108B9010000004C89C1488B01FF5060", 8);
    }

    [Fact]
    public void PreservesOutputArgumentSavedAndRestoredThroughARegister()
    {
        // r9 = rdx; edx = 1; rcx = [rcx+8]; rax = [rcx]; rdx = r9; call [rax+60h]
        AssertOffsets("4C8BCABA01000000488B4908488B014C89CAFF5060", 8);
    }

    [Fact]
    public void AllowsOrdinaryStackPrologueWithoutTreatingSpillsAsObjectFields()
    {
        // push rbx; sub rsp,20h; mov [rsp+18h],rdx; rbx = rcx; then forward.
        AssertOffsets("534883EC204889542418488BD9488B4B08488B01FF5060", 8);
    }

    [Theory]
    [MemberData(nameof(InvalidForwarders))]
    [MemberData(nameof(ClobberedForwarders))]
    public void RejectsUnprovenDispatch(string scenario, string code)
    {
        var result = NvPresentUnwrapper.TryGetSwapChainOffsets(Convert.FromHexString(code), out var offsets);

        Assert.False(result, scenario);
        Assert.Empty(offsets);
    }

    [Fact]
    public void RejectsEveryTruncatedPrefixOfAForwarder()
    {
        var code = Convert.FromHexString("488B4118488B8888150000488B01FF5060");
        for (var length = 0; length < code.Length; length++)
        {
            Assert.False(NvPresentUnwrapper.TryGetSwapChainOffsets(code.AsSpan(0, length), out var offsets));
            Assert.Empty(offsets);
        }
    }

    [Fact]
    public void LimitsPointerDepth()
    {
        AssertOffsets(string.Concat(Enumerable.Repeat("488B4908", 4)) + "488B01FF5060", 8, 8, 8, 8);

        var code = Convert.FromHexString(string.Concat(Enumerable.Repeat("488B4908", 5)) + "488B01FF5060");
        Assert.False(NvPresentUnwrapper.TryGetSwapChainOffsets(code, out var offsets));
        Assert.Empty(offsets);
    }

    [Fact]
    public void LimitsInstructionCount()
    {
        AssertOffsets(string.Concat(Enumerable.Repeat("90", 61)) + DirectForwarder, 8);

        var code = Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 62)) + DirectForwarder);
        Assert.False(NvPresentUnwrapper.TryGetSwapChainOffsets(code, out var offsets));
        Assert.Empty(offsets);
    }

    [Fact]
    public void DoesNotDecodeDispatchBeyondCodeWindow()
    {
        // Seven-byte NOPs keep both fixtures below the independent instruction-count limit.
        const string longNop = "0F1F8000000000";
        var padding = string.Concat(Enumerable.Repeat(longNop, 16))
                      + string.Concat(Enumerable.Repeat("90", 6));
        AssertOffsets(padding + DirectForwarder, 8);

        padding = string.Concat(Enumerable.Repeat(longNop, 17)) + "90";
        var code = Convert.FromHexString(padding + DirectForwarder);

        Assert.False(NvPresentUnwrapper.TryGetSwapChainOffsets(code, out var offsets));
        Assert.Empty(offsets);
    }

    private static object[] Case(string name, string code) => [name, code];

    private static void AssertOffsets(string code, params int[] expected)
    {
        Assert.True(NvPresentUnwrapper.TryGetSwapChainOffsets(Convert.FromHexString(code), out var offsets));
        Assert.Equal(expected, offsets);
    }
}

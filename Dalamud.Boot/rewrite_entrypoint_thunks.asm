PUBLIC EntryPointReplacement
PUBLIC RewrittenEntryPoint_Standalone
PUBLIC RewrittenEntryPoint

; 06 and 07 are invalid opcodes
; CC is int3 = bp
; using 0CCCCCCCCCCCCCCCCh as function terminator
; using 00606060606060606h as placeholders

TERMINATOR = 0CCCCCCCCCCCCCCCCh
PLACEHOLDER = 00606060606060606h

.code

EntryPointReplacement PROC
    start:
        ; stack is aligned to 0x10 and then +8
        lea rax, [start]
        push rax
        mov rax, PLACEHOLDER
        jmp rax

    dq TERMINATOR
EntryPointReplacement ENDP

RewrittenEntryPoint_Standalone PROC
    start:
        ; stack is aligned to 0x10
        sub rsp, 20h
        lea rcx, [embeddedData]
        add rcx, qword ptr [nNethostOffset]
        call qword ptr [pfnLoadLibraryW]
        
        lea rcx, [embeddedData]
        add rcx, qword ptr [nDalamudOffset]
        call qword ptr [pfnLoadLibraryW]
        
        mov rcx, rax
        lea rdx, [pcszEntryPointName]
        call qword ptr [pfnGetProcAddress]
        
        mov rcx, qword ptr [pRewrittenEntryPointParameters]
        jmp rax

    pfnLoadLibraryW:
        dq PLACEHOLDER

    pfnGetProcAddress:
        dq PLACEHOLDER

    pRewrittenEntryPointParameters:
        dq PLACEHOLDER

    nNethostOffset:
        dq PLACEHOLDER

    nDalamudOffset:
        dq PLACEHOLDER

    pcszEntryPointName:
        db "RewrittenEntryPoint", 0

    embeddedData:
    
    dq TERMINATOR
RewrittenEntryPoint_Standalone ENDP

EXTERN RewrittenEntryPoint_AdjustedStack :PROC

RewrittenEntryPoint PROC
    ; stack is aligned to 0x10
    call RewrittenEntryPoint_AdjustedStack
    add rsp, 20h
    ret
RewrittenEntryPoint ENDP

END

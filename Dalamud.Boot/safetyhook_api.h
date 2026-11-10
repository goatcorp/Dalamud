#pragma once

#include <cstdint>

extern "C" {

// Mirrors safetyhook::InlineHook::Error::type with 0 reserved for success
enum boot_safetyhook_error_type : int32_t {
    BOOT_SAFETYHOOK_ERROR_NONE = 0,
    BOOT_SAFETYHOOK_ERROR_BAD_ALLOCATION = 1,
    BOOT_SAFETYHOOK_ERROR_FAILED_TO_DECODE_INSTRUCTION = 2,
    BOOT_SAFETYHOOK_ERROR_SHORT_JUMP_IN_TRAMPOLINE = 3,
    BOOT_SAFETYHOOK_ERROR_IP_RELATIVE_INSTRUCTION_OUT_OF_RANGE = 4,
    BOOT_SAFETYHOOK_ERROR_UNSUPPORTED_INSTRUCTION_IN_TRAMPOLINE = 5,
    BOOT_SAFETYHOOK_ERROR_FAILED_TO_UNPROTECT = 6,
    BOOT_SAFETYHOOK_ERROR_NOT_ENOUGH_SPACE = 7,

    // Not a safetyhook error, the handle passed in was null
    BOOT_SAFETYHOOK_ERROR_INVALID_HANDLE = 100,
    // Not a safetyhook error, an exception was thrown
    BOOT_SAFETYHOOK_ERROR_EXCEPTION = 101,
};

struct boot_safetyhook_error {
    // One of boot_safetyhook_error_type.
    int32_t type;
    // safetyhook::Allocator::Error, if type is BOOT_SAFETYHOOK_ERROR_BAD_ALLOCATION. -1 otherwise.
    int32_t allocator_error;
    // Address of the offending instruction, for the errors that report one. Null otherwise.
    void* ip;
};

// Creates an inline hook, initially disabled.
__declspec(dllexport) void* BootSafetyHookInlineCreate(void* target, void* destination, boot_safetyhook_error* error);

// Disables the hook if enabled, restores the target function and releases the handle.
__declspec(dllexport) void BootSafetyHookInlineDestroy(void* handle);

// Enables the hook. Enabling an already enabled hook is a no-op that succeeds.
__declspec(dllexport) int32_t BootSafetyHookInlineEnable(void* handle, boot_safetyhook_error* error);

// Disables the hook. Disabling an already disabled hook is a no-op that succeeds.
__declspec(dllexport) int32_t BootSafetyHookInlineDisable(void* handle, boot_safetyhook_error* error);

// Checks whether the hook is currently enabled.
__declspec(dllexport) int32_t BootSafetyHookInlineIsEnabled(void* handle);

// Gets the trampoline that calls the target as if it were not hooked.
__declspec(dllexport) void* BootSafetyHookInlineGetTrampoline(void* handle);

}

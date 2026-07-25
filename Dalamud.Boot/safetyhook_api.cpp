#include "pch.h"

#include "safetyhook_api.h"

#include <safetyhook.hpp>

#include "logging.h"

namespace {
    using inline_hook = safetyhook::InlineHook;

    void set_success(boot_safetyhook_error* error) {
        if (error == nullptr)
            return;

        *error = { BOOT_SAFETYHOOK_ERROR_NONE, -1, nullptr };
    }

    void set_error(boot_safetyhook_error* error, boot_safetyhook_error_type type) {
        if (error == nullptr)
            return;

        *error = { type, -1, nullptr };
    }

    void set_error(boot_safetyhook_error* error, const inline_hook::Error& source) {
        if (error == nullptr)
            return;

        // 0 reserved for success
        error->type = static_cast<int32_t>(source.type) + 1;

        if (source.type == inline_hook::Error::BAD_ALLOCATION) {
            error->allocator_error = static_cast<int32_t>(source.allocator_error);
            error->ip = nullptr;
        } else {
            error->allocator_error = -1;
            error->ip = source.ip;
        }
    }

    inline_hook* unwrap(void* handle) {
        return static_cast<inline_hook*>(handle);
    }
}

extern "C" void* BootSafetyHookInlineCreate(void* target, void* destination, boot_safetyhook_error* error) {
    if (target == nullptr || destination == nullptr) {
        set_error(error, BOOT_SAFETYHOOK_ERROR_INVALID_HANDLE);
        return nullptr;
    }

    try {
        // Hook<T> hands out a disabled hook
        auto created = inline_hook::create(target, destination, inline_hook::StartDisabled);

        if (!created) {
            set_error(error, created.error());
            return nullptr;
        }

        set_success(error);
        return new inline_hook(std::move(*created));
    } catch (const std::exception& e) {
        logging::E("BootSafetyHookInlineCreate({}, {}): {}",
            reinterpret_cast<ULONG_PTR>(target), reinterpret_cast<ULONG_PTR>(destination), e.what());
        set_error(error, BOOT_SAFETYHOOK_ERROR_EXCEPTION);
        return nullptr;
    }
}

extern "C" void BootSafetyHookInlineDestroy(void* handle) {
    if (handle == nullptr)
        return;

    try {
        delete unwrap(handle);
    } catch (const std::exception& e) {
        logging::E("BootSafetyHookInlineDestroy({}): {}", reinterpret_cast<ULONG_PTR>(handle), e.what());
    }
}

extern "C" int32_t BootSafetyHookInlineEnable(void* handle, boot_safetyhook_error* error) {
    if (handle == nullptr) {
        set_error(error, BOOT_SAFETYHOOK_ERROR_INVALID_HANDLE);
        return 0;
    }

    try {
        if (const auto result = unwrap(handle)->enable(); !result) {
            set_error(error, result.error());
            return 0;
        }

        set_success(error);
        return 1;
    } catch (const std::exception& e) {
        logging::E("BootSafetyHookInlineEnable({}): {}", reinterpret_cast<ULONG_PTR>(handle), e.what());
        set_error(error, BOOT_SAFETYHOOK_ERROR_EXCEPTION);
        return 0;
    }
}

extern "C" int32_t BootSafetyHookInlineDisable(void* handle, boot_safetyhook_error* error) {
    if (handle == nullptr) {
        set_error(error, BOOT_SAFETYHOOK_ERROR_INVALID_HANDLE);
        return 0;
    }

    try {
        if (const auto result = unwrap(handle)->disable(); !result) {
            set_error(error, result.error());
            return 0;
        }

        set_success(error);
        return 1;
    } catch (const std::exception& e) {
        logging::E("BootSafetyHookInlineDisable({}): {}", reinterpret_cast<ULONG_PTR>(handle), e.what());
        set_error(error, BOOT_SAFETYHOOK_ERROR_EXCEPTION);
        return 0;
    }
}

extern "C" int32_t BootSafetyHookInlineIsEnabled(void* handle) {
    if (handle == nullptr)
        return 0;

    return unwrap(handle)->enabled() ? 1 : 0;
}

extern "C" void* BootSafetyHookInlineGetTrampoline(void* handle) {
    if (handle == nullptr)
        return nullptr;

    return unwrap(handle)->original<void*>();
}

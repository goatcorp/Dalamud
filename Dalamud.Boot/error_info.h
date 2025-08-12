#pragma once

#include <expected>
#include <string>

typedef unsigned long       DWORD;

enum class DalamudBootErrorDescription {
    None,
    ModulePathResolutionFail,
    ModuleResourceLoadFail,
    ModuleResourceVersionReadFail,
    ModuleResourceVersionSignatureFail,
};

class DalamudBootError {
    DalamudBootErrorDescription m_dalamudErrorDescription;
    long m_hresult;

public:
    DalamudBootError(DalamudBootErrorDescription dalamudErrorDescription, long hresult) noexcept;
    DalamudBootError(DalamudBootErrorDescription dalamudErrorDescription) noexcept;

    const char* describe() const;
};

template<typename T>
using DalamudExpected = std::expected<
    std::conditional_t<
        std::is_reference_v<T>,
        std::reference_wrapper<std::remove_reference_t<T>>,
        T
    >,
    DalamudBootError
>;

using DalamudUnexpected = std::unexpected<DalamudBootError>;

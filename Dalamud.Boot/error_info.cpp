#include "error_info.h"

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

DalamudBootError::DalamudBootError(DalamudBootErrorDescription dalamudErrorDescription, long hresult) noexcept
    : m_dalamudErrorDescription(dalamudErrorDescription)
    , m_hresult(hresult) {
}

DalamudBootError::DalamudBootError(DalamudBootErrorDescription dalamudErrorDescription) noexcept
    : DalamudBootError(dalamudErrorDescription, E_FAIL) {
}

const char* DalamudBootError::describe() const {
    switch (m_dalamudErrorDescription) {
        case DalamudBootErrorDescription::ModuleResourceLoadFail:
            return "Failed to load resource.";
        case DalamudBootErrorDescription::ModuleResourceVersionReadFail:
            return "Failed to query version information.";
        case DalamudBootErrorDescription::ModuleResourceVersionSignatureFail:
            return "Invalid version info found.";
        default:
            return "(unavailable)";
    }
}

#include "hardware_info.h"

#include <array>
#include <format>

#include <Windows.h>
#include <intrin.h>
#include <dxgi.h>
#pragma comment(lib, "dxgi.lib")

namespace {
    // Reads the CPU vendor and brand strings via CPUID
    // See https://learn.microsoft.com/en-us/cpp/intrinsics/cpuid-cpuidex?view=msvc-170#example
    void get_cpu_info(wchar_t* vendor, size_t vendorLen, wchar_t* brand, size_t brandLen) {
        std::array<int, 4> cpui{};
        size_t convertedChars = 0;

        // Calling __cpuid with 0x0 as the function_id argument gets the highest valid function ID
        // and, in the same call, the vendor string.
        __cpuid(cpui.data(), 0);

        char vendorA[0x20] = {};
        *reinterpret_cast<int*>(vendorA + 0) = cpui[1];
        *reinterpret_cast<int*>(vendorA + 4) = cpui[3];
        *reinterpret_cast<int*>(vendorA + 8) = cpui[2];
        mbstowcs_s(&convertedChars, vendor, vendorLen, vendorA, _TRUNCATE);

        // Calling __cpuid with 0x80000000 gets the highest valid extended ID. The brand string is
        // returned by extended IDs 0x80000002 through 0x80000004.
        __cpuid(cpui.data(), 0x80000000);
        const auto nExIds = static_cast<unsigned int>(cpui[0]);

        if (nExIds >= 0x80000004) {
            char brandA[0x40] = {};
            for (unsigned int i = 0x80000002; i <= 0x80000004; ++i) {
                __cpuidex(cpui.data(), static_cast<int>(i), 0);
                memcpy(brandA + (i - 0x80000002) * sizeof(cpui), cpui.data(), sizeof(cpui));
            }
            mbstowcs_s(&convertedChars, brand, brandLen, brandA, _TRUNCATE);
        }
    }

    std::wstring get_registry_value_str(HKEY hKeyRoot, const std::wstring& subKey, const std::wstring& valueName) {
        HKEY hKey;
        std::wstring value;
        if (RegOpenKeyExW(hKeyRoot, subKey.c_str(), 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
            wchar_t buffer[512];
            DWORD bufferSize = sizeof(buffer);
            if (RegQueryValueExW(hKey, valueName.c_str(), nullptr, nullptr, reinterpret_cast<LPBYTE>(buffer), &bufferSize) == ERROR_SUCCESS)
                value = buffer;
            RegCloseKey(hKey);
        }
        return value;
    }

    std::wstring get_registry_value_bin(HKEY hKeyRoot, const std::string& subKey, const std::string& valueName) {
        HKEY hKey;
        std::wstring hexStr;
        if (RegOpenKeyExA(hKeyRoot, subKey.c_str(), 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
            DWORD bufferSize = 0;
            if (RegQueryValueExA(hKey, valueName.c_str(), nullptr, nullptr, nullptr, &bufferSize) == ERROR_SUCCESS) {
                std::vector<unsigned char> buffer(bufferSize);
                if (RegQueryValueExA(hKey, valueName.c_str(), nullptr, nullptr, buffer.data(), &bufferSize) == ERROR_SUCCESS) {
                    hexStr.reserve(buffer.size() * 3);
                    for (const auto byte : buffer)
                        hexStr += std::format(L"{:02X} ", byte);
                }
            }
            RegCloseKey(hKey);
        }
        return hexStr;
    }

    void append_gpu_lines(std::vector<std::wstring>& lines) {
        IDXGIFactory1* pFactory = nullptr;
        if (FAILED(CreateDXGIFactory1(__uuidof(IDXGIFactory1), reinterpret_cast<void**>(&pFactory))))
            return;

        IDXGIAdapter1* pAdapter = nullptr;
        for (UINT i = 0; pFactory->EnumAdapters1(i, &pAdapter) != DXGI_ERROR_NOT_FOUND; ++i) {
            DXGI_ADAPTER_DESC1 adapterDescription{};
            pAdapter->GetDesc1(&adapterDescription);
            lines.push_back(std::format(L"GPU Desc: {}", adapterDescription.Description));
            pAdapter->Release();
        }

        pFactory->Release();
    }
}

namespace hardware_info {
    std::vector<std::wstring> collect_lines() {
        static constexpr std::wstring_view BiosPath = L"HARDWARE\\DESCRIPTION\\System\\BIOS";
        static constexpr std::wstring_view CentralProcessorPath = L"HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0";

        wchar_t vendor[0x20] = {};
        wchar_t brand[0x40] = {};
        get_cpu_info(vendor, std::size(vendor), brand, std::size(brand));

        std::vector<std::wstring> lines;
        lines.push_back(std::format(L"CPU Vendor: {}", vendor));
        lines.push_back(std::format(L"CPU Brand: {}", brand));
        lines.push_back(std::format(L"BIOS Vendor: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(BiosPath), L"BIOSVendor")));
        lines.push_back(std::format(L"BIOS Version: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(BiosPath), L"BIOSVersion")));
        lines.push_back(std::format(L"BIOS Release Date: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(BiosPath), L"BIOSReleaseDate")));
        lines.push_back(std::format(L"Base Board Manufacturer: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(BiosPath), L"BaseBoardManufacturer")));
        lines.push_back(std::format(L"Base Board Product: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(BiosPath), L"BaseBoardProduct")));
        lines.push_back(std::format(L"Central Processor Identifier: {}", get_registry_value_str(HKEY_LOCAL_MACHINE, std::wstring(CentralProcessorPath), L"Identifier")));
        lines.push_back(std::format(L"Central Processor Update Revision: {}", get_registry_value_bin(HKEY_LOCAL_MACHINE, "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", "Update Revision")));
        append_gpu_lines(lines);
        return lines;
    }
}

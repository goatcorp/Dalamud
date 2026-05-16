#pragma once

namespace veh
{
    bool add_handler(bool doFullDump, const std::string& workingDirectory, const std::wstring& bootLogPath, bool bootConsole);
    bool remove_handler();
    void raise_external_event(const std::wstring& info);
}

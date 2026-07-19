#pragma once

#include <string>
#include <vector>

namespace hardware_info {
    // Collect hardware info and emit log lines, ready to write out
    std::vector<std::wstring> collect_lines();
}

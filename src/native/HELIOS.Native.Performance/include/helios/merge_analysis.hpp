#pragma once
#include <algorithm>
#include <string>
#include <string_view>
#include <unordered_set>
#include <vector>

namespace helios::merge_analysis {
inline std::string normalize_path(std::string_view path) {
    std::string value(path);
    std::replace(value.begin(), value.end(), '\\', '/');
    return value;
}

inline std::size_t overlap_count(const std::vector<std::string>& left, const std::vector<std::string>& right) {
    std::unordered_set<std::string> normalized;
    for (const auto& item : left) normalized.insert(normalize_path(item));
    std::size_t count = 0;
    for (const auto& item : right) {
        if (normalized.contains(normalize_path(item))) ++count;
    }
    return count;
}
}

#pragma once

#include <string>
#include <unordered_map>
#include <vector>

namespace xtier {

std::vector<std::string> phase1_layout();
std::unordered_map<std::string, std::string> phase1_cache_env();
std::vector<std::string> phase2_tools();
std::vector<std::string> phase4_feature_columns();

}  // namespace xtier


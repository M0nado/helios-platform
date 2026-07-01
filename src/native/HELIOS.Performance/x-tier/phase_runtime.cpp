#include "phase_runtime.hpp"

namespace xtier {

std::vector<std::string> phase1_layout() {
  return {
      R"(D:\DevDrive\src)",    R"(D:\DevDrive\pkg)",    R"(D:\DevDrive\tools)",
      R"(D:\DevDrive\ai)",     R"(D:\DevDrive\docker)", R"(D:\DevDrive\wsl)",
      R"(D:\DevDrive\vm)",     R"(D:\DevDrive\hermes)", R"(D:\DevDrive\data)",
      R"(D:\DevDrive\azure)",  R"(D:\DevDrive\github)",
  };
}

std::unordered_map<std::string, std::string> phase1_cache_env() {
  return {
      {"npm_cache", R"(D:\pkg\npm-cache)"},
      {"pip_cache", R"(D:\pkg\pip-cache)"},
      {"nuget_packages", R"(D:\pkg\nuget-global)"},
      {"cargo_home", R"(D:\tools\cargo)"},
      {"rustup_home", R"(D:\tools\rustup)"},
  };
}

std::vector<std::string> phase2_tools() {
  return {"Git", "VSCode", "GitHub CLI", "Azure CLI", "Python 3.12", "Node.js", ".NET 8 SDK", "PowerShell 7", "Docker", "WSL2", "CUDA"};
}

std::vector<std::string> phase4_feature_columns() {
  return {"tokens_in", "tokens_out", "latency_ms", "tokens_total", "throughput", "success", "task_complexity", "input_length", "time_of_day_bucket"};
}

}  // namespace xtier


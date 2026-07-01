#include "phase_runtime.hpp"
#include "secure_runtime_core.hpp"

#include <cctype>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <sstream>
#include <string>
#include <vector>

namespace fs = std::filesystem;

static std::vector<std::string> read_lines(const fs::path& path) {
  std::ifstream in(path);
  std::vector<std::string> lines;
  std::string line;
  while (std::getline(in, line)) {
    lines.push_back(line);
  }
  return lines;
}

static std::string json_escape(const std::string& in) {
  std::string out;
  out.reserve(in.size() + 16);
  for (char c : in) {
    switch (c) {
      case '\"': out += "\\\""; break;
      case '\\': out += "\\\\"; break;
      case '\b': out += "\\b"; break;
      case '\f': out += "\\f"; break;
      case '\n': out += "\\n"; break;
      case '\r': out += "\\r"; break;
      case '\t': out += "\\t"; break;
      default: out += c; break;
    }
  }
  return out;
}

static bool is_heading(const std::string& line) {
  return line.rfind("Section ", 0) == 0 || line.rfind("Appendix ", 0) == 0 ||
         (!line.empty() && std::isdigit(static_cast<unsigned char>(line[0])) &&
          line.find(u8"—") != std::string::npos);
}

static std::size_t count_substr(const std::string& haystack, const std::string& needle) {
  if (needle.empty()) return 0;
  std::size_t count = 0;
  std::size_t pos = 0;
  while ((pos = haystack.find(needle, pos)) != std::string::npos) {
    ++count;
    pos += needle.size();
  }
  return count;
}

static std::size_t count_words(const std::string& s) {
  std::istringstream iss(s);
  std::size_t n = 0;
  std::string w;
  while (iss >> w) ++n;
  return n;
}

int main(int argc, char** argv) {
  if (argc < 3) {
    std::cerr << "Usage: xtier_brief_compiler <source.txt> <output.txt>\n";
    return 1;
  }
  fs::path source = argv[1];
  fs::path output = argv[2];
  auto lines = read_lines(source);
  std::vector<std::string> headings;
  std::size_t word_count = 0;
  std::size_t section_count = 0;
  std::size_t appendix_count = 0;
  std::size_t table_like = 0;
  std::size_t task_mentions = 0;
  std::size_t skill_mentions = 0;
  std::size_t connector_mentions = 0;
  std::size_t phase_mentions = 0;
  std::size_t mode_mentions = 0;
  std::size_t auto_mentions = 0;
  std::size_t self_heal_mentions = 0;
  std::size_t section_symbol_count = 0;
  std::size_t section_symbol_max = 0;
  std::size_t security_mentions = 0;
  std::size_t firewall_mentions = 0;
  std::size_t quarantine_mentions = 0;
  std::size_t bitlocker_mentions = 0;
  for (const auto& l : lines) {
    word_count += count_words(l);
    if (is_heading(l)) {
      headings.push_back(l);
    }
    if (l.rfind("Section ", 0) == 0) ++section_count;
    if (l.rfind("Appendix ", 0) == 0) ++appendix_count;
    if (l.find("  ") != std::string::npos) ++table_like;

    std::string lower = l;
    for (char& c : lower) c = static_cast<char>(std::tolower(static_cast<unsigned char>(c)));
    task_mentions += count_substr(lower, "task");
    skill_mentions += count_substr(lower, "skill");
    connector_mentions += count_substr(lower, "connector");
    phase_mentions += count_substr(lower, "phase");
    mode_mentions += count_substr(lower, "-mode");
    auto_mentions += count_substr(lower, "auto");
    self_heal_mentions += count_substr(lower, "self-heal") + count_substr(lower, "self heal");
    security_mentions += count_substr(lower, "security");
    firewall_mentions += count_substr(lower, "firewall");
    quarantine_mentions += count_substr(lower, "quarantine");
    bitlocker_mentions += count_substr(lower, "bitlocker");

    std::size_t pos = 0;
    while ((pos = l.find(u8"§", pos)) != std::string::npos) {
      ++section_symbol_count;
      std::size_t p = pos + std::string(u8"§").size();
      while (p < l.size() && std::isspace(static_cast<unsigned char>(l[p]))) ++p;
      std::size_t start = p;
      while (p < l.size() && std::isdigit(static_cast<unsigned char>(l[p]))) ++p;
      if (p > start) {
        auto n = static_cast<std::size_t>(std::stoul(l.substr(start, p - start)));
        if (n > section_symbol_max) section_symbol_max = n;
      }
      pos = p;
    }
  }
  const double inferred_cpu = std::min(100.0, 10.0 + static_cast<double>(mode_mentions) * 1.5);
  const double inferred_memory_mb = 4096.0 + static_cast<double>(word_count) * 0.08;
  const double inferred_io_wait = std::min(1.0, static_cast<double>(section_symbol_count + auto_mentions) / 100.0);
  const bool high_security = (security_mentions + firewall_mentions + quarantine_mentions + bitlocker_mentions) > 25;
  const double inferred_gpu = std::min(100.0, 15.0 + static_cast<double>(skill_mentions) * 1.2);
  const double inferred_net_egress = std::min(1200.0, 50.0 + static_cast<double>(connector_mentions + mode_mentions) * 2.8);
  xtier::secure::SecureRuntimeCore secure_core(75.0, high_security ? 0.55 : 0.72);
  const auto runtime_signals = secure_core.BuildSignals(inferred_cpu, inferred_memory_mb, inferred_io_wait);
  const auto decision = secure_core.Evaluate(runtime_signals);
  const auto plan = secure_core.BuildOptimizationPlan(high_security);
  const xtier::secure::ResourceSample sample{inferred_cpu, inferred_memory_mb, inferred_gpu, inferred_net_egress};
  const auto resource_data = secure_core.BuildResourceOptimizationData(sample, high_security);
  const auto parallel_types = secure_core.MajorParallelizationTypes();
  const auto watch_plan = secure_core.BuildSecurityWatchPlan(high_security);
  const auto folder_plan = secure_core.BuildFolderGovernancePlan(high_security);
  const auto alert_channels = secure_core.AlertChannels();

  fs::create_directories(output.parent_path());
  std::ofstream out(output);
  out << "{\n";
  out << "  \"source\": \"" << json_escape(source.string()) << "\",\n";
  out << "  \"line_count\": " << lines.size() << ",\n";
  out << "  \"word_count\": " << word_count << ",\n";
  out << "  \"heading_count\": " << headings.size() << ",\n";
  out << "  \"section_count\": " << section_count << ",\n";
  out << "  \"appendix_count\": " << appendix_count << ",\n";
  out << "  \"table_like_line_count\": " << table_like << ",\n";
  out << "  \"task_mentions\": " << task_mentions << ",\n";
  out << "  \"skill_mentions\": " << skill_mentions << ",\n";
  out << "  \"connector_mentions\": " << connector_mentions << ",\n";
  out << "  \"phase_mentions\": " << phase_mentions << ",\n";
  out << "  \"mode_mentions\": " << mode_mentions << ",\n";
  out << "  \"auto_mentions\": " << auto_mentions << ",\n";
  out << "  \"self_heal_mentions\": " << self_heal_mentions << ",\n";
  out << "  \"security_mentions\": " << security_mentions << ",\n";
  out << "  \"firewall_mentions\": " << firewall_mentions << ",\n";
  out << "  \"quarantine_mentions\": " << quarantine_mentions << ",\n";
  out << "  \"bitlocker_mentions\": " << bitlocker_mentions << ",\n";
  out << "  \"section_symbol_count\": " << section_symbol_count << ",\n";
  out << "  \"section_symbol_max\": " << section_symbol_max << ",\n";
  out << "  \"phase1_layout_count\": " << xtier::phase1_layout().size() << ",\n";
  out << "  \"runtime_decision\": {\n";
  out << "    \"allow\": " << (decision.allow ? "true" : "false") << ",\n";
  out << "    \"reason\": \"" << json_escape(decision.reason) << "\",\n";
  out << "    \"score\": " << decision.score << "\n";
  out << "  },\n";
  out << "  \"optimization_plan\": {\n";
  out << "    \"cpu_scheduler_boost\": " << plan.at("cpu_scheduler_boost") << ",\n";
  out << "    \"memory_gc_pressure\": " << plan.at("memory_gc_pressure") << ",\n";
  out << "    \"gpu_queue_depth\": " << plan.at("gpu_queue_depth") << ",\n";
  out << "    \"network_egress_factor\": " << plan.at("network_egress_factor") << ",\n";
  out << "    \"model_hotset_prefetch\": " << plan.at("model_hotset_prefetch") << "\n";
  out << "  },\n";
  out << "  \"cpp_security_optimization_data\": {\n";
  out << "    \"cpu_utilization\": " << resource_data.at("cpu_utilization") << ",\n";
  out << "    \"memory_mb\": " << resource_data.at("memory_mb") << ",\n";
  out << "    \"gpu_utilization\": " << resource_data.at("gpu_utilization") << ",\n";
  out << "    \"net_egress_mb_s\": " << resource_data.at("net_egress_mb_s") << ",\n";
  out << "    \"compression_pressure\": " << resource_data.at("compression_pressure") << ",\n";
  out << "    \"multitask_pressure\": " << resource_data.at("multitask_pressure") << ",\n";
  out << "    \"lightweight_score\": " << resource_data.at("lightweight_score") << ",\n";
  out << "    \"kernel_cpu_clamp\": " << resource_data.at("kernel_cpu_clamp") << ",\n";
  out << "    \"kernel_memory_compaction\": " << resource_data.at("kernel_memory_compaction") << ",\n";
  out << "    \"kernel_gpu_scheduling\": " << resource_data.at("kernel_gpu_scheduling") << ",\n";
  out << "    \"kernel_network_throttle\": " << resource_data.at("kernel_network_throttle") << ",\n";
  out << "    \"kernel_parallel_lane_utilization\": " << resource_data.at("kernel_parallel_lane_utilization") << "\n";
  out << "  },\n";
  out << "  \"major_parallelization_types\": [\n";
  for (std::size_t i = 0; i < parallel_types.size(); ++i) {
    out << "    \"" << json_escape(parallel_types[i]) << "\"";
    if (i + 1 < parallel_types.size()) out << ",";
    out << "\n";
  }
  out << "  ],\n";
  out << "  \"security_watch_plan\": {\n";
  out << "    \"internet_watch_interval_seconds\": " << watch_plan.at("internet_watch_interval_seconds") << ",\n";
  out << "    \"port_watch_interval_seconds\": " << watch_plan.at("port_watch_interval_seconds") << ",\n";
  out << "    \"permission_drift_scan_seconds\": " << watch_plan.at("permission_drift_scan_seconds") << ",\n";
  out << "    \"quarantine_trigger_score\": " << watch_plan.at("quarantine_trigger_score") << ",\n";
  out << "    \"alert_escalation_threshold\": " << watch_plan.at("alert_escalation_threshold") << ",\n";
  out << "    \"auto_isolation_level\": " << watch_plan.at("auto_isolation_level") << ",\n";
  out << "    \"egress_lockdown_factor\": " << watch_plan.at("egress_lockdown_factor") << "\n";
  out << "  },\n";
  out << "  \"folder_governance_plan\": {\n";
  out << "    \"sort_priority_score\": " << folder_plan.at("sort_priority_score") << ",\n";
  out << "    \"compression_level\": " << folder_plan.at("compression_level") << ",\n";
  out << "    \"dedupe_intensity\": " << folder_plan.at("dedupe_intensity") << ",\n";
  out << "    \"permission_enforcement_level\": " << folder_plan.at("permission_enforcement_level") << ",\n";
  out << "    \"suspicious_folder_quarantine_level\": " << folder_plan.at("suspicious_folder_quarantine_level") << ",\n";
  out << "    \"readonly_guard_ratio\": " << folder_plan.at("readonly_guard_ratio") << ",\n";
  out << "    \"filesystem_watch_depth\": " << folder_plan.at("filesystem_watch_depth") << "\n";
  out << "  },\n";
  out << "  \"alert_channels\": [\n";
  for (std::size_t i = 0; i < alert_channels.size(); ++i) {
    out << "    \"" << json_escape(alert_channels[i]) << "\"";
    if (i + 1 < alert_channels.size()) out << ",";
    out << "\n";
  }
  out << "  ],\n";
  out << "  \"headings\": [\n";
  for (std::size_t i = 0; i < headings.size(); ++i) {
    out << "    \"" << json_escape(headings[i]) << "\"";
    if (i + 1 < headings.size()) out << ",";
    out << "\n";
  }
  out << "  ]\n";
  out << "}\n";
  std::cout << "Wrote: " << output.string() << "\n";
  return 0;
}

#include "helios_native_performance.h"

#include <chrono>
#include <iostream>
#include <vector>

int main() {
    std::vector<double> values(100000);
    for (size_t i = 0; i < values.size(); ++i) {
        values[i] = static_cast<double>(i % 97);
    }

    const auto started = std::chrono::steady_clock::now();
    const double mean = helios_vector_mean(values.data(), values.size());
    const auto ended = std::chrono::steady_clock::now();
    const auto micros = std::chrono::duration_cast<std::chrono::microseconds>(ended - started).count();

    std::cout << "abi=" << helios_native_abi_version() << '\n';
    std::cout << "mean=" << mean << '\n';
    std::cout << "elapsedMicros=" << micros << '\n';
    return mean > 0.0 ? 0 : 1;
}

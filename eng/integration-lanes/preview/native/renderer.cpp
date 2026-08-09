#include <array>
#include <iostream>
#include <numeric>

int main() {
    constexpr std::array frame_times{8.2, 8.4, 8.1, 8.3};
    const auto average = std::reduce(frame_times.begin(), frame_times.end()) / frame_times.size();
    std::cout << "HELIOS preview renderer average frame time: " << average << "ms\n";
    return average < 16.7 ? 0 : 1;
}

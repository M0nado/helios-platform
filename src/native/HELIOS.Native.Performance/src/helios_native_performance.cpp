#include "helios_native_performance.h"

#include <numeric>
#include <span>

int helios_native_abi_version(void) {
    return HELIOS_NATIVE_ABI_VERSION;
}

double helios_vector_sum(const double* values, size_t length) {
    if (values == nullptr || length == 0) {
        return 0.0;
    }
    const std::span<const double> data(values, length);
    return std::accumulate(data.begin(), data.end(), 0.0);
}

double helios_vector_mean(const double* values, size_t length) {
    if (values == nullptr || length == 0) {
        return 0.0;
    }
    return helios_vector_sum(values, length) / static_cast<double>(length);
}

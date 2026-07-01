#include "helios_performance.h"

extern "C" double helios_weighted_score(const double* values, int count) {
    if (values == nullptr || count <= 0) {
        return 0.0;
    }

    double total = 0.0;
    double weight = 1.0;
    double weightTotal = 0.0;
    for (int i = 0; i < count; ++i) {
        total += values[i] * weight;
        weightTotal += weight;
        weight += 1.0;
    }

    return weightTotal == 0.0 ? 0.0 : total / weightTotal;
}

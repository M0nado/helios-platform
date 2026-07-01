#include "helios_performance.h"
#include <cassert>

int main() {
    double values[] = {1.0, 2.0, 3.0};
    assert(helios_weighted_score(values, 3) > 2.0);
    return 0;
}

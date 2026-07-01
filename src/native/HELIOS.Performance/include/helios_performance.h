#pragma once

#ifdef _WIN32
#define HELIOS_PERFORMANCE_API __declspec(dllexport)
#else
#define HELIOS_PERFORMANCE_API
#endif

extern "C" HELIOS_PERFORMANCE_API double helios_weighted_score(const double* values, int count);

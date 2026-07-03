#pragma once
#include <stddef.h>

#ifdef _WIN32
  #ifdef HELIOS_NATIVE_PERFORMANCE_EXPORTS
    #define HELIOS_NATIVE_API __declspec(dllexport)
  #else
    #define HELIOS_NATIVE_API __declspec(dllimport)
  #endif
#else
  #define HELIOS_NATIVE_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define HELIOS_NATIVE_ABI_VERSION 1

HELIOS_NATIVE_API int helios_native_abi_version(void);
HELIOS_NATIVE_API double helios_vector_sum(const double* values, size_t length);
HELIOS_NATIVE_API double helios_vector_mean(const double* values, size_t length);

#ifdef __cplusplus
}
#endif

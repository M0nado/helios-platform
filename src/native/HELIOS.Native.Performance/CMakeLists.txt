cmake_minimum_required(VERSION 3.22)
project(HELIOSNativePerformance LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_library(HELIOSNativePerformance INTERFACE)
target_include_directories(HELIOSNativePerformance INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/include)

include(CTest)
if(BUILD_TESTING)
  add_executable(
    helios_native_engine_smoke
    ${CMAKE_CURRENT_SOURCE_DIR}/../../../tests/native/aihub_native_engine_smoke.cpp
  )
  target_link_libraries(helios_native_engine_smoke PRIVATE HELIOSNativePerformance)
  if(MSVC)
    target_compile_options(helios_native_engine_smoke PRIVATE /W4 /WX /permissive-)
  else()
    target_compile_options(helios_native_engine_smoke PRIVATE -Wall -Wextra -Werror -pedantic)
  endif()
  add_test(NAME helios_native_engine_smoke COMMAND helios_native_engine_smoke)
endif()

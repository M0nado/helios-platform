cmake_minimum_required(VERSION 3.22)
project(HELIOSNativePerformance LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_library(HELIOSNativePerformance INTERFACE)
target_include_directories(HELIOSNativePerformance INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}/include)

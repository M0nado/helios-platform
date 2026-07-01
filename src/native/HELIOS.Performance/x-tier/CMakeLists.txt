cmake_minimum_required(VERSION 3.16)
project(xtier_brief_compiler LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(xtier_brief_compiler
  main.cpp
  phase_runtime.cpp
  secure_runtime_core.cpp
)

target_include_directories(xtier_brief_compiler PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

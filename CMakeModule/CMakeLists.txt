cmake_minimum_required(VERSION 3.10)

project(RezachtNative)

file(GLOB_RECURSE GENERATED_SOURCES "generated/*.g.c")
add_library(generated writer.c ${GENERATED_SOURCES})
target_include_directories(generated PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

add_executable(generated_test test.c)
target_link_libraries(generated_test PRIVATE generated)
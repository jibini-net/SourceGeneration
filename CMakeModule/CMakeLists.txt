cmake_minimum_required(VERSION 3.10)

project(RezachtCore)

file(GLOB_RECURSE VIEW_FILES "views/*.view")
add_custom_target(
    run_rezacht
    COMMAND bash ./tools/buildall.sh
    WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    DEPENDS tools/rezacht.sh ${VIEW_FILES}
    COMMENT "Applying advanced Rezacht Core technology"
)

file(GLOB_RECURSE GENERATED_SOURCES "generated/*.g.c")
add_library(generated writer.c ${GENERATED_SOURCES})
add_dependencies(generated run_rezacht)
target_include_directories(generated PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})

add_executable(generated_test test.c)
target_link_libraries(generated_test PRIVATE generated)

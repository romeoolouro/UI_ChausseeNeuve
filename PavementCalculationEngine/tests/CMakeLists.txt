# Google Test CMake configuration for PavementCalculationEngine
# Task 1.6: Unit Testing

cmake_minimum_required(VERSION 3.15)

# Find Google Test
find_package(GTest QUIET)

if(NOT GTest_FOUND)
    message(STATUS "Google Test not found via find_package, attempting FetchContent...")
    
    include(FetchContent)
    FetchContent_Declare(
        googletest
        GIT_REPOSITORY https://github.com/google/googletest.git
        GIT_TAG release-1.12.1
    )
    
    # For Windows: Prevent overriding the parent project's compiler/linker settings
    set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
    
    FetchContent_MakeAvailable(googletest)
    
    message(STATUS "Google Test fetched successfully")
endif()

# Test executable
add_executable(PavementTests
    test_pavement_data.cpp
    test_matrix_operations.cpp
    test_pavement_calculator.cpp
)

# Include directories
target_include_directories(PavementTests PRIVATE
    ${CMAKE_SOURCE_DIR}/include
    ${CMAKE_SOURCE_DIR}/src
)

# Link libraries
target_link_libraries(PavementTests PRIVATE
    GTest::gtest_main
)

# Link Eigen if available
if(EXISTS "${CMAKE_SOURCE_DIR}/extern/eigen")
    target_include_directories(PavementTests PRIVATE "${CMAKE_SOURCE_DIR}/extern/eigen")
endif()

# Add source files (not main.cpp)
target_sources(PavementTests PRIVATE
    ${CMAKE_SOURCE_DIR}/src/PavementData.cpp
    ${CMAKE_SOURCE_DIR}/src/MatrixOperations.cpp
    ${CMAKE_SOURCE_DIR}/src/PavementCalculator.cpp
)

# Enable testing
enable_testing()

# Discover tests
include(GoogleTest)
gtest_discover_tests(PavementTests)

message(STATUS "Test executable configured: PavementTests")

# ============================================
# C API Test (Pure C test for DLL validation)
# Task 2.5: C Test Harness
# ============================================

# Only build C API test if we're building the shared library (DLL)
if(BUILD_SHARED_LIBS)
    message(STATUS "Configuring C API test harness...")
    
    # C API test executable (pure C)
    add_executable(c_api_test c_api_test.c)
    
    # Set C standard
    set_property(TARGET c_api_test PROPERTY C_STANDARD 11)
    set_property(TARGET c_api_test PROPERTY C_STANDARD_REQUIRED ON)
    
    # Include directories
    target_include_directories(c_api_test PRIVATE
        ${CMAKE_SOURCE_DIR}/include
    )
    
    # Link to the shared library (DLL)
    target_link_libraries(c_api_test PRIVATE
        PavementCalculationEngine
    )
    
    # Add as test
    add_test(NAME CAPITest COMMAND c_api_test)
    
    message(STATUS "C API test configured: c_api_test")
else()
    message(STATUS "Skipping C API test (BUILD_SHARED_LIBS is OFF)")
endif()

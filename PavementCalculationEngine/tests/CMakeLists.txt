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
    test_pymastic_port.cpp
)

# Include directories
target_include_directories(PavementTests PRIVATE
    ${CMAKE_SOURCE_DIR}/include
    ${CMAKE_SOURCE_DIR}/src
)

# Link libraries
target_link_libraries(PavementTests PRIVATE
    PavementCalculationEngine
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
    
    # PyMastic simple test
    add_executable(pymastic_simple_test test_pymastic_simple.cpp)
    target_link_libraries(pymastic_simple_test PRIVATE PavementCalculationEngine)
    target_include_directories(pymastic_simple_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME PyMasticSimpleTest COMMAND pymastic_simple_test)
    
    # PyMastic minimal test
    add_executable(pymastic_minimal_test test_pymastic_minimal.cpp)
    target_link_libraries(pymastic_minimal_test PRIVATE PavementCalculationEngine)
    target_include_directories(pymastic_minimal_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME PyMasticMinimalTest COMMAND pymastic_minimal_test)
    
    # PyMastic diagnostic test
    add_executable(pymastic_diagnostic_test test_pymastic_diagnostic.cpp)
    target_link_libraries(pymastic_diagnostic_test PRIVATE PavementCalculationEngine)
    target_include_directories(pymastic_diagnostic_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME PyMasticDiagnosticTest COMMAND pymastic_diagnostic_test)
    
    # Bessel validation test
    add_executable(bessel_validation_test test_bessel_validation.cpp)
    target_link_libraries(bessel_validation_test PRIVATE PavementCalculationEngine)
    target_include_directories(bessel_validation_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME BesselValidationTest COMMAND bessel_validation_test)
    
    # PyMastic C API test
    add_executable(pymastic_c_api_test test_pymastic_c_api.c)
    target_link_libraries(pymastic_c_api_test PRIVATE PavementCalculationEngine)
    target_include_directories(pymastic_c_api_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME PyMasticCAPITest COMMAND pymastic_c_api_test)
    
    # PyMastic Tableaux validation test - PRIORITY
    add_executable(pymastic_tableaux_test test_pymastic_tableaux.cpp)
    target_link_libraries(pymastic_tableaux_test PRIVATE PavementCalculationEngine)
    target_include_directories(pymastic_tableaux_test PRIVATE ${CMAKE_SOURCE_DIR}/include)
    add_test(NAME PyMasticTableauxTest COMMAND pymastic_tableaux_test)
    
    # PyMastic C++ Debug test - UNIT COMPARISON
    add_executable(test_pymastic_cpp_debug test_pymastic_cpp_debug.cpp)
    target_link_libraries(test_pymastic_cpp_debug PRIVATE PavementCalculationEngine)
    target_include_directories(test_pymastic_cpp_debug PRIVATE ${CMAKE_SOURCE_DIR}/include)
    
    message(STATUS "C API test configured: c_api_test")
    message(STATUS "PyMastic simple test configured: pymastic_simple_test")
    message(STATUS "PyMastic Tableaux validation test configured: pymastic_tableaux_test")
else()
    message(STATUS "Skipping C API test (BUILD_SHARED_LIBS is OFF)")
endif()

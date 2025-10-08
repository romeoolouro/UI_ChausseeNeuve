cmake_minimum_required(VERSION 3.15)
project(PavementCalculationEngine VERSION 1.0.0 LANGUAGES CXX)

# C++17 required for modern features and Eigen compatibility
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Build options - Task 2.3: DLL/shared library support
option(BUILD_SHARED_LIBS "Build shared library (DLL)" ON)
option(BUILD_TESTS "Build unit tests" ON)
option(BUILD_EXECUTABLE "Build test executable" ON)

# Find dependencies via vcpkg (when available)
find_package(Boost QUIET COMPONENTS math_tr1)
find_package(Eigen3 QUIET)

# Include directories
include_directories(
    ${CMAKE_CURRENT_SOURCE_DIR}/include
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

# If vcpkg dependencies found, include them
if(Boost_FOUND)
    include_directories(${Boost_INCLUDE_DIRS})
    message(STATUS "Found Boost: ${Boost_VERSION}")
endif()

if(Eigen3_FOUND)
    message(STATUS "Found Eigen3: ${Eigen3_VERSION}")
else()
    # Fallback to local Eigen installation
    if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        include_directories("${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        message(STATUS "Using local Eigen installation")
    else()
        message(WARNING "Eigen3 not found via vcpkg or local installation")
    endif()
endif()

# Create directories for source files
file(MAKE_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/src)
file(MAKE_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/include)
file(MAKE_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/tests)

# Source files - Task 2.1/2.2: Added C API, Task 5.2: Added TRMM Solver, Phase 2A: Added PyMastic
set(LIBRARY_SOURCES
    src/PavementData.cpp
    src/MatrixOperations.cpp
    src/PavementCalculator.cpp
    src/PavementAPI.cpp
    src/TRMMSolver.cpp
    src/PyMasticSolver.cpp
    src/PyMasticPythonBridge.cpp
)

set(LIBRARY_HEADERS
    include/PavementData.h
    include/MatrixOperations.h
    include/PavementCalculator.h
    include/Logger.h
    include/Constants.h
    include/PavementAPI.h
    include/TRMMSolver.h
    include/PyMasticSolver.h
    include/PyMasticPythonBridge.h
)

set(EXECUTABLE_SOURCES
    src/main.cpp
)

# ============================================================================
# Task 2.3: Shared Library (DLL) Target
# ============================================================================
if(BUILD_SHARED_LIBS)
    add_library(PavementCalculationEngine SHARED ${LIBRARY_SOURCES} ${LIBRARY_HEADERS})
    
    # DLL export/import configuration
    target_compile_definitions(PavementCalculationEngine PRIVATE PAVEMENT_EXPORTS)
    
    # Set library properties
    set_target_properties(PavementCalculationEngine PROPERTIES
        VERSION ${PROJECT_VERSION}
        SOVERSION 1
        OUTPUT_NAME "PavementCalculationEngine"
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
        LIBRARY_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
        ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/lib"
    )
    
    # Windows-specific DLL settings
    if(WIN32)
        set_target_properties(PavementCalculationEngine PROPERTIES
            PREFIX ""  # No 'lib' prefix on Windows
            WINDOWS_EXPORT_ALL_SYMBOLS OFF  # Manual control via PAVEMENT_EXPORTS
        )
    endif()
    
    # Include directories
    target_include_directories(PavementCalculationEngine
        PUBLIC
            $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
            $<INSTALL_INTERFACE:include>
        PRIVATE
            ${CMAKE_CURRENT_SOURCE_DIR}/src
    )
    
    # Link Eigen (header-only, static)
    if(Eigen3_FOUND)
        target_link_libraries(PavementCalculationEngine PRIVATE Eigen3::Eigen)
    elseif(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        target_include_directories(PavementCalculationEngine PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        target_compile_definitions(PavementCalculationEngine PRIVATE EIGEN_AVAILABLE)
    endif()
    
    # Link Boost (if available, header-only parts)
    if(Boost_FOUND)
        target_link_libraries(PavementCalculationEngine PRIVATE Boost::boost)
    endif()
    
    message(STATUS "Building PavementCalculationEngine as SHARED library (DLL)")
endif()

# ============================================================================
# Static Library Target (fallback)
# ============================================================================
if(NOT BUILD_SHARED_LIBS)
    add_library(PavementCalculationEngine STATIC ${LIBRARY_SOURCES} ${LIBRARY_HEADERS})
    
    target_include_directories(PavementCalculationEngine
        PUBLIC
            $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
            $<INSTALL_INTERFACE:include>
        PRIVATE
            ${CMAKE_CURRENT_SOURCE_DIR}/src
    )
    
    # Link Eigen
    if(Eigen3_FOUND)
        target_link_libraries(PavementCalculationEngine PRIVATE Eigen3::Eigen)
    elseif(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        target_include_directories(PavementCalculationEngine PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
        target_compile_definitions(PavementCalculationEngine PRIVATE EIGEN_AVAILABLE)
    endif()
    
    # Link Boost
    if(Boost_FOUND)
        target_link_libraries(PavementCalculationEngine PRIVATE Boost::boost)
    endif()
    
    message(STATUS "Building PavementCalculationEngine as STATIC library")
endif()

# ============================================================================
# Test Executable (optional)
# ============================================================================
if(BUILD_EXECUTABLE)
    add_executable(PavementCalculationTest ${EXECUTABLE_SOURCES})
    target_link_libraries(PavementCalculationTest PRIVATE PavementCalculationEngine)
    
    set_target_properties(PavementCalculationTest PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}/bin"
    )
    
    message(STATUS "Building PavementCalculationTest executable")
endif()

# ============================================================================
# Unit Tests (optional)
# ============================================================================
if(BUILD_TESTS AND EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/tests")
    enable_testing()
    add_subdirectory(tests)
endif()

# ============================================================================
# Installation Rules
# ============================================================================
install(TARGETS PavementCalculationEngine
    EXPORT PavementCalculationEngineTargets
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

# Install headers
install(FILES ${LIBRARY_HEADERS}
    DESTINATION include/PavementCalculationEngine
)

# Install export configuration
install(EXPORT PavementCalculationEngineTargets
    FILE PavementCalculationEngineTargets.cmake
    NAMESPACE PavementCalculationEngine::
    DESTINATION lib/cmake/PavementCalculationEngine
)

# Install test executable if built
if(BUILD_EXECUTABLE AND TARGET PavementCalculationTest)
    install(TARGETS PavementCalculationTest
        RUNTIME DESTINATION bin
    )
endif()

# ============================================================================
# Configuration Summary
# ============================================================================
message(STATUS "")
message(STATUS "========================================")
message(STATUS "PavementCalculationEngine Configuration")
message(STATUS "========================================")
message(STATUS "Version: ${PROJECT_VERSION}")
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "C++ standard: ${CMAKE_CXX_STANDARD}")
message(STATUS "Shared library: ${BUILD_SHARED_LIBS}")
message(STATUS "Build tests: ${BUILD_TESTS}")
message(STATUS "Build executable: ${BUILD_EXECUTABLE}")
message(STATUS "Eigen3: ${Eigen3_FOUND}")
message(STATUS "Boost: ${Boost_FOUND}")
message(STATUS "========================================")
message(STATUS "")
message(STATUS "=== PavementCalculationEngine Configuration ===")
message(STATUS "CMake version: ${CMAKE_VERSION}")
message(STATUS "C++ standard: ${CMAKE_CXX_STANDARD}")
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "Boost found: ${Boost_FOUND}")
message(STATUS "Eigen3 found: ${Eigen3_FOUND}")
if(EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/extern/eigen")
    message(STATUS "Local Eigen available: YES")
else()
    message(STATUS "Local Eigen available: NO")
endif()
message(STATUS "===============================================")
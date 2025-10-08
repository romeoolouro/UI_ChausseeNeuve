# Pavement Calculation Engine

This is the core calculation engine for pavement analysis, implemented as a C++ library with Python integration.

## Architecture

- **C++ Core**: Native calculation engine for performance-critical operations
- **Python Bridge**: Validated PyMastic integration using subprocess interface
- **DLL Interface**: C API for integration with .NET applications

## Components

### Production Components
- `src/` - C++ source code
  - `PavementAPI.cpp` - Main C API interface
  - `PyMasticPythonBridge.cpp/.h` - Python subprocess integration
  - `TRMMSolver.cpp` - TRMM calculation implementation
- `include/` - Header files
- `pymastic_bridge.py` - Python calculation interface (validated against Tableau I.1)
- `extern/` - External dependencies (PyMastic Python library)

### Build System
- `CMakeLists.txt` - CMake configuration
- `vcpkg.json` - Package dependencies
- `build_dll_clean.bat` - Clean build script

### Development Tools
- `debug-scripts/` - Development and validation scripts
- `tests/` - Unit and integration tests

## Current Status

✅ **PyMastic Python Bridge**: Production-ready with 0.01% accuracy validation
✅ **C API Integration**: Complete for .NET integration
🔄 **C++ PyMastic**: Future optimization target (see docs/PYMASTIC_CPP_DEBUG_PLAN.md)

## Build Instructions

```bash
# Clean build
build_dll_clean.bat

# Or manual build
mkdir build-dll
cd build-dll
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build .
```

## Usage

The engine is used via the C API defined in `PavementAPI.cpp`. The main calculation function uses the validated Python bridge:

```c
double PavementCalculatePyMastic(/* parameters */);
```

For development debugging, see `debug-scripts/` folder.
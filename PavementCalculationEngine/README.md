# Pavement Calculation Engine

This is the core calculation engine for pavement analysis, integrating the validated [PyMastic Python library](https://github.com/Mostafa-Nakhaei/PyMastic) via subprocess interface.

## Architecture

- **Python Bridge (PRODUCTION)**: Subprocess integration with PyMastic Python library
- **C++ Experimental**: TRMM and PyMastic C++ ports (precision errors - under development)
- **DLL Interface**: C API for .NET integration

## Components

### Production Components
- `src/` - C++ source code
  - `PavementAPI.cpp` - Main C API interface
  - `PyMasticPythonBridge.cpp/.h` - **PRODUCTION ALGORITHM** - Python subprocess integration
- `include/` - Header files
- `pymastic_bridge.py` - **PRODUCTION** Python calculation interface (validated 0.01% error)
- `extern/PyMastic/` - Official PyMastic library from https://github.com/Mostafa-Nakhaei/PyMastic

### Experimental Components (Precision Errors)
- `src/TRMMSolver.cpp` - TRMM implementation (precision errors)
- `src/PyMasticSolver.cpp` - C++ PyMastic port (>1500× precision error)
- See `docs/PYMASTIC_CPP_DEBUG_PLAN.md` for debugging strategy

### Build System
- `CMakeLists.txt` - CMake configuration
- `build_dll_clean.bat` - Clean build script

### Development Tools
- `debug-scripts/` - Development and validation scripts
- `tests/` - Unit and integration tests

## Current Status

✅ **PyMastic Python Bridge**: **PRODUCTION-READY** - 0.01% accuracy validation against Tableau I.1  
❌ **TRMM C++ Solver**: Precision errors - Future development  
❌ **PyMastic C++ Port**: Significant precision errors (>1500×) - Future optimization  
✅ **C API Integration**: Complete for .NET integration

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

The engine is used via the C API defined in `PavementAPI.cpp`. The main calculation function uses the **validated Python bridge**:

```c
double PavementCalculatePyMastic(/* parameters */);
```

This function internally calls the PyMastic Python library via subprocess for guaranteed accuracy.

For development debugging, see `debug-scripts/` folder.
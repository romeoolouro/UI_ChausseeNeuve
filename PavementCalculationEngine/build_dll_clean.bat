@echo off
echo === Building PyMastic DLL v2.1 with Version Tracking ===
echo Build Time: %DATE% %TIME%
echo Fix: E*1000 psi conversion

cd /d "%~dp0"

if not exist "build-dll\bin" mkdir "build-dll\bin"

echo.
echo Compiling DLL with all source files...
g++ -shared -o build-dll\bin\PavementCalculationEngine.dll ^
    -DPAVEMENT_EXPORTS ^
    src\PavementAPI.cpp ^
    src\PavementCalculator.cpp ^
    src\PavementData.cpp ^
    src\MatrixOperations.cpp ^
    src\TRMMSolver.cpp ^
    src\PyMasticSolver.cpp ^
    -I./include ^
    -I./extern/eigen ^
    -std=c++17 ^
    -O2 ^
    -Wl,--out-implib,build-dll\bin\libPavementCalculationEngine.dll.a

if %ERRORLEVEL% EQU 0 (
    echo.
    echo === BUILD SUCCESS ===
    echo DLL Location: build-dll\bin\PavementCalculationEngine.dll
    echo Version: PyMastic v2.1 - E*1000 fix - %DATE% %TIME%
) else (
    echo.
    echo === BUILD FAILED ===
    exit /b 1
)

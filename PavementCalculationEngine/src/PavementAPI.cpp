/**
 * @file PavementAPI.cpp
 * @brief Implementation of C API wrapper for Pavement Calculation Engine
 * 
 * This file bridges the C++ calculation engine with a pure C API for P/Invoke.
 * It handles:
 * - C++ exception to C error code translation
 * - Memory management for output structures
 * - Input validation and error reporting
 * - Thread-local error storage
 * 
 * @author Pavement Calculation Team
 * @date 2025-10-04
 */

#include "PavementAPI.h"
#include "PavementData.h"
#include "PavementCalculator.h"
#include "Logger.h"
#include <cstring>
#include <cstdlib>
#include <string>
#include <chrono>

// Alias for convenience
using PavementData = Pavement::CalculationInput;
using PavementOutput = Pavement::CalculationOutput;
using PavementCalc = Pavement::PavementCalculator;

// Thread-local storage for last error message (thread-safe)
#ifdef _WIN32
    static __declspec(thread) char g_last_error[256] = {0};
#else
    static __thread char g_last_error[256] = {0};
#endif

/**
 * @brief Set thread-local error message
 */
static void SetLastError(const char* message) {
    if (message) {
        strncpy(g_last_error, message, sizeof(g_last_error) - 1);
        g_last_error[sizeof(g_last_error) - 1] = '\0';
    }
}

/**
 * @brief Convert C input structure to C++ CalculationInput
 */
static bool ConvertInputToCpp(const PavementInputC* input, PavementData& data) {
    if (!input) {
        SetLastError("Input pointer is NULL");
        return false;
    }
    
    // Basic validation
    if (input->nlayer < 1 || input->nlayer > 20) {
        SetLastError("Number of layers must be between 1 and 20");
        return false;
    }
    
    if (input->nz < 1) {
        SetLastError("Number of calculation points must be at least 1");
        return false;
    }
    
    // Check for NULL arrays
    if (!input->poisson_ratio || !input->young_modulus || !input->thickness) {
        SetLastError("Material property arrays cannot be NULL");
        return false;
    }
    
    if (input->nlayer > 1 && !input->bonded_interface) {
        SetLastError("Bonded interface array cannot be NULL for multi-layer structures");
        return false;
    }
    
    if (!input->z_coords) {
        SetLastError("Z-coordinates array cannot be NULL");
        return false;
    }
    
    // Copy data to C++ structure (using CalculationInput field names)
    data.layerCount = input->nlayer;
    data.poissonRatios.assign(input->poisson_ratio, input->poisson_ratio + input->nlayer);
    data.youngModuli.assign(input->young_modulus, input->young_modulus + input->nlayer);
    data.thicknesses.assign(input->thickness, input->thickness + input->nlayer);
    
    if (input->nlayer > 1) {
        data.interfaceTypes.assign(input->bonded_interface, input->bonded_interface + (input->nlayer - 1));
    }
    
    data.wheelType = input->wheel_type + 1;  // C API: 0=simple, 1=twin; C++: 1=isolated, 2=twin
    data.pressure = input->pressure_kpa / 1000.0;  // kPa -> MPa
    data.contactRadius = input->wheel_radius_m;
    data.wheelSpacing = input->wheel_spacing_m;
    
    return true;
}

/**
 * @brief Allocate and populate output arrays
 */
static bool AllocateOutputArrays(PavementOutputC* output, const PavementOutput& results, int nz) {
    if (!output) {
        return false;
    }
    
    // Allocate arrays
    output->deflection_mm = (double*)malloc(nz * sizeof(double));
    output->vertical_stress_kpa = (double*)malloc(nz * sizeof(double));
    output->horizontal_strain = (double*)malloc(nz * sizeof(double));
    output->radial_strain = (double*)malloc(nz * sizeof(double));
    output->shear_stress_kpa = (double*)malloc(nz * sizeof(double));
    
    // Check allocation success
    if (!output->deflection_mm || !output->vertical_stress_kpa || 
        !output->horizontal_strain || !output->radial_strain || 
        !output->shear_stress_kpa) {
        
        // Free any successful allocations
        PavementFreeOutput(output);
        SetLastError("Failed to allocate output arrays");
        return false;
    }
    
    // Copy data from C++ vectors (mapping CalculationOutput fields)
    for (int i = 0; i < nz; ++i) {
        output->deflection_mm[i] = results.deflection[i];
        output->vertical_stress_kpa[i] = results.sigmaZ[i] * 1000.0;  // MPa -> kPa
        output->horizontal_strain[i] = results.epsilonT[i];
        output->radial_strain[i] = results.epsilonT[i];  // Same as horizontal for axisymmetric
        output->shear_stress_kpa[i] = 0.0;  // Not computed in Phase 1
    }
    
    output->nz = nz;
    
    return true;
}

// ============================================================================
// Public API Implementation
// ============================================================================

extern "C" {

PAVEMENT_API int PavementCalculate(
    const PavementInputC* input,
    PavementOutputC* output
) {
    // Clear previous error
    g_last_error[0] = '\0';
    
    // Validate pointers
    if (!input) {
        SetLastError("Input pointer is NULL");
        if (output) {
            output->success = 0;
            output->error_code = PAVEMENT_ERROR_NULL_POINTER;
            strncpy(output->error_message, "Input pointer is NULL", sizeof(output->error_message) - 1);
        }
        return PAVEMENT_ERROR_NULL_POINTER;
    }
    
    if (!output) {
        SetLastError("Output pointer is NULL");
        return PAVEMENT_ERROR_NULL_POINTER;
    }
    
    // Initialize output structure
    memset(output, 0, sizeof(PavementOutputC));
    
    try {
        // Start timing
        auto start_time = std::chrono::high_resolution_clock::now();
        
        // Convert input
        PavementData inputData;
        if (!ConvertInputToCpp(input, inputData)) {
            output->success = 0;
            output->error_code = PAVEMENT_ERROR_INVALID_INPUT;
            strncpy(output->error_message, g_last_error, sizeof(output->error_message) - 1);
            std::string error_msg = std::string("Input conversion failed: ") + g_last_error;
            Pavement::Logger::GetInstance().Error(error_msg.c_str(), __FILE__, __LINE__);
            return PAVEMENT_ERROR_INVALID_INPUT;
        }
        
        // Validate input
        try {
            inputData.Validate();
        } catch (const std::exception& e) {
            output->success = 0;
            output->error_code = PAVEMENT_ERROR_INVALID_INPUT;
            strncpy(output->error_message, e.what(), sizeof(output->error_message) - 1);
            SetLastError(e.what());
            std::string error_msg = std::string("Input validation failed: ") + e.what();
            Pavement::Logger::GetInstance().Error(error_msg.c_str(), __FILE__, __LINE__);
            return PAVEMENT_ERROR_INVALID_INPUT;
        }
        
        // Perform calculation
        std::string calc_start_msg = "Starting pavement calculation via C API for " + std::to_string(inputData.layerCount) + " layers";
        Pavement::Logger::GetInstance().Info(calc_start_msg.c_str(), __FILE__, __LINE__);
        
        PavementCalc calculator;
        PavementOutput outputData;
        
        try {
            outputData = calculator.Calculate(inputData);
        } catch (const std::exception& e) {
            output->success = 0;
            output->error_code = PAVEMENT_ERROR_CALCULATION;
            std::string error_msg = std::string("Calculation failed: ") + e.what();
            strncpy(output->error_message, error_msg.c_str(), sizeof(output->error_message) - 1);
            SetLastError(error_msg.c_str());
            Pavement::Logger::GetInstance().Error(error_msg.c_str(), __FILE__, __LINE__);
            return PAVEMENT_ERROR_CALCULATION;
        }
        
        // Allocate and populate output arrays
        if (!AllocateOutputArrays(output, outputData, input->nz)) {
            output->success = 0;
            output->error_code = PAVEMENT_ERROR_ALLOCATION;
            strncpy(output->error_message, g_last_error, sizeof(output->error_message) - 1);
            std::string error_msg = std::string("Output allocation failed: ") + g_last_error;
            Pavement::Logger::GetInstance().Error(error_msg.c_str(), __FILE__, __LINE__);
            return PAVEMENT_ERROR_ALLOCATION;
        }
        
        // Calculate elapsed time
        auto end_time = std::chrono::high_resolution_clock::now();
        auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end_time - start_time);
        output->calculation_time_ms = duration.count() / 1000.0;
        
        // Success
        output->success = 1;
        output->error_code = PAVEMENT_SUCCESS;
        strncpy(output->error_message, "Calculation completed successfully", 
                sizeof(output->error_message) - 1);
        
        std::string success_msg = "Calculation completed successfully in " + std::to_string(output->calculation_time_ms) + " ms";
        Pavement::Logger::GetInstance().Info(success_msg.c_str(), __FILE__, __LINE__);
        
        return PAVEMENT_SUCCESS;
        
    } catch (const std::bad_alloc& e) {
        output->success = 0;
        output->error_code = PAVEMENT_ERROR_ALLOCATION;
        strncpy(output->error_message, "Memory allocation failed", sizeof(output->error_message) - 1);
        SetLastError("Memory allocation failed");
        std::string error_msg = std::string("Bad alloc exception in PavementCalculate: ") + e.what();
        Pavement::Logger::GetInstance().Critical(error_msg.c_str(), __FILE__, __LINE__);
        return PAVEMENT_ERROR_ALLOCATION;
        
    } catch (const std::exception& e) {
        output->success = 0;
        output->error_code = PAVEMENT_ERROR_UNKNOWN;
        std::string error_msg = std::string("Exception: ") + e.what();
        strncpy(output->error_message, error_msg.c_str(), sizeof(output->error_message) - 1);
        SetLastError(error_msg.c_str());
        std::string critical_msg = std::string("Exception in PavementCalculate: ") + e.what();
        Pavement::Logger::GetInstance().Critical(critical_msg.c_str(), __FILE__, __LINE__);
        return PAVEMENT_ERROR_UNKNOWN;
        
    } catch (...) {
        output->success = 0;
        output->error_code = PAVEMENT_ERROR_UNKNOWN;
        strncpy(output->error_message, "Unknown exception occurred", sizeof(output->error_message) - 1);
        SetLastError("Unknown exception");
        LOG_CRITICAL("Unknown exception in PavementCalculate");
        return PAVEMENT_ERROR_UNKNOWN;
    }
}

PAVEMENT_API void PavementFreeOutput(PavementOutputC* output) {
    if (!output) {
        return;
    }
    
    // Free all allocated arrays
    if (output->deflection_mm) {
        free(output->deflection_mm);
        output->deflection_mm = nullptr;
    }
    
    if (output->vertical_stress_kpa) {
        free(output->vertical_stress_kpa);
        output->vertical_stress_kpa = nullptr;
    }
    
    if (output->horizontal_strain) {
        free(output->horizontal_strain);
        output->horizontal_strain = nullptr;
    }
    
    if (output->radial_strain) {
        free(output->radial_strain);
        output->radial_strain = nullptr;
    }
    
    if (output->shear_stress_kpa) {
        free(output->shear_stress_kpa);
        output->shear_stress_kpa = nullptr;
    }
    
    // Clear metadata
    output->success = 0;
    output->error_code = PAVEMENT_SUCCESS;
    output->nz = 0;
    output->calculation_time_ms = 0.0;
    output->error_message[0] = '\0';
}

PAVEMENT_API const char* PavementGetVersion(void) {
    return "1.0.0";
}

PAVEMENT_API const char* PavementGetLastError(void) {
    return g_last_error;
}

PAVEMENT_API int PavementValidateInput(
    const PavementInputC* input,
    char* error_message,
    int message_size
) {
    // Clear previous error
    g_last_error[0] = '\0';
    
    if (!input) {
        const char* msg = "Input pointer is NULL";
        SetLastError(msg);
        if (error_message && message_size > 0) {
            strncpy(error_message, msg, message_size - 1);
            error_message[message_size - 1] = '\0';
        }
        return PAVEMENT_ERROR_NULL_POINTER;
    }
    
    try {
        // Convert and validate
        PavementData inputData;
        if (!ConvertInputToCpp(input, inputData)) {
            if (error_message && message_size > 0) {
                strncpy(error_message, g_last_error, message_size - 1);
                error_message[message_size - 1] = '\0';
            }
            return PAVEMENT_ERROR_INVALID_INPUT;
        }
        
        try {
            inputData.Validate();
        } catch (const std::exception& e) {
            SetLastError(e.what());
            if (error_message && message_size > 0) {
                strncpy(error_message, e.what(), message_size - 1);
                error_message[message_size - 1] = '\0';
            }
            return PAVEMENT_ERROR_INVALID_INPUT;
        }
        
        // Success
        if (error_message && message_size > 0) {
            strncpy(error_message, "Input is valid", message_size - 1);
            error_message[message_size - 1] = '\0';
        }
        return PAVEMENT_SUCCESS;
        
    } catch (const std::exception& e) {
        std::string error_msg = std::string("Exception: ") + e.what();
        SetLastError(error_msg.c_str());
        if (error_message && message_size > 0) {
            strncpy(error_message, error_msg.c_str(), message_size - 1);
            error_message[message_size - 1] = '\0';
        }
        return PAVEMENT_ERROR_UNKNOWN;
        
    } catch (...) {
        const char* msg = "Unknown exception occurred";
        SetLastError(msg);
        if (error_message && message_size > 0) {
            strncpy(error_message, msg, message_size - 1);
            error_message[message_size - 1] = '\0';
        }
        return PAVEMENT_ERROR_UNKNOWN;
    }
}

} // extern "C"

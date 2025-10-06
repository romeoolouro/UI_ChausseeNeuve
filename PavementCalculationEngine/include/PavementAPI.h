/**
 * @file PavementAPI.h
 * @brief C API for Pavement Calculation Engine - P/Invoke Compatible Interface
 * 
 * This header provides a pure C API for interoperability with .NET via P/Invoke.
 * All functions use extern "C" linkage and simple C types for cross-language compatibility.
 * 
 * @author Pavement Calculation Team
 * @date 2025-10-04
 * @version 1.0.0
 */

#ifndef PAVEMENT_API_H
#define PAVEMENT_API_H

#ifdef __cplusplus
extern "C" {
#endif

// Platform-specific DLL export/import macros
#ifdef _WIN32
    #ifdef PAVEMENT_EXPORTS
        #define PAVEMENT_API __declspec(dllexport)
    #else
        #define PAVEMENT_API __declspec(dllimport)
    #endif
#else
    #define PAVEMENT_API __attribute__((visibility("default")))
#endif

/**
 * @brief Error codes returned by API functions
 */
typedef enum {
    PAVEMENT_SUCCESS = 0,              ///< Operation completed successfully
    PAVEMENT_ERROR_INVALID_INPUT = 1,  ///< Invalid input parameters
    PAVEMENT_ERROR_NULL_POINTER = 2,   ///< Null pointer provided
    PAVEMENT_ERROR_ALLOCATION = 3,     ///< Memory allocation failed
    PAVEMENT_ERROR_CALCULATION = 4,    ///< Calculation error (singular matrix, overflow, etc.)
    PAVEMENT_ERROR_UNKNOWN = 99        ///< Unknown error occurred
} PavementErrorCode;

/**
 * @brief Wheel type configuration
 */
typedef enum {
    WHEEL_TYPE_SIMPLE = 0,    ///< Simple wheel configuration
    WHEEL_TYPE_TWIN = 1       ///< Twin (dual) wheel configuration
} WheelType;

/**
 * @brief Input data structure for pavement calculation (C-compatible)
 * 
 * Layout: Sequential (no padding) for P/Invoke marshalling
 * All arrays are heap-allocated and must be freed by caller after use
 */
typedef struct {
    // Layer configuration
    int nlayer;                    ///< Number of layers (1 to 20)
    double* poisson_ratio;         ///< Poisson''s ratios for each layer (nlayer elements)
    double* young_modulus;         ///< Young''s moduli in MPa (nlayer elements)
    double* thickness;             ///< Layer thicknesses in meters (nlayer elements)
    int* bonded_interface;         ///< Interface bonding flags (nlayer-1 elements): 1=bonded, 0=unbonded
    
    // Load configuration
    int wheel_type;                ///< Wheel type (0=simple, 1=twin) - see WheelType enum
    double pressure_kpa;           ///< Wheel pressure in kPa (0 to 2000)
    double wheel_radius_m;         ///< Wheel radius in meters (>0)
    double wheel_spacing_m;        ///< Wheel spacing in meters (for twin wheels, >0)
    
    // Calculation points
    int nz;                        ///< Number of vertical calculation points (>0)
    double* z_coords;              ///< Z-coordinates for calculation in meters (nz elements)
} PavementInputC;

/**
 * @brief Output data structure for pavement calculation (C-compatible)
 * 
 * Layout: Sequential (no padding) for P/Invoke marshalling
 * All arrays are allocated by the DLL and must be freed using PavementFreeOutput
 */
typedef struct {
    // Status information
    int success;                   ///< 1 if calculation succeeded, 0 otherwise
    int error_code;                ///< Error code (see PavementErrorCode enum)
    char error_message[256];       ///< Human-readable error message (UTF-8)
    
    // Calculation metadata
    int nz;                        ///< Number of calculation points (matches input if successful)
    double calculation_time_ms;    ///< Calculation time in milliseconds
    
    // Results arrays (allocated by DLL, size = nz)
    double* deflection_mm;         ///< Vertical deflections in mm (positive downward)
    double* vertical_stress_kpa;   ///< Vertical stresses in kPa (positive compression)
    double* horizontal_strain;     ///< Horizontal strains in microstrain
    double* radial_strain;         ///< Radial strains in microstrain
    double* shear_stress_kpa;      ///< Shear stresses in kPa
} PavementOutputC;

/**
 * @brief Main calculation function
 * 
 * Performs pavement structure calculation using layered elastic theory.
 * 
 * @param input Pointer to input structure (must not be NULL)
 * @param output Pointer to output structure (must not be NULL, will be populated by DLL)
 * @return PAVEMENT_SUCCESS on success, error code otherwise
 */
PAVEMENT_API int PavementCalculate(
    const PavementInputC* input,
    PavementOutputC* output
);

/**
 * @brief Numerically stable calculation using TRMM (Transmission and Reflection Matrix Method)
 * 
 * This function uses only negative exponentials exp(-m*h) to avoid overflow
 * when m*h > 30, ensuring all matrix elements remain bounded (less than or equal to 1.0).
 * 
 * Based on academic research by Qiu et al. (2025), Dong et al. (2021), Fan et al. (2022).
 * 
 * @param input Pointer to input structure (must not be NULL)
 * @param output Pointer to output structure (must not be NULL, will be populated by DLL)
 * @return PAVEMENT_SUCCESS on success, error code otherwise
 * 
 * @note Use this function for cases with high m*h values (thick layers, high stiffness contrast)
 * @note Output arrays are allocated by the DLL and must be freed with PavementFreeOutput
 */
PAVEMENT_API int PavementCalculateStable(
    const PavementInputC* input,
    PavementOutputC* output
);

/**
 * @brief Free output structure memory
 * 
 * Releases all memory allocated by PavementCalculate in the output structure.
 * Safe to call multiple times on the same structure (idempotent).
 * 
 * @param output Pointer to output structure (can be NULL, in which case no-op)
 */
PAVEMENT_API void PavementFreeOutput(PavementOutputC* output);

/**
 * @brief Get library version string
 * 
 * @return Version string in format "MAJOR.MINOR.PATCH" (e.g., "1.0.0")
 * @note Returned string is statically allocated and should not be freed
 */
PAVEMENT_API const char* PavementGetVersion(void);

/**
 * @brief Get detailed error message for last error
 * 
 * @return Human-readable error description (UTF-8)
 * @note Returned string is statically allocated and should not be freed
 * @note Thread-local storage ensures thread safety
 */
PAVEMENT_API const char* PavementGetLastError(void);

/**
 * @brief Validate input data structure
 * 
 * Checks input parameters for validity without performing calculation.
 * Useful for pre-flight validation from managed code.
 * 
 * @param input Pointer to input structure (must not be NULL)
 * @param error_message Buffer to receive error message (can be NULL)
 * @param message_size Size of error_message buffer (ignored if error_message is NULL)
 * @return PAVEMENT_SUCCESS if input is valid, error code otherwise
 */
PAVEMENT_API int PavementValidateInput(
    const PavementInputC* input,
    char* error_message,
    int message_size
);

#ifdef __cplusplus
}
#endif

#endif // PAVEMENT_API_H

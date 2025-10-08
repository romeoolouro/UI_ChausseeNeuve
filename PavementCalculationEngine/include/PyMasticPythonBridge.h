#pragma once

#include <string>
#include <vector>
#include <memory>

/**
 * @brief Python PyMastic Bridge - Interface to validated PyMastic Python
 * 
 * This class provides a C++ interface to the validated PyMastic Python implementation.
 * It uses subprocess calls to execute the Python code and parse JSON results.
 * 
 * Accuracy: 0.01% vs academic Tableau I.1 (validated)
 * Performance: ~1-2 seconds per calculation (subprocess overhead)
 * 
 * @author Pavement Calculation Engine Team
 * @date 2025-10-08
 */
class PyMasticPythonBridge {
public:
    struct Input {
        double q_kpa;                          ///< Pressure in kPa
        double a_m;                           ///< Radius in meters
        std::vector<double> z_depths_m;       ///< Measurement depths in meters
        std::vector<double> H_thicknesses_m;  ///< Layer thicknesses in meters (excluding infinite)
        std::vector<double> E_moduli_mpa;     ///< Elastic moduli in MPa
        std::vector<double> nu_poisson;       ///< Poisson ratios (dimensionless)
        std::vector<int> bonded_interfaces;   ///< Interface bonding: 1=bonded, 0=frictionless
    };
    
    struct Output {
        bool success;                         ///< Calculation success flag
        std::vector<double> displacement_z_m; ///< Vertical displacement in meters
        std::vector<double> stress_z_mpa;     ///< Vertical stress in MPa
        std::vector<double> strain_z_microdef; ///< Vertical strain in microstrain
        std::vector<double> strain_r_microdef; ///< Radial strain in microstrain
        std::string error_message;            ///< Error message if success=false
    };
    
    /**
     * @brief Calculate pavement response using validated PyMastic Python
     * @param input Calculation parameters
     * @return Computed results or error information
     */
    static Output Calculate(const Input& input);
    
private:
    /**
     * @brief Execute Python subprocess with JSON input/output
     * @param json_input JSON string with calculation parameters
     * @return JSON string with results or error
     */
    static std::string ExecutePythonBridge(const std::string& json_input);
    
    /**
     * @brief Convert Input struct to JSON string
     */
    static std::string InputToJson(const Input& input);
    
    /**
     * @brief Parse JSON result string to Output struct
     */
    static Output ParseJsonOutput(const std::string& json_output);
};
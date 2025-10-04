#pragma once
#include <vector>
#include <stdexcept>
#include <string>

namespace Pavement {

/**
 * @brief Encapsulated input data structure for pavement calculation
 * 
 * This structure replaces the global variables from the original C++ code:
 * - nbrecouche, Mu, Young, epais, tabInterface, roue, Poids, a, d
 * 
 * Provides thread-safety and reusability by eliminating global state.
 */
struct CalculationInput {
    // Layer configuration
    int layerCount;                          // Replaces: nbrecouche
    std::vector<double> poissonRatios;       // Replaces: Mu
    std::vector<double> youngModuli;         // Replaces: Young (MPa)
    std::vector<double> thicknesses;         // Replaces: epais (meters)
    std::vector<int> interfaceTypes;         // Replaces: tabInterface (0=bonded, 1=semi, 2=unbonded)
    
    // Load configuration
    int wheelType;                           // Replaces: roue (1=isolated, 2=twin)
    double pressure;                         // Replaces: Poids (MPa)
    double contactRadius;                    // Replaces: a (meters)
    double wheelSpacing;                     // Replaces: d (meters, for twin wheels)
    
    // Constructor with validation
    CalculationInput();
    
    // Validation method
    void Validate() const;
    
    // String representation for debugging
    std::string ToString() const;
    
    // Initialize with default values
    void SetDefaults();
};

/**
 * @brief Encapsulated output data structure for calculation results
 * 
 * Contains all solicitation values at layer interfaces.
 * Size = 2 * layerCount - 1 (top and bottom of each layer except infinite platform)
 */
struct CalculationOutput {
    std::vector<double> sigmaT;      // Horizontal stress (MPa)
    std::vector<double> epsilonT;    // Horizontal strain (microdef)
    std::vector<double> sigmaZ;      // Vertical stress (MPa) 
    std::vector<double> epsilonZ;    // Vertical strain (microdef)
    std::vector<double> deflection;  // Vertical displacement (mm)
    
    // Constructor
    CalculationOutput();
    
    // Resize all vectors to match result count
    void Resize(int size);
    
    // Clear all results
    void Clear();
    
    // String representation for debugging
    std::string ToString() const;
};

/**
 * @brief Internal working data for calculations
 * 
 * Encapsulates intermediate calculation arrays that were global:
 * - MuCalcul, zcalcul, YoungCalcul, k
 */
struct WorkingData {
    std::vector<double> muCalcul;           // Replaces: MuCalcul(2 * nbrecouche)
    std::vector<double> zCalcul;            // Replaces: zcalcul(2 * nbrecouche + 1) 
    std::vector<double> youngCalcul;        // Replaces: YoungCalcul(2 * nbrecouche)
    int matrixSize;                         // Replaces: k = 4 * nbrecouche - 2
    
    // Constructor
    WorkingData();
    
    // Initialize based on layer count
    void Initialize(int layerCount);
    
    // Clear all working arrays
    void Clear();
};

}  // namespace Pavement
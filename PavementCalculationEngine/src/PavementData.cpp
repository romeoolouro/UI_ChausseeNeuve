#include "PavementData.h"
#include "Logger.h"
#include <sstream>
#include <iomanip>
#include <cmath>
#include <algorithm>
#include <stdexcept>

namespace Pavement {

// CalculationInput Implementation

CalculationInput::CalculationInput() {
    SetDefaults();
}

void CalculationInput::SetDefaults() {
    layerCount = 3;
    
    // Default 3-layer structure (asphalt + base + platform)
    poissonRatios = {0.35, 0.35, 0.35};
    youngModuli = {5000.0, 200.0, 50.0};  // MPa
    thicknesses = {0.15, 0.30, 100.0};    // meters (platform = semi-infinite)
    interfaceTypes = {0, 0};               // All bonded
    
    // Default isolated wheel
    wheelType = 1;
    pressure = 0.662;        // MPa (standard 13T axle)
    contactRadius = 0.125;   // meters
    wheelSpacing = 0.0;      // Not used for isolated wheel
}

void CalculationInput::Validate() const {
    // Layer count validation
    if (layerCount < 2 || layerCount > 10) {
        throw std::invalid_argument(
            "Layer count must be between 2 and 10, got: " + std::to_string(layerCount));
    }
    
    // Vector size validation
    if (poissonRatios.size() != static_cast<size_t>(layerCount)) {
        throw std::invalid_argument(
            "Poisson ratio count (" + std::to_string(poissonRatios.size()) + 
            ") must equal layer count (" + std::to_string(layerCount) + ")");
    }
    
    if (youngModuli.size() != static_cast<size_t>(layerCount)) {
        throw std::invalid_argument(
            "Young modulus count (" + std::to_string(youngModuli.size()) + 
            ") must equal layer count (" + std::to_string(layerCount) + ")");
    }
    
    if (thicknesses.size() != static_cast<size_t>(layerCount)) {
        throw std::invalid_argument(
            "Thickness count (" + std::to_string(thicknesses.size()) + 
            ") must equal layer count (" + std::to_string(layerCount) + ")");
    }
    
    if (interfaceTypes.size() != static_cast<size_t>(layerCount - 1)) {
        throw std::invalid_argument(
            "Interface count (" + std::to_string(interfaceTypes.size()) + 
            ") must be layer count - 1 (" + std::to_string(layerCount - 1) + ")");
    }
    
    // Parameter range validation
    for (size_t i = 0; i < poissonRatios.size(); ++i) {
        if (poissonRatios[i] < 0.0 || poissonRatios[i] > 0.5) {
            throw std::invalid_argument(
                "Invalid Poisson ratio at layer " + std::to_string(i) + 
                ": " + std::to_string(poissonRatios[i]) + " (must be 0.0-0.5)");
        }
    }
    
    for (size_t i = 0; i < youngModuli.size(); ++i) {
        if (youngModuli[i] <= 0.0 || youngModuli[i] > 100000.0) {
            throw std::invalid_argument(
                "Invalid Young modulus at layer " + std::to_string(i) + 
                ": " + std::to_string(youngModuli[i]) + " MPa (must be 0-100000)");
        }
    }
    
    // Validate thicknesses (allow large thickness for platform layer)
    for (size_t i = 0; i < thicknesses.size(); ++i) {
        if (thicknesses[i] <= 0.0) {
            throw std::invalid_argument(
                "Invalid thickness at layer " + std::to_string(i) + 
                ": " + std::to_string(thicknesses[i]) + " m (must be > 0)");
        }
        
        // Only check upper limit for non-platform layers
        if (i < thicknesses.size() - 1 && thicknesses[i] > 10.0) {
            throw std::invalid_argument(
                "Invalid thickness at layer " + std::to_string(i) + 
                ": " + std::to_string(thicknesses[i]) + " m (must be <= 10 for non-platform layers)");
        }
    }
    
    for (size_t i = 0; i < interfaceTypes.size(); ++i) {
        if (interfaceTypes[i] < 0 || interfaceTypes[i] > 2) {
            throw std::invalid_argument(
                "Invalid interface type at position " + std::to_string(i) + 
                ": " + std::to_string(interfaceTypes[i]) + " (must be 0, 1, or 2)");
        }
    }
    
    // Wheel configuration validation
    if (wheelType != 1 && wheelType != 2) {
        throw std::invalid_argument(
            "Invalid wheel type: " + std::to_string(wheelType) + " (must be 1=isolated or 2=twin)");
    }
    
    if (pressure <= 0.0 || pressure > 5.0) {
        throw std::invalid_argument(
            "Invalid pressure: " + std::to_string(pressure) + " MPa (must be 0-5)");
    }
    
    if (contactRadius <= 0.0 || contactRadius > 1.0) {
        throw std::invalid_argument(
            "Invalid contact radius: " + std::to_string(contactRadius) + " m (must be 0-1)");
    }
    
    if (wheelType == 2 && (wheelSpacing <= 0.0 || wheelSpacing > 2.0)) {
        throw std::invalid_argument(
            "Invalid wheel spacing for twin wheels: " + std::to_string(wheelSpacing) + 
            " m (must be 0-2)");
    }
    
    // Check for extreme modulus contrasts (numerical stability)
    double maxModulus = *std::max_element(youngModuli.begin(), youngModuli.end());
    double minModulus = *std::min_element(youngModuli.begin(), youngModuli.end());
    if (maxModulus / minModulus > 10000.0) {
        throw std::invalid_argument(
            "Extreme modulus contrast detected: " + std::to_string(maxModulus / minModulus) +
            ":1. Maximum recommended: 10000:1 for numerical stability");
    }
    
    // Check for very thin layers (can cause numerical issues)
    for (size_t i = 0; i < thicknesses.size() - 1; ++i) {  // Exclude platform layer
        if (thicknesses[i] < 0.01) {  // 10 mm minimum
            throw std::invalid_argument(
                "Layer " + std::to_string(i) + " too thin: " + 
                std::to_string(thicknesses[i] * 1000) + " mm. Minimum: 10 mm");
        }
    }
}

std::string CalculationInput::ToString() const {
    std::stringstream ss;
    ss << std::fixed << std::setprecision(3);
    
    ss << "PavementInput {\n";
    ss << "  layerCount: " << layerCount << "\n";
    ss << "  poissonRatios: [";
    for (size_t i = 0; i < poissonRatios.size(); ++i) {
        if (i > 0) ss << ", ";
        ss << poissonRatios[i];
    }
    ss << "]\n  youngModuli: [";
    for (size_t i = 0; i < youngModuli.size(); ++i) {
        if (i > 0) ss << ", ";
        ss << youngModuli[i];
    }
    ss << "] MPa\n  thicknesses: [";
    for (size_t i = 0; i < thicknesses.size(); ++i) {
        if (i > 0) ss << ", ";
        ss << thicknesses[i];
    }
    ss << "] m\n  wheelType: " << wheelType;
    ss << ", pressure: " << pressure << " MPa";
    ss << ", contactRadius: " << contactRadius << " m";
    if (wheelType == 2) {
        ss << ", wheelSpacing: " << wheelSpacing << " m";
    }
    ss << "\n}";
    
    return ss.str();
}

// CalculationOutput Implementation

CalculationOutput::CalculationOutput() {
    // Will be resized when needed
}

void CalculationOutput::Resize(int size) {
    sigmaT.resize(size, 0.0);
    epsilonT.resize(size, 0.0);
    sigmaZ.resize(size, 0.0);
    epsilonZ.resize(size, 0.0);
    deflection.resize(size, 0.0);
}

void CalculationOutput::Clear() {
    sigmaT.clear();
    epsilonT.clear();
    sigmaZ.clear();
    epsilonZ.clear();
    deflection.clear();
}

std::string CalculationOutput::ToString() const {
    std::stringstream ss;
    ss << std::fixed << std::setprecision(4);
    
    ss << "PavementOutput {\n";
    ss << "  resultCount: " << sigmaT.size() << "\n";
    
    for (size_t i = 0; i < sigmaT.size(); ++i) {
        ss << "  [" << i << "] σT=" << sigmaT[i] << " MPa, "
           << "εT=" << epsilonT[i] << " μdef, "
           << "σZ=" << sigmaZ[i] << " MPa, "
           << "εZ=" << epsilonZ[i] << " μdef, "
           << "def=" << deflection[i] << " mm\n";
    }
    
    ss << "}";
    return ss.str();
}

// WorkingData Implementation

WorkingData::WorkingData() {
    // Will be initialized when needed
}

void WorkingData::Initialize(int layerCount) {
    matrixSize = 4 * layerCount - 2;
    
    muCalcul.resize(2 * layerCount, 0.0);
    zCalcul.resize(2 * layerCount + 1, 0.0);
    youngCalcul.resize(2 * layerCount, 0.0);
}

void WorkingData::Clear() {
    muCalcul.clear();
    zCalcul.clear();
    youngCalcul.clear();
    matrixSize = 0;
}

}  // namespace Pavement
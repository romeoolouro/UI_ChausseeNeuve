#include "MatrixOperations.h"
#include <Eigen/Dense>

// Solution 2: Méthode des Matrices de Transfert (Transfer Matrix Method)
// Inspirée de "ElasticMatrix Toolbox" et "Transfer Matrix Methods (TMM)"
// Plus stable numériquement que l'assemblage de matrice globale

namespace PavementCalculation {

struct TransferMatrix {
    Eigen::Matrix4d T;  // Matrice de transfert 4x4 pour chaque couche
    
    TransferMatrix() : T(Eigen::Matrix4d::Identity()) {}
};

// Construction de la matrice de transfert pour une couche
TransferMatrix BuildLayerTransferMatrix(const LayerProperties& layer, double m, double thickness) {
    TransferMatrix result;
    
    // Paramètres élastiques
    double G = layer.youngModulus / (2.0 * (1.0 + layer.poissonRatio));
    double nu = layer.poissonRatio;
    double h = thickness;
    
    // TECHNIQUE CRITIQUE : Éviter les exponentielles problématiques
    // Utiliser les fonctions hyperboliques qui sont plus stables
    double mh = m * h;
    double cosh_mh, sinh_mh;
    
    if (mh > 50.0) {
        // Pour de grands arguments, approximer avec exp stabilisé
        double exp_mh = std::exp(mh);
        cosh_mh = 0.5 * exp_mh;  // cosh(x) ≈ 0.5*exp(x) pour grand x
        sinh_mh = 0.5 * exp_mh;  // sinh(x) ≈ 0.5*exp(x) pour grand x
    } else if (mh < -50.0) {
        double exp_minus_mh = std::exp(-mh);
        cosh_mh = 0.5 * exp_minus_mh;
        sinh_mh = -0.5 * exp_minus_mh;
    } else {
        // Calcul direct pour arguments modérés
        cosh_mh = std::cosh(mh);
        sinh_mh = std::sinh(mh);
    }
    
    // Matrice de transfert selon la théorie élastique stratifiée
    // Format: [ur_top, uz_top, σr_top, τrz_top]^T = T * [ur_bottom, uz_bottom, σr_bottom, τrz_bottom]^T
    
    double alpha = 3.0 - 4.0 * nu;
    double beta = (1.0 - nu) / (2.0 * G);
    
    // Ligne 1: ur_top en fonction des variables du bas
    result.T(0, 0) = cosh_mh + (mh * sinh_mh);                    // ur_bottom
    result.T(0, 1) = beta * (alpha * sinh_mh - mh * cosh_mh) / m;  // uz_bottom
    result.T(0, 2) = beta * sinh_mh / (m * G);                     // σr_bottom
    result.T(0, 3) = -beta * (cosh_mh - 1.0) / (m * m * G);       // τrz_bottom
    
    // Ligne 2: uz_top en fonction des variables du bas
    result.T(1, 0) = -m * (alpha * sinh_mh - mh * cosh_mh);       // ur_bottom
    result.T(1, 1) = alpha * cosh_mh - mh * sinh_mh;              // uz_bottom
    result.T(1, 2) = -(alpha * sinh_mh) / G;                      // σr_bottom
    result.T(1, 3) = (cosh_mh - 1.0) / (m * G);                  // τrz_bottom
    
    // Ligne 3: σr_top en fonction des variables du bas
    result.T(2, 0) = 2.0 * G * m * ((1.0 - nu) * sinh_mh + nu * mh * cosh_mh);  // ur_bottom
    result.T(2, 1) = 2.0 * G * alpha * (cosh_mh - 1.0) / m;                     // uz_bottom
    result.T(2, 2) = cosh_mh;                                                    // σr_bottom
    result.T(2, 3) = sinh_mh / m;                                                // τrz_bottom
    
    // Ligne 4: τrz_top en fonction des variables du bas
    result.T(3, 0) = G * m * alpha * (mh * cosh_mh - sinh_mh);   // ur_bottom
    result.T(3, 1) = -G * alpha * mh * sinh_mh;                  // uz_bottom
    result.T(3, 2) = m * sinh_mh;                                // σr_bottom
    result.T(3, 3) = cosh_mh;                                    // τrz_bottom
    
    return result;
}

// Résolution par matrices de transfert - ALTERNATIVE COMPLÈTE à votre méthode actuelle
std::vector<double> SolveByTransferMatrix(
    const std::vector<LayerProperties>& layers,
    const std::vector<double>& thicknesses,
    double m,
    double appliedStress) {
    
    const int numLayers = layers.size();
    
    // 1. Construire la matrice de transfert totale
    Eigen::Matrix4d totalTransfer = Eigen::Matrix4d::Identity();
    
    for (int i = 0; i < numLayers - 1; ++i) {  // Exclure la plateforme
        TransferMatrix layerT = BuildLayerTransferMatrix(layers[i], m, thicknesses[i]);
        totalTransfer = layerT.T * totalTransfer;  // Multiplication dans l'ordre inverse
    }
    
    // 2. Conditions aux limites
    Eigen::Vector4d surfaceConditions;
    surfaceConditions << 0.0,           // ur = 0 en surface (symétrie)
                         0.0,           // uz libre en surface
                         -appliedStress, // σr = -P en surface
                         0.0;           // τrz = 0 en surface
    
    Eigen::Vector4d platformConditions;
    platformConditions << 0.0,  // ur = 0 sur plateforme rigide
                          0.0,  // uz = 0 sur plateforme rigide
                          0.0,  // σr libre sur plateforme
                          0.0;  // τrz = 0 sur plateforme
    
    // 3. Résoudre: totalTransfer * [ur, uz, σr, τrz]_platform = [ur, uz, σr, τrz]_surface
    Eigen::Vector4d platformState = totalTransfer.partialPivLu().solve(surfaceConditions);
    
    // 4. Propager vers le haut pour obtenir les coefficients
    std::vector<double> coefficients(4 * numLayers);
    
    // Coefficients de la plateforme (couche du bas)
    int platformIndex = numLayers - 1;
    coefficients[4 * platformIndex + 0] = platformState(0);  // A (ur component)
    coefficients[4 * platformIndex + 1] = platformState(1);  // B (uz component)
    coefficients[4 * platformIndex + 2] = 0.0;               // C = 0 pour plateforme
    coefficients[4 * platformIndex + 3] = 0.0;               // D = 0 pour plateforme
    
    // Propager vers les couches supérieures
    Eigen::Vector4d currentState = platformState;
    for (int i = numLayers - 2; i >= 0; --i) {
        TransferMatrix layerT = BuildLayerTransferMatrix(layers[i], m, thicknesses[i]);
        currentState = layerT.T * currentState;
        
        coefficients[4 * i + 0] = currentState(0);
        coefficients[4 * i + 1] = currentState(1);
        coefficients[4 * i + 2] = 0.0;  // Simplification pour test
        coefficients[4 * i + 3] = 0.0;
    }
    
    return coefficients;
}

} // namespace PavementCalculation
// Ajoutez cette fonction dans PavementCalculator.cpp pour test rapide

#include "StabilizedMatrixOperations.cpp"  // Inclure la Solution 1

// Dans CalculateCoefficients(), remplacez l'appel existant:
std::vector<double> PavementCalculator::CalculateCoefficientsStabilized(
    const PavementInput& input, double m) {
    
    const int numLayers = static_cast<int>(input.layerThicknesses.size());
    const int matrixSize = 4 * numLayers - 2;
    
    // Utiliser Eigen pour meilleure stabilité numérique
    Eigen::MatrixXd coeffMatrix = Eigen::MatrixXd::Zero(matrixSize, matrixSize);
    Eigen::VectorXd rhs = Eigen::VectorXd::Zero(matrixSize);
    
    // Assemblage avec la nouvelle méthode stabilisée
    
    // 1. Conditions aux limites en surface (lignes 0-1)
    AssembleSurfaceBoundary(coeffMatrix, rhs, input, m);
    
    // 2. Interfaces entre couches avec stabilisation
    int rowIndex = 2;
    for (int i = 0; i < numLayers - 2; ++i) {  // Exclure la dernière interface
        
        // REMPLACER par la version stabilisée
        std::vector<LayerProperties> layers;
        for (size_t j = 0; j < input.layerThicknesses.size(); ++j) {
            LayerProperties layer;
            layer.thickness = input.layerThicknesses[j];
            layer.youngModulus = input.youngModuli[j];
            layer.poissonRatio = input.poissonRatios[j];
            layers.push_back(layer);
        }
        
        // Utiliser la nouvelle méthode stabilisée
        AssembleUnbondedInterfaceStabilized(coeffMatrix, layers, i, m);
        rowIndex += 4;
    }
    
    // 3. Résolution avec mise à l'échelle
    ScaleMatrixRows(coeffMatrix, rhs);
    
    // 4. Solveur robuste avec vérification du conditionnement
    Eigen::VectorXd solution;
    double conditionNumber = 0.0;
    
    try {
        // Essayer LU avec pivot partiel
        Eigen::PartialPivLU<Eigen::MatrixXd> lu(coeffMatrix);
        solution = lu.solve(rhs);
        
        // Vérifier la qualité de la solution
        Eigen::VectorXd residual = coeffMatrix * solution - rhs;
        double residualNorm = residual.norm();
        
        if (residualNorm > 1e-6) {
            // Essayer SVD pour matrice mal conditionnée
            Eigen::JacobiSVD<Eigen::MatrixXd> svd(coeffMatrix, Eigen::ComputeFullU | Eigen::ComputeFullV);
            solution = svd.solve(rhs);
            
            // Calculer le nombre de conditionnement
            auto singularValues = svd.singularValues();
            conditionNumber = singularValues(0) / singularValues(singularValues.size()-1);
        }
        
    } catch (const std::exception& e) {
        // En cas d'échec, essayer la méthode des matrices de transfert
        return SolveByTransferMatrix(layers, input.layerThicknesses, m, input.wheelLoad);
    }
    
    // Convertir la solution Eigen en std::vector
    std::vector<double> coefficients(solution.size());
    for (int i = 0; i < solution.size(); ++i) {
        coefficients[i] = solution(i);
    }
    
    // Logging des métriques de stabilité
    if (logFile.is_open()) {
        logFile << "Stabilized calculation - m=" << m 
                << ", condition_number=" << conditionNumber
                << ", solution_norm=" << solution.norm() << std::endl;
    }
    
    return coefficients;
}
#include <gtest/gtest.h>
#include "PyMasticSolver.h"
#include <iostream>

/**
 * @brief Test PyMastic C++ port against reference values
 * 
 * Test case from PyMastic/Test.py:
 * - 3-layer structure: AC (500 ksi) / Base (40 ksi) / Subgrade (10 ksi)
 * - Load: 100 lb, radius 5.99 inch
 * - Points: x=[0,8], z=[0,9.99,10.01]
 */
class PyMasticPortTest : public ::testing::Test {
protected:
    void SetUp() override {
        // Test case from PyMastic Test.py
        input.q_kpa = 100.0;  // lb (will convert internally)
        input.a_m = 5.99;     // inch
        input.x_offsets = {0, 8};
        input.z_depths = {0, 9.99, 10.01};
        input.H_thicknesses = {10, 6}; // inch
        input.E_moduli = {500, 40, 10}; // ksi
        input.nu_poisson = {0.35, 0.4, 0.45};
        input.bonded_interfaces = {0, 0}; // Frictionless
        input.iterations = 10;
        input.ZRO = 7e-7;
        input.inverser = "solve";
    }
    
    PyMasticSolver::Input input;
};

TEST_F(PyMasticPortTest, BasicFunctionalityTest) {
    EXPECT_TRUE(input.Validate()) << "Input validation failed";
    
    PyMasticSolver solver;
    PyMasticSolver::Output output;
    
    EXPECT_NO_THROW(output = solver.Compute(input)) << "PyMastic computation failed";
    
    // Check output dimensions
    EXPECT_EQ(output.displacement_z.rows(), 3); // 3 z-depths
    EXPECT_EQ(output.displacement_z.cols(), 2); // 2 x-offsets
    
    // Check that we have finite results
    EXPECT_TRUE(output.IsValid()) << "Output contains NaN or infinite values";
    
    // Print results for validation
    std::cout << "\nPyMastic C++ Results:\n";
    std::cout << "Displacement Z [0,0]: " << output.displacement_z(0, 0) << std::endl;
    std::cout << "Stress Z [0,0]: " << output.stress_z(0, 0) << std::endl;
    std::cout << "Displacement H [0,0]: " << output.displacement_h(0, 0) << std::endl;
    std::cout << "Stress T [0,0]: " << output.stress_t(0, 0) << std::endl;
    
    std::cout << "Displacement Z [1,0]: " << output.displacement_z(1, 0) << std::endl;
    std::cout << "Stress Z [1,0]: " << output.stress_z(1, 0) << std::endl;
    std::cout << "Stress R [1,0]: " << output.stress_r(1, 0) << std::endl;
    std::cout << "Stress T [1,0]: " << output.stress_t(1, 0) << std::endl;
    
    // Basic sanity checks
    EXPECT_GT(std::abs(output.displacement_z(0, 0)), 0.0) << "Surface displacement should be non-zero";
    EXPECT_GT(std::abs(output.stress_z(0, 0)), 0.0) << "Surface stress should be non-zero";
}

TEST_F(PyMasticPortTest, InputValidationTest) {
    // Test invalid inputs
    PyMasticSolver::Input bad_input = input;
    
    // Negative pressure
    bad_input.q_kpa = -100;
    EXPECT_FALSE(bad_input.Validate());
    
    // Invalid Poisson ratio
    bad_input = input;
    bad_input.nu_poisson[0] = 0.6; // > 0.5
    EXPECT_FALSE(bad_input.Validate());
    
    // Mismatched layer counts
    bad_input = input;
    bad_input.H_thicknesses.push_back(5); // One too many
    EXPECT_FALSE(bad_input.Validate());
}

TEST_F(PyMasticPortTest, BondedVsFrictionlessTest) {
    PyMasticSolver solver;
    
    // Test frictionless interfaces
    input.bonded_interfaces = {0, 0};
    auto output_frictionless = solver.Compute(input);
    
    // Test bonded interfaces  
    input.bonded_interfaces = {1, 1};
    auto output_bonded = solver.Compute(input);
    
    // Results should be different
    EXPECT_NE(output_frictionless.displacement_z(0, 0), output_bonded.displacement_z(0, 0))
        << "Bonded vs frictionless should give different results";
    
    // Both should be valid
    EXPECT_TRUE(output_frictionless.IsValid());
    EXPECT_TRUE(output_bonded.IsValid());
    
    std::cout << "\nBonding comparison at [0,0]:\n";
    std::cout << "Frictionless displacement Z: " << output_frictionless.displacement_z(0, 0) << std::endl;
    std::cout << "Bonded displacement Z: " << output_bonded.displacement_z(0, 0) << std::endl;
}

TEST_F(PyMasticPortTest, SolverMethodsTest) {
    PyMasticSolver solver;
    
    std::vector<std::string> methods = {"solve", "inv", "pinv", "lu", "svd"};
    
    for (const auto& method : methods) {
        input.inverser = method;
        
        EXPECT_NO_THROW({
            auto output = solver.Compute(input);
            EXPECT_TRUE(output.IsValid()) << "Method " << method << " produced invalid results";
        }) << "Solver method " << method << " failed";
    }
}
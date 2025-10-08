#include "PyMasticPythonBridge.h"
#include <sstream>
#include <cstdlib>
#include <iostream>
#include <stdexcept>

#ifdef _WIN32
#include <windows.h>
#include <io.h>
#include <fcntl.h>
#else
#include <unistd.h>
#include <sys/wait.h>
#endif

PyMasticPythonBridge::Output PyMasticPythonBridge::Calculate(const Input& input) {
    try {
        // Convert input to JSON
        std::string json_input = InputToJson(input);
        
        // Execute Python bridge
        std::string json_output = ExecutePythonBridge(json_input);
        
        // Parse results
        return ParseJsonOutput(json_output);
        
    } catch (const std::exception& e) {
        Output error_output;
        error_output.success = false;
        error_output.error_message = std::string("PyMastic bridge error: ") + e.what();
        return error_output;
    }
}

std::string PyMasticPythonBridge::ExecutePythonBridge(const std::string& json_input) {
#ifdef _WIN32
    // Windows implementation using CreateProcess
    SECURITY_ATTRIBUTES sa;
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.lpSecurityDescriptor = NULL;
    sa.bInheritHandle = TRUE;
    
    HANDLE hChildStdInRead, hChildStdInWrite;
    HANDLE hChildStdOutRead, hChildStdOutWrite;
    
    // Create pipes for stdin/stdout
    if (!CreatePipe(&hChildStdInRead, &hChildStdInWrite, &sa, 0) ||
        !CreatePipe(&hChildStdOutRead, &hChildStdOutWrite, &sa, 0)) {
        throw std::runtime_error("Failed to create pipes");
    }
    
    // Ensure pipe handles are not inherited
    SetHandleInformation(hChildStdInWrite, HANDLE_FLAG_INHERIT, 0);
    SetHandleInformation(hChildStdOutRead, HANDLE_FLAG_INHERIT, 0);
    
    PROCESS_INFORMATION pi;
    STARTUPINFO si;
    ZeroMemory(&si, sizeof(STARTUPINFO));
    si.cb = sizeof(STARTUPINFO);
    si.hStdInput = hChildStdInRead;
    si.hStdOutput = hChildStdOutWrite;
    si.hStdError = hChildStdOutWrite;
    si.dwFlags |= STARTF_USESTDHANDLES;
    
    // Command: python pymastic_bridge.py
    char cmd[] = "python pymastic_bridge.py";
    
    if (!CreateProcess(NULL, cmd, NULL, NULL, TRUE, 0, NULL, NULL, &si, &pi)) {
        CloseHandle(hChildStdInRead);
        CloseHandle(hChildStdInWrite);
        CloseHandle(hChildStdOutRead);
        CloseHandle(hChildStdOutWrite);
        throw std::runtime_error("Failed to start Python process");
    }
    
    // Close child's handles
    CloseHandle(hChildStdInRead);
    CloseHandle(hChildStdOutWrite);
    
    // Write input to child's stdin
    DWORD written;
    if (!WriteFile(hChildStdInWrite, json_input.c_str(), static_cast<DWORD>(json_input.length()), &written, NULL)) {
        CloseHandle(hChildStdInWrite);
        CloseHandle(hChildStdOutRead);
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
        throw std::runtime_error("Failed to write to Python process");
    }
    CloseHandle(hChildStdInWrite);
    
    // Read output from child's stdout
    std::string output;
    char buffer[4096];
    DWORD read;
    
    while (ReadFile(hChildStdOutRead, buffer, sizeof(buffer), &read, NULL) && read > 0) {
        output.append(buffer, read);
    }
    
    CloseHandle(hChildStdOutRead);
    
    // Wait for process to complete
    WaitForSingleObject(pi.hProcess, INFINITE);
    
    DWORD exit_code;
    GetExitCodeProcess(pi.hProcess, &exit_code);
    
    CloseHandle(pi.hProcess);
    CloseHandle(pi.hThread);
    
    if (exit_code != 0) {
        throw std::runtime_error("Python process failed with exit code " + std::to_string(exit_code));
    }
    
    return output;
    
#else
    // Unix implementation using popen (simpler)
    std::string command = "python3 pymastic_bridge.py";
    FILE* pipe = popen(command.c_str(), "w");
    if (!pipe) {
        throw std::runtime_error("Failed to start Python process");
    }
    
    // Write input
    fwrite(json_input.c_str(), 1, json_input.length(), pipe);
    
    int exit_code = pclose(pipe);
    if (exit_code != 0) {
        throw std::runtime_error("Python process failed");
    }
    
    // For Unix, we'd need a more complex solution with bidirectional pipes
    // This is a simplified version
    return "{}";
#endif
}

std::string PyMasticPythonBridge::InputToJson(const Input& input) {
    std::ostringstream json;
    json << "{";
    json << "\"q_kpa\": " << input.q_kpa << ",";
    json << "\"a_m\": " << input.a_m << ",";
    
    // z_depths_m array
    json << "\"z_depths_m\": [";
    for (size_t i = 0; i < input.z_depths_m.size(); ++i) {
        if (i > 0) json << ",";
        json << input.z_depths_m[i];
    }
    json << "],";
    
    // H_thicknesses_m array
    json << "\"H_thicknesses_m\": [";
    for (size_t i = 0; i < input.H_thicknesses_m.size(); ++i) {
        if (i > 0) json << ",";
        json << input.H_thicknesses_m[i];
    }
    json << "],";
    
    // E_moduli_mpa array
    json << "\"E_moduli_mpa\": [";
    for (size_t i = 0; i < input.E_moduli_mpa.size(); ++i) {
        if (i > 0) json << ",";
        json << input.E_moduli_mpa[i];
    }
    json << "],";
    
    // nu_poisson array
    json << "\"nu_poisson\": [";
    for (size_t i = 0; i < input.nu_poisson.size(); ++i) {
        if (i > 0) json << ",";
        json << input.nu_poisson[i];
    }
    json << "],";
    
    // bonded_interfaces array
    json << "\"bonded_interfaces\": [";
    for (size_t i = 0; i < input.bonded_interfaces.size(); ++i) {
        if (i > 0) json << ",";
        json << input.bonded_interfaces[i];
    }
    json << "]";
    
    json << "}";
    return json.str();
}

PyMasticPythonBridge::Output PyMasticPythonBridge::ParseJsonOutput(const std::string& json_output) {
    Output result;
    
    // Simple JSON parsing (would use a proper library in production)
    try {
        // Find success field
        size_t success_pos = json_output.find("\"success\":");
        if (success_pos != std::string::npos) {
            size_t true_pos = json_output.find("true", success_pos);
            size_t false_pos = json_output.find("false", success_pos);
            result.success = (true_pos != std::string::npos && (false_pos == std::string::npos || true_pos < false_pos));
        } else {
            result.success = false;
        }
        
        if (!result.success) {
            // Find error message
            size_t error_start = json_output.find("\"error_message\":");
            if (error_start != std::string::npos) {
                error_start = json_output.find("\"", error_start + 16);
                if (error_start != std::string::npos) {
                    size_t error_end = json_output.find("\"", error_start + 1);
                    if (error_end != std::string::npos) {
                        result.error_message = json_output.substr(error_start + 1, error_end - error_start - 1);
                    }
                }
            }
            return result;
        }
        
        // Parse arrays (simplified - would use proper JSON library in production)
        auto parseArray = [&](const std::string& field_name) -> std::vector<double> {
            std::vector<double> values;
            size_t field_pos = json_output.find("\"" + field_name + "\":");
            if (field_pos != std::string::npos) {
                size_t array_start = json_output.find("[", field_pos);
                size_t array_end = json_output.find("]", array_start);
                if (array_start != std::string::npos && array_end != std::string::npos) {
                    std::string array_content = json_output.substr(array_start + 1, array_end - array_start - 1);
                    std::istringstream iss(array_content);
                    std::string token;
                    while (std::getline(iss, token, ',')) {
                        // Remove whitespace
                        token.erase(0, token.find_first_not_of(" \t\n\r"));
                        token.erase(token.find_last_not_of(" \t\n\r") + 1);
                        if (!token.empty()) {
                            values.push_back(std::stod(token));
                        }
                    }
                }
            }
            return values;
        };
        
        result.displacement_z_m = parseArray("displacement_z_m");
        result.stress_z_mpa = parseArray("stress_z_mpa");
        result.strain_z_microdef = parseArray("strain_z_microdef");
        result.strain_r_microdef = parseArray("strain_r_microdef");
        
    } catch (const std::exception& e) {
        result.success = false;
        result.error_message = std::string("JSON parsing error: ") + e.what();
    }
    
    return result;
}
#pragma once

#include <string>
#include <iostream>
#include <fstream>
#include <sstream>
#include <mutex>
#include <chrono>
#include <iomanip>

namespace Pavement {

/**
 * Simple logging system for pavement calculation engine.
 * Thread-safe logging with multiple severity levels.
 */
class Logger {
public:
    enum class Level {
        DEBUG,
        INFO,
        WARNING,
        ERROR,
        CRITICAL
    };

    /**
     * Get singleton logger instance.
     */
    static Logger& GetInstance() {
        static Logger instance;
        return instance;
    }

    /**
     * Set minimum logging level (messages below this level are ignored).
     */
    void SetLevel(Level level) {
        std::lock_guard<std::mutex> lock(mutex_);
        currentLevel_ = level;
    }

    /**
     * Enable/disable file logging.
     */
    void SetFileOutput(const std::string& filename) {
        std::lock_guard<std::mutex> lock(mutex_);
        if (logFile_.is_open()) {
            logFile_.close();
        }
        if (!filename.empty()) {
            logFile_.open(filename, std::ios::app);
        }
    }

    /**
     * Log a message with specified level.
     */
    void Log(Level level, const std::string& message, 
             const std::string& file = "", int line = -1) {
        std::lock_guard<std::mutex> lock(mutex_);
        
        if (level < currentLevel_) {
            return;  // Skip messages below current level
        }

        std::stringstream ss;
        
        // Timestamp
        auto now = std::chrono::system_clock::now();
        auto time_t = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
            now.time_since_epoch()) % 1000;
        
        ss << "[" << std::put_time(std::localtime(&time_t), "%Y-%m-%d %H:%M:%S")
           << "." << std::setfill('0') << std::setw(3) << ms.count() << "] ";
        
        // Level
        ss << "[" << LevelToString(level) << "] ";
        
        // File and line (if provided)
        if (!file.empty() && line > 0) {
            // Extract just filename from path
            size_t pos = file.find_last_of("/\\");
            std::string filename = (pos != std::string::npos) ? file.substr(pos + 1) : file;
            ss << "[" << filename << ":" << line << "] ";
        }
        
        // Message
        ss << message;
        
        std::string logLine = ss.str();
        
        // Console output (colored for terminals that support it)
        if (level >= Level::ERROR) {
            std::cerr << logLine << std::endl;
        } else {
            std::cout << logLine << std::endl;
        }
        
        // File output
        if (logFile_.is_open()) {
            logFile_ << logLine << std::endl;
            logFile_.flush();
        }
    }

    /**
     * Convenience methods for different log levels.
     */
    void Debug(const std::string& message, const std::string& file = "", int line = -1) {
        Log(Level::DEBUG, message, file, line);
    }

    void Info(const std::string& message, const std::string& file = "", int line = -1) {
        Log(Level::INFO, message, file, line);
    }

    void Warning(const std::string& message, const std::string& file = "", int line = -1) {
        Log(Level::WARNING, message, file, line);
    }

    void Error(const std::string& message, const std::string& file = "", int line = -1) {
        Log(Level::ERROR, message, file, line);
    }

    void Critical(const std::string& message, const std::string& file = "", int line = -1) {
        Log(Level::CRITICAL, message, file, line);
    }

private:
    Logger() : currentLevel_(Level::INFO) {}
    ~Logger() {
        if (logFile_.is_open()) {
            logFile_.close();
        }
    }

    // Prevent copying
    Logger(const Logger&) = delete;
    Logger& operator=(const Logger&) = delete;

    std::string LevelToString(Level level) const {
        switch (level) {
            case Level::DEBUG:    return "DEBUG";
            case Level::INFO:     return "INFO ";
            case Level::WARNING:  return "WARN ";
            case Level::ERROR:    return "ERROR";
            case Level::CRITICAL: return "CRIT ";
            default:              return "UNKNOWN";
        }
    }

    Level currentLevel_;
    std::ofstream logFile_;
    std::mutex mutex_;
};

// Convenience macros for logging with file/line information
#define LOG_DEBUG(msg)    Pavement::Logger::GetInstance().Debug(msg, __FILE__, __LINE__)
#define LOG_INFO(msg)     Pavement::Logger::GetInstance().Info(msg, __FILE__, __LINE__)
#define LOG_WARNING(msg)  Pavement::Logger::GetInstance().Warning(msg, __FILE__, __LINE__)
#define LOG_ERROR(msg)    Pavement::Logger::GetInstance().Error(msg, __FILE__, __LINE__)
#define LOG_CRITICAL(msg) Pavement::Logger::GetInstance().Critical(msg, __FILE__, __LINE__)

} // namespace Pavement

using System.Text;

namespace LeadSearchParser.Utils;

public class Logger
{
    private readonly string _logFile;
    private readonly bool _enabled;
    private readonly object _lockObject = new();

    public Logger(string logFile, bool enabled = true)
    {
        _logFile = logFile;
        _enabled = enabled;
        
        if (_enabled)
        {
            var logDir = Path.GetDirectoryName(_logFile);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }
    }

    public void Info(string message)
    {
        Log("INFO", message, ConsoleColor.Cyan);
    }

    public void Success(string message)
    {
        Log("SUCCESS", message, ConsoleColor.Green);
    }

    public void Warning(string message)
    {
        Log("WARNING", message, ConsoleColor.Yellow);
    }

    public void Error(string message)
    {
        Log("ERROR", message, ConsoleColor.Red);
    }

    private void Log(string level, string message, ConsoleColor color)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logMessage = $"[{timestamp}] [{level}] {message}";

        lock (_lockObject)
        {
            // Console output with color
            Console.ForegroundColor = color;
            Console.WriteLine(logMessage);
            Console.ResetColor();

            // File output
            if (_enabled)
            {
                try
                {
                    File.AppendAllText(_logFile, logMessage + Environment.NewLine, Encoding.UTF8);
                }
                catch
                {
                    // Ignore file write errors
                }
            }
        }
    }
}



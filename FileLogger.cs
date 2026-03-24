namespace GraphSmtpBridge;

public class FileLogger
{
    private readonly bool _enabled;
    private readonly string _logPath;

    public FileLogger(IConfiguration config)
    {
        _enabled = config.GetValue<bool>("SmtpBridge:EnableLogging", false);
        _logPath = config.GetValue<string>("SmtpBridge:LogFilePath", "C:\\SmtpBridgeLogs\\smtp_bridge.log");
        
        if (_enabled)
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch { }
            }
        }
    }

    public void Log(string message)
    {
        if (!_enabled) return;
        try
        {
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(_logPath, logMessage);
        }
        catch { /* Ignite errors to avoid app crash */ }
    }
}

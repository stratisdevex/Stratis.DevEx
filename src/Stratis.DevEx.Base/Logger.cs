namespace Stratis.DevEx;

using NLog;

public abstract class Logger
{ 
    public abstract class Op : IDisposable
    {
        public Op(Logger l)
        {
            L = l;
        }

        public Logger L;

        protected bool isCompleted = false;

        protected bool isAbandoned = false;

        public abstract void Complete();

        public abstract void Abandon();

        public abstract void Dispose();
    }

    public bool IsConfigured { get; protected set; } = false;

    public bool IsDebug { get; protected set; } = false;

    public abstract void Info(string messageTemplate, params object[] args);

    public abstract void Debug(string messageTemplate, params object[] args);

    public abstract void Error(string messageTemplate, params object[] args);

    public abstract void Error(Exception ex, string messageTemplate, params object[] args);

    public abstract void Warn(string messageTemplate, params object[] args);

    public abstract void Fatal(string messageTemplate, params object[] args);

    public abstract Op Begin(string messageTemplate, params object[] args);

    public abstract void Close();
}

#region Console logger
public class ConsoleOp : Logger.Op
{
    public ConsoleOp(ConsoleLogger l, string opName) : base(l) 
    { 
        this.opName = opName;
        timer.Start();
        L.Info(opName + "...");
    }

    public override void Complete()
    {
        timer.Stop();
        L.Info($"{opName} completed in {timer.ElapsedMilliseconds}ms.");
        isCompleted = true;
    }

    public override void Abandon()
    {
        timer.Stop();
        isAbandoned = true;
        L.Error($"{opName} abandoned after {timer.ElapsedMilliseconds}ms.");
    }

    public override void Dispose()
    {
        if (timer.IsRunning) timer.Stop();
        if (!(isCompleted || isAbandoned))
        {
            L.Error($"{opName} abandoned after {timer.ElapsedMilliseconds}ms.");
        }
    }

    string opName;
    Stopwatch timer = new Stopwatch();
}

public class ConsoleLogger : Logger
{
    public ConsoleLogger(bool debug = false) : base()
    {
        IsDebug = debug;
    }

    public override void Info(string messageTemplate, params object[] args) => Console.WriteLine("[INFO] " + messageTemplate, args);

    public override void Debug(string messageTemplate, params object[] args)
    {
        if (IsDebug)
        {
            Console.WriteLine("[DEBUG] " + messageTemplate, args);
        }
    }

    public override void Error(string messageTemplate, params object[] args) => Console.WriteLine("[ERROR] " + messageTemplate, args);

    public override void Error(Exception ex, string messageTemplate, params object[] args) => Console.WriteLine("[ERROR] " + messageTemplate, args);

    public override void Warn(string messageTemplate, params object[] args) => Console.WriteLine("[WARN] " + messageTemplate, args);

    public override void Fatal(string messageTemplate, params object[] args) => Console.WriteLine("[FATAL] " + messageTemplate, args);

    public override Op Begin(string messageTemplate, params object[] args) => new ConsoleOp(this, String.Format(messageTemplate, args));

    public override void Close() {}
}
#endregion

#region NReco file logger
/*
/// <summary>
/// This is a file logger implementation that doesn't require any 3rd-party NuGet packages except for the Microsoft.Extensions.Logging base libraries and can work in libraries
/// like Roslyn analyzers where loading 3rd-party packages is problematic.
/// </summary>
public class FileLogger : Logger
{
    public FileLogger(string logFileName, bool debug = false, string category = "DevEx") : base()
    {
        this.IsDebug = debug;
        var factory = new LoggerFactory();
        loggerProvider = new FileLoggerProvider(logFileName, new FileLoggerOptions() { MinLevel = debug ? LogLevel.Debug : LogLevel.Information, 
            FormatLogEntry = FileLogger.FormatLogEntry});
        factory.AddProvider(loggerProvider);
        logger = factory.CreateLogger(category);
        this.logFileName = logFileName;
    }

    public static string FormatLogEntry(LogMessage entry)
    {
        {
            var logBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(entry.Message))
            {
                DateTime timeStamp = DateTime.Now;
                logBuilder.Append(timeStamp.ToString());
                logBuilder.Append(' ');
                logBuilder.Append(NReco.Logging.File.FileLogger.GetShortLogLevel(entry.LogLevel));
                logBuilder.Append(" [");
                logBuilder.Append(Runtime.LogName);
                logBuilder.Append("]");
                logBuilder.Append(" [");
                logBuilder.Append(entry.EventId.Id.ToString());
                logBuilder.Append("] ");
                logBuilder.Append(entry.Message);
                return logBuilder.ToString();
            }
            else
            {
                return "";
            }
        }
    }

    public override void Info(string messageTemplate, params object[] args) => logger.LogInformation(Runtime.SessionId, messageTemplate, args);

    public override void Debug(string messageTemplate, params object[] args) => logger.LogDebug(Runtime.SessionId, messageTemplate, args);

    public override void Error(string messageTemplate, params object[] args) => logger.LogError(Runtime.SessionId, messageTemplate, args);

    public override void Error(Exception ex, string messageTemplate, params object[] args) => logger.LogError(Runtime.SessionId, ex, messageTemplate, args);

    public override void Warn(string messageTemplate, params object[] args) => logger.LogWarning(Runtime.SessionId, messageTemplate, args);

    public override void Fatal(string messageTemplate, params object[] args) => logger.LogCritical(Runtime.SessionId, messageTemplate, args);

    public override Op Begin(string messageTemplate, params object[] args) => new FileLoggerOp(this, String.Format(messageTemplate, args));

    public override void Close()
    {
        Info("Closing {0} log to file {1}...", Runtime.LogName, this.logFileName);
        this.loggerProvider.Dispose();
    }

    protected string logFileName;
    
    protected ILogger logger;

    protected ILoggerProvider loggerProvider;
}

public class FileLoggerOp : Logger.Op
{
    public FileLoggerOp(FileLogger l, string opName) : base(l)
    {
        this.opName = opName;
        timer.Start();
        this.l = l;
        l.Info(opName + "...");
    }

    public override void Complete()
    {
        timer.Stop();
        l.Info($"{0} completed in {1}ms.", opName, timer.ElapsedMilliseconds);
        isCompleted = true;
    }

    public override void Abandon()
    {
        timer.Stop();
        isAbandoned = true;
        l.Error($"{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
    }

    public override void Dispose()
    {
        if (timer.IsRunning) timer.Stop();
        if (!(isCompleted || isAbandoned))
        {
            l.Error($"{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
        }
    }

    string opName;
    Stopwatch timer = new Stopwatch();
    FileLogger l;
}
*/
#endregion

#region NLog file logger
public class FileLogger : Logger
{
    public FileLogger(string logFileName, bool debug = false, string category = "DevEx") : base()
    {
        var config = new NLog.Config.LoggingConfiguration();
        var logfile = new NLog.Targets.FileTarget("logfile") { FileName = logFileName };
        LogManager.Configuration = config;
        this.logFileName = logFileName;
        this.logger = LogManager.GetCurrentClassLogger();

    }

    #region Overriden methods
    public override void Info(string messageTemplate, params object[] args) => logger.Info(messageTemplate, args);

    public override void Debug(string messageTemplate, params object[] args) => logger.Debug(messageTemplate, args);

    public override void Error(string messageTemplate, params object[] args) => logger.Error(messageTemplate, args);

    public override void Error(Exception ex, string messageTemplate, params object[] args) => logger.Error(ex, messageTemplate, args);

    public override void Warn(string messageTemplate, params object[] args) => logger.Warn(messageTemplate, args);

    public override void Fatal(string messageTemplate, params object[] args) => logger.Fatal(messageTemplate, args);

    public override Op Begin(string messageTemplate, params object[] args) => new FileLoggerOp(this, String.Format(messageTemplate, args));

    public override void Close()
    {
        Info("Closing {0} log to file {1}...", Runtime.LogName, this.logFileName);
        LogManager.Flush();
        LogManager.Shutdown();
    }

    #endregion

    #region Fields
    protected string logFileName;
    protected NLog.Logger logger;
    #endregion
}

public class FileLoggerOp : Logger.Op
{
    public FileLoggerOp(FileLogger l, string opName) : base(l)
    {
        this.opName = opName;
        timer.Start();
        this.l = l;
        l.Info(opName + "...");
    }

    public override void Complete()
    {
        timer.Stop();
        l.Info($"{0} completed in {1}ms.", opName, timer.ElapsedMilliseconds);
        isCompleted = true;
    }

    public override void Abandon()
    {
        timer.Stop();
        isAbandoned = true;
        l.Error($"{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
    }

    public override void Dispose()
    {
        if (timer.IsRunning) timer.Stop();
        if (!(isCompleted || isAbandoned))
        {
            l.Error($"{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
        }
    }

    string opName;
    Stopwatch timer = new Stopwatch();
    FileLogger l;
}
#endregion
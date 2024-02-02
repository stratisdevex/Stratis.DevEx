namespace Stratis.DevEx
{
    using NLog;
    using NLog.Config;

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

        public static string NLogDebugLayout = "${longdate}${pad:padding=6:inner=${level:uppercase=true}} ${logger}(${threadid}) ${callsite:skipFrames=2:includeNamespace=false}: ${message:withexception=true}";

        public static string NLogLayout = "${longdate}${pad:padding=6:inner=${level:uppercase=true}} ${logger}(${threadid}) ${message:withexception=true}";
        
        

        public abstract void Info(string messageTemplate, params object[] args);

        public abstract void Debug(string messageTemplate, params object[] args);

        public abstract void Error(string messageTemplate, params object[] args);

        public abstract void Error(Exception ex, string messageTemplate, params object[] args);

        public abstract void Warn(string messageTemplate, params object[] args);

        public abstract void Fatal(string messageTemplate, params object[] args);

        public abstract Op Begin(string messageTemplate, params object[] args);

        public abstract void Close();

        public abstract void SetLogLevelDebug();
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

        public override void Close() { }

        public override void SetLogLevelDebug() => IsDebug = true;
    }
    #endregion

    #region NLog file logger
    public class FileLogger : Logger
    {
        public FileLogger(string logFileName, bool debug = false, string logname = "DevEx", bool logToConsole = false) : base()
        {
            this.logToConsole = logToConsole;
            var config = new LoggingConfiguration();
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = logFileName,
                Layout = debug ? NLogDebugLayout : NLogLayout
            };
            config.AddTarget(logfile);
            config.AddRule(new LoggingRule("*", LogLevel.Info, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Warn, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Error, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Fatal, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Debug, logfile));
            if (debug)
            {
                config.Variables["logLevel"] = "Debug";
            }
           
            if (logToConsole || (Runtime.EntryAssembly?.FullName.StartsWith("OmniSharp") ?? false))
            {
                this.logToConsole = true;
                var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
                {
                    Layout = debug ? NLogDebugLayout : NLogLayout
                };
                config.AddTarget(logconsole);
                config.AddRule(new LoggingRule("*", LogLevel.Info, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Warn, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Error, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Fatal, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Debug, logconsole));
            }
            
            /*
            var highlightRule = new NLog.Targets.ConsoleRowHighlightingRule();
            highlightRule.Condition = NLog.Conditions.ConditionParser.ParseExpression("level == LogLevel.Info");
            highlightRule.ForegroundColor = NLog.Targets.ConsoleOutputColor.Green;
            logconsole.RowHighlightingRules.Add(highlightRule);
            */
            
            LogManager.Configuration = config;
            this.logFileName = logFileName;
            this.logger = LogManager.GetLogger(logname);
        }

        #region Overriden methods
        public override void Info(string messageTemplate, params object[] args) => logger.Info(messageTemplate, args);

        public override void Debug(string messageTemplate, params object[] args) => logger.Debug(messageTemplate, args);

        public override void Error(string messageTemplate, params object[] args) => logger.Error(messageTemplate, args);

        public override void Error(Exception ex, string messageTemplate, params object[] args) => logger.Error(ex, messageTemplate, args);

        public override void Warn(string messageTemplate, params object[] args) => logger.Warn(messageTemplate, args);

        public override void Fatal(string messageTemplate, params object[] args) => logger.Fatal(messageTemplate, args);

        public override Op Begin(string messageTemplate, params object[] args) => new FileLoggerOp(this, messageTemplate, args);

        public override void Close()
        {
            Info("Closing {0} log file {1}...", Runtime.LogName, this.logFileName);
            LogManager.Flush();
            LogManager.Shutdown();
        }

        public override void SetLogLevelDebug()
        {
            var config = LogManager.Configuration;
            config.RemoveTarget("logfile");
            if (config.AllTargets.Any(t => t.Name == "logconsole")) config.RemoveTarget("logconsole");
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = this.logFileName,
                Layout = NLogDebugLayout
            };
            config.AddTarget(logfile);
            config.AddRule(new LoggingRule("*", LogLevel.Info, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Warn, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Error, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Fatal, logfile));
            config.AddRule(new LoggingRule("*", LogLevel.Debug, logfile));
            if (this.logToConsole)
            {
                var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
                {
                    Layout = NLogDebugLayout
                };               
                config.AddTarget(logconsole);
                config.AddRule(new LoggingRule("*", LogLevel.Info, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Warn, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Error, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Fatal, logconsole));
                config.AddRule(new LoggingRule("*", LogLevel.Debug, logconsole));
            }
            config.Variables["logLevel"] = "Debug";
            this.IsDebug = true;
            LogManager.ReconfigExistingLoggers();
        }
        #endregion

        #region Methods
        public string NLogFormatMessage(string messageTemplate, params object[] args)
        {
            var ev = LogEventInfo.Create(LogLevel.Info, "none", this.logger.Factory.DefaultCultureInfo, messageTemplate, args);
            return ev.FormattedMessage;
        }
        #endregion

        #region Fields
        protected string logFileName;
        protected bool logToConsole;
        protected NLog.Logger logger;
        #endregion
    }

    public class FileLoggerOp : Logger.Op
    {
        public FileLoggerOp(FileLogger l, string opName, params object[] args) : base(l)
        {
            this.opName = l.NLogFormatMessage(opName, args);
            timer.Start();
            this.l = l;
            l.Info(this.opName + "...");
        }

        public override void Complete()
        {
            timer.Stop();
            l.Info("{0} completed in {1}ms.", opName, timer.ElapsedMilliseconds);
            isCompleted = true;
        }

        public override void Abandon()
        {
            timer.Stop();
            isAbandoned = true;
            l.Error("{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
        }

        public override void Dispose()
        {
            if (timer.IsRunning) timer.Stop();
            if (!(isCompleted || isAbandoned))
            {
                l.Error("{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
            }
        }

        string opName;
        Stopwatch timer = new Stopwatch();
        FileLogger l;
    }
    #endregion

    #region NLog console logger
    public class ConsoleLogger2 : Logger
    {
        public ConsoleLogger2(bool debug = false, string logname = "BASE", bool logToConsole = false) : base()
        {
            var config = new LoggingConfiguration();
          
            if (debug)
            {
                config.Variables["logLevel"] = "Debug";
            }
            var logconsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                Layout = debug ? NLogDebugLayout : NLogLayout
            };
            config.AddTarget(logconsole);
            config.AddRule(new LoggingRule("*", LogLevel.Info, logconsole));
            config.AddRule(new LoggingRule("*", LogLevel.Warn, logconsole));
            config.AddRule(new LoggingRule("*", LogLevel.Error, logconsole));
            config.AddRule(new LoggingRule("*", LogLevel.Fatal, logconsole));
            config.AddRule(new LoggingRule("*", LogLevel.Debug, logconsole));
            
            /*
            var highlightRule = new NLog.Targets.ConsoleRowHighlightingRule();
            highlightRule.Condition = NLog.Conditions.ConditionParser.ParseExpression("level == LogLevel.Info");
            highlightRule.ForegroundColor = NLog.Targets.ConsoleOutputColor.Green;
            logconsole.RowHighlightingRules.Add(highlightRule);
            */

            LogManager.Configuration = config;
            this.logger = LogManager.GetLogger(logname);
        }

        #region Overriden methods
        public override void Info(string messageTemplate, params object[] args) => logger.Info(messageTemplate, args);

        public override void Debug(string messageTemplate, params object[] args) => logger.Debug(messageTemplate, args);

        public override void Error(string messageTemplate, params object[] args) => logger.Error(messageTemplate, args);

        public override void Error(Exception ex, string messageTemplate, params object[] args) => logger.Error(ex, messageTemplate, args);

        public override void Warn(string messageTemplate, params object[] args) => logger.Warn(messageTemplate, args);

        public override void Fatal(string messageTemplate, params object[] args) => logger.Fatal(messageTemplate, args);

        public override Op Begin(string messageTemplate, params object[] args) => new ConsoleLogger2Op(this, messageTemplate, args);

        public override void Close()
        {
            LogManager.Flush();
            LogManager.Shutdown();
        }

        public override void SetLogLevelDebug()
        {
            var config = LogManager.Configuration;
            config.Variables["logLevel"] = "Debug";
            LogManager.ReconfigExistingLoggers();
        }
        #endregion

        #region Methods
        public string NLogFormatInfoMessage(string messageTemplate, params object[] args)
        {
            var ev = LogEventInfo.Create(LogLevel.Info, "none", this.logger.Factory.DefaultCultureInfo, messageTemplate, args);
            return ev.FormattedMessage;
        }
        #endregion

        #region Fields
        protected NLog.Logger logger;
        #endregion
    }

    public class ConsoleLogger2Op : Logger.Op
    {
        public ConsoleLogger2Op(ConsoleLogger2 l, string opName, params object[] args) : base(l)
        {
           
            this.opName = l.NLogFormatInfoMessage(opName, args);
            timer.Start();
            this.l = l;
            l.Info(this.opName + "...");
        }

        public override void Complete()
        {
            timer.Stop();
            l.Info("{0} completed in {1}ms.", opName, timer.ElapsedMilliseconds);
            isCompleted = true;
        }

        public override void Abandon()
        {
            timer.Stop();
            isAbandoned = true;
            l.Error("{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
        }

        public override void Dispose()
        {
            if (timer.IsRunning) timer.Stop();
            if (!(isCompleted || isAbandoned))
            {
                l.Error("{0} abandoned after {1}ms.", opName, timer.ElapsedMilliseconds);
            }
        }

        string opName;
        Stopwatch timer = new Stopwatch();
        ConsoleLogger2 l;
    }
    #endregion
}
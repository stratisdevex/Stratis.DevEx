namespace Stratis.DevEx
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Threading;

    using SharpConfig;

    public abstract class Runtime
    {
        #region Constructors
        static Runtime()
        {
            AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
            EntryAssembly = Assembly.GetEntryAssembly();
            IsUnitTestRun = EntryAssembly?.FullName.StartsWith("testhost") ?? false;
            SessionId = Rng.Next(0, 99999);            
            Logger = new ConsoleLogger2();
        }

        public Runtime(CancellationToken ct)
        {
            Ct = ct;
        }

        public Runtime() : this(Cts.Token) { }
        #endregion

        #region Properties
        public bool Initialized { get; protected set; }

        public static bool RuntimeInitialized { get; protected set; }

        public static Configuration GlobalConfig { get; set; } = new Configuration();

        public static bool DebugEnabled { get; set; }

        public static bool InteractiveConsole { get; set; } = false;

        public static string PathSeparator { get; } = Environment.OSVersion.Platform == PlatformID.Win32NT ? "\\" : "/";

        public static string ToolName { get; set; } = "Stratis DevEx";
        
        public static string LogName { get; set; } = "BASE";

        public static Logger Logger { get; protected set; }

        public static string UserHomeDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string AppDataDir => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string LocalAppDataDir => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string StratisDevDir => Path.Combine(AppDataDir, "StratisDev");

        public static Random Rng { get; } = new Random();

        public static int SessionId { get; protected set; }

        public static CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public static CancellationToken Ct { get; protected set; } = Cts.Token;

        public static Assembly? EntryAssembly { get; protected set; }

        public static string AssemblyLocation { get; } = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Runtime))!.Location)!;

        public static Version AssemblyVersion { get; } = Assembly.GetAssembly(typeof(Runtime))!.GetName().Version!;
        
        public static bool IsUnitTestRun { get; set; }

        public static string RunFile => StratisDevDir.CombinePath(ToolName + ".run");
        #endregion

        #region Methods
        public static void Initialize(string toolname, string logname, bool logToConsole = false)
        {
            lock (__lock)
            {
                Info("Initialize called on thread id {0}.", Thread.CurrentThread.ManagedThreadId);
                if (RuntimeInitialized)
                {
                    Info("Runtime already initialized.");
                    return;
                }
                if (!IsUnitTestRun)
                {
                    Logger.Close();
                    ToolName = toolname;
                    LogName = logname;
                    var fulllogfilename = StratisDevDir.CombinePath($"{ToolName}.{SessionId}.log");
                    Logger = new FileLogger(fulllogfilename, false, LogName, logToConsole);
                    Info("{0} initialized from entry assembly {1} with log file {2}...", ToolName, EntryAssembly?.GetName().FullName ?? "(none)", fulllogfilename); ;
                    var globalCfgFile = StratisDevDir.CombinePath(ToolName + ".cfg");
                    if (!File.Exists(globalCfgFile))
                    {
                        Info("Creating new global configuration file for {0}...", ToolName);
                        var newcfg = CreateDefaultConfig();
                        newcfg.SaveToFile(globalCfgFile);
                    }
                    else
                    {
                        Info("Loading existing global configuration file...");
                    }
                    GlobalConfig = Configuration.LoadFromFile(globalCfgFile);
                    if (GlobalSetting("General", "Debug", false, true))
                    {
                        Logger.SetLogLevelDebug();
                        Info("Debug mode enabled.");
                    }
                    Info("Loaded {0} section(s) with {1} value(s) from global configuration at {2}.", GlobalConfig.SectionCount, GlobalConfig.Sum(s => s.SettingCount), globalCfgFile);
                    var d = GlobalSetting("General", "DeleteLogsOlderThan", 2, true);
                    var logfiles = Directory.GetFiles(StratisDevDir, ToolName + ".*.log", SearchOption.AllDirectories) ?? new string[] { };
                    Info("{0} existing log file(s) found.", logfiles.Length);
                    foreach(var l in logfiles)
                    {
                        var age = DateTime.Now.Subtract(File.GetLastWriteTime(l));
                        if (age.TotalDays >= d)
                        {
                            File.Delete(l);
                            Info("Deleted log file {0} that is more than {1} day(s) old.", Path.GetFileName(l), d);
                        }
                    }
                }
                else
                {
                    GlobalConfig = CreateDefaultConfig();
                    GlobalConfig["General"]["Debug"].BoolValue = true;
                    Logger.SetLogLevelDebug();
                    Info("{0} initialized from unit test host assembly {1}...", ToolName, EntryAssembly!);
                    Info("Debug mode enabled.");
                }
                RuntimeInitialized = true;
            }
        }
       
        [DebuggerStepThrough]
        public static void Info(string messageTemplate, params object[] args) => Logger.Info(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Debug(string messageTemplate, params object[] args) => Logger.Debug(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(string messageTemplate, params object[] args) => Logger.Error(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(Exception ex, string messageTemplate, params object[] args) => Logger.Error(ex, messageTemplate, args);

        [DebuggerStepThrough]
        public static void Warn(string messageTemplate, params object[] args) => Logger.Warn(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Fatal(string messageTemplate, params object[] args) => Logger.Fatal(messageTemplate, args);

        [DebuggerStepThrough]
        public static Logger.Op Begin(string messageTemplate, params object[] args) => Logger.Begin(messageTemplate, args);

        [DebuggerStepThrough]
        public static string FailIfFileNotFound(string filePath)
        {
            if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            {
                return filePath;
            }
            else if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }
            else return filePath;
        }

        [DebuggerStepThrough]
        public static string WarnIfFileExists(string filename)
        {
            if (File.Exists(filename)) Warn("File {0} exists, overwriting...", filename);
            return filename;
        }

        [DebuggerStepThrough]
        public void FailIfNotInitialized()
        {
            if (!Initialized)
            {
                throw new RuntimeNotInitializedException(this);
            }
        }

        [DebuggerStepThrough]
        public T FailIfNotInitialized<T>(Func<T> r) => Initialized ? r() : throw new RuntimeNotInitializedException(this);

        [DebuggerStepThrough]
        public static void SetProps<T>(T o, Dictionary<string, object> setprops)
        {
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var kv in setprops)
            {
                properties.Where(x => x.Name == kv.Key).First().SetValue(o, kv.Value);
            }
        }

        [DebuggerStepThrough]
        public static object? GetProp(object o, string name)
        {
            PropertyInfo[] properties = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return properties.FirstOrDefault(x => x.Name == name)?.GetValue(o);
        }

        private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Logger != null)
            {
                Error((Exception)e.ExceptionObject, "Unhandled runtime error occurred.");
            }
        }

        public static string? RunCmd(string cmdName, string arguments = "", string? workingDir = null, DataReceivedEventHandler? outputHandler = null, DataReceivedEventHandler? errorHandler = null,
            bool checkExists = true, bool isNETFxTool = false, bool isNETCoreTool = false)
        {
            if (checkExists && !(File.Exists(cmdName) || (isNETCoreTool && File.Exists(cmdName.Replace(".exe", "")))))
            {
                Error("The executable {0} does not exist.", cmdName);
                return null;
            }
            using (Process p = new Process())
            {
                var output = new StringBuilder();
                var error = new StringBuilder();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;

                if (isNETFxTool && System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    p.StartInfo.FileName = "mono";
                    p.StartInfo.Arguments = cmdName + " " + arguments;
                }
                else if (isNETCoreTool && System.Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    p.StartInfo.FileName = File.Exists(cmdName) ? cmdName : cmdName.Replace(".exe", "");
                    p.StartInfo.Arguments = arguments;

                }
                else
                {
                    p.StartInfo.FileName = cmdName;
                    p.StartInfo.Arguments = arguments;
                }

                p.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data is not null)
                    {
                        output.AppendLine(e.Data);
                        Debug(e.Data);
                        outputHandler?.Invoke(sender, e);
                    }
                };
                p.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data is not null)
                    {
                        error.AppendLine(e.Data);
                        Error(e.Data);
                        errorHandler?.Invoke(sender, e);
                    }
                };
                if (workingDir is not null)
                {
                    p.StartInfo.WorkingDirectory = workingDir;
                }
                Debug("Executing cmd {0} in working directory {1}.", cmdName + " " + arguments, p.StartInfo.WorkingDirectory);
                try
                {
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    return error.ToString().IsNotEmpty() ? null : output.ToString();
                }

                catch (Exception ex)
                {
                    Error(ex, "Error executing command {0} {1}", cmdName, arguments);
                    return null;
                }
            }
        }

        public static Dictionary<string, object> RunCmd(string filename, string arguments, string workingdirectory)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = filename;
            info.Arguments = arguments;
            info.WorkingDirectory = workingdirectory;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var output = new Dictionary<string, object>();
            using (var process = new Process())
            {
                process.StartInfo = info;
                try
                {
                    if (!process.Start())
                    {
                        output["error"] = ("Could not start {file} {args} in {dir}.", info.FileName, info.Arguments, info.WorkingDirectory);
                        return output;
                    }
                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    if (stdout != null && stdout.Length > 0)
                    {
                        output["stdout"] = stdout;
                    }
                    if (stderr != null && stderr.Length > 0)
                    {
                        output["stderr"] = stderr;
                    }
                    return output;
                }
                catch (Exception ex)
                {
                    output["exception"] = ex;
                    return output;
                }
            }
        }

        public static async Task<Dictionary<string, object>> RunCmdAsync(string filename, string arguments, string workingdirectory)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = filename;
            info.Arguments = arguments;
            info.WorkingDirectory = workingdirectory;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var output = new Dictionary<string, object>();
            using (var process = new Process())
            {
                process.StartInfo = info;
                try
                {
                    if (!process.Start())
                    {
                        output["error"] = ("Could not start {file} {args} in {dir}.", info.FileName, info.Arguments, info.WorkingDirectory);
                        return output;
                    }
                    var stdout = await process.StandardOutput.ReadToEndAsync();
                    var stderr = await process.StandardError.ReadToEndAsync();
                    if (stdout != null && stdout.Length > 0)
                    {
                        output["stdout"] = stdout;
                    }
                    if (stderr != null && stderr.Length > 0)
                    {
                        output["stderr"] = stderr;
                    }
                    return output;
                }
                catch (Exception ex)
                {
                    output["exception"] = ex;
                    return output;
                }
            }
        }

        public static bool CheckRunCmdError(Dictionary<string, object> output) => output.ContainsKey("error") || output.ContainsKey("exception");

        public static string GetRunCmdError(Dictionary<string, object> output) => (output.ContainsKey("error") ? (string) output["error"] : "") 
            + (output.ContainsKey("exception") ? (string)output["exception"] : "");
        
        public static bool CheckRunCmdOutput(Dictionary<string, object> output, string checktext)
        {
            if (output.ContainsKey("error") || output.ContainsKey("exception"))
            {
                if (output.ContainsKey("error"))
                {
                    Error((string)output["error"]);
                }
                if (output.ContainsKey("exception"))
                {
                    Error((Exception) output["exception"], "Exception thrown during process execution.");
                }
                return false;
            }
            else
            {
                if (output.ContainsKey("stderr"))
                {
                    var stderr = (string)output["stderr"];
                    Info(stderr);
                }
                if (output.ContainsKey("stdout"))
                {
                    var stdout = (string)output["stdout"];
                    Info(stdout);
                    if (stdout.Contains(checktext))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive = false)
        {
            using var op = Begin("Copying {0} to {1}", sourceDir, destinationDir);
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
            op.Complete();
        }

        public static async Task CopyDirectoryAsync(string sourceDir, string destinationDir, bool recursive = false)
        {
            using var op = Begin("Copying {0} to {1}", sourceDir, destinationDir);
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Get the files in the source directory and copy to the destination directory
            foreach (var file in dir.GetFiles())
            {
                var of = Path.Combine(destinationDir, file.Name);
                using (FileStream sourceStream = file.Open(FileMode.Open))
                {
                    using (FileStream destinationStream = File.Create(of))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                    }
                }
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    await CopyDirectoryAsync(subDir.FullName, newDestinationDir, true);
                }
            }
            op.Complete();
        }

        public static async Task CopyFileAsync(string src, string dst)
        {
            using (var srcStream = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, useAsync: true))
            using (var dstStream = new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.Write, 0x1000, useAsync: true))
            {
                await srcStream.CopyToAsync(dstStream);
            }
        }

        public static async Task<bool> DownloadFileAsync(string name, Uri downloadUrl, string downloadPath)
        {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            using (var op = Begin("Downloading {0} from {1} to {2}", name, downloadUrl, downloadPath))
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        var b = await client.DownloadDataTaskAsync(downloadUrl);
                        if (b != null)
                        {
                            File.WriteAllBytes(downloadPath, b);
                            op.Complete();
                            return true;
                        }
                        else
                        {
                            op.Abandon();
                            Error("Downloading {file} to {path} from {url} did not return any data.", name, downloadPath, downloadUrl);
                            return false;
                        }
                    }
                    catch (Exception ex) 
                    {
                        op.Abandon();
                        Error(ex, "Exception thrown downloading {file} to {path} from {url}.", name, downloadPath, downloadUrl);
                        return false;
                    }
                }
            }
#pragma warning restore SYSLIB0014 // Type or member is obsolete
        }

        public static string ViewFilePath(string path, string? relativeTo = null)
        {
            if (!DebugEnabled)
            {
                if (path is null)
                {
                    return string.Empty;
                }
                else if (relativeTo is null)
                {
                    return (Path.GetFileName(path) ?? path);
                }
                else
                {
                    return (IOExtensions.GetRelativePath(relativeTo, path));
                }
            }
            else return path;
        }

        public static Configuration CreateDefaultConfig()
        {
            var cfg = new Configuration();
            var general = cfg.Add("General");
            general.Add("Debug", false);
            general.Add("DeleteLogsOlderThan", 2);
            return cfg;
        }

        public static void SaveConfig(Configuration cfg, string filename) => cfg.SaveToFile(WarnIfFileExists(filename));

        public static Configuration LoadConfig(string filename) => Configuration.LoadFromFile(FailIfFileNotFound(filename));

        public static Configuration BindConfig(Configuration cfg1, Configuration cfg2)
        {
            foreach (var section in cfg2)
            {
                if (!cfg1.Contains(section.Name))
                {
                    cfg1.Add(section.Name);
                }
                foreach (var setting in section)
                {
                    if (!cfg1[section.Name].Contains(setting.Name))
                    {
                        cfg1[section.Name].Add(setting);
                    }
                    else
                    {
                        cfg1[section.Name][setting.Name].RawValue = setting.RawValue;
                    }
                }
            }
            return cfg1;
        }

        public static T GlobalSetting<T>(string section, string setting, T defaultval, bool setDefault=false)
        {
            return GlobalConfig[section][setting].IsEmpty ? defaultval! : GlobalConfig[section][setting].GetValueOrDefault(defaultval!, setDefault);
        }
        #endregion

        #region Fields
        protected static object __lock = new object();
        #endregion
    }
}
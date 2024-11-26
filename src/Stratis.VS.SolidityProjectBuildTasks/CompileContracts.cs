using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Stratis.VS.StratisEVM.SolidityCompilerIO;

namespace Stratis.VS
{
    public class CompileContracts : Task
    {
        [Required]
        public string ExtDir { get; set; }

        [Required]
        public string ProjectDir { get; set; }

        [Required]
        public ITaskItem[] Contracts { get; set; }

        public Dictionary<string, Source> Sources => Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

        public override bool Execute()
        {
            var solcpath = Directory.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) ? Path.Combine(ProjectDir, "node_modules", "solc", "solc.js") :
                Path.Combine(ExtDir, "node_modules", "solc", "solc.js");
            var cmdline = "node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"";
            var sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

            if (!(Directory.Exists(Path.Combine(ExtDir, "node_modules")) && File.Exists(Path.Combine(ExtDir, "node_modules", "solidity", "dist", "cli", "server.js"))))
            {
                if (!InstallSolidityLanguageServer())
                {
                    return false;
                }
            }

            Log.LogMessage(MessageImportance.High, "Compiling {0} file(s) in directory {1}...", Contracts.Count(), ProjectDir);
            Log.LogCommandLine(MessageImportance.High, cmdline);

            var psi = new ProcessStartInfo("cmd.exe", "/c " + cmdline)
            {
                UseShellExecute = false,
                WorkingDirectory = ExtDir,
                CreateNoWindow = true,  
                RedirectStandardOutput = true,  
                RedirectStandardInput = true,   
                RedirectStandardError = true,
            };
            var p = new Process()
            {
                StartInfo = psi   
            };

            var i = new SolidityCompilerInput()
            {
                Language = "Solidity",
                Settings = new Settings()
                {
                    EvmVersion = "byzantium",
                    OutputSelection = new Dictionary<string, Dictionary<string, string[]>>()
                    {
                        {"*", new Dictionary<string, string[]>()
                            {
                                {"*", new [] {"evm.bytecode" } }
                            }  
                        }
                    }
                },
                Sources =  this.Sources        
            };

            try
            {
                if (!p.Start())
                {
                    Log.LogError("Could not start npm process", psi.FileName + " " + psi.Arguments);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(new Exception($"Exception thrown starting process {psi.FileName} {psi.Arguments}", ex));
                return false;
            }

            CompactJson.Serializer.Write(i, p.StandardInput, false);
            p.StandardInput.WriteLine(Environment.NewLine);
            p.StandardInput.Close();
            var o = p.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(o))
            {
                var err = p.StandardError.ReadToEnd();
                Log.LogError("Compilation failed: {0}", err);
                if (!p.HasExited)
                {
                    p.Kill();
                }
                return false;   
            }
            if (!p.HasExited)
            {
                p.Kill();
            }
            var on = o.Split('\n');
            var jo = on.First().TrimStart().StartsWith(">") ? on.Skip(1).Aggregate((s, n) => s + "\n" + n) : o;
            Log.LogMessage(MessageImportance.Low, jo);
            SolidityCompilerOutput output;
            try
            {
                output = CompactJson.Serializer.Parse<SolidityCompilerOutput>(jo);
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
            bool haserror = false;
            if (output.errors.Length > 0)
            {
                foreach (var error in output.errors)
                {
                    if (error.severity == "warning")
                    {
                        if (error.sourceLocation == null)
                        {
                            Log.LogWarning(error.message);
                        }
                        else
                        {
                            var (file, startl, startc, endl, endc) = GetFileLineColFromError(error);
                            Log.LogWarning(error.type, error.errorCode, error.component, file, startl, startc, endl, endc, error.message);
                        }
                    }
                    else
                    {
                        haserror = true;
                        if (error.sourceLocation == null)
                        {
                            Log.LogError(error.message);
                        }
                        else
                        {
                            var (file, startl, startc, endl, endc) = GetFileLineColFromError(error);
                            Log.LogError(error.type, error.errorCode, error.component, file, startl, startc, endl, endc, error.message);
                        }
                    }
                }
                
            }
            return !haserror;
        }

        protected (int, int) GetLineColFromPos(string text, int pos)
        {
            int line = 1, col = 0;
            for (int i = 0; i < pos; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    col = 0;
                }
                else
                {
                    col++;
                }
            }
            return (line, col);
        }

        protected (string,int,int,int,int) GetFileLineColFromError(Error error)
        {
            var fn = error.sourceLocation?.file ?? throw new ArgumentNullException(nameof(error.sourceLocation));
            var fm = error.formattedMessage ?? throw new ArgumentNullException(nameof(error.formattedMessage));
            var file = Sources[fn].Urls[0];
            var text = File.ReadAllText(file);
            var (startl, startcol) = GetLineColFromPos(text, error.sourceLocation.start);
            var (endl, endcol) = GetLineColFromPos(text, error.sourceLocation.end);
            return (file, startl, startcol, endl, endcol);
            
            /*
            var s = fm.Split(new[] { "-->" }, StringSplitOptions.None);
            if (s.Length == 1)
            {
                return (file,0,0);  
            }
            var pos = s[1].Split('\n')[0].Split(':');
            var line = Convert.ToInt32(pos[0]);
            var col = Convert.ToInt32(pos[1]);
            return (file, line, col);
            */
        }

        protected bool InstallSolidityLanguageServer()
        {
            Log.LogMessage(MessageImportance.High, "Installing vscode-solidity language server...");
            var output = RunCmd("cmd.exe", "/c npm install solidity-0.0.165.tgz --force --quiet --no-progress", ExtDir);
            if (CheckRunCmdOutput(output, "Run `npm audit` for details."))
            {
                Log.LogMessage(MessageImportance.High, "vscode-solidity language server installed.");
                return true;
            }
            else
            {
                Log.LogError("Could not install vscode-solidity language server.");
                return false;
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

        public bool CheckRunCmdError(Dictionary<string, object> output) => output.ContainsKey("error") || output.ContainsKey("exception");

        public string GetRunCmdError(Dictionary<string, object> output) => (output.ContainsKey("error") ? (string)output["error"] : "")
            + (output.ContainsKey("exception") ? (string)output["exception"] : "");

        public bool CheckRunCmdOutput(Dictionary<string, object> output, string checktext)
        {
            if (output.ContainsKey("error") || output.ContainsKey("exception"))
            {
                if (output.ContainsKey("error"))
                {
                    Log.LogError((string)output["error"]);
                }
                if (output.ContainsKey("exception"))
                {
                    Log.LogErrorFromException(new Exception("Exception thrown during process execution.", (Exception)output["exception"]));
                }
                return false;
            }
            else
            {
                if (output.ContainsKey("stderr"))
                {
                    var stderr = (string)output["stderr"];
                    Log.LogMessage(MessageImportance.Low, stderr);
                }
                if (output.ContainsKey("stdout"))
                {
                    var stdout = (string)output["stdout"];
                    Log.LogMessage(MessageImportance.Low, stdout);
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
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string EVMVersion { get; set; }

        public string BindingsNS { get; set; } = "Ethereum";

        public Dictionary<string, Source> Sources => Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

        public override bool Execute()
        {
            if (File.Exists(Path.Combine(ProjectDir, "package.json")) && !File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")))
            {
                Log.LogMessage(MessageImportance.High, "Installing NPM dependencies in project directory {0}...", ProjectDir);
             
                var npmoutput = RunCmd("cmd.exe", "/c npm install", ProjectDir);
                if (!CheckRunCmdError(npmoutput))
                {
                    Log.LogMessage(MessageImportance.High, ((string)npmoutput["stdout"]).Trim());
                    
                }
                else
                {
                    Log.LogError("Could not install NPM dependencies: " + GetRunCmdError(npmoutput));
                }
            }
            var solcpath = File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) ? Path.Combine(ProjectDir, "node_modules", "solc", "solc.js") :
                Path.Combine(ExtDir, "node_modules", "solc", "solc.js");
            if (!File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")))
            {
                Log.LogWarning("solc compiler not present in project node_modules directory. Falling back to embedded compiler.");
            }
            if (!File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) && !(Directory.Exists(Path.Combine(ExtDir, "node_modules")) && File.Exists(Path.Combine(ExtDir, "node_modules", "solidity", "dist", "cli", "server.js"))))
            {
                if (!InstallSolidityLanguageServer())
                {
                    Log.LogError("No solc compiler available. Stopping.");
                    return false;
                }
            }

            var cmdline = "node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"" + " --include-path=\"" + Path.Combine(ProjectDir, "node_modules") + "\"";
            var sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });
            Log.LogMessage(MessageImportance.High, "Compiling {0} file(s) in directory {1} using solc compiler targeting EVM version {2}...", Contracts.Count(), ProjectDir, EVMVersion);
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
                    EvmVersion = EVMVersion,
                    OutputSelection = new Dictionary<string, Dictionary<string, string[]>>()
                    {
                        {"*", new Dictionary<string, string[]>()
                            {
                                {"*", new [] {"abi", "evm.bytecode", "evm.gasEstimates" } }
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
            if (output.errors != null && output.errors.Length > 0)
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

            if (haserror)
            {
                return false;
            }

            var outputdir = Path.Combine(ProjectDir, OutputPath);
            if (!Directory.Exists(outputdir))
            {
                Directory.CreateDirectory(outputdir);
            }

            File.WriteAllText(Path.Combine(outputdir, "compileroutput.json"), Serialize(output));
            foreach (var c in output.contracts)
            {
                //File.WriteAllText(Path.Combine(outputdir, c.Key + "compileroutput.json"), Serialize(c.Value)); 
                foreach (var cs in c.Value)
                {
                    //var cs = c.Value.Values.First();
                    if (Sources.ContainsKey(c.Key))
                    {
                        if (cs.Value.evm.bytecode._object != null)
                        {
                            File.WriteAllText(Path.Combine(outputdir, c.Key + "." + cs.Key + ".bin"), cs.Value.evm.bytecode._object);
                        }
                        if (!string.IsNullOrEmpty(cs.Value.evm.bytecode.opcodes))
                        {
                            File.WriteAllText(Path.Combine(outputdir, c.Key + "." + cs.Key + ".opcodes.txt"), cs.Value.evm.bytecode.opcodes);
                        }
                        if (cs.Value.evm.gasEstimates != null)
                        {
                            File.WriteAllText(Path.Combine(outputdir, c.Key + "." + cs.Key + ".gas.json"), Serialize(cs.Value.evm.gasEstimates));
                        }
                        if (cs.Value.abi != null)
                        {
                            File.WriteAllText(Path.Combine(outputdir, c.Key + "." + cs.Key + ".abi"), Serialize(cs.Value.abi));
                        }
                    }
                }
            }
            
            if (!InstallNethereumGeneratorToolIfNotPresent())
            {
                Log.LogError("Cannot generate .NET bindings for Solidity project.");
                return true;
            }
            if (!Directory.Exists(Path.Combine(ProjectDir, "bindings")))
            {
                Directory.CreateDirectory(Path.Combine(ProjectDir, "bindings"));
            }

            foreach (var c in output.contracts)
            {
                foreach (var cs in c.Value)
                {
                    if (Sources.ContainsKey(c.Key))
                    {
                        if (cs.Value.evm.bytecode._object != null && cs.Value.abi != null)
                        {
                            if (CheckRunCmdOutput(RunCmd("cmd.exe", $"/c  dotnet Nethereum.Generator.Console generate from-abi -abi {Path.Combine(outputdir, c.Key + "." + cs.Key + ".abi")} -bin {Path.Combine(outputdir, c.Key + "." + cs.Key + ".bin")} -o bindings -ns {BindingsNS}", ProjectDir), ""))
                            {
                                Log.LogMessage(MessageImportance.High, $"Created .NET bindings for {c.Key + "." + cs.Key} contract at {Path.Combine(ProjectDir, "bindings", c.Key + "." + cs.Key)}.");
                            }
                            else
                            {
                                Log.LogError($"Could not create .NET bindings for {c.Key + "." + cs.Key} contract.");
                            }
                        }
                    }
                }
            }

            return true;
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
                Log.LogMessage(MessageImportance.High, "Solidity language server installed.");
                return true;
            }
            else
            {
                Log.LogError("Could not install Solidity language server.");
                return false;
            }
        }
        
        protected bool InstallNethereumGeneratorToolIfNotPresent()
        {
            if (!File.Exists(Path.Combine(ProjectDir, ".config", "dotnet-tools.json")))
            {
                Log.LogMessage(MessageImportance.High, $"Installing .NET tool manifest...");
                if (!CheckRunCmdOutput(RunCmd("cmd.exe", "/c dotnet new tool-manifest", ProjectDir), "was created successfully"))
                {
                    Log.LogError("Could not install .NET tools manifest in " + ProjectDir);
                    return false;
                }
            }
            if (!CheckRunCmdOutput(RunCmd("cmd.exe", "/c dotnet tool list", ProjectDir), "nethereum.generator.console"))
            {
                Log.LogMessage(MessageImportance.High, $"Installing Nethereum generator .NET tool...");
                if (!CheckRunCmdOutput(RunCmd("cmd.exe", "/c dotnet tool install Nethereum.Generator.Console", ProjectDir), "successfully installed"))
                {
                    Log.LogError("Could not install Nethereum.Generator.Console .NET tool in " + ProjectDir);
                    return false;
                }
            }
            return true;
        }

        public Dictionary<string, object> RunCmd(string filename, string arguments, string workingdirectory)
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
                    Log.LogCommandLine($"{filename} {arguments}");
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
            + (output.ContainsKey("exception") ? (string)output["exception"] : "" + (output.ContainsKey("stderr") ? (string)output["stderr"] : ""));

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
                    Log.LogErrorFromException((Exception)output["exception"]);
                }
                return false;
            }
            else
            {
                if (output.ContainsKey("stderr"))
                {
                    var stderr = (string)output["stderr"];
                    Log.LogMessage(MessageImportance.High, stderr);
                    return false;
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
                else if (checktext == "" && !output.ContainsKey("stdout"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static byte[] FromHexString(string input)
        {
            int PerformModularArithmeticCalculation(char value) => (value % 32 + 9) % 25;

            if (input.Length % 2 != 0)
                throw new ArgumentException("Input has invalid length", nameof(input));

            if (input.StartsWith("0x"))
                input = input.Substring(2);

            if (string.IsNullOrEmpty(input))    
                return Array.Empty<byte>();

            var dest = new byte[input.Length >> 1];
            for (int i = 0, j = 0; j < dest.Length; j++)
                dest[j] = (byte)((PerformModularArithmeticCalculation(input[i++]) << 4) +
                    PerformModularArithmeticCalculation(input[i++]));

            return dest;
        }

        public string Serialize<T>(T obj)
        {
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                CompactJson.Serializer.Write(obj, new StringWriter(sb), true);
                return sb.ToString();
            }
        }
    }
}

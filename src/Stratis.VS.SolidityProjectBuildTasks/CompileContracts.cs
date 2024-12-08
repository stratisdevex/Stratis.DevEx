using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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


        public Dictionary<string, Source> Sources => Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

        public override bool Execute()
        {
            
            var solcpath = File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) ? Path.Combine(ProjectDir, "node_modules", "solc", "solc.js") :
                Path.Combine(ExtDir, "node_modules", "solc", "solc.js");
            var cmdline = "node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"" + " --include-path=\"" + Path.Combine(ProjectDir, "node_modules") + "\"";
            var sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

            if (!File.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) && !(Directory.Exists(Path.Combine(ExtDir, "node_modules")) && File.Exists(Path.Combine(ExtDir, "node_modules", "solidity", "dist", "cli", "server.js"))))
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
                    EvmVersion = "shanghai",
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
            
            foreach(var c in output.contracts)
            {
                var cs = c.Value.Values.First();
                if (Sources.ContainsKey(c.Key))
                {
                    if (cs.evm.bytecode._object != null)
                    {
                        File.WriteAllBytes(Path.Combine(outputdir, c.Key + ".bin"), FromHexString(c.Value.Values.First().evm.bytecode._object));
                    }
                    if (!string.IsNullOrEmpty(cs.evm.bytecode.opcodes))
                    {
                        File.WriteAllText(Path.Combine(outputdir, c.Key + ".opcodes.txt"), cs.evm.bytecode.opcodes);
                    }
                    if (cs.evm.gasEstimates != null)
                    {
                        File.WriteAllText(Path.Combine(outputdir, c.Key + ".gas.txt"), Deserialize(cs.evm.gasEstimates));
                    }
                    if (cs.abi != null)
                    {
                        File.WriteAllText(Path.Combine(outputdir, c.Key + ".abi"), Deserialize(cs.abi));
                    }
                }
            }

            InstallNethereumGeneratorToolIfNotPresent();
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
                Log.LogMessage(MessageImportance.High, "vscode-solidity language server installed.");
                return true;
            }
            else
            {
                Log.LogError("Could not install vscode-solidity language server.");
                return false;
            }
        }
        
        protected bool InstallNethereumGeneratorToolIfNotPresent()
        {
            //Log.LogMessage(MessageImportance.High, $"Install Nethereum generator tool...");
            //var output = RunCmd("cmd.exe", "dotnet tool install Nethereum.Generator.Console", ProjectDir);
            return true;
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
                    Log.LogErrorFromException((Exception)output["exception"]);
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

        public string Deserialize<T>(T obj)
        {
            var sb = new StringBuilder();
            CompactJson.Serializer.Write(obj, new StringWriter(sb), true);
            return sb.ToString();   
        }
    }
}

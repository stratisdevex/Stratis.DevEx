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

        public override bool Execute()
        {
            var solcpath = Directory.Exists(Path.Combine(ProjectDir, "node_modules", "solc", "solc.js")) ? Path.Combine(ProjectDir, "node_modules", "solc", "solc.js") :
                Path.Combine(ExtDir, "node_modules", "solc", "solc.js");
            var sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } });

            Log.LogMessage(MessageImportance.High, "Compiling {0} file(s) in directory {1} using solcjs compiler at {2}...", Contracts.Count(), ProjectDir, Path.GetDirectoryName(solcpath));
            var psi = new ProcessStartInfo("cmd.exe", "/c node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"")
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
                Sources =   sources        
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
            var on = o.Split('\n');
            var jo = on.First().TrimStart().StartsWith(">") ? on.Skip(1).Aggregate((s, n) => s + "\n" + n) : o;
            Log.LogMessage(MessageImportance.High, jo);
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
            if (output.errors.Length > 0)
            {
                foreach(var error in output.errors)
                {
                    //var (error.sourceLocation.file)
                    if (error.severity == "warning")
                    {
                        if (error.sourceLocation == null)
                        {
                            Log.LogWarning(error.message);
                        }
                        
                        //Log.LogWarning(error.type, error.errorCode,"", error.sourceLocation.file, error.sourceLocation.l);
                    }
                    //Log.LogError(error.component, error.)
                }
            }

           
            if (!p.HasExited)
            {
                p.Kill();
            }
            return true;
        }
    }
}

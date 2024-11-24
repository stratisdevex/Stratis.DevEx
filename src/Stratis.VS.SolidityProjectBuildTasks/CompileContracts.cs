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
            Log.LogMessage(MessageImportance.High, "Compiling {0} files in directory {1} using solcjs compiler at {2}.", Contracts.Count(), ProjectDir, Path.GetDirectoryName(solcpath));
            var psi = new ProcessStartInfo("cmd.exe", "/c node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"")
            {
                UseShellExecute = false,
                WorkingDirectory = ProjectDir,
                CreateNoWindow = true,  
                RedirectStandardOutput = true,  
                RedirectStandardInput = true,   
                RedirectStandardError = false,
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
                Sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } })
            };

            if (!p.Start())
            {
                Log.LogError("Could not start npm process", psi.FileName + " " + psi.Arguments);
                return false;
            }
            var ser = new Newtonsoft.Json.JsonSerializer();
            ser.Serialize(p.StandardInput, i);
            p.StandardInput.WriteLine(Environment.NewLine);
            p.StandardInput.Close();
            Log.LogMessage(MessageImportance.High, p.StandardOutput.ReadToEnd());
            if (!p.HasExited)
            {
                p.Kill();
            }
            return true;
        }
    }
}

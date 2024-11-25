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
            Log.LogMessage(MessageImportance.High, "Compiling {0} file(s) in directory {1} using solcjs compiler at {2}...", Contracts.Count(), ProjectDir, Path.GetDirectoryName(solcpath));
            var psi = new ProcessStartInfo("cmd.exe", "/c node \"" + solcpath + "\" --standard-json --base-path=\"" + ProjectDir + "\"")
            {
                UseShellExecute = false,
                WorkingDirectory = ProjectDir,
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
                Sources = Contracts.ToDictionary(k => k.GetMetadata("Filename"), v => new Source() { Urls = new[] { Path.Combine(v.GetMetadata("RelativeDir"), v.ItemSpec) } })
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
                Log.LogErrorFromException(ex);
                return false;
            }

            CompactJson.Serializer.Write(i, p.StandardInput, false);
            p.StandardInput.WriteLine(Environment.NewLine);
            p.StandardInput.Close();
            var o = p.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(o))
            {
                var err = p.StandardError.ReadToEnd();
                if (!p.HasExited)
                {
                    p.Kill();
                }
                Log.LogError("Compilation failed: {0}", err);
                return false;   
            }
            var on = o.Split('\n');
            var jo = on.First().TrimStart().StartsWith(">") ? on.Skip(1).Aggregate((s, n) => s + "\n" + n) : o;
            Log.LogMessage(MessageImportance.High, p.StandardOutput.ReadToEnd());
            if (!p.HasExited)
            {
                p.Kill();
            }
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Stratis.DevEx;

namespace Stratis.VS.StratisEVM
{
    public class SolidityCompiler : Runtime
    {
        public static async Task CompileFileAsync(string file, string workspaceDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Solidity Compiler");
            VSUtil.LogInfo("Solidity Compiler", string.Format("Compiling {0} in {1}...", file, workspaceDir));
            var binfiles = Directory.GetFiles(AssemblyLocation, "*.bin", SearchOption.TopDirectoryOnly);
            foreach ( var binfile in binfiles ) 
            {
                File.Delete(binfile);   
            }
            
           
            var cmd = "cmd.exe";
            var args = "/c node " + Path.Combine("node_modules", "solc", "solc.js") + " --base-path=\"" + workspaceDir + "\"" + " \"" + file + "\" --bin";
            if (Directory.Exists(Path.Combine(workspaceDir, "node_modules")))
            {
                args += " --include-path=" + Path.Combine(workspaceDir, "node_modules");
            }
            var output = await ThreadHelper.JoinableTaskFactory.RunAsync(async () => await RunCmdAsync(cmd, args, AssemblyLocation));
            if (CheckRunCmdError(output)) 
            {
                VSUtil.LogError("Solidity Compiler", "Could not run Solidity Compiler process: " + cmd + " " + args + ": " + GetRunCmdError(output));
                return;
            }
            if (output.ContainsKey("stdout"))
            {
                VSUtil.LogInfo("Solidity Compiler", (string)output["stdout"]);
            }

            if (output.ContainsKey("stderr"))
            {
                VSUtil.LogInfo("Solidity Compiler", (string)output["stderr"]);
            }
            if (output.ContainsKey("stdout") || output.ContainsKey("stderr"))
            {
                return;
            }
            else
            {
                binfiles = Directory.GetFiles(AssemblyLocation, "*.bin", SearchOption.TopDirectoryOnly);
                if (binfiles is null || binfiles.Length == 0) 
                {
                    VSUtil.LogError("Solidity Compiler", "Could not read Solidity compiler output. No compiler output files found.");
                    return;
                }
                else
                {
                    string b = null;
                    foreach (var binfile in binfiles)
                    {
                        if (binfile.Contains(Path.GetFileNameWithoutExtension(file)))
                        {
                            b = File.ReadAllText(binfile);   
                            VSUtil.LogInfo("Solidity Compiler", "======= " + file + "======= " + "\nBinary: \n" + b);
                        }
                        File.Delete(binfile);
                    }
                    if (b == null)
                    {
                        VSUtil.LogError("Solidity Compiler", "Error reading Solidity compiler output: could not find compiler output file for " + file + ".");

                    }

                    return;
                }
                

            }
        }
    }
}

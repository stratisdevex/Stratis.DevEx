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
using static Microsoft.VisualStudio.ProjectSystem.VS.HResult.Ole;

namespace Stratis.VS.StratisEVM
{
    public class SolidityCompiler : Runtime
    {
        public static async Task CompileFileAsync(string file, string workspaceDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Solidity Compiler");
            VSUtil.LogInfo("Solidity Compiler", string.Format("Compiling {0} in {1}...", file, workspaceDir));
            await TaskScheduler.Default;
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
            var output = await RunCmdAsync(cmd, args, AssemblyLocation);
            if (CheckRunCmdError(output)) 
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogError("Solidity Compiler", "Could not run Solidity Compiler process: " + cmd + " " + args + ": " + GetRunCmdError(output));
                return;
            }
            else if (output.ContainsKey("stdout"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("Solidity Compiler", (string)output["stdout"]);
                return;
            }
            else if (output.ContainsKey("stderr"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("Solidity Compiler", (string)output["stderr"]);
                return;
            }
            else
            {
                binfiles = Directory.GetFiles(AssemblyLocation, "*.bin", SearchOption.TopDirectoryOnly);
                if (binfiles is null || binfiles.Length == 0) 
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
                        }
                        File.Delete(binfile);
                    }
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (b == null)
                    {
                        VSUtil.LogError("Solidity Compiler", "Error reading Solidity compiler output: could not find compiler output file for " + file + ".");
                    }
                    else
                    {
                        VSUtil.LogInfo("Solidity Compiler", "======= " + file + "======= " + "\nBinary: \n" + b);
                    }
                    return;
                }
            }
        }

        public static async Task InstallNPMPackagesAsync(string projectDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Stratis EVM");
            VSUtil.LogInfo("Stratis EVM", string.Format("Installing NPM dependencies in project directory {0}...", projectDir));
            await TaskScheduler.Default;
            var output = await RunCmdAsync("cmd.exe", "/c npm install", projectDir);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (CheckRunCmdError(output))
            {
                VSUtil.LogError("Stratis EVM", "Could not install NPM dependencies: " + GetRunCmdError(output));
                return;
            }
            VSUtil.LogInfo("Stratis EVM", ((string)output["stdout"]).Trim());
        }
    }
}

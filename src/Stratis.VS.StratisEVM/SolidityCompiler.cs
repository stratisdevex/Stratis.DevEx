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
        public static async Task CompileFileAsync(string file)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Solidity Compiler");
            var cmd = "cmd.exe";
            var args = "/c node " + Path.Combine("node_modules", "solc", "solc.js") + " \"" + file + "\" --bin";
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
        }
    }
}

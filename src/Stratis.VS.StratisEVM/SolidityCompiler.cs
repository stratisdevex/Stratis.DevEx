using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.Arguments = "/c solc " + file + " --bin";
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            using (var process = new Process())
            {
                process.StartInfo = info;
                process.EnableRaisingEvents = true;
                try
                {
                    if (!process.Start())
                    {
                        VSUtil.LogError("Could not start Solidity Compiler process {process}.", info.FileName + " " + info.Arguments);
                        return;
                    }
                    var stdout = await process.StandardOutput.ReadToEndAsync();
                    var stderr = await process.StandardError.ReadToEndAsync();
                    if (stdout != null && stdout.Length > 0) 
                    {
                        VSUtil.LogInfo("Solidity Compiler", stdout);
                    }
                    if (stderr != null && stderr.Length > 0)
                    {
                        VSUtil.LogError("Solidity Compiler", stderr);
                    }
                    await process.WaitForExitAsync();
                }
                catch (Exception ex)
                {
                    VSUtil.LogError("Solidity Compiler", ex);
                    return;
                }
            }
        }
    }
}

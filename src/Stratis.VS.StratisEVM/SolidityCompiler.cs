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
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.Arguments = "/c solc " + file + " --bin";
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            var process = new Process();
            process.StartInfo = info;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null && e.Data.Length > 0)
                {
                    VSUtil.LogInfo("Solidity Compiler", e.Data.Trim());
                }
            };
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null && e.Data.Length > 0)
                {
                    VSUtil.LogError("Solidity Compiler", e.Data.Trim());
                }
            };
            try
            {
                if (!process.Start())
                {
                    VSUtil.LogError("Could not start Solidity Compiler process {process}.", info.FileName + " " + info.Arguments);
                    return;
                }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
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

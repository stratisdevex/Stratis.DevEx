using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    internal class TestChain : Runtime
    {
        internal static PipeClient<MessagePack>? PipeClient { get; set; }

        internal static Process? Run()
        {
            var psi = new ProcessStartInfo();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                psi.FileName = Path.Combine(AssemblyLocation, "testchain", "Stratis.DevEx.TestChain.exe");
            }
            else
            {
                psi.FileName = "dotnet";
                psi.Arguments = Path.Combine(AssemblyLocation, "testchain", "Stratis.DevEx.TestChain.exe");
            }
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            var output = new StringBuilder();
            var error = new StringBuilder();
            var p = new Process()
            {
                StartInfo = psi,
            };
            
            p.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    output.AppendLine(e.Data);
                }
            };
            p.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                {
                    error.AppendLine(e.Data);
                    Debug(e.Data);
                }
            };
            
            using var op = Begin("Starting TestChain: executing cmd {c} {a}.", psi.FileName, psi.Arguments); 
            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                op.Complete();
                return p;
            }

            catch (Exception ex)
            {
                Error(ex, "Error executing command {0} {1}.", psi.FileName, psi.Arguments);
                return null;
            }

        }

        internal static bool IsInitialized; 
    }
}

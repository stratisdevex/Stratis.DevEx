using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Stratis.DevEx;
using Stratis.DevEx.Ethereum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stratis.VS.StratisEVM
{
    public class SolidityCompiler : Runtime
    {
        public static string SlitherPath => Path.Combine(TaskToolsDir, "slither-0.10.3.exe");

        public static string TaskToolsDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomProjectSystems", "Solidity", "Tools");

        public static async Task CompileFileAsync(string file, string workspaceDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "StratisEVM");
            VSUtil.LogInfo("StratisEVM", string.Format("Compiling {0} in {1} using solc.js compiler...", file, workspaceDir));
            await TaskScheduler.Default;
            var binfiles = Directory.GetFiles(workspaceDir, "*.bin", SearchOption.TopDirectoryOnly);
            foreach ( var binfile in binfiles ) 
            {
                File.Delete(binfile);   
            }
            var cmd = "cmd.exe";
            var solcpath = File.Exists(Path.Combine(workspaceDir, "node_modules", "solc", "solc.js")) ? Path.Combine(workspaceDir, "node_modules", "solc", "solc.js") : Path.Combine(AssemblyLocation, "node_modules", "solc", "solc.js");
            var args = "/c node " + "\"" + solcpath + "\"" + " --base-path=\"" + workspaceDir + "\"" + " \"" + file + "\" --bin";
            if (Directory.Exists(Path.Combine(workspaceDir, "node_modules")))
            {
                args += " --include-path=" + Path.Combine(workspaceDir, "node_modules");
            }
            var output = await RunCmdAsync(cmd, args, workspaceDir);
            if (CheckRunCmdError(output)) 
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogError("StratisEVM", "Could not run process: " + cmd + " " + args + ": " + GetRunCmdError(output));
                return;
            }
            else if (output.ContainsKey("stdout"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("StratisEVM", (string)output["stdout"]);
                return;
            }
            else if (output.ContainsKey("stderr"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("StratisEVM", (string)output["stderr"]);
                return;
            }
            else
            {
                binfiles = Directory.GetFiles(workspaceDir, "*.bin", SearchOption.TopDirectoryOnly);
                if (binfiles is null || binfiles.Length == 0) 
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VSUtil.LogError("StratisEVM", "Could not read Solidity compiler output. No compiler output files found.");
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
                        VSUtil.LogError("StratisEVM", "Error reading Solidity compiler output: could not find compiler output file for " + file + ".");
                    }
                    else
                    {
                        VSUtil.LogInfo("StratisEVM", "======= " + file + "======= " + "\nBinary: \n" + b);
                    }
                    return;
                }
            }
        }

        public static async Task InstallNPMPackagesAsync(string projectDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "StratisEVM");
            VSUtil.LogInfo("StratisEVM", string.Format("Installing NPM dependencies in project directory {0}...", projectDir));
            await TaskScheduler.Default;
            var output = await RunCmdAsync("cmd.exe", "/c npm install", projectDir);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (CheckRunCmdError(output))
            {
                VSUtil.LogError("StratisEVM", "Could not install NPM dependencies: " + GetRunCmdError(output));
                return;
            }
            VSUtil.LogInfo("StratisEVM", ((string)output["stdout"]).Trim());
        }

        public static SolidityCompilerIO2.SolidityCompilerOutput ParseOutputFile(string file) => 
            JsonConvert.DeserializeObject<SolidityCompilerIO2.SolidityCompilerOutput>(File.ReadAllText(file));


        public static bool InstallSolcCompiler(string compilerVersion)
        {
            string solcPath = Path.Combine(TaskToolsDir, ".solc-select", "artifacts", "solc-" + compilerVersion, "solc-" + compilerVersion);
            if (File.Exists(solcPath))
            {
                return true;
            }
            VSUtil.LogInfo("StratisEVM", $"Installing solc {compilerVersion} compiler...");
            var solcselectpath = Path.Combine(TaskToolsDir, "solc-select.exe");
            if (!File.Exists(solcselectpath))
            {
                VSUtil.LogError("StratsEVM", $"Could not find solc-select executable. Could not install solc {compilerVersion} compiler");
                return false;

            }
            var output = RunCmd("cmd.exe", $"/c solc-select.exe install {compilerVersion}", TaskToolsDir);
            if (CheckRunCmdOutput(output, $"Version '{compilerVersion}' installed") && File.Exists(solcPath))
            {
                VSUtil.LogInfo("StratisEVM", $"solc {compilerVersion} compiler installed.");
                return true;
            }
            else
            {
                VSUtil.LogError("StratisEVM", $"Could not install solc {compilerVersion} compiler: " + GetRunCmdError(output));
                return false;
            }
        }

        public static string GetSolcVersion(string filepath, string projectDir)
        {
            var packagejsonFilePath = Path.Combine(projectDir, "package.json");
            if (File.Exists(packagejsonFilePath))
            {
                var packagejson = PackageJsonFile.Parse(File.ReadAllText(packagejsonFilePath));
                if (packagejson.Dependencies.ContainsKey("solc"))
                {
                    return packagejson.Dependencies["solc"];
                }
            }
            var solidityversion = SolidityFileParser.GetSolidityVersionRange(filepath);
            if ( solidityversion.StartsWith("^"))
            {
                return solidityversion.Substring(1);
            }
            else
            {
                VSUtil.LogError("StratisEVM", $"Could not parse Solidity file version {solidityversion}.");
                return null;
            }

        }
        public static async Task<bool> InstallSolcCompilerAsync(string compilerVersion)
        {
            string solcPath = Path.Combine(TaskToolsDir, ".solc-select", "artifacts", "solc-" + compilerVersion, "solc-" + compilerVersion);
            if (File.Exists(solcPath))
            {
                return true;
            }
            var solcselectpath = Path.Combine(TaskToolsDir, "solc-select.exe");
            if (!File.Exists(solcselectpath))
            {
                VSUtil.LogError("StratisEVM", "Could not find solc-select executable.");
                return false;

            }
            var output = await RunCmdAsync("cmd.exe", $"/c solc-select.exe install {compilerVersion}", TaskToolsDir);
            if (CheckRunCmdOutput(output, $"Version '{compilerVersion}' installed") && File.Exists(solcPath))
            {
                VSUtil.LogInfo("StratisEVM", $"solc {compilerVersion} compiler installed at {solcPath}.");
                return true;
            }
            else
            {
                VSUtil.LogError("StratisEVM", $"Could not install solc {compilerVersion} compiler: " + GetRunCmdError(output));
                return false;
            }
        }

        public static async Task<SlitherAnalysis> AnalyzeAsync(string filePath, string projectDir, string outputDir, string compilerVersion = null)
        {
            var relativeFilePath = GetWindowsRelativePath(filePath, projectDir);
            compilerVersion = compilerVersion ?? GetSolcVersion(filePath, projectDir);
            if (compilerVersion is null)
            {
                VSUtil.LogError("StratisEVM", "Could not determine solc version. Falling back to 0.8.27.");
                compilerVersion = "0.8.27";
            }
            if (!await InstallSolcCompilerAsync(compilerVersion))
            {
                VSUtil.LogError("StratisEVM", $"Could not install solc {compilerVersion} compiler.");
                return null;    
            }
            string slitherAnalysisOutputPath = Path.Combine(outputDir, relativeFilePath + "slither-analysis.json");
            string solcPath = Path.Combine(TaskToolsDir, ".solc-select", "artifacts", "solc-" + compilerVersion, "solc-" + compilerVersion);
            string slitherargs = $"\"{filePath}\" --compile-force-framework solc --solc \"{solcPath}\" --solc-args \"--base-path {projectDir} --include-path {Path.Combine(projectDir, "node_modules")} \" --json -";            
            var slithercmdrun = await RunCmdAsync(SlitherPath, slitherargs, projectDir);
            var stdout = slithercmdrun.ContainsKey("stdout") ? (string)slithercmdrun["stdout"] : "";
            var stderr = slithercmdrun.ContainsKey("stderr") ? (string)slithercmdrun["stderr"] : "";
            if (stdout.Contains("\"success\": true"))
            {
                var r = JsonConvert.DeserializeObject<SlitherAnalysis>(stdout);
                VSUtil.LogInfo("StratisEVM", $"Slither analysis of {filePath} completed successfully.");
                return r;
            }
            else
            {
                VSUtil.LogError("StratisEVM", $"Slither analysis of {filePath} did not complete successfully. {stdout} {stderr}");
                return null;
            }
        }
    }
}

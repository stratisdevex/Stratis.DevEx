using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
            Log.LogMessage(MessageImportance.High, "Compiling {0} files in directory:{1}", Contracts.Count(), ProjectDir);
           

            var psi = new ProcessStartInfo("cmd.exe", "/c node" + solcpath + " --base-path=\"" + ProjectDir + "\"")
            {
                WorkingDirectory = ProjectDir,
                CreateNoWindow = true,  
                RedirectStandardOutput = false,  
                RedirectStandardInput = false,   
                RedirectStandardError = false,
            };
            var p = new Process()
            {
                StartInfo = psi   
            };
            return false;
        }
    }
}

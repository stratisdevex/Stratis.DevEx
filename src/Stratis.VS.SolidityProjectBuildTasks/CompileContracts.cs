using System;
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
            Log.LogMessage(MessageImportance.High, "{0}:{1}", ExtDir, Contracts.Select(c => c.ItemSpec).ToArray());
            return true;
        }
    }
}

using System;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Stratis.VS
{
    public class CompileContracts : Task
    {
        [Required]
        public ITaskItem[] Contracts { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("{0}", Contracts);
            return true;
        }
    }
}

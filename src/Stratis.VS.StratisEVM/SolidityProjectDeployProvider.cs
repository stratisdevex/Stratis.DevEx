﻿using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow ;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;

namespace Stratis.VS.StratisEVM
{
    [Export(typeof(IDeployProvider))]
    [AppliesTo(SolidityUnconfiguredProject.UniqueCapability)]
    internal class SolidityProjectDeployProvider : IDeployProvider
    {
        [ImportingConstructor] 
        public SolidityProjectDeployProvider() 
        {
            

        }

        public bool IsDeploySupported => true;

        public async Task DeployAsync(CancellationToken cancellationToken, TextWriter outputPaneWriter)
        {
            await outputPaneWriter.WriteLineAsync("Get out your popcorn, folks...");
            //await DoMassiveWorkAsync(cancellationToken);
            //outputPaneWriter.WriteLine("The party's over. Get back to work!");
        }

        /// <summary>
        /// Alerts a project that a deployment operation was successful. Called immediately after the project finishes deployment regardless of the result of other projects in the solution.
        /// </summary>
        public void Commit()
        {
        }

        //[Import]
        //private ProjectProperties Properties { get; set; }

        /// <summary>
        /// Alerts a deployment project that a deployment operation has failed. Called immediately after the project fails deployment regardless of the result of other projects in the solution.
        /// </summary>
        public void Rollback()
        {
        }


        [Import]
        internal UnconfiguredProject UnconfiguredProject { get; private set; }

    }
}

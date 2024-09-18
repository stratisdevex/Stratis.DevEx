using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.ComponentModel.Composition;

namespace Stratis.VS.StratisEVM
{
    /// <summary>
    /// Updates nodes in the project tree by overriding property values calculated so far by lower priority providers.
    /// </summary>
    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(SolidityUnconfiguredProject.UniqueCapability)]
    // TODO: Consider removing the Order attribute as it typically should not be needed when creating a new project type. It may be needed when customizing an existing project type to override the default behavior (e.g. the default C# implementation).
    [Order(1000)]
    internal class SolidityProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {
        /// <summary>
        /// Calculates new property values for each node in the project tree.
        /// </summary>
        /// <param name="propertyContext">Context information that can be used for the calculation.</param>
        /// <param name="propertyValues">Values calculated so far for the current node by lower priority tree properties providers.</param>
        public void CalculatePropertyValues(
            IProjectTreeCustomizablePropertyContext propertyContext,
            IProjectTreeCustomizablePropertyValues propertyValues)
        {
            // Only set the icon for the root project node.  We could choose to set different icons for nodes based
            // on various criteria, not just Capabilities, if we wished.
            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
            {
                // TODO: Provide a moniker that represents the desired icon (you can use the "Custom Icons" item template to add a .imagemanifest to the project)
                propertyValues.Icon = KnownMonikers.JSProjectNode.ToProjectSystemType();
            }
        }
    }
}

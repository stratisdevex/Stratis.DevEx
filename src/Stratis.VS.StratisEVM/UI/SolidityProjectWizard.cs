using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
using EnvDTE;

namespace Stratis.VS.StratisEVM
{
    public class SolidityProjectWizard : IWizard
    {
        private string customMessage;

        // This method is called before opening any item that
        // has the OpenInEditor attribute.
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        // This method is only called for item templates,
        // not for project templates.
        public void ProjectItemFinishedGenerating(ProjectItem
            projectItem)
        {
        }

        // This method is called after the project is created.
        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                // Display a form to the user. The form collects
                // input for the custom message.
                //inputForm = new SolidityProjectWizardUserInputForm();
                //inputForm.ShowDialog();

                //customMessage = SolidityProjectWizardUserInputForm.CustomMessage;

                wizard = new Window1();
                wizard.ShowDialog();
                // Add custom parameters.
                replacementsDictionary.Add("$evmversion$", wizard.SelectedEVMVersion);
                replacementsDictionary.Add("$soliditycompilerversion$", wizard.SelectedCompilerVersion);
                //    customMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
        {
            if (filePath.EndsWith(".sol") || filePath.EndsWith("package.json") || filePath.EndsWith("remappings.txt"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        Window1 wizard;
    }
}
using System;
using System.Collections.Generic;

using Eto.Drawing;
using Eto.Forms;

using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    public partial class MainForm : Form
    {
        #region Constructors
        public MainForm()
        {
			Style = "main";
			MinimumSize = new Size(900, 600);
			Title = "Stratis DevEx";
			#pragma warning disable CS0618 // Type or member is obsolete
			navigation = new TreeView()
            #pragma warning restore CS0618 // Type or member is obsolete
            {
                Size = new Size(100, 150)
            };
			navigation.DataStore = new TreeItem(
				new TreeItem() { Image = Globe, Text = "About" },
				new TreeItem() { Image = TestIcon, Text = "Projects"}
			);
            navigation.Activated += Navigation_NodeMouseClick;
            navigation.NodeMouseClick += Navigation_NodeMouseClick; ;
			//CreateTreeItem(0, "Item");
            projectViews = new List<WebView>();
            projectViews.Add(new WebView());
            projectViews[0].LoadHtml(@"<html><head><title>Hello!</title></head><body></body></html>");
			splitter = new Splitter();
			splitter.Panel1 = navigation;
			splitter.Panel2 = projectViews[0];
			splitter.Panel1MinimumSize = 300;
			splitter.Panel2MinimumSize = 600;
            Content = splitter;
           
            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command { MenuText = "Quit", Shortcut = GuiApp.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => GuiApp.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
					// File submenu
					new SubMenuItem { Text = "&File", Items = { clickMe } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand
            };

            // create toolbar			
            //ToolBar = new ToolBar { Items = { clickMe } };
        }

       
        #endregion

        #region Methods
        public void ReadMessage(CompilationMessage m)
        {

        }

        public void ReadMessage(ControlFlowGraphMessage m)
        {
            var projects = (TreeItem) navigation.DataStore[1];
            projects.Children.Add(new TreeItem
            {
                Key = m.EditorEntryAssembly + m.AssemblyName,
                Text = m.AssemblyName,
                Image = m.EditorEntryAssembly switch
                {
                   var x when x.StartsWith("VBCSCompiler") => VisualStudio,
                   var x when x.StartsWith("OmniSharp") => VSCode,
                   var x when x.StartsWith("JetBrains.Roslyn.Worker") => JetbrainsRider,
                   _ => Globe
                }
            });
            navigation.RefreshItem(projects);
        }

        #endregion

        #region Fields
        protected static readonly Icon TestIcon = Icon.FromResource("Stratis.DevEx.Gui.Images.TestIcon.ico");
        protected static readonly Icon JetbrainsRider = Icon.FromResource("Stratis.DevEx.Gui.Images.jetbrainsrider.png");
        protected static readonly Icon VisualStudio = Icon.FromResource("Stratis.DevEx.Gui.Images.visualstudio.png");
        protected static readonly Icon VSCode = Icon.FromResource("Stratis.DevEx.Gui.Images.vscode.png");
        protected static readonly Icon Globe = Icon.FromResource("Stratis.DevEx.Gui.Images.TestImage.png");
        #pragma warning disable CS0618 // Type or member is obsolete
		internal TreeView navigation;
		#pragma warning restore CS0618 // Type or member is obsolete
		
		internal List<WebView> projectViews;
        protected Splitter splitter;
        #endregion

        #region Event Handlers
        private void Navigation_NodeMouseClick(object? sender, TreeViewItemEventArgs e)
        {
            MessageBox.Show(this, "I was clicked!: " + e.Item.Key);
        }

        #endregion
    }
}

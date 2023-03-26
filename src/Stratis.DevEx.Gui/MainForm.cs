using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Eto.Drawing;
using Eto.Forms;

using Microsoft.Msagl.Drawing;

using Stratis.DevEx.Drawing;
using Stratis.DevEx.Pipes;

namespace Stratis.DevEx.Gui
{
    public partial class MainForm : Form
    {
        #region Constructors
        public MainForm()
        {
            Info("This is the UI thread.");
            
            #if Windows
            Eto.WinForms.Forms.Controls.WebView2Loader.EnsureWebView2Runtime();
            #endif

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
            projectView = new WebView();
            projectView.DocumentLoaded += ProjectView_DocumentLoaded;
            projectView.LoadHtml(@"<html><head><title>Hello!</title></head><body><h1>Hi!</h1></body></html>");
			splitter = new Splitter();
			splitter.Panel1 = navigation;
			splitter.Panel2 = projectView;
			splitter.Panel1MinimumSize = 300;
			splitter.Panel2MinimumSize = 600;
            projectViews = new Dictionary<string, Dictionary<string, object>>();
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

        private void ProjectView_DocumentLoaded(object? sender, WebViewLoadedEventArgs e)
        {
            Info("WebView loaded {url}.", e.Uri);
        }


        #endregion

        #region Methods
        public void ReadMessage(CompilationMessage m)
        {

        }

        public void ReadMessage(ControlFlowGraphMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var docid = projectid + "_" + m.Document;
            var projects = (TreeItem) navigation.DataStore[1];
            if (projects.Children.Any(c => c.Key == projectid))
            {
                Debug("Project {proj} already exists in tree, updating...", projectid);
                UpdateProjectDocsTree(m);
                App.AsyncInvoke(() => projectView.LoadHtml((string)projectViews[projectid][docid + "_" + "CFG"]));
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
                App.AsyncInvoke(() => projectView.LoadHtml((string)projectViews[projectid][docid + "_" + "CFG"])); 
            }
            navigation.RefreshItem(projects);
        }

        public void AddProjectToTree(ControlFlowGraphMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var docid = projectid + "_" + m.Document;
            var cfg = Program.CreateGraph(m);
            var doc = new TreeItem()
            {
                Key = docid,
                Text = m.Document,
                Image = CSharp
            };
            projectViews.Add(projectid, new Dictionary<string, object>() { { docid + "_" + "CFG", Html.DrawControlFlowGraph(cfg) } });
            Projects.Children.Add(new TreeItem(doc)
            {
                Key = m.EditorEntryAssembly + "_" + m.AssemblyName,
                Text = m.AssemblyName,
                Image = m.EditorEntryAssembly switch
                {
                    var x when x.StartsWith("VBCSCompiler") => VisualStudio,
                    var x when x.StartsWith("OmniSharp") => VSCode,
                    var x when x.StartsWith("JetBrains.Roslyn.Worker") => JetbrainsRider,
                    _ => Globe
                },
            });
        }

        public void UpdateProjectDocsTree(ControlFlowGraphMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var docid = projectid + "_" + m.Document;
            var project = (TreeItem)Projects.Children.First(c => c.Key == projectid);
            var cfg = Program.CreateGraph(m);
            projectViews[projectid][docid + "_" + "CFG"] = Html.DrawControlFlowGraph(cfg);
            if (project.Children.Any(c => c.Key == docid))
            {
                Debug("Document {doc} exists in project {proj}, updating...", docid, projectid);
            }
            else
            {
                Debug("Document {doc} does not exist in project {proj}, adding...", docid, projectid);
                projectViews[projectid][docid + "_" + "CFG"] = Html.DrawControlFlowGraph(cfg);
                TreeItem doc = new TreeItem()
                {
                    Key = docid,
                    Text = m.Document,
                    Image = CSharp
                };
                project.Children.Add(doc);
            }
        }

        #region Logging
        [DebuggerStepThrough]
        public static void Info(string messageTemplate, params object[] args) => Runtime.Info(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Debug(string messageTemplate, params object[] args) => Runtime.Debug(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(string messageTemplate, params object[] args) => Runtime.Error(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Error(Exception ex, string messageTemplate, params object[] args) => Runtime.Error(ex, messageTemplate, args);

        [DebuggerStepThrough]
        public static void Warn(string messageTemplate, params object[] args) => Runtime.Warn(messageTemplate, args);

        [DebuggerStepThrough]
        public static void Fatal(string messageTemplate, params object[] args) => Runtime.Fatal(messageTemplate, args);

        [DebuggerStepThrough]
        public static Logger.Op Begin(string messageTemplate, params object[] args) => Runtime.Begin(messageTemplate, args);
        #endregion

        #endregion

        #region Properties
        protected GuiApp App => (GuiApp) Application.Instance;
        protected TreeItem Projects => (TreeItem)navigation.DataStore[1];
        #endregion

        #region Fields
        protected static readonly Icon TestIcon = Icon.FromResource("Stratis.DevEx.Gui.Images.TestIcon.ico");
        protected static readonly Icon JetbrainsRider = Icon.FromResource("Stratis.DevEx.Gui.Images.jetbrainsrider.png");
        protected static readonly Icon VisualStudio = Icon.FromResource("Stratis.DevEx.Gui.Images.visualstudio.png");
        protected static readonly Icon VSCode = Icon.FromResource("Stratis.DevEx.Gui.Images.vscode.png");
        protected static readonly Icon CSharp = Icon.FromResource("Stratis.DevEx.Gui.Images.csharp.png");
        protected static readonly Icon Globe = Icon.FromResource("Stratis.DevEx.Gui.Images.TestImage.png");
        #pragma warning disable CS0618 // Type or member is obsolete
		internal TreeView navigation;
		#pragma warning restore CS0618 // Type or member is obsolete
		
		internal WebView projectView;
        protected Splitter splitter;
        internal Dictionary<string, Dictionary<string, object>> projectViews;
        #endregion

        #region Event Handlers
        private void Navigation_NodeMouseClick(object? sender, TreeViewItemEventArgs e)
        {
            MessageBox.Show(this, "I was clicked!: " + e.Item.Key);
        }

        #endregion
    }
}

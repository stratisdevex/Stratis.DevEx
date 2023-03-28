using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            CreateMenuAndToolbar();
			#pragma warning disable CS0618 // Type or member is obsolete
			navigation = new TreeView()
            #pragma warning restore CS0618 // Type or member is obsolete
            {
                Size = new Size(100, 150)
            };
			navigation.DataStore = new TreeItem(
				new TreeItem() { Image = Globe, Text = "About", Key = "About" },
				new TreeItem() { Image = TestIcon, Text = "Projects", Key = "Projects"}
			);
            navigation.Activated += Navigation_NodeMouseClick;
            navigation.NodeMouseClick += Navigation_NodeMouseClick; ;
            projectView = new TabControl();
            projectViews = new Dictionary<string, object>()
            {
                {"Projects",  @"<html><head><title>Projects</title></head><body><h1>Projects</h1></body></html>"}, 
                {"About",  @"<html><head><title>About</title></head><body><h1>About</h1></body></html>"}
            };

            projectControlFlowView = new WebView();
            projectControlFlowViewPage = new TabPage(projectControlFlowView)
            {
                Text = "Control Flow",
            };
            projectCHAView = new WebView();
            projectCHAViewPage = new TabPage(projectCHAView)
            {
                Text = "Class Hierarchy"
            };
            projectDisassemblyView = new WebView();
            projectDisassemblyViewPage = new TabPage(projectDisassemblyView)
            {
                Text = "Disassembly"
            };
            projectSourceView = new WebView();
            projectSourceViewPage = new TabPage(projectSourceView)
            {
                Text = "Source"
            };

            
            projectView.Pages.Add(projectControlFlowViewPage);
            projectView.Pages.Add(projectCHAViewPage);
            projectView.Pages.Add(projectDisassemblyViewPage);
            projectView.Pages.Add(projectSourceViewPage);

            projectControlFlowView.LoadHtml(@"<html><head><title>Hello!</title></head><body><h1>Hi!</h1></body></html>");
            projectView.SelectedPage = projectControlFlowViewPage;

			splitter = new Splitter();
			splitter.Panel1 = navigation;
			splitter.Panel2 = projectView;
            splitter.Position = 200;
            Content = splitter;
        }

        private void ProjectView_DocumentLoaded(object? sender, WebViewLoadedEventArgs e)
        {
            Info("WebView loaded {url}.", e.Uri);
        }

        #endregion

        #region Methods
        public void CreateMenuAndToolbar()
        {
            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command { MenuText = "Quit", Shortcut = GuiApp.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => GuiApp.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            var refreshCommand = new Command((_, _) => App.AsyncInvoke(() => projectControlFlowView.Reload()))
            {
                MenuText = "Refresh",
                ToolBarText = "Refresh",
                Image = Refresh
            };
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
            ToolBar = new ToolBar { Items = { refreshCommand } };
        }

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
                App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[docid + "_" + "ControlFlow"]));
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
                App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[docid + "_" + "ControlFlow"])); 
                //projectView.sw
            }
            navigation.RefreshItem(projects);
        }

        public void AddProjectToTree(ControlFlowGraphMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var docid = projectid + "_" + m.Document;
            var cfg = Program.CreateGraph(m);
            var doc = new TreeItem()
            {
                Key = docid,
                Text = m.Document.Replace(projectDir + Path.DirectorySeparatorChar, ""),
                Image = CSharp
            };
            projectViews.Add(projectid, @"<html><head><title>Control Flow</title></head><body><h1>" + m.AssemblyName + "</h1></body></html>");
            projectViews.Add(docid + "_" + "ControlFlow", Html.DrawControlFlowGraph(cfg)); 
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
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var docid = projectid + "_" + m.Document;
            var project = (TreeItem)Projects.Children.First(c => c.Key == projectid);
            var cfg = Program.CreateGraph(m);
            projectViews[docid + "_" + "ControlFlow"] = Html.DrawControlFlowGraph(cfg);
            if (project.Children.Any(c => c.Key == docid))
            {
                Debug("Document {doc} exists in project {proj}, updating...", docid, projectid);
            }
            else
            {
                Debug("Document {doc} does not exist in project {proj}, adding...", docid, projectid);
                TreeItem doc = new TreeItem()
                {
                    Key = docid,
                    Text = m.Document.Replace(projectDir + Path.DirectorySeparatorChar, ""),
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

        #region Event Handlers
        private void Navigation_NodeMouseClick(object? sender, TreeViewItemEventArgs e)
        {
            switch (e.Item.Key)
            {
                case var x when x.EndsWith(".cs"):
                    App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[e.Item.Key + "_ControlFlow"]));
                    break;
                default:
                    App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[e.Item.Key]));
                    break;
            }
        }
        #endregion

        #region Fields
        protected static readonly Icon TestIcon = Icon.FromResource("Stratis.DevEx.Gui.Images.TestIcon.ico");
        protected static readonly Icon JetbrainsRider = Icon.FromResource("Stratis.DevEx.Gui.Images.jetbrainsrider.png");
        protected static readonly Icon VisualStudio = Icon.FromResource("Stratis.DevEx.Gui.Images.visualstudio.png");
        protected static readonly Icon VSCode = Icon.FromResource("Stratis.DevEx.Gui.Images.vscode.png");
        protected static readonly Icon CSharp = Icon.FromResource("Stratis.DevEx.Gui.Images.csharp.png");
        protected static readonly Icon Globe = Icon.FromResource("Stratis.DevEx.Gui.Images.TestImage.png");
        protected static readonly Icon Refresh = Icon.FromResource("Stratis.DevEx.Gui.Images.refresh.png");
        #pragma warning disable CS0618 // Type or member is obsolete
        internal TreeView navigation;
        #pragma warning restore CS0618 // Type or member is obsolete

        protected TabControl projectView;
        protected Splitter splitter;
        internal Dictionary<string, object> projectViews;

        protected WebView projectControlFlowView;
        protected TabPage projectControlFlowViewPage;
        protected WebView projectCHAView;
        protected TabPage projectCHAViewPage;
        protected WebView projectSourceView;
        protected TabPage projectSourceViewPage;
        protected WebView projectDisassemblyView;
        protected TabPage projectDisassemblyViewPage;
        #endregion
    }
}

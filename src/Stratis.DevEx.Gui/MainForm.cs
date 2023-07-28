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
using Stratis.DevEx.CodeAnalysis.IL;

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
            navigation.NodeMouseClick += Navigation_NodeMouseClick;
            aboutView = new WebView();
            aboutView.LoadHtml(@"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>");
            projectView = new TabControl();
            projectViews = new Dictionary<string, object>()
            {
                {"Projects",  @"<html><head><title>Projects</title></head><body><h1>Projects</h1></body></html>"}, 
                {"About",  @"<html><head><title>About</title></head><body><h1>About</h1></body></html>"}
            };
            projectSummaryView = new WebView();
            projectSummaryViewPage = new TabPage(projectSummaryView)
            {
                Text = "Summary"
            };
            projectControlFlowView = new WebView();
            projectControlFlowViewPage = new TabPage(projectControlFlowView)
            {
                Text = "Control Flow",
            };
            projectCallGraphView = new WebView();
            projectCallGraphViewPage = new TabPage(projectCallGraphView)
            {
                Text = "Call Graph",
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

            projectView.Pages.Add(projectSummaryViewPage);
            projectView.Pages.Add(projectControlFlowViewPage);
            projectView.Pages.Add(projectCallGraphViewPage);
            projectView.Pages.Add(projectDisassemblyViewPage);
            projectView.Pages.Add(projectSourceViewPage);

            projectControlFlowView.LoadHtml(@"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>");
            projectView.SelectedPage = projectSummaryViewPage;

			splitter = new Splitter();
			splitter.Panel1 = navigation;
			splitter.Panel2 = aboutView;
            splitter.Position = 200;
            Content = splitter;
        }
        #endregion

        #region Methods
        public void ReadMessage(CompilationMessage m)
        {
            if (m.EditorEntryAssembly == "(none)")
            {
                Info("Not processing message from unknown editor assembly for assembly {asm}.", m.AssemblyName);
                return;
            }
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var asm = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".dll");
            var pdb = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".pdb");
            File.WriteAllBytes(asm, m.Assembly);
            File.WriteAllBytes(pdb, m.Pdb);
            Info("Updated assembly {asm} at {now}.", asm, DateTime.Now);
            var projects = (TreeItem)navigation.DataStore[1];
            if (projects.Children.Any(c => c.Key == projectid))
            {
                Debug("Project {proj} already exists in tree, updating...", projectid);
                UpdateProjectDocsTree(m);
                App.AsyncInvoke(() => projectDisassemblyView.LoadHtml((string)projectViews[projectid + "_" + "Disassembly"]));
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
                App.AsyncInvoke(() => projectDisassemblyView.LoadHtml((string)projectViews[projectid + "_" + "Disassembly"]));
            }
        }

        public void ReadMessage(ControlFlowGraphMessage m)
        {
            if (m.EditorEntryAssembly == "(none)")
            {
                Info("Not processing message from unknown editor assembly for document {doc}.", m.Document);
                return;
            }
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
            }
            navigation.RefreshItem(projects);
        }

        public void ReadMessage(SummaryMessage m)
        {
            if (m.EditorEntryAssembly == "(none)")
            {
                Info("Not processing message from unknown editor assembly for document {doc}.", m.Document);
                return;
            }
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var docid = projectid + "_" + m.Document;
            //File.WriteAllText(projectDir.CombinePath(DateTime.Now.Millisecond.ToString() + ".html"), Html.DrawCallGraph(cg));
            var projects = (TreeItem)navigation.DataStore[1];
            if (projects.Children.Any(c => c.Key == projectid))
            {
                Debug("Project {proj} already exists in tree, updating...", projectid);
                UpdateProjectDocsTree(m);
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
            }
            App.AsyncInvoke(() => LoadProjectOrDocView(docid));
            navigation.RefreshItem(projects);
        }

        protected void AddProjectToTree(CompilationMessage m)
        {
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            projectViews.Add(projectid, @"<html><head><title>Summary</title></head><body><h1>" + m.AssemblyName + "</h1></body></html>");
            projectViews[projectid + "_" + "Disassembly"] = GetDisassembly(projectid, Array.Empty<string>());
            var project = new TreeItem()
            {
                Key = projectid,
                Text = m.AssemblyName,
                Image = m.EditorEntryAssembly switch
                {
                    var x when x.StartsWith("VBCSCompiler") => VisualStudio,
                    var x when x.StartsWith("OmniSharp") => VSCode,
                    var x when x.StartsWith("JetBrains.Roslyn.Worker") => JetbrainsRider,
                    _ => Globe
                },
            };

            foreach (var d in m.Documents)
            {
                var docid = projectid + "_" + d;
                var c = new TreeItem()
                {
                    Key = docid,
                    Text = d.Replace(projectDir + Path.DirectorySeparatorChar, ""),
                    Image = CSharp
                };
                project.Children.Add(c);
            }

            Projects.Children.Add(project);
        }

        protected void UpdateProjectDocsTree(CompilationMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var project = (TreeItem)Projects.Children.First(c => c.Key == projectid);
            projectViews[projectid + "_" + "Disassembly"] = Html.DrawDisassembly(GetDisassembly(projectid, Array.Empty<string>()));

            foreach (var d in m.Documents)
            {
                var docid = projectid + "_" + d;
                if (!project.Children.Any(c => c.Key == docid))
                {
                    var c = new TreeItem()
                    {
                        Key = docid,
                        Text = d.Replace(projectDir + Path.DirectorySeparatorChar, ""),
                        Image = CSharp
                    };
                    project.Children.Add(c);
                }
            }
            List<ITreeItem> toremove = new List<ITreeItem>();
            foreach (var c in project.Children)
            {
                if (!m.Documents.Any(d => d == projectDir + Path.DirectorySeparatorChar + c.Text))
                {
                    toremove.Add(c);
                }
            }
            foreach(var c in toremove)
            {
                project.Children.Remove(c);
            }
        }

        protected void AddProjectToTree(SummaryMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var docid = projectid + "_" + m.Document;
            var doc = new TreeItem()
            {
                Key = docid,
                Text = m.Document.Replace(projectDir + Path.DirectorySeparatorChar, ""),
                Image = CSharp
            };
            Graph cg = Program.CreateCallGraph(m.Implements, m.Invocations);
            projectViews.Add(projectid, @"<html><head><title>Summary</title></head><body><h1>" + m.AssemblyName + "</h1></body></html>");
            projectViews.Add(docid + "_" + "Summary", Html.DrawSummary(m.Summary));
            projectViews.Add(docid + "_" + "CallGraph", Html.DrawCallGraph(cg));
            projectViews[projectid + "_" + "Disassembly"] = GetDisassembly(projectid, m.ClassNames);
            var child = new TreeItem(doc)
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
            };
            Projects.Children.Add(child);
        }

        protected void UpdateProjectDocsTree(SummaryMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var docid = projectid + "_" + m.Document;
            var project = (TreeItem)Projects.Children.First(c => c.Key == projectid);
            Graph cg = Program.CreateCallGraph(m.Implements, m.Invocations);
            projectViews[docid + "_" + "Summary"] = Html.DrawSummary(m.Summary);
            projectViews[docid + "_" + "CallGraph"] = Html.DrawCallGraph(cg);
            projectViews[projectid + "_" + "Disassembly"] = GetDisassembly(projectid, m.ClassNames);
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
        protected void AddProjectToTree(ControlFlowGraphMessage m)
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
            var child = new TreeItem(doc)
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
            };
            Projects.Children.Add(child);
        }

        protected void UpdateProjectDocsTree(ControlFlowGraphMessage m)
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

        protected void showProjectView() => splitter.Panel2 = projectView;

        protected void showAboutView() => splitter.Panel2 = aboutView;

        protected string GetProjectOrDocViewHtml(string id, string component)
        {
            var key = id + "_" + component;
            if (projectViews.ContainsKey(key))
            {
                return (string)projectViews[key];
            }
            else
            {
                return @"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>";
            }
        }
        protected void LoadProjectOrDocView(string id)
        {
            projectSummaryView.LoadHtml(GetProjectOrDocViewHtml(id, "Summary"));
            projectControlFlowView.LoadHtml(GetProjectOrDocViewHtml(id, "ControlFlow"));
            projectCallGraphView.LoadHtml(GetProjectOrDocViewHtml(id, "CallGraph"));
            projectDisassemblyView.LoadHtml(GetProjectOrDocViewHtml(id, "Disassembly"));
            //projectView.SelectedPage = projectSummaryViewPage;
        }

        protected string GetDisassembly(string projectid, string[] classNames)
        {
            var asm = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".dll");
            var pdb = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".pdb");
            if (!File.Exists(asm) || !File.Exists(pdb))
            {
                Info("Could not find assembly file {asm}.", asm);
                return htmlplaceholder;
            }
            else if (!File.Exists(pdb))
            {
                Info("Could not find PDB file {pdb}.", pdb);
                return htmlplaceholder;
            }
            else
            {
                try
                {
                    var output = new CSharpSourceEmitter.SourceEmitterOutputString();
                    Disassembler.Run(asm, output);
                    return Html.DrawDisassembly(output.Data);
                }
                catch (Exception e)
                {
                    Error(e, "Exception thrown during disassembly of {asm}.", asm);
                    return htmlplaceholder;
                }
            }
        }

        protected void RefreshWebViews()
        {
            App.AsyncInvoke(() => projectSummaryView.Reload());
            App.AsyncInvoke(() => projectControlFlowView.Reload());
            App.AsyncInvoke(() => projectCallGraphView.Reload());
        }
        protected void CreateMenuAndToolbar()
        {
            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command { MenuText = "Quit", Shortcut = GuiApp.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => GuiApp.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            var refreshCommand = new Command((_, _) => RefreshWebViews())
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
                case "About":
                    showAboutView();
                    break;
                case "Projects":
                    showProjectView();
                    break;
                case var x when x.EndsWith(".cs"):
                    showProjectView();
                    App.AsyncInvoke(() => LoadProjectOrDocView(e.Item.Key));
                    break;
                default:
                    showProjectView();
                    App.AsyncInvoke(() => LoadProjectOrDocView(e.Item.Key));
                    break;
            }
        }

        private void ProjectView_DocumentLoaded(object? sender, WebViewLoadedEventArgs e)
        {
            Info("WebView loaded {url}.", e.Uri);
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

        protected const string htmlplaceholder = @"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>";
        #pragma warning disable CS0618 // Type or member is obsolete
        internal TreeView navigation;
        #pragma warning restore CS0618 // Type or member is obsolete

        protected WebView aboutView;
        protected TabControl projectView;
        protected Splitter splitter;
        internal Dictionary<string, object> projectViews;

        protected WebView projectControlFlowView;
        protected TabPage projectControlFlowViewPage;
        protected WebView projectCallGraphView;
        protected TabPage projectCallGraphViewPage;
        protected WebView projectSummaryView;
        protected TabPage projectSummaryViewPage;
        protected WebView projectSourceView;
        protected TabPage projectSourceViewPage;
        protected WebView projectDisassemblyView;
        protected TabPage projectDisassemblyViewPage;
        #endregion
    }
}

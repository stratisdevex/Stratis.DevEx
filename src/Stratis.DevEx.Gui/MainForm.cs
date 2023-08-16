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
using Microsoft.Cci.Ast;

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
				new TreeItem() { Image = TestIcon, Text = "Projects", Key = "Projects"},
                new TreeItem() { Image = BlockchainNode, Text = "Nodes", Key = "Nodes" }
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
            projectSummaryView.BrowserContextMenuEnabled = true;
            projectSummaryViewPage = new TabPage(projectSummaryView)
            {
                Text = "Summary"
            };
            projectControlFlowView = new WebView();
            projectControlFlowView.BrowserContextMenuEnabled = true;
            projectControlFlowViewPage = new TabPage(projectControlFlowView)
            {
                Text = "Control Flow",
            };
            projectCallGraphView = new WebView();
            projectCallGraphView.BrowserContextMenuEnabled = true;
            projectCallGraphViewPage = new TabPage(projectCallGraphView)
            {
                Text = "Call Graph",
            };
            projectDisassemblyView = new WebView();
            projectDisassemblyView.BrowserContextMenuEnabled = true;
            projectDisassemblyViewPage = new TabPage(projectDisassemblyView)
            {
                Text = "Disassembly"
            };
            
            projectView.Pages.Add(projectSummaryViewPage);
            projectView.Pages.Add(projectControlFlowViewPage);
            projectView.Pages.Add(projectCallGraphViewPage);
            projectView.Pages.Add(projectDisassemblyViewPage);
            
            projectControlFlowView.LoadHtml(@"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>");
            projectView.SelectedPage = projectSummaryViewPage;

            nodeView = new TabControl();
            this.NodesActivated += MainForm_NodesActivated;
			splitter = new Splitter();
			splitter.Panel1 = navigation;
			splitter.Panel2 = aboutView;
            splitter.Position = 200;
            Content = splitter;

            uITimer = new UITimer(UpdateUIProjectElements); ;
            uITimer.Interval = 0.5;
            uITimer.Start();
        }

        
        #endregion

        #region Methods
        protected void UpdateUIProjectElements(object? sender, EventArgs e)
        {
            if (Program.SummaryMessages.Any())
            {
                ReadMessage(Program.SummaryMessages.Pop());
                Program.SummaryMessages.Clear();
            }
            if (Program.ControlFlowGraphMessages.Any())
            {
                ReadMessage(Program.ControlFlowGraphMessages.Pop());
                Program.ControlFlowGraphMessages.Clear();
            }
            if (Program.CompilationMessages.Any())
            {
                ReadMessage(Program.CompilationMessages.Pop());
                Program.CompilationMessages.Clear();
            }
        }
        
        public void ReadMessage(CompilationMessage m)
        {
            if (lastCompilationMessageIdRead == m.CompilationId)
            {
                Debug("Alreading read compilation message for compilation id {id}, skipping.", m.CompilationId);
                return;
            }
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
            Debug("Updated assembly {asm} at {now}.", asm, DateTime.Now);
            var projects = (TreeItem)navigation.DataStore[1];
            if (projects.Children.Any(c => c.Key == projectid))
            {
                Debug("Project {proj} already exists in tree, updating...", projectid);
                UpdateProjectDocsTree(m);
                App.AsyncInvoke(() => projectDisassemblyView.LoadHtml(GetProjectOrDocViewHtml(projectid, "Disassembly")));
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
                App.AsyncInvoke(() => projectDisassemblyView.LoadHtml(GetProjectOrDocViewHtml(projectid, "Disassembly")));
            }
            navigation.RefreshItem(projects);
            lastCompilationMessageIdRead = m.CompilationId;
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
            }
            else
            {
                Debug("Project {proj} does not exists in tree, adding...", projectid);
                AddProjectToTree(m);
            }
            navigation.RefreshItem(projects);
            App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[docid + "_" + "ControlFlow"]));
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
            navigation.RefreshItem(projects);
            App.AsyncInvoke(() => LoadProjectOrDocView(docid));
        }

        protected void AddProjectToTree(CompilationMessage m)
        {
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            projectViews.Add(projectid, @"<html><head><title>Summary</title></head><body><h1>" + m.AssemblyName + "</h1></body></html>");
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

            /*
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
            */
            Projects.Children.Add(project);
            var dis = GetDisassembly(projectid, Array.Empty<string>());
            if (dis is not null && !string.IsNullOrEmpty(dis.Data))
            {
                disassembly[projectid] = dis;
                projectViews[projectid + "_" + "Disassembly"] = Html.DrawDisassembly(dis.Data);
                Info("Set disassembly for project {id} to HTML string of length {len} bytes.", projectid, dis.Data.Length);
            }
            else
            {
                disassembly[projectid] = null;
                Info("Disassembly for project {id} failed.", projectid);
            }
        }

        protected void UpdateProjectDocsTree(CompilationMessage m)
        {
            var projectid = m.EditorEntryAssembly + "_" + m.AssemblyName;
            var projectDir = Path.GetDirectoryName(m.ConfigFile)!;
            var project = (TreeItem)Projects.Children.First(c => c.Key == projectid);

            var dis = GetDisassembly(projectid, Array.Empty<string>());
            if (dis is not null && !string.IsNullOrEmpty(dis.Data))
            {
                disassembly[projectid] = dis;
                projectViews[projectid + "_" + "Disassembly"] = Html.DrawDisassembly(dis.Data);
                Info("Set disassembly for project {id} to HTML string of length {len} bytes.", projectid, dis.Data.Length);
            }
            else
            {
                disassembly[projectid] = null;
                Info("Disassembly for project {id} failed.", projectid);
            }
            /*
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
            */
            List<ITreeItem> toremove = new List<ITreeItem>();
            foreach (var c in project.Children)
            {
                if (!m.Documents.Any(d => d == projectDir + Path.DirectorySeparatorChar + c.Text))
                {
                    toremove.Add(c);
                }
            }
            foreach (var c in toremove)
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
            if (disassembly.ContainsKey(projectid) && disassembly[projectid] is not null)
            {
                string cdishtml = "";
                foreach (var c in m.ClassNames)
                {
                    if (disassembly[projectid]!.classOutput.ContainsKey(c))
                    {
                        cdishtml = cdishtml + Html.DrawDisassembly(disassembly[projectid]!.classOutput[c].ToString() + Environment.NewLine);
                    }
                }
                projectViews[docid + "_" + "Disassembly"] = cdishtml;
                Info("Set disassembly for document {id} to HTML string of length {len} bytes.", docid, cdishtml.Length);
            }
            else
            {
                Error("Disassembly of document {id} failed.", docid);
            }

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
            if (disassembly.ContainsKey(projectid) && disassembly[projectid] is not null)
            {
                string cdishtml = "";
                foreach (var c in m.ClassNames)
                {
                    if (disassembly[projectid]!.classOutput.ContainsKey(c))
                    {
                        cdishtml = cdishtml + Html.DrawDisassembly(disassembly[projectid]!.classOutput[c].ToString() + Environment.NewLine);
                    }
                }
                projectViews[docid + "_" + "Disassembly"] = cdishtml;
                Info("Set disassembly for document {id} to HTML string of length {len} bytes.", docid, cdishtml.Length);
            }
            else
            {
                Error("Disassembly of document {id} failed.", docid);
            }

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
            var doc = new TreeItem()
            {
                Key = docid,
                Text = m.Document.Replace(projectDir + Path.DirectorySeparatorChar, ""),
                Image = CSharp
            };
            projectViews.Add(projectid, @"<html><head><title>Control Flow</title></head><body><h1>" + m.AssemblyName + "</h1></body></html>");
            var cfg = Program.CreateGraph(m);
            var html = Html.DrawControlFlowGraph(cfg);
            if (!string.IsNullOrEmpty(html))
            {
                projectViews[docid + "_" + "ControlFlow"] = html;
                Info("Set control-flow graph for document {id} to HTML string of length {len} bytes.", docid, html.Length);
            }
            else
            {
                Error("Failed to create control-flow graph for document {doc}.", docid);
            }
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
            var html = Html.DrawControlFlowGraph(cfg);
            if (!string.IsNullOrEmpty(html))
            {
                projectViews[docid + "_" + "ControlFlow"] = html;
                Info("Set control-flow graph for document {id} to HTML string of length {len} bytes.", docid, html.Length);
            }
            else
            {
                Error("Failed to create control-flow graph for document {doc}.", docid);
            }
            
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
            //App.AsyncInvoke(() => projectControlFlowView.LoadHtml((string)projectViews[docid + "_" + "ControlFlow"]));
        }

        protected void showProjectView() => splitter.Panel2 = projectView;

        protected void showAboutView() => splitter.Panel2 = aboutView;

        protected string GetProjectOrDocViewHtml(string id, string component)
        {
            var key = id + "_" + component;
            if (projectViews.ContainsKey(key))
            {
                var v = (string)projectViews[key];
                Info("Key {key} in projectViews dictionary has length {len}.", key, v.Length);
                return v;
            }
            else
            {
                Error("Key {key} is not present in projectViews dictionary.", key);
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

        protected SmartContractSourceEmitterOutput? GetDisassembly(string projectid, string[] classNames)
        {
            var asm = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".dll");
            var pdb = Path.Combine(Program.assemblyCacheDir.FullName, projectid + ".pdb");
            if (!File.Exists(asm) || !File.Exists(pdb))
            {
                Debug("Could not find assembly file {asm}.", asm);
                return null;
            }
            else if (!File.Exists(pdb))
            {
                Debug("Could not find PDB file {pdb}.", pdb);
                return null;
            }
            else
            {
                try
                {
                    var output = new SmartContractSourceEmitterOutput();
                    Disassembler.Run(asm, output);
                    return output;
                }
                catch (Exception e)
                {
                    Error(e, "Exception thrown during disassembly of {asm}.", asm);
                    return null;
                }
            }
        }

        protected void RefreshWebViews()
        {
            if (projectView.SelectedPage == projectSummaryViewPage)
            {
                App.AsyncInvoke(() => projectSummaryView.Reload());
            }
            else if (projectView.SelectedPage == projectControlFlowViewPage)
            {
                App.AsyncInvoke(() => projectControlFlowView.Reload());
            }
            else if (projectView.SelectedPage == projectCallGraphViewPage)
            {
                App.AsyncInvoke(() => projectCallGraphView.Reload());
            }
            else if (projectView.SelectedPage == projectDisassemblyViewPage)
            {
                App.AsyncInvoke(() => projectDisassemblyView.Reload());
            }
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

        #region Events
        public event EventHandler<TreeViewItemEventArgs> NodesActivated;
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
                case "Nodes":
                    NodesActivated?.Invoke(sender, e);
                    break;
                default:
                    showProjectView();
                    App.AsyncInvoke(() => LoadProjectOrDocView(e.Item.Key));
                    break;
            }
        }

        private void MainForm_NodesActivated(object? sender, TreeViewItemEventArgs e)
        {
            throw new NotImplementedException();
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
        protected static readonly Icon Cirrus = Icon.FromResource("Stratis.DevEx.Gui.Images.cirrus.png");
        protected static readonly Icon BlockchainNode = Icon.FromResource("Stratis.DevEx.Gui.Images.blockchainnode.png");

        protected const string htmlplaceholder = @"<html><head><title>Hello!</title></head><body><div style='align:centre'><h1>Stratis DevEx</h1><img src='https://avatars.githubusercontent.com/u/122446986?s=200&v=4'/></div></body></html>";
        #pragma warning disable CS0618 // Type or member is obsolete
        internal TreeView navigation;
        #pragma warning restore CS0618 // Type or member is obsolete

        protected UITimer uITimer;
        protected Splitter splitter;
        protected WebView aboutView;
        protected TabControl projectView;
        protected TabControl nodeView;
      
        protected WebView projectControlFlowView;
        protected TabPage projectControlFlowViewPage;
        protected WebView projectCallGraphView;
        protected TabPage projectCallGraphViewPage;
        protected WebView projectSummaryView;
        protected TabPage projectSummaryViewPage;
        protected WebView projectDisassemblyView;
        protected TabPage projectDisassemblyViewPage;

        internal Dictionary<string, object> projectViews;
        protected Dictionary<string, DateTime> projectControlFlowViewLastUpdated = new Dictionary<string, DateTime>();

        protected Dictionary<string, SmartContractSourceEmitterOutput?> disassembly = new Dictionary<string, SmartContractSourceEmitterOutput?>();
        protected long lastCompilationMessageIdRead = 0; 
        #endregion
    }
}

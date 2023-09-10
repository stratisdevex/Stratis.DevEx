using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Eto.Forms;
using SharpConfig;
using Cobra.Api.Node.Cirrus;
using Cobra.Api.Node.Cirrus.Models;
using System.Globalization;
//using System.Windows.Forms;

namespace Stratis.DevEx.Gui
{
    public class Nodes : Runtime
    {
        #region Types
        public class Node 
        {
            #region Properties
            [Category("Data")]
            public string name;
            public Uri url;
            public bool isdefault;
            public string? folder;
            public Client client;
            #endregion
            public Node(string name, Uri uri, bool isdefault = false, string? folder = null)
            {
                this.name = name;
                this.url = uri;
                this.isdefault = isdefault;
                this.folder = folder;
                this.client = new Client(this.url.ToString(), httpClient);
            }
        }

       
        #endregion

        #region Properties
        public static GuiApp App => (GuiApp) Application.Instance;

        public static MainForm MainForm => (MainForm)App.MainForm;

        public static Configuration Config { get; protected set; } = new Configuration();

        public static List<Node> GuiNodes { get; } = new List<Node>(); 

        public static string[] GuiFolders() => GuiNodes.Where(n => n.folder is not null).Select(n => n.folder!).ToArray();
        #endregion

        #region Methods
        public static void InitMainForm(MainForm mainForm)
        {
            LoadConfig();
            foreach (var node in GuiNodes) 
            {
                mainForm.nodesTreeItem.Children.Add(new TreeItem() { Image = Icons.Cirrus, Text = node.name, Key = node.name + "_Node"});
            }

            mainForm.nodeView.Pages.Add(mainForm.nodeSummary);
            mainForm.navigation.RefreshItem(mainForm.nodesTreeItem);
            mainForm.NodesMouseClick += MainForm_NodesMouseClick;
        }

        public static void LoadConfig()
        {
            var cfgfile = Path.Combine(StratisDevDir, "Stratis.DevEx.Gui.Nodes.cfg");
            if (!File.Exists(cfgfile)) 
            {
                Info("GUI nodes configuration file {file} does not exist, creating...", cfgfile);
                Config = new Configuration();
                Config.SaveToFile(cfgfile);
            }
            else
            {
                Info("GUI nodes configuration file {file} exists, loading...", cfgfile);
                Config = Configuration.LoadFromFile(cfgfile);
                foreach (var s in Config)
                {
                    if (s.GetSettingsNamed("Url") is not null && s.GetSettingsNamed("Url").Any())
                    {
                        
                        var urls = s.GetSettingsNamed("Url").First().StringValue;
                        if (Uri.TryCreate(urls, UriKind.Absolute, out Uri? url))
                        {
                            var n = new Node(s.Name, url);
                            if (s.GetSettingsNamed("Folder") is not null && s.GetSettingsNamed("Folder").Any())
                            {
                                n.folder = s.GetSettingsNamed("Folder").First().StringValue;
                            }
                            if (s.GetSettingsNamed("Default") is not null && s.GetSettingsNamed("Default").Any())
                            {
                                n.isdefault = s.GetSettingsNamed("Default").First().BoolValue;
                            }
                            GuiNodes.Add(n);
                        }
                        else
                        {
                            Error("Could not parse url string {u}. Skipping node config entry {n}.", urls, s.Name);
                            continue;
                        }

                    }
                }
            }
        }

        public static async Task UpdateNodeStatus(string name)
        {
            try 
            {
                var node = GuiNodes.Single(n => n.name == name);
                var c = await node.client.StatusAsync(false);
                var status = c.Result;
                if (status is null) return;
                nodeStore[node] = status;
            }
            catch (Exception e) 
            {
                Error(e, "Error retrieving status for node {n}.", name);
            }
        }

        public static void ShowNodeView(Node? node)
        {
            if (node is not null)
            {
                MainForm.nodeSummaryPropertyGrid.SelectedObject = nodeStore[node];
                //MainForm.Bindings.Remove(MainForm.Bindings.First(b => b.n))
            }
            MainForm.splitter.Panel2 = MainForm.nodeView;
        }
        #endregion

        #region Event Handlers
        private static async void MainForm_NodesMouseClick(object? sender, TreeViewItemEventArgs e)
        {
            switch (e.Item.Key)
            {
                case var _n when _n.EndsWith("_Node"):
                    var name = _n.Replace("_Node", "");
                    await UpdateNodeStatus(name);
                    App.Invoke(()=>ShowNodeView(GuiNodes.Single(n => n.name == name))); 
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Fields
        internal static HttpClient httpClient = new HttpClient();
        internal static PropertyStore nodeStore = new PropertyStore();
        #endregion
    }
}

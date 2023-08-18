using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto.Forms;
using SharpConfig;

namespace Stratis.DevEx.Gui
{
    public class Nodes : Runtime
    {
        #region Types
        public struct Node 
        {
            public string name;
            public Uri url;
            public bool isdefault;
            public string? folder;
            public Node(string name, Uri uri, bool isdefault, string? folder = null)
            {
                this.name = name;
                this.url = uri;
                this.isdefault = isdefault;
                this.folder = folder;
            }
        }
        #endregion

        #region Properties
        public static Configuration Config { get; protected set; } = new Configuration();

        public static List<Node> ConfigNodes { get; } = new List<Node>(); 
        #endregion

        #region Methods
        public static void InitMainForm(MainForm mainForm)
        {
            LoadConfig();
            mainForm.NodesActivated += MainForm_NodesActivated;
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
                        var n = new Node();
                        var urls = s.GetSettingsNamed("Url").First().StringValue;
                        if (Uri.TryCreate(urls, UriKind.Absolute, out Uri? url))
                        {
                            n.name = s.Name;
                            n.url = url;
                            if (s.GetSettingsNamed("Folder") is not null && s.GetSettingsNamed("Folder").Any())
                            {
                                n.folder = s.GetSettingsNamed("Folder").First().StringValue;
                            }
                            if (s.GetSettingsNamed("Default") is not null && s.GetSettingsNamed("Default").Any())
                            {
                                n.isdefault = s.GetSettingsNamed("Default").First().BoolValue;
                            }
                            ConfigNodes.Add(n);
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

        #endregion

        #region Event Handlers
        private static void MainForm_NodesActivated(object? sender, TreeViewItemEventArgs e)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Net.Http;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using Stratis.DevEx;
using Stratis.DevEx.Ethereum.Explorers;

namespace Stratis.VS.StratisEVM.UI.ViewModel
{
    public enum BlockchainInfoKind
    {
        Folder,
        UserFolder,
        Network,
        Endpoint,
        Account,
        Contract
    }
    
    public class BlockchainInfo
    {
        #region Constructors
        public BlockchainInfo(BlockchainInfoKind kind, string name, BlockchainInfo parent = null, Dictionary<string,object> data = null)
        {
            Kind = kind;
            Name = name;
            Parent = parent;
            Data = data;
        }
        #endregion

        #region Properties
        public BlockchainInfoKind Kind { get; set; }
        
        public string Name { get; set; }
        
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
        
        [JsonProperty(ItemIsReference = true)]
        public BlockchainInfo Parent { get; set; }

        [JsonProperty(ItemReferenceLoopHandling = ReferenceLoopHandling.Serialize)] 
        public ObservableCollection<BlockchainInfo> Children = new ObservableCollection<BlockchainInfo>();

        public string Key => ((this.Parent?.Key) ?? "Root") + "_" + this.Kind + "_" + this.Name;

        [JsonIgnore]
        public string ToolTip
        {
            get
            {
                switch (Kind)
                {
                    case BlockchainInfoKind.Network:
                        return string.Format("Chain id: {0}", Data["ChainId"]);
                    default:
                        return "";
                }
            }
        }
        #endregion

        #region Methods
        public BlockchainInfo AddChild(BlockchainInfoKind kind, string name, Dictionary<string, object> data = null)
        {
            var info = new BlockchainInfo(kind, name, this, data);
            Children.Add(info);
            return info;
        }

        public BlockchainInfo AddNetwork(string name, BigInteger chainid, string uri)
        {
            var data = new Dictionary<string, object>()
            {
                {"ChainId", chainid },
                {"EndpointUri", uri }
            };
            return AddChild(BlockchainInfoKind.Network, name, data);    
        }

        public override int GetHashCode() => Key.GetHashCode();

        public override bool Equals(object obj) => obj is BlockchainInfo bi ? Key == bi.Key : false; 
            
        public void DeleteChild(BlockchainInfo child) => Children.Remove(child);

        public void DeleteChild(string name, BlockchainInfoKind kind) => Children.Remove(GetChild(name, kind));

        public bool HasChild(string name, BlockchainInfoKind kind) => Children.Count(bi => bi.Name == name && bi.Kind == kind) > 0;
        
        public BlockchainInfo GetChild(string name, BlockchainInfoKind kind) => Children.Single(c =>  c.Name == name && c.Kind == kind);

        public IEnumerable<BlockchainInfo> GetChildren(BlockchainInfoKind kind) => Children.Where(c => c.Kind == kind);

        public IEnumerable<BlockchainInfo> GetEndPoints() => GetChildren(BlockchainInfoKind.Endpoint);

        public bool Save(string path, out Exception e)
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings()
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,   
                    
                });
#if !IS_VSIX
                File.WriteAllText(Path.Combine(Runtime.AssemblyLocation, path + ".json"), json);
#else
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                VSUtil.SaveUserSettings(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider, path, json);
#endif

                e = null;
                return true;
            }
            catch (Exception ex) 
            {
                e = ex;
                return false;   
            }
        }

        public static BlockchainInfo Load(string path, out Exception e)
        {
            void FixParents(BlockchainInfo bi, BlockchainInfo p = null) 
            {
                if (bi == null) return;
                bi.Parent = p;
                foreach (var c in bi.Children)
                {
                    FixParents(c, bi);
                }                
            }

            try
            {
#if !IS_VSIX
                if (!File.Exists(Path.Combine(Runtime.AssemblyLocation, path + ".json")))
                {
                    e = null;
                    return null;
                }
                var b = JsonConvert.DeserializeObject<BlockchainInfo>(File.ReadAllText(Path.Combine(Runtime.AssemblyLocation, path + ".json")),
                    new JsonSerializerSettings()
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    });
                FixParents(b);
                e = null;
                return b;
#else
                Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
                var json = VSUtil.LoadUserSettings(Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider, path, "");
                var b = JsonConvert.DeserializeObject<BlockchainInfo>(json,
                    new JsonSerializerSettings()
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                    });
                FixParents(b);
                e = null;
                return b;
#endif   
            }
            catch (Exception ex) 
            {
                e = ex;
                return null;   
            }
        }
        #endregion
    }

    public class BlockchainViewModel : INotifyPropertyChanged
    {
        #region Constructors
        public BlockchainViewModel()
        {
            Objects = LoadTreeData();
        }
        #endregion

        #region Fields
        internal ObservableCollection<BlockchainInfo> objects;
        #endregion

        #region Properties
        public ObservableCollection<BlockchainInfo> Objects
        {
            get => objects;
            set
            {
                if (objects != null)
                {
                    objects.CollectionChanged -= OnRootCollectionChanged;
                }

                objects = value;
                if (value != null)
                {
                    value.CollectionChanged += OnRootCollectionChanged;
                }

                RaisePropertyChangedEvent("Objects");
            }
        }

        public string AssemblyLocation => Runtime.AssemblyLocation;

        public static BlockscoutClient BlockscoutClient { get; } = new BlockscoutClient(new HttpClient());  

        #endregion

        #region Methods
        private void OnRootCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
            RaisePropertyChangedEvent("Objects");

        /// <summary>
        /// Fires the <see cref="PropertyChanged"/> event for a
        /// given property.
        /// </summary>
        /// <param name="propertyName">The changed property.</param>
        private void RaisePropertyChangedEvent(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public BlockchainInfo TryFindObjectByName(string name, BlockchainInfo parent = null)
        {
            ObservableCollection<BlockchainInfo> objs;
            objs = parent is null ? objects : parent.Children;
            foreach (BlockchainInfo bi in objs)
            {
                if (bi.Name == name)
                {
                    return bi;
                }
                else
                {
                    var b = TryFindObjectByName(name, bi);
                    if (b != null) return b;
                }
            }

            return null;
        }

        public static ObservableCollection<BlockchainInfo> CreateInitialTreeData()
        {
            var data = new ObservableCollection<BlockchainInfo>();
            var root = new BlockchainInfo(BlockchainInfoKind.Folder, "EVM Networks");
            var mainnet = root.AddNetwork("Stratis Mainnet", 50505, "https://rpc.stratisevm.com:8545");
            var endpoints = mainnet.AddChild(BlockchainInfoKind.Folder, "Endpoints");
            endpoints.AddChild(BlockchainInfoKind.Endpoint, "https://rpc.stratisevm.com:8545");
            data.Add(root); 
            //var testnet = new BlockchainInfo(BlockchainInfoKind.Network, "Stratis Testnet");
            //mainnet.AddChild(BlockchainInfoKind.Endpoint, "auroria.stratisevm.com", new Uri("https://auroria.rpc.stratisevm.com"));
            //data.Add(mainnet);
            //data.Add(testnet);
            return data;
        }

        public static ObservableCollection<BlockchainInfo> LoadTreeData()
        {
            var b = BlockchainInfo.Load("BlockchainExplorerTree", out var e);
            if (b == null)
            {
                return CreateInitialTreeData();
            }
            else
            {
                return new ObservableCollection<BlockchainInfo> { b };
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Refreshes the data.
        /// </summary>


        ///<summary>
        ///Occurs when a property value changes.
        ///</summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion
    }

    /*
    public class BlockchainInfoReferenceResolver : IReferenceResolver
    {
        public BlockchainInfoReferenceResolver() { }

        protected Dictionary<string, BlockchainInfo> references = new Dictionary<string, BlockchainInfo>();

        public void AddReference(object context, string reference, object value)
        {
            if (value is BlockchainInfo bi)
            {
                references.Add(reference, value);
            }
        }

    }
    */
}

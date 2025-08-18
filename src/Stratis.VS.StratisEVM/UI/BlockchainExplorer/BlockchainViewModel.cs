using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stratis.DevEx;
using Stratis.DevEx.Ethereum;
using Stratis.DevEx.Ethereum.Explorers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Stratis.DevEx.Result;

namespace Stratis.VS.StratisEVM.UI.ViewModel
{
    public enum BlockchainInfoKind
    {
        Folder,
        UserFolder,
        Network,
        Endpoint,
        Account,
        Contract,
        DeployProfile
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
                    case BlockchainInfoKind.Account:
                        return Name;
                    default:
                        return "";
                }
            }
        }

        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                switch (Kind)
                {
                    case BlockchainInfoKind.Account:
                        return (Data.ContainsKey("Label") && !string.IsNullOrEmpty((string) Data["Label"]))? (string)Data["Label"] :
                        Name.Substring(0, 6) + "..." + new string(Name.Reverse().Take(6).Reverse().ToArray());
                    default:
                        return Name;
                }
            }
        }

        [JsonIgnore]
        public string NetworkChainId => Kind == BlockchainInfoKind.Network && Data.ContainsKey("ChainId") ? Data["ChainId"].ToString() : "";
        #endregion

        [JsonIgnore]
        public string DeployProfileEndpoint => Kind == BlockchainInfoKind.DeployProfile && Data.ContainsKey("Endpoint") ? (string) Data["Endpoint"] : "";

        [JsonIgnore]
        public string DeployProfileAccount => Kind == BlockchainInfoKind.DeployProfile && Data.ContainsKey("Account") ? (string)Data["Account"] : "";

        #region Methods
        public BlockchainInfo AddChild(string name, BlockchainInfoKind kind, Dictionary<string, object> data = null)
        {
            var info = new BlockchainInfo(kind, name, this, data);
            Children.Add(info);
            return info;
        }

        public BlockchainInfo AddNetwork(string name, string uri, BigInteger chainid, string nid)
        {
            var data = new Dictionary<string, object>()
            {
                {"EndpointUri", uri},
                {"ChainId", chainid},
                {"NetworkId", nid }
            };
            var network =  AddChild(name, BlockchainInfoKind.Network, data);
            network.AddChild("Endpoints", BlockchainInfoKind.Folder);
            network.AddChild("Accounts", BlockchainInfoKind.Folder);
            network.AddChild("Contracts", BlockchainInfoKind.Folder);
            network.AddChild("Deploy Profiles", BlockchainInfoKind.Folder);
            return network;
        }

        public BlockchainInfo AddAccount(string pubkey, string label = null)
        {
            var data = new Dictionary<string, object>()
            {
                {"Label",  label}
            };
            return AddChild(pubkey, BlockchainInfoKind.Account,  data);  
        }

        public BlockchainInfo AddDeployProfile(string name, string endpoint, string account, string pkey = null)
        {
            var data = new Dictionary<string, object>()
            {
                {"Endpoint",  endpoint},
                {"Account",  account},
            };
            if (!string.IsNullOrEmpty(pkey))
            {
                data["PrivateKey"] = SetDeployProfilePrivateKey(pkey);
            }
            return AddChild(name, BlockchainInfoKind.DeployProfile, data);
        }

        public BlockchainInfo AddContract(string address, BlockchainInfo deployProfile, string project, string solidityFile, string abi, string transactionHash, DateTime deployedOn, string label = null)
        {
            var data = new Dictionary<string, object>()
            {
                {"DeployProfile",  deployProfile.Name},
                {"Creator",  deployProfile.Data["Account"]},
                {"Project",  project},
                {"SolidityFile",  solidityFile},
                {"Abi",  abi},
                {"TransactionHash",  transactionHash},
                {"DeployedOn",  deployedOn.ToString("g")},
            };
            if (!string.IsNullOrEmpty(label))
            {
                data["Label"] = label;
            }
            return AddChild(address, BlockchainInfoKind.Contract, data);
        }

        public override int GetHashCode() => Key.GetHashCode();

        public override bool Equals(object obj) => obj is BlockchainInfo bi ? Key == bi.Key : false; 
            
        public void DeleteChild(BlockchainInfo child) => Children.Remove(child);

        public void DeleteChild(string name, BlockchainInfoKind kind) => Children.Remove(GetChild(name, kind));

        public bool HasChild(string name, BlockchainInfoKind kind) => Children.Count(bi => bi.Name == name && bi.Kind == kind) > 0;
        
        public BlockchainInfo GetChild(string name, BlockchainInfoKind kind) => Children.Single(c =>  c.Name == name && c.Kind == kind);

        public IEnumerable<BlockchainInfo> GetChildren(BlockchainInfoKind kind) => Children.Where(c => c.Kind == kind);

        public IEnumerable<string> GetNetworkEndPoints() => GetChild("Endpoints", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.Endpoint).Select(bi => bi.Name);

        public IEnumerable<string> GetNetworkAccounts() => GetChild("Accounts", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.Account).Select(bi => bi.Name);

        public IEnumerable<string> GetNetworkDeployProfiles() => GetChild("Deploy Profiles", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.DeployProfile).Select(bi => bi.Name);

       
        /*
        public IEnumerable<(string,long, string)> GetAllDeployProfiles()
        {
            return
            GetChildren(BlockchainInfoKind.Network)
            .SelectMany(bi => bi.GetNetworkDeployProfiles().Select(b => (bi.Name, (long) bi.Data["ChainId"], b )))
            .Concat(
                GetChildren(BlockchainInfoKind.UserFolder)
                .SelectMany(f => f.GetChildren(BlockchainInfoKind.Network))
                .SelectMany(ni => ni.GetNetworkDeployProfiles().Select(b => (ni.Parent.Name + "\\" + ni.Name, (long)ni.Data["ChainId"], b)))
            );
        }
        */
        public Dictionary<string, BlockchainInfo> GetAllDeployProfiles()
        {
            return GetChildren(BlockchainInfoKind.UserFolder)
                .SelectMany(f => f.GetChildren(BlockchainInfoKind.Network))
                .SelectMany(ni => ni.GetChild("Deploy Profiles", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.DeployProfile)
                .Select(b => (ni.Parent.Name + "\\" + ni.Name + "(" + (long)ni.Data["ChainId"] + ")", b)))
                .Concat(
                    GetChildren(BlockchainInfoKind.Network)
                    .SelectMany(bi => bi.GetChild("Deploy Profiles", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.DeployProfile))
                    .Select(bi => (bi.Name + "(" + (bi.Parent.Parent.Data["ChainId"].ToString()) + ")", bi))
                ).ToDictionary(b => b.Item1, b => b.Item2);
            
        }

        public Dictionary<string, BlockchainInfo> GetAllContracts()
        {
            return GetChildren(BlockchainInfoKind.UserFolder)
                .SelectMany(f => f.GetChildren(BlockchainInfoKind.Network))
                .SelectMany(ni => ni.GetChild("Contracts", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.Contract)
                .Select(b => (ni.Parent.Name + "\\" + ni.Name + "(" + (long)ni.Data["ChainId"] + ")", b)))
                .Concat(
                    GetChildren(BlockchainInfoKind.Network)
                    .SelectMany(bi => bi.GetChild("Contracts", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.Contract))
                    .Select(bi => (bi.Name + "(" + (long)bi.Parent.Parent.Data["ChainId"] + ")", bi))
                ).ToDictionary(b => b.Item1, b => b.Item2);
        }

        public BlockchainInfo GetDeployProfile(string name)
        {
            var d = GetAllDeployProfiles();
            return d.ContainsKey(name) ? d[name] : null;
        }

        public BlockchainInfo GetNetworkDeployProfile(string name) => GetChild("Deploy Profiles", BlockchainInfoKind.Folder).GetChildren(BlockchainInfoKind.DeployProfile).SingleOrDefault(bi => bi.Name == name);
            
        
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
                VSUtil.SaveUserSettings(StratisEVMPackage.Instance, path, json);
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
                var json = VSUtil.LoadUserSettings(StratisEVMPackage.Instance, path, "");
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

        public string GetDeployProfilePrivateKey()
        {
            var data = (byte[])Data["PrivateKey"];
            var length = BitConverter.ToInt32(data, 0);
            var kdata = data.Skip(4).ToArray();
            ProtectedMemory.Unprotect(kdata, MemoryProtectionScope.SameLogon);
            return Encoding.UTF8.GetString(kdata.Take(length).ToArray());  
        }

        public byte[] SetDeployProfilePrivateKey(string pkey)
        {
            int roundUp(int numToRound, int multiple)
            {
                if (multiple == 0)
                    return numToRound;

                int remainder = numToRound % multiple;
                if (remainder == 0)
                    return numToRound;

                return numToRound + multiple - remainder;
            }

            byte[] pkeydata = UnicodeEncoding.UTF8.GetBytes(pkey);
            var len = pkeydata.Length;
            var lb = BitConverter.GetBytes(len);
            var round = roundUp(pkeydata.Length, 16);
            Array.Resize(ref pkeydata, round);
            ProtectedMemory.Protect(pkeydata, MemoryProtectionScope.SameLogon);
            return lb.Concat(pkeydata).ToArray();
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
            var mainnet = root.AddNetwork("Stratis Mainnet", "https://rpc.stratisevm.com", 105105, "105105");
            var endpoints = mainnet.GetChild("Endpoints", BlockchainInfoKind.Folder);
            endpoints.AddChild("https://rpc.stratisevm.com:8545", BlockchainInfoKind.Endpoint);
            //data.Add(mainnet);
            var testnet = root.AddNetwork("Stratis Testnet", "https://auroria.rpc.stratisevm.com:8545", 205205, "205205");
            endpoints = testnet.GetChild("Endpoints", BlockchainInfoKind.Folder);
            endpoints.AddChild("https://auroria.rpc.stratisevm.com:8545", BlockchainInfoKind.Endpoint);
            //data.Add(testnet);

#if IS_VSIX
            var result = ThreadHelper.JoinableTaskFactory.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync("http://127.0.0.1:7545")));
#else
            var result = Task.Run(() => ExecuteAsync(Network.GetNetworkDetailsAsync("http://127.0.0.1:7545"))).Result;
#endif
            if (result.Succeeded(out var r))
            {
                var local1 = root.AddNetwork("Ganache", "http://localhost:7545", 1337, "5777");
                endpoints = local1.GetChild("Endpoints", BlockchainInfoKind.Folder);
                endpoints.AddChild("http://127.0.0.1:7545", BlockchainInfoKind.Endpoint);
                var accounts = local1.GetChild("Accounts", BlockchainInfoKind.Folder);
                foreach (var a in r.Value.Item3)
                {
                    accounts.AddAccount(a);
                }
               
                var dp = local1.GetChild("Deploy Profiles", BlockchainInfoKind.Folder);
                dp.AddDeployProfile("Deploy locally", "http://127.0.0.1:7545", r.Value.Item3[0]);
                //data.Add(local1);
            }
             data.Add(root);
            //var testnet = new BlockchainInfo(BlockchainInfoKind.Network, "Stratis Testnet");
            //mainnet.AddChild(BlockchainInfoKind.Endpoint, "auroria.stratisevm.com", new Uri("https://auroria.rpc.stratisevm.com"));

            root.Save("BlockchainExplorerTree", out var _);
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

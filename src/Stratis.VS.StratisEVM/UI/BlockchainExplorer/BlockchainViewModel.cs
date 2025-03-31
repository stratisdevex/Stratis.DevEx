
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;

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
        public BlockchainInfo(BlockchainInfoKind kind, string name, BlockchainInfo parent = null, object data = null)
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
        public BlockchainInfo Parent { get; set; }
       
        public object Data { get; set; }
        public ObservableCollection<BlockchainInfo> Children = new ObservableCollection<BlockchainInfo>();
        #endregion

        #region Methods
        public BlockchainInfo AddChild(BlockchainInfoKind kind, string name, object data = null)
        {
            var info = new BlockchainInfo(kind, name, this, data);
            Children.Add(info);
            return info;
        }

        public BlockchainInfo[] GetChildren(string name) => Children.Where(c =>  c.Name == name).ToArray(); 
        #endregion
    }

    public class BlockchainViewModel : INotifyPropertyChanged
    {
        #region Constructors
        public BlockchainViewModel()
        {
            Objects = CreateInitialTreeData();
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
            RaisePropertyChangedEvent("Categories");

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

        public static ObservableCollection<BlockchainInfo> CreateTestData()
        {
            
            var data = new ObservableCollection<BlockchainInfo>();
            var eth = new BlockchainInfo(BlockchainInfoKind.Network, "Ethereum");
            var accts = eth.AddChild(BlockchainInfoKind.Folder, "Accounts");
            accts.AddChild(BlockchainInfoKind.Account, "acct 1", "This is account 1");
            //var wall = eth.AddChild(BlockchainInfoKind.Folder, "Accounts");
            data.Add(eth);
            return data;
        }

        public static ObservableCollection<BlockchainInfo> CreateInitialTreeData()
        {
            var data = new ObservableCollection<BlockchainInfo>();
            var mainnet = new BlockchainInfo(BlockchainInfoKind.Network, "Stratis Mainnet");
            var endpoints = mainnet.AddChild(BlockchainInfoKind.Folder, "Endpoints");
            endpoints.AddChild(BlockchainInfoKind.Endpoint, "rpc.stratisevm.com", new Uri("https://rpc.stratisevm.com:8545"));
            data.Add(mainnet);
            //var testnet = new BlockchainInfo(BlockchainInfoKind.Network, "Stratis Testnet");
            //mainnet.AddChild(BlockchainInfoKind.Endpoint, "auroria.stratisevm.com", new Uri("https://auroria.rpc.stratisevm.com"));
            //data.Add(mainnet);
            //data.Add(testnet);
            return data;
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
}

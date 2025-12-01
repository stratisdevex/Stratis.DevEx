using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Stratis.DevEx;

namespace Stratis.VS.StratisEVM.UI.ViewModel
{
    public enum SolidityStaticAnalysisInfoKind
    {
        Folder,
        Detector
    }

    public class SolidityStaticAnalysisInfo
    {
        #region Constructors
        public SolidityStaticAnalysisInfo(SolidityStaticAnalysisInfoKind kind, string name, SolidityStaticAnalysisInfo parent = null, Dictionary<string, object> data = null)
        {
            Kind = kind;
            Name = name;
            Parent = parent;
            Data = data;
        }
        #endregion

        #region Properties
        public SolidityStaticAnalysisInfoKind Kind { get; set; }

        public string Name { get; set; }

        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        [JsonProperty(ItemIsReference = true)]
        public SolidityStaticAnalysisInfo Parent { get; set; }

        [JsonProperty(ItemReferenceLoopHandling = ReferenceLoopHandling.Serialize)]
        public ObservableCollection<SolidityStaticAnalysisInfo> Children = new ObservableCollection<SolidityStaticAnalysisInfo>();

        public string Key => ((this.Parent?.Key) ?? "Root") + "_" + this.Kind + "_" + this.Name;

        [JsonIgnore]
        public string ToolTip
        {
            get
            {
                switch (Kind)
                {                  
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
                    default:
                        return Name;
                }
            }
        }
        #endregion

        #region Methods
        public SolidityStaticAnalysisInfo AddChild(string name, SolidityStaticAnalysisInfoKind kind, Dictionary<string, object> data = null)
        {
            var info = new SolidityStaticAnalysisInfo(kind, name, this, data);
            Children.Add(info);
            return info;
        }

        public SolidityStaticAnalysisInfo AddFolder(string name) => AddChild(name, SolidityStaticAnalysisInfoKind.Folder);
        
        #endregion
    }

    public class SolidityStaticAnalysisViewModel : INotifyPropertyChanged
    {
        #region Constructors
        public SolidityStaticAnalysisViewModel()
        {
            Objects = CreateInitialTreeData();
        }
        #endregion

        #region Fields
        internal ObservableCollection<SolidityStaticAnalysisInfo> objects;
        #endregion

        #region Properties
        public ObservableCollection<SolidityStaticAnalysisInfo> Objects
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

        public SolidityStaticAnalysisInfo TryFindObjectByName(string name, SolidityStaticAnalysisInfo parent = null)
        {
            ObservableCollection<SolidityStaticAnalysisInfo> objs;
            objs = parent is null ? objects : parent.Children;
            foreach (SolidityStaticAnalysisInfo si in objs)
            {
                if (si.Name == name)
                {
                    return si;
                }
                else
                {
                    var b = TryFindObjectByName(name, si);
                    if (b != null) return b;
                }
            }

            return null;
        }

        
        public static ObservableCollection<SolidityStaticAnalysisInfo> CreateInitialTreeData()
        {
            var data = new ObservableCollection<SolidityStaticAnalysisInfo>();
            var root = new SolidityStaticAnalysisInfo(SolidityStaticAnalysisInfoKind.Folder, "Analysis");
            root.AddFolder("High");
            root.AddFolder("Medium");
            root.AddFolder("Low");
            root.AddFolder("Informational");
            root.AddFolder("Optimization");
            data.Add(root);
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

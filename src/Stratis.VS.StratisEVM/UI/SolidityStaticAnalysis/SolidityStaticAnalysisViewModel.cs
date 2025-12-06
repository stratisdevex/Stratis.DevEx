using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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
                    case SolidityStaticAnalysisInfoKind.Detector:
                        var desc = ((string)Data["Description"]).Split('\n')[0];
                        return descRegex.Replace(desc, "");                        
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

        static Regex descRegex = new Regex("\\s\\(.+\\)\\:", RegexOptions.Compiled);
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

        public SolidityStaticAnalysisInfo High => TryFindObjectByName("High");
        public SolidityStaticAnalysisInfo Medium => TryFindObjectByName("Medium");
        public SolidityStaticAnalysisInfo Low => TryFindObjectByName("Low");
        public SolidityStaticAnalysisInfo Informational => TryFindObjectByName("Informational");
        public SolidityStaticAnalysisInfo Optimization => TryFindObjectByName("Optimization");
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

        public void ClearAnalysis()
        {
            High.Children.Clear();
            Medium.Children.Clear();
            Low.Children.Clear();
            Informational.Children.Clear();
            Optimization.Children.Clear();
        }
        
        public void AddDetectorResult(Detector detector)
        {            
            var impact = detector.impact.ToLower();
            SolidityStaticAnalysisInfo parent;
            switch (impact)
            {
                case "high":
                    parent = High;
                    break;
                case "medium":
                    parent = Medium;
                    break;
                case "low":
                    parent = Low;
                    break;
                case "informational":
                    parent = Informational;
                    break;
                case "optimization":
                    parent = Optimization;
                    break;
                default: throw new ArgumentException($"Unknown detector impact level: {impact}");
            }
            parent.AddChild(detector.id, SolidityStaticAnalysisInfoKind.Detector, new Dictionary<string, object>()
            {
                { "Description", detector.description },
                { "Markdown", detector.markdown },
                { "FirstMarkdownElement", detector.first_markdown_element },
                { "Id", detector.id },
                { "Check", detector.check },
                { "Impact", detector.impact },
                { "Confidence", detector.confidence },
                { "Elements", detector.elements }
            });
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

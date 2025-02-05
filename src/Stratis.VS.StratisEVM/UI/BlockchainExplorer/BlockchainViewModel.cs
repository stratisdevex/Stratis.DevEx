using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;

namespace Stratis.VS.StratisEVM.ViewModel
{
    public class BlockchainInfo
    {
        #region Constructors
        public BlockchainInfo(string kind, string name, BlockchainInfo parent, object data = null)
        {
            Kind = kind;
            Name = name;
            Parent = parent;
            Data = data;
        }


        #endregion

        #region Properties
        internal string Kind { get; set; }
        internal string Name { get; set; }
        internal BlockchainInfo Parent { get; set; }
        internal object Data { get; set; }
        internal ObservableCollection<BlockchainInfo> Children = new ObservableCollection<BlockchainInfo>();
        #endregion
    }

    internal class BlockchainViewModel : INotifyPropertyChanged
    {
        internal ObservableCollection<BlockchainInfo> objects;

        internal ObservableCollection<BlockchainInfo> Objects
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


        private void OnRootCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChangedEvent("Categories");
        }



        /// <summary>
        /// Refreshes the data.
        /// </summary>
        

        ///<summary>
        ///Occurs when a property value changes.
        ///</summary>
        public event PropertyChangedEventHandler PropertyChanged;


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
    }
}

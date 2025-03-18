using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Stratis.DevEx.Ethereum;
using Stratis.DevEx.Ethereum.Explorers;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for StratisExplorerSummaryControl.xaml
    /// </summary>
    public partial class StratisDashboardToolWindowControl : UserControl
    {
        public StratisDashboardToolWindowControl()
        {
            InitializeComponent();
            
         
        }

        public async Task<StatsResponse> GetStatsAsync()
        {
            var bsc = new BlockscoutClient(new System.Net.Http.HttpClient());
            return await bsc.Get_statsAsync();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Stats = await GetStatsAsync();
            TotalBlocksTextBlock.Text = Stats.Total_blocks.ToString();
            TotalTransactionsTextBlock.Text = Stats.Total_transactions.ToString();
            AverageBlockTimeTextBlock.Text = Stats.Average_block_time.ToString();
            TotalAddressesTextBlock.Text = Stats.Total_addresses.ToString();
            TransactionsTodayTextBlock.Text = Stats.Transactions_today.ToString();
            NetworkUtilizationTextBlock.Text = Stats.Network_utilization_percentage.ToString(); 
        }

        StatsResponse Stats { get; set; }
    }
}

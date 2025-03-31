using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Stratis.DevEx.Ethereum;
using Stratis.DevEx.Ethereum.Explorers;

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for StratisEVMBlockchainHomeUserControl.xaml
    /// </summary>
    public partial class StratisEVMBlockchainHomeUserControl : UserControl
    {
        public StratisEVMBlockchainHomeUserControl()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hc = new HttpClient();
            var Stats = await GetStatsAsync(hc);
            TotalBlocksTextBlock.Text = Int64.Parse(Stats.Total_blocks).ToString("N");
            AverageBlockTimeTextBlock.Text = (Stats.Average_block_time / 1000.0).ToString() + "s";
            TransactionsTodayTextBlock.Text = Stats.Transactions_today;
            TotalTransactionsTextBlock.Text = Int64.Parse(Stats.Total_transactions).ToString("N");
            TotalAddressesTextBlock.Text = Int64.Parse(Stats.Total_addresses).ToString("N");
            NetworkUtilizationTextBlock.Text = Stats.Network_utilization_percentage.ToString("N");
            var blocks = await GetLatestBlocks(hc);
            BlocksListView.ItemsSource = blocks;
            var transactions = await GetLatestTransactions(hc);
            TransactionsListView.ItemsSource = transactions;
        }

        public async Task<StatsResponse> GetStatsAsync(HttpClient hc)
        {
            var bsc = new BlockscoutClient(hc);
            return await bsc.Get_statsAsync();
        }

        public static async Task<ICollection<Transaction>> GetLatestTransactions(HttpClient hc)
        {
            var bsc = new BlockscoutClient(hc);
            var r = await bsc.Get_txsAsync();
            return r.Items.ToArray();
        }

        public static async Task<ICollection<Block>> GetLatestBlocks(HttpClient hc)
        {
            var bsc = new BlockscoutClient(hc);
            var r = await bsc.Get_blocksAsync();
            return r.Items.ToArray();
        }

        public static Transaction[] SampleTransactionData => BlockscoutSampleData.Transactions;

        public static Block[] SampleBlockData => BlockscoutSampleData.Blocks;
    }
}

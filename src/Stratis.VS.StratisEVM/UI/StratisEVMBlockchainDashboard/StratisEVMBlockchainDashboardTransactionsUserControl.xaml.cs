using Stratis.DevEx.Ethereum.Explorers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace Stratis.VS.StratisEVM.UI
{
    /// <summary>
    /// Interaction logic for StratisEVMTransactions.xaml
    /// </summary>
    public partial class StratisEVMBlockchainDashboardTransactionsUserControl : UserControl
    {
        public StratisEVMBlockchainDashboardTransactionsUserControl()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var hc = new HttpClient();
            var stats = await GetStatsAsync(hc);
            var pt = await GetLatestPendingTransactions(hc);
            TransactionsTodayTextBlock.Text = stats.Transactions_today;   
            PendingTransactionsTodayTextBlock.Text = pt.Count.ToString();   
            GasUsedTodayTextBlock.Text = stats.Gas_used_today;
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

        public static async Task<ICollection<Transaction>> GetLatestPendingTransactions(HttpClient hc)
        {
            var bsc = new BlockscoutClient(hc);
            var r = await bsc.Get_txsAsync(filter:"pending");
            return r.Items.ToArray();
        }
    }
}

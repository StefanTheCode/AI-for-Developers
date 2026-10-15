using System;
using System.Threading;
using System.Windows.Forms;
using LegacyErp.Data;

namespace LegacyErp.UI
{
    public partial class MainForm : Form
    {
        private readonly OrderRepository _repo = new OrderRepository("...");

        public MainForm()
        {
            InitializeComponent();
            // ISSUE: subscribes to a long-lived static event, never unsubscribes
            AppEvents.DataChanged += OnDataChanged;
        }

        private void OnDataChanged(object sender, EventArgs e) { gridOrders.Refresh(); }

        // ISSUE: long-running DB work directly on UI thread -> app freezes
        private void btnLoad_Click(object sender, EventArgs e)
        {
            var data = _repo.SearchOrders(txtName.Text);
            gridOrders.DataSource = data;
        }

        // ISSUE: background thread touches a control directly -> cross-thread exception
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                var data = _repo.SearchOrders("");
                gridOrders.DataSource = data; // cross-thread!
                lblStatus.Text = "Done";      // cross-thread!
            }).Start();
        }

        // ISSUE: dialog shown without using -> leak
        private void btnEdit_Click(object sender, EventArgs e)
        {
            var dlg = new EditForm();
            dlg.ShowDialog();
        }

        // ISSUE: async void with no try/catch -> unobserved exception crashes process
        private async void btnSync_Click(object sender, EventArgs e)
        {
            await System.Threading.Tasks.Task.Delay(100);
            gridOrders.Refresh();
        }
    }

    public static class AppEvents { public static event EventHandler DataChanged; }
    public class EditForm : Form { }
}

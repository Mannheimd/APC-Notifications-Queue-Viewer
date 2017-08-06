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

namespace APC_Notifications_Queue_Viewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            StoreLoginCreds();

            Testing();
        }

        public async void Testing()
        {
            List<Ticket> tickets = await InforTasks.GetTickets(@"https://crm.crmcloud.infor.com:443/sdata/slx/dynamic/-/tickets", "");//, @"?include=assignedto,EscalationCloudOppsStaff,TicketHistory,TicketProblem&where=(AssignedTo.OwnerDescription%20eq%20'APC%20Notification'%20AND%20StatusCode%20ne%20'k6UJ9A000038'%20AND%20StatusCode%20ne%20'k6UJ9A000037'%20AND%20StatusCode%20ne%20'kCRMAA00004S'%20AND%20StatusCode%20ne%20'kCRMAA0000GV'%20AND%20StatusCode%20ne%20'kCRMAA0000GR'%20AND%20StatusCode%20ne%20'kCRMAA00005U'%20AND%20StatusCode%20ne%20'kCRMAA0000GS'%20AND%20StatusCode%20ne%20'kCRMAA0001DN'%20AND%20StatusCode%20ne%20'kCRMAA0001DO'%20AND%20StatusCode%20ne%20'kCRMAA0000GX'%20AND%20StatusCode%20ne%20'kCRMAA0000H0')");
            MessageBox.Show(tickets.Count.ToString());
        }

        public void StoreLoginCreds()
        {
            InforTasks.SecureCreds("christopher.manders", "Epicpassword01");
        }
    }
}

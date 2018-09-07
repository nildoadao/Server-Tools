using Server_Tools.Control;
using Server_Tools.Model;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FileUpdatePage.xam
    /// </summary>
    public partial class FileUpdatePage : Page
    {
        public FileUpdatePage()
        {
            InitializeComponent();            
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckRedfishSupport();
        }

        private async void CheckRedfishSupport()
        {
            IdracRedfishController idrac = new IdracRedfishController(new Server("", "", ""));
            var support = await idrac.CheckRedfishSupport();
            {
                if (support)
                {
                    OutputTextBox.AppendText("Suporta RedFish\n");
                }
                else
                {
                    OutputTextBox.AppendText("Não suporta Redfish\n");
                }
            }
        }
    }
}

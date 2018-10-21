using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
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

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para StoragePage.xaml
    /// </summary>
    public partial class StoragePage : Page
    {
        public StoragePage()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;
            var server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            Connect(server);         
        }

        private bool ValidateForm()
        {
            if(String.IsNullOrEmpty(ServerTextBox.Text) | String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Insira os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async void Connect(Server server)
        {
            if (!NetworkHelper.IsConnected(server.Host))
            {
                MessageBox.Show("Servidor inacessivel", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var idrac = new StorageController(server);
            try
            {
                if (await idrac.CheckRedfishSupport(StorageController.Controllers) == false)
                {
                    MessageBox.Show(string.Format("O servidor {0} não suporta a API Redfish", server), "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }                  
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao conectar: {0}", ex.Message), "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var storageWindow = new StorageWindow(server) { Title = server.Host };
            storageWindow.Show();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new HomePage());
        }
    }
}

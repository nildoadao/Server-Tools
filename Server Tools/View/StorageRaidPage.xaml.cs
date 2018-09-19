using Server_Tools.Control;
using Server_Tools.Model;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para StorageRaidPage.xam
    /// </summary>
    public partial class StorageRaidPage : Page
    {
        public StorageRaidPage()
        {
            InitializeComponent();
        }

        private void CheckPhysicalDiskButton_Click(object sender, RoutedEventArgs e)
        {
            if(ServerTextBox.Text.Trim().Equals("") | UserTextBox.Text.Trim().Equals("") | PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }      
    }
}

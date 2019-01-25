using Microsoft.Win32;
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
    /// Interação lógica para OsInstallPage.xam
    /// </summary>
    public partial class OsInstallPage : Page
    {
        OpenFileDialog CsvDialog;
        OpenFileDialog ImageDialog;

        public OsInstallPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CsvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            CsvDialog.FileOk += CsvDialog_FileOk;
            ImageDialog = new OpenFileDialog()
            {
                Filter = "Imagens bootaveis|*iso;*img"
            };
            ImageDialog.FileOk += ImageDialog_FileOk;
        }

        private void ImageDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ImageDialog.FileName.Contains(@"\"))
                ImageTextBox.Text = ImageDialog.FileName.Replace(@"\", @"/\");
            else
                ImageTextBox.Text = ImageDialog.FileName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password) | String.IsNullOrEmpty(ServerTextBox.Text))
            {
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            ServersListBox.Items.Add(server);
            ServerTextBox.Clear();

            if (!KeepCheckbox.IsChecked.Value)
            {
                UserTextBox.Clear();
                PasswordBox.Clear();
            }
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach (Server server in FileHelper.ReadCsvFile(CsvDialog.FileName))
                    ServersListBox.Items.Add(server);
            }
            catch
            {
                MessageBox.Show("Falha ao carregar arquivo CSV, certifique que o arquivo está no formato correto e tente novamente\n\nFormato esperado:\n<hostname>;<usuario>;<senha>",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Clear();
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void ShareImageButton_Click(object sender, RoutedEventArgs e)
        {
            ImageDialog.ShowDialog();
        }

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para coleta do TSR", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if(String.IsNullOrEmpty(ImageTextBox.Text) || String.IsNullOrEmpty(ShareUserTextBox.Text) || String.IsNullOrEmpty(SharePasswordBox.Password))
            {
                MessageBox.Show("Insira os dados do File Share", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            List<Task> tasks = new List<Task>();
            foreach(Server server in ServersListBox.Items)
            {
                tasks.Add(InstallImage(server, ImageTextBox.Text, ShareUserTextBox.Text, SharePasswordBox.Password));
            }
            await Task.WhenAll(tasks);
        }

        private Task InstallImage(Server server, string image, string user, string password)
        {
            OutputTextBox.AppendText(string.Format("Iniciando instalação para {0}\n", server.Host));

            return Task.Run(() =>
            {
                try
                {
                    IdracSshController idrac = new IdracSshController(server);

                    string command = string.Format("racadm remoteimage -c -u {0} -p {1} -l {2}", user, password, image);

                    idrac.RunCommand(command);
                    idrac.RunCommand("racadm set BIOS.OneTimeBoot.OneTimeBootMode OneTimeBootSeq");
                    idrac.RunCommand("racadm set BIOS.OneTimeBoot.OneTimeBootDev VCD-DVD");

                    if (GetPowerStatus(server).Equals("ON"))
                        idrac.RunCommand("racadm serveraction powercycle");
                    else
                        idrac.RunCommand("racadm serveraction powerup");

                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText(string.Format("Iniciada instalação no servidor {0}\n", server.Host));
                    });
                }
                catch
                {
                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText(string.Format("Falha na instalação de S.O no servidor {0}\n", server.Host));
                    });
                }
            });            
        }

        private string GetPowerStatus(Server server)
        {
            string command = "racadm serveraction powerstatus";
            IdracSshController idrac = new IdracSshController(server);
            string result = idrac.RunCommand(command);
            return result.Split(':')[1].Trim();
        }
    }
}

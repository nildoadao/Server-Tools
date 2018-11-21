using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FileUpdatePage.xam
    /// </summary>
    public partial class FileUpdatePage : Page
    {
        OpenFileDialog FirmwareDialog;
        OpenFileDialog CsvDialog;
        DispatcherTimer timer;

        public FileUpdatePage()
        {
            InitializeComponent();
        }

        // Classe interna para dados do DataGrid

        private class ServerJob
        {
            public Server Server { get; set; }
            public IdracJob Job { get; set; }
            public string SerialNumber { get; set; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FirmwareDialog = new OpenFileDialog()
            {
                Filter = "Idrac Firmware (*.exe)(*.d7)(*.pm)| *.exe;*.d7;*.pm"
            };
            FirmwareDialog.FileOk += FirmwareDialog_FileOk;
            CsvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            CsvDialog.FileOk += CsvDialog_FileOk;
            JobsDataGrid.ItemsSource = new List<ServerJob>();
            timer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach(var item in FileHelper.ReadCsvFile(CsvDialog.FileName))
                {
                    ServersListBox.Items.Add(item);
                }
            }
            catch
            {
                MessageBox.Show("Falha ao carregar arquivo CSV, certifique que o arquivo está no formato correto e tente novamente\n\nFormato esperado:\n<hostname>;<usuario>;<senha>",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateJobsAsync();
        }

        private void FirmwareDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FirmwareTextBox.Text = FirmwareDialog.FileName;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;
            
            string firmware = FirmwareTextBox.Text;
            string option = "";
            foreach(RadioButton item in InstallOptionGroup.Children)
            {
                if (item.IsChecked.Value)
                {
                    option = item.Content.ToString();
                    break;
                }
            }

            foreach(Server server in ServersListBox.Items)
            {
                UpdateFirmwareAsync(server, firmware, option);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ServerTextBox.Text) || String.IsNullOrEmpty(UserTextBox.Text) || String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Insira as informações da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            ServersListBox.Items.Add(server);
            ServerTextBox.Clear();
            UserTextBox.Clear();
            PasswordBox.Clear();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void OpenFirmwareButton_Click(object sender, RoutedEventArgs e)
        {
            FirmwareDialog.ShowDialog();
        }

        private async void UpdateFirmwareAsync(Server server, string path, string option)
        {
            if (!await NetworkHelper.CheckConnectionAsync(server.Host))
            {
                OutputTextBox.AppendText(string.Format("O servidor {0} não está acessivel, verifique a conexão e tente novamente\n", server.Host));
                return;
            }
            try
            {
                UpdateController idrac = new UpdateController(server);

                if(!await idrac.CheckRedfishSupportAsync(UpdateController.FirmwareInventory))
                {
                    OutputTextBox.AppendText(string.Format("A versão da Idrac do {0} servidor não possui suporte a função de update de firmware\n", server.Host));
                    return;
                }

                OutputTextBox.AppendText(string.Format("Iniciando upload do firmware para {0}...\n", server.Host));
                ChassisController chassisIdrac = new ChassisController(server);                
                IdracJob job = await idrac.UpdateFirmwareAsync(path, option);
                Chassis chassis = await chassisIdrac.GetChassisAsync();
                var jobs = (List<ServerJob>) JobsDataGrid.ItemsSource;
                jobs.Add(new ServerJob { Server = server, Job = job, SerialNumber = chassis.SKU });
                OutputTextBox.AppendText(string.Format("Upload concluido, criado Job {0} para update\n", job.Id));
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Erro ao atualizar {0} {1}\n", server.Host, ex.Message));
            }           
        }

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um servidor para aplicar o update.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if (String.IsNullOrEmpty(FirmwareTextBox.Text))
            {
                MessageBox.Show("Selecione um firmware.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async void UpdateJobsAsync()
        {
            var updatedJobs = new List<ServerJob>();
            foreach (ServerJob job in JobsDataGrid.ItemsSource)
            {
                try
                {
                    var idrac = new JobController(job.Server);
                    var updatedJob = await idrac.GetJobAsync(job.Job.Id);
                    var chassisIdrac = new ChassisController(job.Server);
                    var chassis = await chassisIdrac.GetChassisAsync();
                    updatedJobs.Add(new ServerJob { Server = job.Server, Job = updatedJob, SerialNumber = chassis.SKU });
                }
                catch
                {
                    OutputTextBox.AppendText("Falha ao atualizar status dos Jobs\n");
                }
            }
            JobsDataGrid.ItemsSource = updatedJobs;
        }
    }
}

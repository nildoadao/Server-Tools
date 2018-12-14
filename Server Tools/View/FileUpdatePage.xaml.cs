using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
        ConcurrentDictionary<string, ServerJob> currentJobs;

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
            currentJobs = new ConcurrentDictionary<string, ServerJob>();
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
            JobsDataGrid.ItemsSource = currentJobs.Values;
            timer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 5)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach(var item in FileHelper.ReadCsvFile(CsvDialog.FileName))
                    ServersListBox.Items.Add(item);
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

            UpdateButton.IsEnabled = false;
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
            UpdateFirmwareAsync(firmware, option);
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

            if (!KeepCheckbox.IsChecked.Value)
            {
                UserTextBox.Clear();
                PasswordBox.Clear();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Clear();
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

        private async void UpdateFirmwareAsync(string path, string option)
        {
            List<Server> servers = new List<Server>();
            foreach (Server item in ServersListBox.Items)
                servers.Add(item);

            foreach (Server server in servers)
            {
                if (!await NetworkHelper.CheckConnectionAsync(server.Host))
                {
                    OutputTextBox.AppendText(string.Format("O servidor {0} não está acessivel, verifique a conexão e tente novamente\n", server.Host));
                    continue;
                }
                try
                {
                    UpdateController idrac = new UpdateController(server);

                    if (!await idrac.CheckRedfishSupportAsync(UpdateController.FirmwareInventory))
                    {
                        OutputTextBox.AppendText(string.Format("A versão da Idrac do {0} servidor não possui suporte a função de update de firmware\n", server.Host));
                        continue;
                    }
                    OutputTextBox.AppendText(string.Format("Iniciando upload do firmware para {0}...\n", server.Host));
                    ChassisController chassisIdrac = new ChassisController(server);
                    IdracJob job = await idrac.UpdateFirmwareAsync(path, option);
                    Chassis chassis = await chassisIdrac.GetChassisAsync();
                    currentJobs.TryAdd(job.Id, new ServerJob() { Job = job, Server = server, SerialNumber = chassis.SKU });
                    OutputTextBox.AppendText(string.Format("Upload concluido, criado Job {0} para update\n", job.Id));
                }
                catch (Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Erro ao atualizar {0} {1}\n", server.Host, ex.Message));
                }
            }
            UpdateButton.IsEnabled = true;
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
            timer.Stop();
            foreach (ServerJob job in currentJobs.Values)
            {
                try
                {
                    var idrac = new JobController(job.Server);
                    var updatedJob = await idrac.GetJobAsync(job.Job.Id);
                    currentJobs.AddOrUpdate(job.Job.Id, new ServerJob() { Job = updatedJob, SerialNumber = job.SerialNumber, Server = job.Server },
                        (key, existingVal) =>
                        {
                            existingVal.Job.Message = updatedJob.Message;
                            existingVal.Job.PercentComplete = updatedJob.PercentComplete;
                            existingVal.Job.JobState = updatedJob.JobState;
                            return existingVal;
                        });
                }
                catch
                {
                    OutputTextBox.AppendText(string.Format("Falha ao atualizar status do Job : {0}\n", job.Job.Id));
                    continue;
                }
            }
            JobsDataGrid.ItemsSource = currentJobs.Values;
            timer.Start();
        }
    }
}

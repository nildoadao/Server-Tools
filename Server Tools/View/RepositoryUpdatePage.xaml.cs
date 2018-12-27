using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using Server_Tools.Util;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para RepositoryUpdatePage.xam
    /// </summary>
    public partial class RepositoryUpdatePage : Page
    {
        FileDialog CsvDialog;
        DispatcherTimer timer;
        ConcurrentDictionary<string, ServerJob> currentJobs;

        // Classe interna para os dados do DataGrid

        private class ServerJob
        {
            public Server Server { get; set; }
            public IdracJob Job { get; set; }
            public string SerialNumber { get; set; }            
        }

        public RepositoryUpdatePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            currentJobs = new ConcurrentDictionary<string, ServerJob>();
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

        private void CsvDialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                var servers = FileHelper.ReadCsvFile(CsvDialog.FileName);
                foreach (Server server in servers)
                    ServersListBox.Items.Add(server);
            }
            catch
            {
                MessageBox.Show("Falha ao carregar arquivo CSV, certifique que o arquivo está no formato correto e tente novamente\n\nFormato esperado:\n<hostname>;<usuario>;<senha>", 
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await UpdateJobsAsync();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ServerTextBox.Text) || String.IsNullOrEmpty(UserTextBox.Text) || String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
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

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            UpdateButton.IsEnabled = false;
            string reboot = RebootRadioButton.IsChecked.Value ? "TRUE" : "FALSE";
            string repository = FileTextBox.Text;
            await UpdateFirmwareAsync(repository, reboot);           
        }

        private async Task UpdateFirmwareAsync(string repository, string reboot)
        {
            foreach (Server server in ServersListBox.Items)
            {
                if (!await NetworkHelper.CheckConnectionAsync(server.Host))
                {
                    OutputTextBox.AppendText(string.Format("Servidor {0} ínacessivel.\n", server));
                    continue;
                }
                try
                {
                    OutputTextBox.AppendText(string.Format("Atualizando firmwares de {0}...\n", server));
                    string jobId = "";
                    var idrac = new IdracSshController(server);
                    string command = string.Format(@"racadm update -f Catalog.xml.gz -e {0} -a {1} -t FTP", repository, reboot);
                    string response = idrac.RunCommand(command);
                    foreach (var item in response.Split(' '))
                    {
                        if (item.Contains("JID"))
                        {
                            jobId = item.Split('"').FirstOrDefault();
                            break;
                        }
                    }
                    if (String.IsNullOrEmpty(jobId)) // Caso Haja problema na criação do Job ele retorna ""
                        continue;

                    var idracJob = new JobController(server);
                    var idracChassis = new ChassisController(server);
                    IdracJob job = await idracJob.GetJobAsync(jobId);
                    Chassis chassis = await idracChassis.GetChassisAsync();
                    currentJobs.TryAdd(job.Id, new ServerJob() { Server = server, Job = job, SerialNumber = chassis.SKU });
                    OutputTextBox.AppendText(string.Format("Criado {0} par atualizaçao do servidor {1}", jobId, server));
                }
                catch (Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Falha ao atualizar {0} {1}\n", server, ex.Message));
                }
            }
            UpdateButton.IsEnabled = true;
        }

        private async Task UpdateJobsAsync()
        {
            timer.Stop();
            foreach (var job in currentJobs.Values)
            {
                try
                {
                    var idrac = new JobController(job.Server);
                    var updatedJob = await idrac.GetJobAsync(job.Job.Id);
                    currentJobs.AddOrUpdate(job.Job.Id, new ServerJob() { Server = job.Server, Job = updatedJob, SerialNumber = job.SerialNumber },
                        (key, existingVal) =>
                        {
                            existingVal.Job.Message = updatedJob.Message;
                            existingVal.Job.PercentComplete = updatedJob.PercentComplete;
                            existingVal.Job.JobState = updatedJob.JobState;
                            return existingVal;
                        });
                }
                catch(Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Falha ao obter dados de {0} {1}\n", job.Server, ex.Message));
                    timer.Start();
                    return;
                }
            }
            JobsDataGrid.ItemsSource = currentJobs.Values;
            timer.Start();
        }


        // Valida as entradas do formulário

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um servidor para atualização", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
                return true;
        }
    }
}

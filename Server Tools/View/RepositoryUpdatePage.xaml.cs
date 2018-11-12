using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Threading;
using System.ComponentModel;
using Server_Tools.Util;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para RepositoryUpdatePage.xam
    /// </summary>
    public partial class RepositoryUpdatePage : Page
    {
        FileDialog CsvDialog;
        DispatcherTimer timer;

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

        private void CsvDialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                var servers = FileHelper.ReadCsvFile(CsvDialog.FileName);
                foreach (Server server in servers)
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText("Falha ao carregar CSV: " + ex.Message + "\n");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateJobs();
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

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            UpdateButton.IsEnabled = false;
            string reboot = RebootRadioButton.IsChecked.Value ? "TRUE" : "FALSE";
            foreach(Server server in ServersListBox.Items)
            {
                OutputTextBox.AppendText(string.Format("Atualizando firmwares de {0}...\n", server));
                UpdateFirmware(server, reboot);
            }
            UpdateButton.IsEnabled = true;
        }

        private async void UpdateFirmware(Server server, string reboot)
        {
            if(!await NetworkHelper.CheckConnectionAsync(server.Host))
            {
                OutputTextBox.AppendText(string.Format("Servidor {0} ínacessivel.\n", server));
                return;
            }
            try
            {
                string jobId = "";
                var idrac = new IdracSshController(server);
                string command = string.Format(@"racadm update -f Catalog.xml.gz -e ftp.dell.com/Catalog -a {0} -t FTP", reboot);
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
                    return;

                var idracJob = new JobController(server);
                var idracChassis = new ChassisController(server);
                IdracJob job = await idracJob.GetJob(jobId);
                Chassis chassis = await idracChassis.GetChassisInformation();
                var jobs = (List<ServerJob>) JobsDataGrid.ItemsSource;
                jobs.Add(new ServerJob { Server = server, Job = job, SerialNumber = chassis.SKU });
                OutputTextBox.AppendText(string.Format("Criado {0} par atualizaçao do servidor {1}", jobId, server));
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao atualizar {0} {1}\n", server, ex.Message));
            }
        }

        private async void UpdateJobs()
        {
            var updatedJobs = new List<ServerJob>();
            var jobs = (List<ServerJob>) JobsDataGrid.ItemsSource;

            foreach (var job in jobs)
            {
                try
                {
                    var idrac = new JobController(job.Server);
                    var updatedJob = await idrac.GetJob(job.Job.Id);
                    var chassis = new ChassisController(job.Server);
                    var serial = await chassis.GetChassisInformation();
                    updatedJobs.Add(new ServerJob { Server = job.Server, Job = updatedJob, SerialNumber = serial.SKU });
                }
                catch(Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Falha ao obter dados de {0} {1}\n", job.Server, ex.Message));
                }
            }
            jobs = updatedJobs;
            JobsDataGrid.ItemsSource = jobs;
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

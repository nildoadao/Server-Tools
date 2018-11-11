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
        FileDialog csvDialog;
        ObservableCollection<ServerJob> jobQueue;
        DispatcherTimer timer;

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
            csvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            csvDialog.FileOk += CsvDialog_FileOk;
            jobQueue = new ObservableCollection<ServerJob>();
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
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(csvDialog.FileName);
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
            if (!String.IsNullOrEmpty(ServerTextBox.Text))
            {
                if (String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password))
                {
                    MessageBox.Show("Insira usuario e senha da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
                    ServersListBox.Items.Add(server);
                    ServerTextBox.Clear();
                    UserTextBox.Clear();
                    PasswordBox.Clear();
                }
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            csvDialog.ShowDialog();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            string reboot = RebootRadioButton.IsChecked.Value ? "TRUE" : "FALSE";
            foreach(Server server in ServersListBox.Items)
            {
                OutputTextBox.AppendText(string.Format("Atualizando firmwares de {0}...\n", server));
                UpdateFirmware(server, reboot);
            }
        }

        private async void UpdateFirmware(Server server, string reboot)
        {
            try
            {
                string jobId = await CreateJob(server, reboot);
                var idrac = new JobController(server);
                var idracChassis = new ChassisController(server);
                IdracJob job = await idrac.GetJob(jobId);
                Chassis chassis = await idracChassis.GetChassisInformation();
                jobQueue.Add(new ServerJob { Server = server, Job = job, SerialNumber = chassis.SKU });
                OutputTextBox.AppendText(string.Format("Criado {0} par atualizaçao do servidor {1}", jobId, server));
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao atualizar {0} {1}\n", server, ex.Message));
            }
        }

        private async Task<string> CreateJob(Server server, string reboot)
        {
            string jobId = "";
            await Task.Run(() =>
            {
                try
                {
                    if (!NetworkHelper.IsConnected(server.Host))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(string.Format("Servidor {0} ínacessivel.\n", server));
                        });
                        return;
                    }
                    var sshIdrac = new IdracSshController(server);
                    string command = string.Format("racadm update -f Catalog.xml.gz -e ftp.dell.com/Catalog -a {0} -t FTP", reboot);
                    string response = sshIdrac.RunCommand(command);
                    foreach (var item in response.Split(' '))
                    {
                        if (item.Contains("JID"))
                        {
                            jobId = item.Split('"').FirstOrDefault();
                            break;
                        }
                    }                   
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText(string.Format("Falha ao criar Job para {0}: {1}\n", server, ex.Message));
                    });
                }
            });
            return jobId;
        }

        private async void UpdateJobs()
        {
            var updatedJobs = new ObservableCollection<ServerJob>();
            foreach (var job in jobQueue)
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
            jobQueue = updatedJobs;
            JobsDataGrid.ItemsSource = jobQueue;
        }

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

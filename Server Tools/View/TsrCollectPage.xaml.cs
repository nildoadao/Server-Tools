using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interação lógica para TsrCollectPage.xam
    /// </summary>
    public partial class TsrCollectPage : Page
    {
        OpenFileDialog CsvDialog;

        public TsrCollectPage()
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

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Clear();
        }

        private void CollectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            List<string> types = new List<string>();

            if (SysInfoCheckBox.IsChecked.Value)
                types.Add("SysInfo");
            if (OsAppCheckBox.IsChecked.Value)
                types.Add("OSAppNoPII");
            if (OsAppAllCheckBox.IsChecked.Value)
                types.Add("OSAppAll");
            if (TtyLogCheckBox.IsChecked.Value)
                types.Add("TTYLog");

            string type = String.Join(",", types);

            foreach (Server server in ServersListBox.Items)
                CollectAsync(server, type);      
        }

        private async void CollectAsync(Server server, string type)
        {
            if (!await NetworkHelper.CheckConnectionAsync(server.Host))
            {
                OutputTextBox.AppendText(string.Format("Servidor {0} inacessivel.\n", server.Host));
                return;
            }
            try
            {
                OutputTextBox.AppendText(string.Format("Coletando Logs de {0}...\n", server.Host));
                string collectCommand = string.Format("racadm techsupreport collect -t {0}", type);
                IdracSshController idrac = new IdracSshController(server);
                string result = idrac.RunCommand(collectCommand);
                string jobLine = result.Split('\n').FirstOrDefault();
                string jobId = jobLine.Split('=')[1].Trim();
                IdracJob job = await new JobController(server).GetJobAsync(jobId);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow)sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        ExportTsr(server);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao coletar TSR de {0}, {1}\n", server.Host, ex.Message));
            }
        }

        private void ExportTsr(Server server)
        {
            string exportCommand = string.Format(@"racadm -r {0} -u {1} -p {2} techsupreport export -f {3}\{4}.zip",
                server.Host, server.User, server.Password, KnownFolders.Downloads.Path, server.Host);

            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo()
            {
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = string.Format(@"/C {0}", exportCommand)
            };

            System.Diagnostics.Process process = new System.Diagnostics.Process()
            {
                StartInfo = info
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
                OutputTextBox.AppendText(string.Format("Logs de {0} coletados com sucesso ! salvo em {1}\n", server.Host, KnownFolders.Downloads.Path));
            else
                OutputTextBox.AppendText(string.Format("Falha ao coletar os Logs de {0}\n", server.Host));
        }

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para coleta do TSR", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (!OsAppAllCheckBox.IsChecked.Value & !OsAppCheckBox.IsChecked.Value & !TtyLogCheckBox.IsChecked.Value & !SysInfoCheckBox.IsChecked.Value)
            {
                MessageBox.Show("Selecione ao menos um item para a coleção", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

    }
}

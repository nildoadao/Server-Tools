using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para CreateRaidPage.xam
    /// </summary>
    public partial class CreateRaidPage : Page
    {
        private Server server;

        // Classe interna para os dados do DataGrid
        private class DiskItem
        {
            public bool IsSelected { get; set; }
            public PhysicalDisk Disk { get; set; }
        }

        public CreateRaidPage(Server server)
        {
            this.server = server;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CapacityBytesTextBox.IsEnabled = false;
            OptimumIOSizeTextBox.IsEnabled = false;
            VdNameTextBox.IsEnabled = false;
            UpdateForm();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            var selectedDisks = from DiskItem item in DiskDataGrid.Items
                                where item.IsSelected
                                select item.Disk;

            var raidDisks = new List<PhysicalDisk>();
            raidDisks.AddRange(selectedDisks);
            var level = (RaidLevel) RaidCombobox.SelectedItem;
            var enclousure = (Enclousure) ControllersCombobox.SelectedItem;

            if (VdNameCheckBox.IsChecked.Value)
            {
                string name = VdNameTextBox.Text;
                if (OptionalCheckBox.IsChecked.Value)
                {                    
                    long capacity = 0;
                    long optimal = 0;
                    try
                    {
                        capacity = long.Parse(CapacityBytesTextBox.Text);
                        optimal = long.Parse(OptimumIOSizeTextBox.Text);
                    }
                    catch
                    {
                        MessageBox.Show("Valores invalidos para Capacity Bytes e Optimal IO.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    CreateRaidAsync(raidDisks, enclousure, level.ToString(), name, capacity, optimal);
                }                  
                else
                    CreateRaidAsync(raidDisks, enclousure, level.ToString(), name);
            }
            else
                CreateRaidAsync(raidDisks, enclousure, level.ToString());
        }

        private void OptionalCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CapacityBytesTextBox.IsEnabled = OptionalCheckBox.IsChecked.Value;
            OptimumIOSizeTextBox.IsEnabled = OptionalCheckBox.IsChecked.Value;
        }

        private void VdNameCheckBox_Click(object sender, RoutedEventArgs e)
        {
            VdNameTextBox.IsEnabled = VdNameCheckBox.IsChecked.Value;
        }

        private void UpdateForm()
        {
            UpdateControllersAsync();
            UpdateRaid();
            UpdateDataGridAsync();
        }

        private void UpdateRaid()
        {
            var raidLevels = new List<int>() { 0, 1, 5, 10, 50 };
            foreach(var item in raidLevels)
            {
                RaidCombobox.Items.Add(item);
            }
        }

        private async void UpdateDataGridAsync()
        {
            try
            {
                var idrac = new StorageController(server);
                List<PhysicalDisk> disks = await idrac.GetAllPhysicalDisksAsync();
                var datagridItems = new List<DiskItem>();
                foreach (var disk in disks)
                {
                    datagridItems.Add(new DiskItem { IsSelected = false, Disk = disk });
                }
                DiskDataGrid.ItemsSource = datagridItems;
            }
            catch
            {
                MessageBox.Show("Falha ao receber dados dos discos do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateControllersAsync()
        {
            try
            {
                var idrac = new StorageController(server);
                List<Enclousure> enclousures = await idrac.GetAllEnclousuresAsync();
                foreach (var enclousure in enclousures)
                {
                    ControllersCombobox.Items.Add(enclousure);
                }
            }
            catch
            {
                MessageBox.Show("Falha ao receber dados das controladoras", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckForm()
        {
            if (DiskDataGrid.Items == null)
            {
                MessageBox.Show("Não há discos para se criar um Raid", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
 
            var selectedDisks = from DiskItem disk in DiskDataGrid.Items
                                where disk.IsSelected
                                select disk;

            if(selectedDisks.Count() == 0)
            {
                MessageBox.Show("É preciso selecionar ao menos um disco para criar um Raid", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (ControllersCombobox.SelectedIndex == -1)
            {
                MessageBox.Show("Selecione uma controladora", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if(RaidCombobox.SelectedIndex == -1)
            {
                MessageBox.Show("Selecione um nivel de Raid", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if(VdNameCheckBox.IsChecked.Value && String.IsNullOrEmpty(VdNameTextBox.Text))
            {
                MessageBox.Show("Informe o nome do VD", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (OptionalCheckBox.IsChecked.Value && (String.IsNullOrEmpty(OptimumIOSizeTextBox.Text) || (String.IsNullOrEmpty(CapacityBytesTextBox.Text))))
            {
                MessageBox.Show("Informe os parametros opcionais", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        public async void CreateRaidAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level)
        {
            try
            {
                var idrac = new StorageController(server);
                IdracJob job = await idrac.CreateVirtualDiskAsync(disks, enclousure, level);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow)sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        MessageBox.Show("Raid criado com sucesso !", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                load.Show();
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao criar o Job {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void CreateRaidAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level, string name)
        {
            try
            {
                var idrac = new StorageController(server);
                IdracJob job = await idrac.CreateVirtualDiskAsync(disks, enclousure, level, name);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow)sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        MessageBox.Show("Raid criado com sucesso !", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao criar o Job {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void CreateRaidAsync(List<PhysicalDisk> disks, Enclousure enclousure, string level, string name, long capacity, long optimal)
        {
            try
            {
                var idrac = new StorageController(server);
                IdracJob job = await idrac.CreateVirtualDiskAsync(disks, enclousure, level, capacity, optimal, name);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow)sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        MessageBox.Show("Raid criado com sucesso !", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao criar o Job {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

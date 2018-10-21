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

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para CreateRaidPage.xam
    /// </summary>
    public partial class CreateRaidPage : Page
    {
        private List<PhysicalDisk> disks;
        private List<Enclousure> enclousures;
        private Server server;
        private ObservableCollection<DiskItem> datagridItems;

        internal class DiskItem
        {
            public bool IsSelected { get; set; }
            public PhysicalDisk Disk { get; set; }
        }

        public CreateRaidPage(List<PhysicalDisk> disks, List<Enclousure> enclousures, Server server)
        {
            this.disks = disks;
            this.enclousures = enclousures;
            this.server = server;
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateControllersCombobox();
            PopulateRaidCombobox();
            PopulateDataGrid();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            var selectedDisks = from item in datagridItems
                            where item.IsSelected
                            select item.Disk;

            var raidDisks = new List<PhysicalDisk>();

            foreach(var disk in selectedDisks)
            {
                raidDisks.Add(disk);
            }

            int level = (int) RaidCombobox.SelectedItem;
            Enclousure enclousure = (Enclousure) ControllersCombobox.SelectedItem;
            CreateRaid(raidDisks, enclousure, level);
        }

        private void PopulateRaidCombobox()
        {
            var raidLevels = new List<int>() { 0, 1, 5, 10, 50 };
            foreach(var item in raidLevels)
            {
                RaidCombobox.Items.Add(item);
            }
        }

        private void PopulateDataGrid()
        {
            datagridItems = new ObservableCollection<DiskItem>();
            foreach (var disk in disks)
            {
                datagridItems.Add(new DiskItem { IsSelected = false, Disk = disk });
            }
            DiskDataGrid.ItemsSource = datagridItems;
        }

        private void PopulateControllersCombobox()
        {
            foreach (var controller in enclousures)
            {
                ControllersCombobox.Items.Add(controller);
            }
        }

        private bool ValidateForm()
        {
            var selectedDisks = from disk in datagridItems
                                where disk.IsSelected
                                select disk;

            if(selectedDisks.Count() <= 1)
            {
                MessageBox.Show("É preciso selecionar ao menos dois discos para criar um Raid", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
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
            return true;
        }

        public async void CreateRaid(List<PhysicalDisk> disks, Enclousure enclousure, int level)
        {
            var idrac = new StorageController(server);
            try
            {
                IdracJob job = await idrac.CreateVirtualDisk(disks, enclousure, level);
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

    }
}

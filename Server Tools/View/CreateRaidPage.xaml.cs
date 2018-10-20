using Server_Tools.Idrac.Models;
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
    /// Interação lógica para CreateRaidPage.xam
    /// </summary>
    public partial class CreateRaidPage : Page
    {
        private IEnumerable<PhysicalDisk> disks;
        private IEnumerable<RaidController> controllers;

        public CreateRaidPage(IEnumerable<PhysicalDisk> disks, IEnumerable<RaidController> controllers)
        {
            this.disks = disks;
            this.controllers = controllers;
            InitializeComponent();
        }

    }
}

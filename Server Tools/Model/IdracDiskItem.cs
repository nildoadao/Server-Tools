
namespace Server_Tools.Model
{
    class IdracDiskItem
    {
        private bool isSelected;
        private IdracPhysicalDisk disk;

        public bool IsSelected { get => isSelected; set => isSelected = value; }
        public string DiskName { get => disk.Name; }
        public string DiskState { get => disk.State; }
        public string DiskStatus { get => disk.Status; }
        public string Size { get => disk.Size; }
        public bool IsAssigned { get => disk.IsAssigned; }

        public IdracDiskItem(IdracPhysicalDisk disk, bool isSelected)
        {
            this.disk = disk;
            this.isSelected = isSelected;
        }
    }
}

namespace OHotel.NETCoreMVC.Models
{
    internal class StaffPowerPG
    {
        public int MINo { get; set; }
        public string MCName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public int PowerView { get; set; }
        public int PowerAdd { get; set; }
        public int PowerDel { get; set; }
        public int PowerUpdate { get; set; }
        public int PowerGrant { get; set; }
        public string Power1 { get; set; } = "";
        public string Power2 { get; set; } = "";
    }
}
namespace OHotel.NETCoreMVC.DTO
{
    public class ManageItemDTO
    {
        public int MCNo { get; set; } = 0;
        public string ItemName { get; set; } = "";
        public string MIAction { get; set; } = "";
        public int PowerView { get; set; } = 0;
        public int PowerAdd { get; set; } = 0;
        public int PowerDel { get; set; }= 0;
        public int PowerUpdate { get; set; }= 0;
        public int PowerGrant { get; set; }= 0;
        public string Power1 { get; set; } = "";
        public string Power2 { get; set; } = "";
        public int State { get; set; } = 0;
        public int MSNo { get; set; } = 0;
        public int Order { get; set; } = 0;
    }
}

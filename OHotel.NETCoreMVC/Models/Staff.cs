namespace OHotel.NETCoreMVC.Models
{
    //後端使用者Model
    public class Staff
    {
        public int STNo { get; set; } = 0;
        public string STName { get; set; } = "";
        public string LoginName { get; set; } = "";
        public string LoginPasswd { get; set; } = "";
        public string Tel { get; set; } = "";
        public string EMail { get; set; } = "";
        public int AllPower { get; set; } = 0;
        public int LAmount { get; set; } = 0;
        public string CTime { get; set; } = "";
        public string MTime { get; set; } = "";
        public ICollection<StaffPower> StaffPowerList { get; set; } = new List<StaffPower>();//使用者權限
    }
}

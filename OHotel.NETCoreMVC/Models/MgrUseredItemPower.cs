namespace OHotel.NETCoreMVC.Models
{
    /// <summary>
    /// 後端使用者:使用的項目以及權限
    /// </summary>
    public class MgrUseredItemPower : StatusMessage
    {
        public string MgrUserId { get; set; } = "";
        public string MgrUserName { get; set; } = "";
        public int MgrUserAllPower { get; set; } = 0;
        public string MgrUserLoginTime { get; set; } = "";
        public string MgrUserCTime { get; set; } = "";
        public string MgrUserMTime { get; set; } = "";

        public ICollection<MgrUsersPower>? MgrUserPowerList { get; set; } = new List<MgrUsersPower>();
    }
}

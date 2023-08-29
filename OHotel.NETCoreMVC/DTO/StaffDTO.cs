namespace OHotel.NETCoreMVC.DTO
{
    public class StaffDTO
    {
        public string? STName { get; set; }
        public string? LoginName { get; set; }
        public string? LoginPasswd { get; set; }
        public string? Tel { get; set; }
        public string? EMail { get; set; }
        public short State { get; set; }
        public short AllPower { get; set; }
        public DateTime? LoginTime { get; set; }
        public int LAmount { get; set; }
        public DateTime? CTime { get; set; }
        public DateTime? MTime { get; set; }
        public int MSNo { get; set; }
    }
}

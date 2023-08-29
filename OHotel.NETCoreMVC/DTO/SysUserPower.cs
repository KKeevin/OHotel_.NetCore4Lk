using System.ComponentModel.DataAnnotations;

namespace OHotel.NETCoreMVC.DTO
{
    public class SysUserPower
    {
        [Key]
        public int Spno { get; set; }
        public int Suno { get; set; }
        //public SysUser SysUser { get; set; }
        public int Mino { get; set; }
        //public ManageItem ManageItem { get; set; }
        public short Pv { get; set; }
        public short Pa { get; set; }
        public short Pd { get; set; }
        public short Pu { get; set; }
        public short Pg { get; set; }
        public short P1 { get; set; }
        public short P2 { get; set; }
    }
}
using OHotel.NETCoreMVC.Models;

namespace OHotel.NETCoreMVC.Helper
{    public interface IVerifyHelper
    {
        Task<StatusMessage> VerifyJwtPartner(string PID);
        Task<MgrUseredItemPower> VerifyJwtMgr(string ItemId, string MgrId);//後端管理者驗證並回傳該項目權限
        string SanitizeInput(string input);
    }
}
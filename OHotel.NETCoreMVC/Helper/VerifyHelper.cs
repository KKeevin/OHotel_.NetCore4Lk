using EasyCLib.NET.Sdk;
using System.Security.Cryptography;
using OHotel.NETCoreMVC.Models;
using System.Text.RegularExpressions;

namespace OHotel.NETCoreMVC.Helper
{
    public class VerifyHelper : IVerifyHelper
    {
        public IDbFunction _IDbFunction;
        public readonly IEncryptDecrypt _IEncryptDecrypt;
        public readonly IToolsCLib _ToolsCLib;
        private readonly IConfiguration _Configuration;
        public VerifyHelper(IDbFunction dbFunction, IEncryptDecrypt encryptDecrypt, IToolsCLib toolsCLib, IConfiguration configuration)
        {
            _IDbFunction = dbFunction;
            _IEncryptDecrypt = encryptDecrypt;
            _Configuration = configuration;
            _ToolsCLib = toolsCLib;
        }
        /// <summary>
        /// 防止SQL非法注入
        /// </summary>
        public string SanitizeInput(string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"[^\p{L}\p{N}_.-]", "", RegexOptions.None);
        }

        /// <summary>
        /// 驗證JWT Partner 身分有效性（使用參數化查詢防止 SQL 注入）
        /// </summary>
        public async Task<StatusMessage> VerifyJwtPartner(string PID)
        {
            var status = new StatusMessage();
            if (string.IsNullOrEmpty(PID))
            {
                status.Status = "ERROR";
                status.Message = "錯誤#HV1001:PartnerID錯誤!";
                return status;
            }
            if (!int.TryParse(PID, out _))
            {
                status.Status = "ERROR";
                status.Message = "錯誤#HV1001:PartnerID格式錯誤!";
                return status;
            }
            try
            {
                var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel")
                    ?? throw new InvalidOperationException("Connection string not found.");
                _IDbFunction.DbConnect(connStr);
                var found = _IDbFunction.SelectDbDataViewWithParams(
                    "SELECT STName FROM Staff WHERE State=0 AND STNo=@STNo",
                    "STName",
                    new Dictionary<string, object> { ["@STNo"] = PID });
                _IDbFunction.DbClose();
                if (!found || _IDbFunction.SqlDataView.Count < 1)
                {
                    status.Status = "ERROR";
                    status.Message = "錯誤#HV1002: PartnerID Not Allowed!";
                    return status;
                }
            }
            catch
            {
                status.Status = "ERROR";
                status.Message = "錯誤#HV1002: PartnerID Not Allowed!";
                return status;
            }
            status.Status = "SUCCESS";
            status.Message = "成功!";
            return await Task.FromResult(status);
        }
        /// <summary>
        /// 驗證後端使用者編號是否還可使用（使用參數化查詢防止 SQL 注入）
        /// </summary>
        public async Task<MgrUseredItemPower> VerifyJwtMgr(string ItemId, string MgrId)
        {
            var result = new MgrUseredItemPower();
            ItemId = _ToolsCLib.SpecialChar(ItemId);
            MgrId = _ToolsCLib.SpecialChar(MgrId);
            if (string.IsNullOrEmpty(MgrId))
            {
                result.Status = "ERROR";
                result.Message = "錯誤#HV2001:ItemId or MgrId ERROR!";
                return result;
            }
            if (!int.TryParse(MgrId, out _) || (!string.IsNullOrEmpty(ItemId) && !int.TryParse(ItemId, out _)))
            {
                result.Status = "ERROR";
                result.Message = "錯誤#HV2001:ItemId or MgrId 格式錯誤!";
                return result;
            }
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel")
                ?? throw new InvalidOperationException("Connection string 'SQLCD_Read_OHotel' not found.");
            _IDbFunction.DbConnect(connStr);
            try
            {
                var staffParams = new Dictionary<string, object> { ["@MgrId"] = MgrId };
                if (!_IDbFunction.SelectDbDataViewWithParams("SELECT * FROM Staff WHERE State=0 AND STNo=@MgrId", "Staff", staffParams) || _IDbFunction.SqlDataView.Count < 1)
                {
                    result.Status = "ERROR";
                    result.Message = "錯誤#HV2002: MgrId Not Allowed!";
                    return result;
                }
                var r = _IDbFunction.SqlDataView[0];
                result.MgrUserId = r["STNo"]?.ToString() ?? "";
                result.MgrUserName = r["STName"]?.ToString() ?? "";
                result.MgrUserLoginTime = r["LoginTime"] != DBNull.Value ? Convert.ToDateTime(r["LoginTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "";
                result.MgrUserAllPower = Convert.ToInt16(r["AllPower"]);
                result.MgrUserCTime = r["CTime"] != DBNull.Value ? Convert.ToDateTime(r["CTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "";
                result.MgrUserMTime = r["MTime"] != DBNull.Value ? Convert.ToDateTime(r["MTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "";
                result.Status = "SUCCESS";
                result.Message = "成功!";
                if (result.MgrUserAllPower == 1)
                {
                    result.MgrUserPowerList ??= new List<MgrUsersPower>();
                    result.MgrUserPowerList.Add(new MgrUsersPower { MgrUserId = MgrId, MgrItemId = ItemId, MgrPV = 1, MgrPA = 1, MgrPD = 1, MgrPU = 1, MgrPG = 1, MgrP1 = 1, MgrP2 = 1 });
                }
                else if (!string.IsNullOrEmpty(ItemId))
                {
                    var powerParams = new Dictionary<string, object> { ["@MgrId"] = MgrId, ["@ItemId"] = ItemId };
                    if (_IDbFunction.SelectDbDataViewWithParams("SELECT * FROM StaffPower WHERE STNo=@MgrId AND MINo=@ItemId", "StaffPower", powerParams) && _IDbFunction.SqlDataView.Count > 0)
                    {
                        var p = _IDbFunction.SqlDataView[0];
                        result.MgrUserPowerList ??= new List<MgrUsersPower>();
                        result.MgrUserPowerList.Add(new MgrUsersPower
                        {
                            MgrUserId = MgrId,
                            MgrItemId = ItemId,
                            MgrPV = Convert.ToInt16(p["PV"]),
                            MgrPA = Convert.ToInt16(p["PA"]),
                            MgrPD = Convert.ToInt16(p["PD"]),
                            MgrPU = Convert.ToInt16(p["PU"]),
                            MgrPG = Convert.ToInt16(p["PG"]),
                            MgrP1 = Convert.ToInt16(p["P1"]),
                            MgrP2 = Convert.ToInt16(p["P2"])
                        });
                    }
                }
            }
            catch
            {
                result.Status = "ERROR";
                result.Message = "錯誤#HV2002: MgrId Not Allowed!";
            }
            finally
            {
                _IDbFunction.DbClose();
            }
            return await Task.FromResult(result);
        }
    }
}
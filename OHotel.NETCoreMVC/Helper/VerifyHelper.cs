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
        private string Sql = "";
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
        public string SanitizeInput(string input)
        {
            // 移除非法字符以防止SQL注入
            string sanitizedInput = Regex.Replace(input, @"[^\p{L}\p{N}_.-]", "", RegexOptions.None);

            return sanitizedInput;
        }

        /// <summary>
        /// 驗證JWT Partner 身分有效性
        /// </summary>
        /// <param name="APICODE"></param>
        /// <returns></returns>
        public async Task<StatusMessage> VerifyJwtPartner(string PID)
        {
            StatusMessage _Status = new StatusMessage();

            if (String.IsNullOrEmpty(PID))
            {
                _Status.Status = "ERROR";
                _Status.Message = "錯誤#HV1001:PartnerID錯誤!";
                return _Status;
            }
            //--取得 PID 相關訊息
            try
            {
                await Task.Run(() =>
                {
                    //取得合作夥伴 PID
                    Sql = "select STName from Staff where State=0 and STNo='" + PID + "'";
                    if (_IDbFunction.SelectDbDataView(Sql, "STName") && _IDbFunction.SqlDataView.Count < 1)
                    {
                        _Status.Status = "ERROR";
                        _Status.Message = "錯誤#HV1002: PartnerID Not Allowed!";
                    }
                    _IDbFunction.SqlDataView.Dispose();

                });
            }
            catch { }
            ///--如已經不存在該名單 回傳錯誤
            if (_Status.Status == "ERROR")
            {
                return _Status;
            }
            _Status.Status = "SUCCESS";
            _Status.Message = "成功!";
            return _Status;
        }
        /// <summary>
        /// 驗證後端使用者編號是否還可使用
        /// </summary>
        /// <param name="MgrId"></param>
        /// <returns></returns>

        public async Task<MgrUseredItemPower> VerifyJwtMgr(string ItemId, string MgrId)
        {
            //--設定連線
            _IDbFunction.DbConnect(_Configuration.GetConnectionString("SQLCD_Read_OHotel"));
            MgrUseredItemPower _UseredItemPower = new MgrUseredItemPower();
            ItemId = _ToolsCLib.SpecialChar(ItemId);//執行項目編號
            MgrId = _ToolsCLib.SpecialChar(MgrId);//使用者編號
            if (String.IsNullOrEmpty(MgrId))
            {
                _UseredItemPower.Status = "ERROR";
                _UseredItemPower.Message = "錯誤#HV2001:ItemId or MgrId ERROR!";
                return _UseredItemPower;
            }
            //--取得 SUID 相關訊息
            try
            {
                await Task.Run(() =>
                {
                    //確認後端管理者可使用
                    Sql = "select * from Staff ";
                    Sql += " where State=0 and STNo='" + MgrId + "'";
                    if (_IDbFunction.SelectDbDataView(Sql, "Staff") && _IDbFunction.SqlDataView.Count < 1)
                    {
                        _UseredItemPower.Status = "ERROR";
                        _UseredItemPower.Message = "錯誤#HV2002: MgrId Not Allowed!";
                    }
                    else
                    {
                        string? STNo = _IDbFunction.SqlDataView[0]["STNo"].ToString() ?? "";
                        string? STName = _IDbFunction.SqlDataView[0]["STName"].ToString() ?? "";
                        string? LoginTime = (_IDbFunction.SqlDataView[0]["LoginTime"] != DBNull.Value ? Convert.ToDateTime(_IDbFunction.SqlDataView[0]["LoginTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "") ?? "";
                        int AllPower = Convert.ToInt16(_IDbFunction.SqlDataView[0]["AllPower"]);//最高權限者
                        string? CTime = (_IDbFunction.SqlDataView[0]["CTime"] != DBNull.Value ? Convert.ToDateTime(_IDbFunction.SqlDataView[0]["CTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "");
                        string? MTime = (_IDbFunction.SqlDataView[0]["MTime"] != DBNull.Value ? Convert.ToDateTime(_IDbFunction.SqlDataView[0]["MTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "");
                        _UseredItemPower.Status = "SUCCESS";
                        _UseredItemPower.Message = "成功!";
                        _UseredItemPower.MgrUserId = STNo;
                        _UseredItemPower.MgrUserName = STName;
                        _UseredItemPower.MgrUserAllPower = AllPower;
                        _UseredItemPower.MgrUserLoginTime = LoginTime;
                        _UseredItemPower.MgrUserCTime = CTime;
                        _UseredItemPower.MgrUserMTime = MTime;
                        ///最高權限者 項目權限都可使用
                        if (AllPower == 1)
                        {
                            _UseredItemPower.MgrUserPowerList?.Add(new MgrUsersPower()
                            {
                                MgrUserId = MgrId,
                                MgrItemId = ItemId,
                                MgrPV = 1,
                                MgrPA = 1,
                                MgrPD = 1,
                                MgrPU = 1,
                                MgrPG = 1,
                                MgrP1 = 1,
                                MgrP2 = 1,
                            });
                        }
                        else
                        {

                            ///--取得該使用者該項目的權限
                            Sql = "select * from StaffPower where STNo='" + MgrId + "' and MINo='" + ItemId + "'";
                            if (_IDbFunction.SelectDbReader(Sql) && _IDbFunction.SqlServerReader.Read())
                            {
                                _UseredItemPower.MgrUserPowerList?.Add(new MgrUsersPower()
                                {
                                    MgrUserId = MgrId,
                                    MgrItemId = ItemId,
                                    MgrPV = Convert.ToInt16(_IDbFunction.SqlServerReader["PV"]),
                                    MgrPA = Convert.ToInt16(_IDbFunction.SqlServerReader["PA"]),
                                    MgrPD = Convert.ToInt16(_IDbFunction.SqlServerReader["PD"]),
                                    MgrPU = Convert.ToInt16(_IDbFunction.SqlServerReader["PU"]),
                                    MgrPG = Convert.ToInt16(_IDbFunction.SqlServerReader["PG"]),
                                    MgrP1 = Convert.ToInt16(_IDbFunction.SqlServerReader["P1"]),
                                    MgrP2 = Convert.ToInt16(_IDbFunction.SqlServerReader["P2"]),
                                });
                            }
                            _IDbFunction.SqlServerReader.Close();
                        }
                    }
                    _IDbFunction.SqlDataView.Dispose();
                });
            }
            catch { }
            _IDbFunction.DbClose();
            return _UseredItemPower;
        }
    }
}
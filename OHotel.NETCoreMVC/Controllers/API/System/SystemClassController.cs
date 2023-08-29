using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.Data.SqlClient;
using OHotel.NETCoreMVC.DTO;
using OHotel.NETCoreMVC.Helper;
using OHotel.NETCoreMVC.Models;

namespace OHotel.NETCoreMVC.Controllers.API.System
{
    // ※ [ 230628 KEVINN ]: 增加部分JWT Token取得會員資訊去判斷有無特殊權限
    // ╳ 以下是命名路徑位置: (例如) Values/GetClass 而完整的話長得像是這樣: https:// localhost:44367/api/EHotelFood/GetEHotel_Food 的樣子
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SystemClassController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private IVerifyHelper _VerifyHelper;
        private string ItemID { get; set; } = "1";
        private string Sql { get; set; } = "";
        public SystemClassController(IDbFunction dbFunction, IConfiguration configuration, IVerifyHelper verifyHelper)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
            _VerifyHelper = verifyHelper;
        }

        /// <summary>
        /// ※ 檢視及查詢【JWT權限判定】
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(string? search = "", string? column = "MCName", int page = 1, int pageSize = 5, string selectFrom = "ManageClass", string orderBy = "[Order] ASC")
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // 獲得目前頁數及搜尋結果
                        string dataSql = $"SELECT * FROM {selectFrom} WHERE State = 0 AND {column} LIKE '%' + @Search + '%' ORDER BY {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                        string countSql = $"SELECT COUNT(*) FROM {selectFrom} WHERE State = 0 AND {column} LIKE '%' + @Search + '%'";

                        await connection.OpenAsync();

                        using (SqlCommand dataCommand = new SqlCommand(dataSql, connection))
                        using (SqlCommand countCommand = new SqlCommand(countSql, connection))
                        {
                            // SanitizeInput: 防止 SQL 注入，使用參數化查詢處理用戶輸入
                            dataCommand.Parameters.AddWithValue("@Search", $"%{_VerifyHelper.SanitizeInput(search)}%");
                            dataCommand.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                            dataCommand.Parameters.AddWithValue("@PageSize", pageSize);

                            countCommand.Parameters.AddWithValue("@Search", $"%{_VerifyHelper.SanitizeInput(search)}%");

                            using (SqlDataReader dataReader = await dataCommand.ExecuteReaderAsync())
                            {
                                List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();

                                while (await dataReader.ReadAsync())
                                {
                                    var result = Enumerable.Range(0, dataReader.FieldCount).ToDictionary(dataReader.GetName, dataReader.GetValue);
                                    results.Add(result);
                                }

                                int totalCount = (int)await countCommand.ExecuteScalarAsync();
                                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                                var paging = new { TotalPages = totalPages, CurrentPage = page, TotalCount = totalCount };
                                var resultWithPaging = new { Paging = paging, Data = results };

                                return Ok(resultWithPaging);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        /// <summary>
        /// ※ 新增【JWT權限判定】
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddManageClass([FromBody] ManageClassDTO manageClass)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPA == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string insertSql = @"
                            INSERT INTO ManageClass (
                                MCName,
                                MCIcon,
                                MCXtrol,
                                [Order],
                                State,
                                MSNo)
                            VALUES (
                                @MCName,
                                @MCIcon,
                                @MCXtrol,
                                @Order,
                                @State,
                                @MSNo)";

                        using SqlCommand insertCommand = new SqlCommand(insertSql, connection);
                        insertCommand.Parameters.AddWithValue("@MCName", manageClass.MCName);
                        insertCommand.Parameters.AddWithValue("@MCIcon", manageClass.MCIcon);
                        insertCommand.Parameters.AddWithValue("@MCXtrol", manageClass.MCXtrol);
                        insertCommand.Parameters.AddWithValue("@Order", manageClass.Order);
                        insertCommand.Parameters.AddWithValue("@State", manageClass.State);
                        insertCommand.Parameters.AddWithValue("@MSNo", manageClass.MSNo);

                        await connection.OpenAsync();
                        int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return BadRequest();
                        }
                        return Ok();
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// ※ 編輯【JWT權限判定】
        /// </summary>
        [HttpPatch("{mCNo}")]
        public async Task<IActionResult> UpdateManageClass(int mCNo, [FromBody] ManageClassDTO manageClass)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string sql = @"UPDATE ManageClass SET
                        MCName = @MCName,
                        MCXtrol = @MCXtrol,
                        MCIcon = @MCIcon,
                        [Order] = @Order
                        WHERE MCNo = @MCNo";

                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@MCNo", mCNo);
                            command.Parameters.AddWithValue("@MCName", manageClass.MCName);
                            command.Parameters.AddWithValue("@MCXtrol", manageClass.MCXtrol);
                            command.Parameters.AddWithValue("@MCIcon", manageClass.MCIcon);
                            command.Parameters.Add("@Order", SqlDbType.Int).Value = manageClass.Order;
                            await connection.OpenAsync();
                            int result = await command.ExecuteNonQueryAsync();
                            if (result > 0)
                            {
                                return Ok(UserUsedPower);
                            }
                            else
                            {
                                return NotFound(UserUsedPower);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// ※ 取得特定{MCNo}資料 (編輯用)【JWT權限判定】
        /// </summary>
        [HttpGet("{MCNo}")]
        public async Task<IActionResult> GetManageClass_ByMCNo(int MCNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string sql = @"SELECT * FROM ManageClass WHERE MCNo = @MCNo";
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@MCNo", MCNo);
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();
                                while (await reader.ReadAsync())
                                {
                                    var result = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                                    results.Add(result);
                                }
                                return Ok(results);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// ※ 取得控制器資料 (編輯用)【JWT權限判定】
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetManageClass()
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string sql = "SELECT * FROM ManageClass WHERE State = 0 order by ManageClass.[Order] ASC";
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();
                                while (await reader.ReadAsync())
                                {
                                    var result = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                                    results.Add(result);
                                }
                                return Ok(results);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// ※ 多選刪除(更新狀態)【JWT權限判定】
        /// </summary>
        [HttpDelete("batch")]
        public async Task<IActionResult> DeleteManageClassBatch([FromQuery(Name = "MCNo")] List<int> MCNos)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPD == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string deleteSql = "UPDATE ManageClass SET State = 1 WHERE MCNo IN ({0})";
                        string parameterNames = string.Join(", ", MCNos.Select((_, index) => "@MCNo" + index));
                        // 使用 String.Format 將子句中的參數名稱動態替換。避免 SQL 注入攻擊。
                        string formattedDeleteSql = string.Format(deleteSql, parameterNames);

                        using (SqlCommand deleteCommand = new(formattedDeleteSql, connection))
                        {
                            for (int i = 0; i < MCNos.Count; i++)
                            {
                                deleteCommand.Parameters.AddWithValue("@MCNo" + i, MCNos[i]);
                            }

                            await connection.OpenAsync();
                            int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                            connection.Close(); // 關閉 SQL 連接

                            if (rowsAffected == 0)
                            {
                                return NotFound("MCNo可能輸入錯誤");
                            }

                            return Ok(UserUsedPower);
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges"); // 權限不足
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}

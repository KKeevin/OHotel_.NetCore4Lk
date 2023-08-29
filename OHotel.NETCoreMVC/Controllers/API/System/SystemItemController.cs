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
    // ※ [ 230718 KEVINN ]: 最後彙整
    // ※ [ 230628 KEVINN ]: 在該則內容完整增加JWT Token取得會員資訊去判斷有無特殊權限
    // ※ [ 230519 KEVINN ]: 整支SystemItemController有優化過 基本上算是最完善的版本 可複製此當範例套用
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SystemItemController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private IVerifyHelper _VerifyHelper;
        private string ItemID { get; set; } = "2"; 
        private string Sql { get; set; } = "";
        public SystemItemController(IDbFunction dbFunction, IConfiguration configuration, IVerifyHelper verifyHelper)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
            _VerifyHelper = verifyHelper;
        }
        /// <summary>
        /// ※查詢所有資料
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetManageItem()
        { // 使用SqlReader
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        string sql = "SELECT * FROM ManageItem WHERE State = 0 order by [Order] ASC";
                        using (SqlCommand command = new(sql, connection))
                        {
                            await connection.OpenAsync();  // 這邊用了CommandBehavior.CloseConnection，它會在關閉 SqlDataReader 時同時關閉資料庫連線
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
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
                    return BadRequest("權限不足");
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        /// <summary>
        /// ※ 檢視及查詢【JWT權限判定】
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(string? search = "", string? column = "ItemName", int page = 1, int pageSize = 5, string selectFrom = "ManageItem", string orderBy = "ManageItem.[Order] ASC")
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
                        string dataSql = $"SELECT ManageItem.*, ManageClass.MCName FROM {selectFrom} INNER JOIN ManageClass ON (ManageClass.MCNo = ManageItem.MCNo) WHERE ManageItem.State = 0 AND {column} LIKE '%' + @Search + '%' ORDER BY {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

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
                    return BadRequest("權限不足"); // 權限不足Insufficient privileges
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
        public async Task<IActionResult> AddManageItem([FromBody] ManageItemDTO manageItem)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPA == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = @"
                            INSERT INTO ManageItem (
                                MCNo,
                                ItemName,
                                MIAction,
                                PowerView,
                                PowerAdd,
                                PowerDel,
                                PowerUpdate,
                                PowerGrant,
                                Power1,
                                Power2,
                                [Order],
                                State,
                                MSNo)
                            VALUES (
                                @MCNo,
                                @ItemName,
                                @MIAction,
                                @PowerView,
                                @PowerAdd,
                                @PowerDel,
                                @PowerUpdate,
                                @PowerGrant,
                                @Power1,
                                @Power2,
                                @Order,
                                @State,
                                @MSNo)";

                        using (SqlCommand insertCommand = new(Sql, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@MCNo", manageItem.MCNo);
                            insertCommand.Parameters.AddWithValue("@ItemName", manageItem.ItemName);
                            insertCommand.Parameters.AddWithValue("@MIAction", manageItem.MIAction);
                            insertCommand.Parameters.AddWithValue("@PowerView", manageItem.PowerView);
                            insertCommand.Parameters.AddWithValue("@PowerAdd", manageItem.PowerAdd);
                            insertCommand.Parameters.AddWithValue("@PowerDel", manageItem.PowerDel);
                            insertCommand.Parameters.AddWithValue("@PowerUpdate", manageItem.PowerUpdate);
                            insertCommand.Parameters.AddWithValue("@PowerGrant", manageItem.PowerGrant);
                            insertCommand.Parameters.AddWithValue("@Power1", manageItem.Power1);
                            insertCommand.Parameters.AddWithValue("@Power2", manageItem.Power2);
                            insertCommand.Parameters.AddWithValue("@Order", manageItem.Order);
                            insertCommand.Parameters.AddWithValue("@State", manageItem.State);
                            insertCommand.Parameters.AddWithValue("@MSNo", manageItem.MSNo);

                            await connection.OpenAsync();
                            int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                            connection.Close(); // 關閉 SQL 連接

                            if (rowsAffected == 0)
                            {
                                return BadRequest(UserUsedPower);
                            }

                            return Ok(UserUsedPower);
                        }
                    }
                }
                else
                {
                    return BadRequest("權限不足");
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
        [HttpPatch("{MINo}")]
        public async Task<IActionResult> UpdateManageItem(int MINo, [FromBody] ManageItemDTO manageItem)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = @"UPDATE ManageItem SET
                            ItemName = @ItemName,
                            MIAction = @MIAction,
                            PowerView = @PowerView,
                            PowerAdd = @PowerAdd,
                            PowerDel = @PowerDel,
                            PowerUpdate = @PowerUpdate,
                            PowerGrant = @PowerGrant,
                            Power1 = @Power1,
                            Power2 = @Power2,
                            MSNo = @MSNo, 
                            MCNo = @MCNo, 
                            [Order] = @Order
                            WHERE MINo = @MINo";

                        using (SqlCommand command = new(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@MINo", MINo);
                            command.Parameters.AddWithValue("@ItemName", manageItem.ItemName);
                            command.Parameters.AddWithValue("@MIAction", manageItem.MIAction);
                            command.Parameters.AddWithValue("@PowerView", manageItem.PowerView);
                            command.Parameters.AddWithValue("@PowerAdd", manageItem.PowerAdd);
                            command.Parameters.AddWithValue("@PowerDel", manageItem.PowerDel);
                            command.Parameters.AddWithValue("@PowerUpdate", manageItem.PowerUpdate);
                            command.Parameters.AddWithValue("@PowerGrant", manageItem.PowerGrant);
                            command.Parameters.AddWithValue("@Power1", manageItem.Power1);
                            command.Parameters.AddWithValue("@Power2", manageItem.Power2);
                            command.Parameters.AddWithValue("@MSNo", manageItem.MSNo);
                            command.Parameters.AddWithValue("@MCNo", manageItem.MCNo);
                            command.Parameters.AddWithValue("@Order", manageItem.Order);

                            await connection.OpenAsync();
                            int result = await command.ExecuteNonQueryAsync();

                            connection.Close(); // 關閉 SQL 連接

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
                    return BadRequest("權限不足");
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        /// <summary>
        /// ※ 取得特定{MINo}資料 (編輯用)【JWT權限判定】
        /// </summary>
        [HttpGet("{MINo}")]
        public async Task<IActionResult> GetManageItem_ByMINo(int MINo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = @"SELECT * FROM ManageItem WHERE MINo = @MINo";
                        using (SqlCommand command = new(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@MINo", MINo);
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
                            {
                                List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();
                                while (await reader.ReadAsync())
                                {
                                    var result = Enumerable.Range(0, reader.FieldCount)
                                        .ToDictionary(reader.GetName, reader.GetValue);
                                    results.Add(result);
                                }
                                return Ok(results);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("權限不足");
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
                        Sql = "SELECT * FROM ManageClass WHERE State = 0 order by ManageClass.[Order] ASC";
                        using (SqlCommand command = new SqlCommand(Sql, connection))
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
                    return BadRequest("權限不足");
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
        public async Task<IActionResult> DeleteItemClassBatch([FromQuery(Name = "MINo")] List<int> MINos)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPD == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = "UPDATE ManageItem SET State = 1 WHERE MINo IN ({0})";
                        string parameterNames = string.Join(", ", MINos.Select((_, index) => "@MINo" + index));
                        // 使用 String.Format 將子句中的參數名稱動態替換。避免 SQL 注入攻擊。
                        string formattedSql = string.Format(Sql, parameterNames);

                        using (SqlCommand deleteCommand = new(formattedSql, connection))
                        {
                            for (int i = 0; i < MINos.Count; i++)
                            {
                                deleteCommand.Parameters.AddWithValue("@MINo" + i, MINos[i]);
                            }

                            await connection.OpenAsync();
                            int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                            connection.Close(); // 關閉 SQL 連接

                            if (rowsAffected == 0)
                            {
                                return NotFound("MINo可能輸入錯誤");
                            }

                            return Ok(UserUsedPower);
                        }
                    }
                }
                else
                {
                    return BadRequest("權限不足");
                }
            }
            catch (Exception ex)
            {
                return Unauthorized(ex.Message);
            }
        }
        ///// <summary>
        ///// 230621 混合四方Sdk: 檢視的 API (範例參考用)
        ///// </summary>
        //[HttpGet("{tableName}")]
        //public IActionResult GetManageItemEZ(string tableName = "ManageItem")
        //{
        //    string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
        //    List<Dictionary<string, object>> dbInfoList = new List<Dictionary<string, object>>();

        //    try
        //    {
        //        Sql = $"SELECT * FROM {tableName} WHERE State = 0 order by [Order] ASC";
        //        _IDbFunction.DbConnect(connectionString);

        //        if (_IDbFunction.SelectDbDataView(Sql, tableName) && _IDbFunction.SqlDataView.Count > 0)
        //        {
        //            for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
        //            {
        //                var dbInfo = new Dictionary<string, object>();

        //                for (int j = 0; j < _IDbFunction.SqlDataView.Table.Columns.Count; j++)
        //                {
        //                    var columnName = _IDbFunction.SqlDataView.Table.Columns[j].ColumnName;
        //                    var value = _IDbFunction.SqlDataView[i][columnName];
        //                    dbInfo[columnName] = value;
        //                }
        //                dbInfoList.Add(dbInfo);
        //            }
        //        }
        //        else
        //        {
        //            return NotFound();
        //        }
        //    }
        //    catch
        //    {
        //        return BadRequest();
        //    }
        //    finally
        //    {
        //        _IDbFunction.SqlDataView?.Dispose();
        //        _IDbFunction.DbClose();
        //    }
        //    return Ok(dbInfoList);
        //}
        ///// <summary>
        ///// 230621 混合四方Sdk: 權限判斷進行編輯的API 使用了_IDbFunction.AlterDb(Sql)直接帶入直接更新去(範例參考用)
        ///// </summary>
        //[HttpPatch]
        //public async Task<IActionResult> UpdateStaffTel(string Tel = "0988660606")
        //{
        //    string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
        //    string authorizationHeader = HttpContext.Request.Headers["Authorization"];
        //    try
        //    {
        //        string IdentityName = HttpContext.User.Identity?.Name ?? "";
        //        MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr("8", IdentityName.ToString());

        //        if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1)
        //        {
        //            _IDbFunction.DbConnect(connectionString);
        //            Sql = $"UPDATE Staff SET Tel = '{Tel}' WHERE STNo = {IdentityName}";

        //            if (_IDbFunction.AlterDb(Sql))
        //            {
        //                return Ok(UserUsedPower);
        //            }
        //            else
        //            {
        //                return NotFound(UserUsedPower);
        //            }
        //        }
        //        else
        //        {
        //            return BadRequest("權限不足");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Unauthorized(ex.Message);
        //    }
        //    finally
        //    {
        //        _IDbFunction.DbClose();
        //    }
        //}
    }
}

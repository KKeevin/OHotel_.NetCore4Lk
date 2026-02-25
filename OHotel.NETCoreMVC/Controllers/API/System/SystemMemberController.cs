using System.Data;
using EasyCLib.NET.Sdk;
using OHotelCLib.Alenher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using OHotel.NETCoreMVC.DTO;
using OHotel.NETCoreMVC.Helper;
using OHotel.NETCoreMVC.Models;

namespace OHotel.NETCoreMVC.Controllers.API.System
{
    [Authorize]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SystemMemberController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private IVerifyHelper _VerifyHelper;
        private string ItemID { get; set; } = "3"; 
        private string Sql { get; set; } = "";
        private bool IsSqlite => string.Equals(_Configuration["DatabaseProvider"] ?? "", "Sqlite", StringComparison.OrdinalIgnoreCase);
        public SystemMemberController(IDbFunction dbFunction, IConfiguration configuration, IVerifyHelper verifyHelper)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
            _VerifyHelper = verifyHelper;
        }
        /// <summary>
        /// ※ 檢視及查詢【JWT權限判定】
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(string? search = "", string? column = "STName", int page = 1, int pageSize = 5, string selectFrom = "Staff", string orderBy = "STNo ASC")
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1)
                {
                    var safeCol = column == "STNo" || column == "STName" || column == "LoginName" || column == "AllPower" || column == "LoginTime" || column == "LAmount" ? column : "STNo";
                    var searchVal = $"%{_VerifyHelper.SanitizeInput(search)}%";
                    var offset = (page - 1) * pageSize;

                    if (IsSqlite)
                    {
                        _IDbFunction.DbConnect(connectionString ?? "");
                        var dataSql = $"SELECT * FROM {selectFrom} WHERE State = 0 AND {safeCol} LIKE @Search ORDER BY {orderBy} LIMIT @PageSize OFFSET @Offset";
                        var countSql = $"SELECT COUNT(*) FROM {selectFrom} WHERE State = 0 AND {safeCol} LIKE @Search";
                        var prms = new Dictionary<string, object> { ["@Search"] = searchVal, ["@Offset"] = offset, ["@PageSize"] = pageSize };

                        if (!_IDbFunction.SelectDbDataViewWithParams(dataSql, "Data", prms))
                        {
                            _IDbFunction.DbClose();
                            return Ok(new { Paging = new { TotalPages = 0, CurrentPage = page, TotalCount = 0 }, Data = new List<IDictionary<string, object>>() });
                        }
                        var results = new List<IDictionary<string, object>>();
                        for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
                        {
                            var row = _IDbFunction.SqlDataView[i];
                            var dict = new Dictionary<string, object>();
                            foreach (DataColumn col in _IDbFunction.SqlDataView.Table.Columns)
                                dict[col.ColumnName] = row[col.ColumnName] ?? DBNull.Value;
                            results.Add(dict);
                        }
                        _IDbFunction.SelectDbDataViewWithParams(countSql, "Cnt", new Dictionary<string, object> { ["@Search"] = searchVal });
                        var totalCount = _IDbFunction.SqlDataView.Count > 0 ? Convert.ToInt32(_IDbFunction.SqlDataView[0][0]) : 0;
                        _IDbFunction.DbClose();
                        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                        return Ok(new { Paging = new { TotalPages = totalPages, CurrentPage = page, TotalCount = totalCount }, Data = results });
                    }

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        string dataSql = $"SELECT * FROM {selectFrom} WHERE State = 0 AND {column} LIKE '%' + @Search + '%' ORDER BY {orderBy} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                        string countSql = $"SELECT COUNT(*) FROM {selectFrom} WHERE State = 0 AND {column} LIKE '%' + @Search + '%'";

                        using (SqlCommand dataCommand = new(dataSql, connection))
                        using (SqlCommand countCommand = new(countSql, connection))
                        {
                            dataCommand.Parameters.AddWithValue("@Search", searchVal);
                            dataCommand.Parameters.AddWithValue("@Offset", offset);
                            dataCommand.Parameters.AddWithValue("@PageSize", pageSize);
                            countCommand.Parameters.AddWithValue("@Search", searchVal);

                            await connection.OpenAsync();

                            using (SqlDataReader dataReader = await dataCommand.ExecuteReaderAsync())
                            {
                                List<IDictionary<string, object>> results = new();

                                while (await dataReader.ReadAsync())
                                {
                                    var result = Enumerable.Range(0, dataReader.FieldCount).ToDictionary(dataReader.GetName, dataReader.GetValue);
                                    results.Add(result);
                                }

                                await dataReader.CloseAsync();

                                int totalCount = (int)await countCommand.ExecuteScalarAsync();
                                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                                var paging = new { TotalPages = totalPages, CurrentPage = page, TotalCount = totalCount };
                                var resultWithPaging = new { Paging = paging, Data = results };

                                connection.Close();

                                return Ok(resultWithPaging);
                            }
                        }
                    }
                }
                else
                {
                    return BadRequest("Insufficient privileges");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        /// <summary>
        /// ※查詢所有資料
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetManageMember()
        { // 使用SqlReader
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1)
                {
                    if (IsSqlite)
                    {
                        _IDbFunction.DbConnect(connectionString ?? "");
                        if (!_IDbFunction.SelectDbDataView("SELECT * FROM Staff WHERE State = 0 ORDER BY STNo ASC", "Staff"))
                        {
                            _IDbFunction.DbClose();
                            return Ok(new List<IDictionary<string, object>>());
                        }
                        var results = new List<IDictionary<string, object>>();
                        for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
                        {
                            var row = _IDbFunction.SqlDataView[i];
                            var dict = new Dictionary<string, object>();
                            foreach (DataColumn col in _IDbFunction.SqlDataView.Table.Columns)
                                dict[col.ColumnName] = row[col.ColumnName] ?? DBNull.Value;
                            results.Add(dict);
                        }
                        _IDbFunction.DbClose();
                        return Ok(results);
                    }
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = "SELECT * FROM Staff WHERE State = 0 order by STNo ASC";
                        using (SqlCommand command = new(Sql, connection))
                        {
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(global::System.Data.CommandBehavior.CloseConnection))
                            {
                                List<IDictionary<string, object>> results = new();
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
        /// ※獲取會員操作權限
        /// </summary>
        [HttpGet("{STNo}")]
        public async Task<IActionResult> GetStaffPower(int STNo)
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
                        Sql = @"SELECT * FROM StaffPower WHERE STNo = @STNo";
                        using (SqlCommand command = new SqlCommand(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@STNo", STNo);
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(global::System.Data.CommandBehavior.CloseConnection))
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
        /// ※這支參考宇謙版本改寫的
        /// </summary>
        [HttpGet("{STNo}")]
        public async Task<IActionResult> GetStaffPower2(int STNo)
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
                        Sql = @"SELECT Staff.*, StaffPower.* 
                       FROM Staff 
                       INNER JOIN StaffPower ON StaffPower.STNo = Staff.STNo
                       WHERE Staff.State = 0 AND Staff.STNo = @STNo";
                        using (SqlCommand command = new SqlCommand(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@STNo", STNo);
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(global::System.Data.CommandBehavior.CloseConnection))
                            {
                                Staff staff = null;
                                List<StaffPower> staffPowerList = new List<StaffPower>();
                                while (await reader.ReadAsync())
                                {
                                    if (staff == null)
                                    {
                                        staff = new Staff()
                                        {
                                            STNo = Convert.ToInt32(reader["STNo"]),
                                            STName = reader["STName"].ToString(),
                                            LoginName = reader["LoginName"].ToString(),
                                            LoginPasswd = reader["LoginPasswd"].ToString(),
                                            Tel = reader["Tel"].ToString(),
                                            EMail = reader["EMail"].ToString(),
                                            AllPower = Convert.ToInt32(reader["AllPower"]),
                                            LAmount = Convert.ToInt32(reader["LAmount"]),
                                            CTime = reader["CTime"] != DBNull.Value ? Convert.ToDateTime(reader["CTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                            MTime = reader["MTime"] != DBNull.Value ? Convert.ToDateTime(reader["MTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                            StaffPowerList = staffPowerList
                                        };
                                    }

                                    StaffPower staffPower = new StaffPower()
                                    {
                                        SPNo = Convert.ToInt32(reader["SPNo"]),
                                        STNo = Convert.ToInt32(reader["STNo"]),
                                        MINo = Convert.ToInt32(reader["MINo"]),
                                        PV = Convert.ToInt32(reader["PV"]),
                                        PA = Convert.ToInt32(reader["PA"]),
                                        PD = Convert.ToInt32(reader["PD"]),
                                        PU = Convert.ToInt32(reader["PU"]),
                                        PG = Convert.ToInt32(reader["PG"]),
                                        P1 = Convert.ToInt32(reader["P1"]),
                                        P2 = Convert.ToInt32(reader["P2"])
                                    };

                                    staffPowerList.Add(staffPower);
                                }

                                if (staff != null)
                                {
                                    return Ok(staff);
                                }
                                else
                                {
                                    return NotFound(); // Or return any other appropriate HTTP response
                                }
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
        /// ※修改個別會員各別項目權限設置
        /// </summary>
        [HttpPatch("{STNo}/staffpower/{MINo}")]
        public async Task<IActionResult> UpdateStaffPower(int STNo, int MINo, [FromBody] Staff staff)
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
                        await connection.OpenAsync();

                        // Update StaffPower record in the database
                        string updateStaffPowerSql = @"UPDATE StaffPower 
                                       SET PV = @PV, PA = @PA, PD = @PD, PU = @PU, PG = @PG, P1 = @P1, P2 = @P2 
                                       WHERE STNo = @STNo AND MINo = @MINo";
                        using (SqlCommand updateStaffPowerCommand = new SqlCommand(updateStaffPowerSql, connection))
                        {
                            updateStaffPowerCommand.Parameters.AddWithValue("@STNo", STNo);
                            updateStaffPowerCommand.Parameters.AddWithValue("@MINo", MINo);
                            updateStaffPowerCommand.Parameters.AddWithValue("@PV", staff.StaffPowerList.First().PV);
                            updateStaffPowerCommand.Parameters.AddWithValue("@PA", staff.StaffPowerList.First().PA);
                            updateStaffPowerCommand.Parameters.AddWithValue("@PD", staff.StaffPowerList.First().PD);
                            updateStaffPowerCommand.Parameters.AddWithValue("@PU", staff.StaffPowerList.First().PU);
                            updateStaffPowerCommand.Parameters.AddWithValue("@PG", staff.StaffPowerList.First().PG);
                            updateStaffPowerCommand.Parameters.AddWithValue("@P1", staff.StaffPowerList.First().P1);
                            updateStaffPowerCommand.Parameters.AddWithValue("@P2", staff.StaffPowerList.First().P2);

                            int rowsAffected = await updateStaffPowerCommand.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }

                        string updateStaffSql =
                            @"UPDATE Staff 
                        SET STName = @STName, LoginName = @LoginName, 
                        Tel = @Tel, EMail = @EMail, AllPower = @AllPower, MTime = @MTime
                    WHERE STNo = @STNo";
                        // LoginPasswd = @LoginPasswd, 
                        using (SqlCommand updateStaffCommand = new SqlCommand(updateStaffSql, connection))
                        {
                            updateStaffCommand.Parameters.AddWithValue("@STNo", STNo);
                            updateStaffCommand.Parameters.AddWithValue("@STName", staff.STName);
                            updateStaffCommand.Parameters.AddWithValue("@LoginName", staff.LoginName);
                            //updateStaffCommand.Parameters.AddWithValue("@LoginPasswd", staff.LoginPasswd);
                            updateStaffCommand.Parameters.AddWithValue("@Tel", staff.Tel);
                            updateStaffCommand.Parameters.AddWithValue("@EMail", staff.EMail);
                            updateStaffCommand.Parameters.AddWithValue("@AllPower", staff.AllPower);
                            updateStaffCommand.Parameters.AddWithValue("@MTime", staff.MTime);

                            int rowsAffected = await updateStaffCommand.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }
                        }

                        // Retrieve the updated Staff record with StaffPower information
                        string selectStaffSql = @"SELECT Staff.*, StaffPower.* 
                                  FROM Staff 
                                  INNER JOIN StaffPower ON StaffPower.STNo = Staff.STNo
                                  WHERE Staff.State = 0 AND Staff.STNo = @STNo";
                        using (SqlCommand selectStaffCommand = new SqlCommand(selectStaffSql, connection))
                        {
                            selectStaffCommand.Parameters.AddWithValue("@STNo", STNo);

                            using (SqlDataReader reader = await selectStaffCommand.ExecuteReaderAsync())
                            {
                                Staff updatedStaff = null;
                                List<StaffPower> staffPowerList = new List<StaffPower>();
                                while (await reader.ReadAsync())
                                {
                                    if (updatedStaff == null)
                                    {
                                        updatedStaff = new Staff()
                                        {
                                            STNo = Convert.ToInt32(reader["STNo"]),
                                            STName = reader["STName"].ToString(),
                                            LoginName = reader["LoginName"].ToString(),
                                            LoginPasswd = reader["LoginPasswd"].ToString(),
                                            Tel = reader["Tel"].ToString(),
                                            EMail = reader["EMail"].ToString(),
                                            AllPower = Convert.ToInt32(reader["AllPower"]),
                                            LAmount = Convert.ToInt32(reader["LAmount"]),
                                            CTime = reader["CTime"] != DBNull.Value ? Convert.ToDateTime(reader["CTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                            MTime = reader["MTime"] != DBNull.Value ? Convert.ToDateTime(reader["MTime"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                            StaffPowerList = staffPowerList
                                        };
                                    }

                                    StaffPower updatedStaffPower = new StaffPower()
                                    {
                                        SPNo = Convert.ToInt32(reader["SPNo"]),
                                        STNo = Convert.ToInt32(reader["STNo"]),
                                        MINo = Convert.ToInt32(reader["MINo"]),
                                        PV = Convert.ToInt32(reader["PV"]),
                                        PA = Convert.ToInt32(reader["PA"]),
                                        PD = Convert.ToInt32(reader["PD"]),
                                        PU = Convert.ToInt32(reader["PU"]),
                                        PG = Convert.ToInt32(reader["PG"]),
                                        P1 = Convert.ToInt32(reader["P1"]),
                                        P2 = Convert.ToInt32(reader["P2"])
                                    };

                                    staffPowerList.Add(updatedStaffPower);
                                }

                                if (updatedStaff != null)
                                {
                                    return Ok(updatedStaff);
                                }
                                else
                                {
                                    return NotFound();
                                }
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
        /// ※權限編輯
        /// </summary>
        [HttpGet("{STNo}/{MINo}")]
        public async Task<IActionResult> GetOrUpdateStaffPower(int STNo, int MINo, int? PV, int? PA, int? PD, int? PU, int? PG, int? P1, int? P2)
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
                        string selectSql = @"SELECT * FROM StaffPower WHERE STNo = @STNo AND MINo = @MINo";
                        string insertSql = @"INSERT INTO StaffPower (STNo, MINo, PV, PA, PD, PU, PG, P1, P2) VALUES (@STNo, @MINo, @PV, @PA, @PD, @PU, @PG, @P1, @P2)";
                        string updateSql = @"UPDATE StaffPower SET {0} WHERE STNo = @STNo AND MINo = @MINo";
                        string deleteSql = @"DELETE FROM StaffPower WHERE STNo = @STNo AND MINo = @MINo";

                        using (SqlCommand selectCommand = new(selectSql, connection))
                        using (SqlCommand insertCommand = new(insertSql, connection))
                        using (SqlCommand updateCommand = new(updateSql, connection))
                        using (SqlCommand deleteCommand = new(deleteSql, connection))
                        {
                            selectCommand.Parameters.AddWithValue("@STNo", STNo);
                            selectCommand.Parameters.AddWithValue("@MINo", MINo);

                            insertCommand.Parameters.AddWithValue("@STNo", STNo);
                            insertCommand.Parameters.AddWithValue("@MINo", MINo);
                            insertCommand.Parameters.AddWithValue("@PV", PV != null ? PV : (object)0);
                            insertCommand.Parameters.AddWithValue("@PA", PA != null ? PA : (object)0);
                            insertCommand.Parameters.AddWithValue("@PD", PD != null ? PD : (object)0);
                            insertCommand.Parameters.AddWithValue("@PU", PU != null ? PU : (object)0);
                            insertCommand.Parameters.AddWithValue("@PG", PG != null ? PG : (object)0);
                            insertCommand.Parameters.AddWithValue("@P1", P1 != null ? P1 : (object)0);
                            insertCommand.Parameters.AddWithValue("@P2", P2 != null ? P2 : (object)0);

                            updateCommand.Parameters.AddWithValue("@STNo", STNo);
                            updateCommand.Parameters.AddWithValue("@MINo", MINo);
                            updateCommand.Parameters.AddWithValue("@PV", PV ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@PA", PA ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@PD", PD ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@PU", PU ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@PG", PG ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@P1", P1 ?? (object)DBNull.Value);
                            updateCommand.Parameters.AddWithValue("@P2", P2 ?? (object)DBNull.Value);

                            deleteCommand.Parameters.AddWithValue("@STNo", STNo);
                            deleteCommand.Parameters.AddWithValue("@MINo", MINo);

                            try
                            {
                                await connection.OpenAsync();

                                using (SqlDataReader reader = await selectCommand.ExecuteReaderAsync(global::System.Data.CommandBehavior.CloseConnection))
                                {
                                    if (reader.HasRows)
                                    {
                                        if (PV == null && PA == null && PD == null && PU == null && PG == null && P1 == null && P2 == null)
                                        {
                                            await deleteCommand.ExecuteNonQueryAsync();
                                            return Ok("Data deleted");
                                        }
                                        else
                                        {
                                            List<string> updateColumns = new List<string>();
                                            if (PV != null)
                                            {
                                                updateColumns.Add("PV = @PV");
                                            }
                                            if (PA != null)

                                            {
                                                updateColumns.Add("PA = @PA");
                                            }
                                            if (PD != null)
                                            {
                                                updateColumns.Add("PD = @PD");
                                            }
                                            if (PU != null)
                                            {
                                                updateColumns.Add("PU = @PU");
                                            }
                                            if (PG != null)
                                            {
                                                updateColumns.Add("PG = @PG");
                                            }
                                            if (P1 != null)
                                            {
                                                updateColumns.Add("P1 = @P1");
                                            }
                                            if (P2 != null)
                                            {
                                                updateColumns.Add("P2 = @P2");
                                            }

                                            string updateColumnsSql = string.Join(", ", updateColumns);
                                            string formattedUpdateSql = string.Format(updateSql, updateColumnsSql);
                                            updateCommand.CommandText = formattedUpdateSql;

                                            await updateCommand.ExecuteNonQueryAsync();
                                            return Ok("Existing data updated");
                                        }
                                    }
                                    else
                                    {
                                        if (PV == null && PA == null && PD == null && PU == null && PG == null && P1 == null && P2 == null)
                                        {
                                            await deleteCommand.ExecuteNonQueryAsync();
                                            return Ok("Data deleted");
                                        }
                                        else
                                        {
                                            await insertCommand.ExecuteNonQueryAsync();
                                            return Ok("New data created");
                                        }
                                    }
                                }
                            }
                            catch (SqlException ex)
                            {
                                return BadRequest(ex.Message);
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
        /// ※透過編號查詢
        /// </summary>
        [HttpGet("{STNo}")]
        public async Task<IActionResult> GetManageMember_BySTNo(int STNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPV == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new(connectionString))
                    {
                        Sql = @"SELECT * FROM Staff WHERE STNo = @STNo";
                        using (SqlCommand command = new(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@STNo", STNo);
                            await connection.OpenAsync();
                            using (SqlDataReader reader = await command.ExecuteReaderAsync(global::System.Data.CommandBehavior.CloseConnection))
                            {
                                List<IDictionary<string, object>> results = new();
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
        /// ※新增
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddStaff([FromBody] StaffDTO staff)
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
                        INSERT INTO Staff (
                            STName,
                            LoginName,
                            LoginPasswd,
                            Tel,
                            EMail,
                            State,
                            AllPower,
                            LoginTime,
                            LAmount,
                            CTime,
                            MTime,
                            MSNo)
                        VALUES (
                            @STName,
                            @LoginName,
                            @LoginPasswd,
                            @Tel,
                            @EMail,
                            @State,
                            @AllPower,
                            @LoginTime,
                            @LAmount,
                            @CTime,
                            @MTime,
                            @MSNo)";

                        using (SqlCommand insertCommand = new(Sql, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@STName", staff.STName);
                            insertCommand.Parameters.AddWithValue("@LoginName", staff.LoginName);
                            insertCommand.Parameters.AddWithValue("@LoginPasswd", staff.LoginPasswd);
                            insertCommand.Parameters.AddWithValue("@Tel", staff.Tel);
                            insertCommand.Parameters.AddWithValue("@EMail", staff.EMail);
                            insertCommand.Parameters.AddWithValue("@State", staff.State);
                            insertCommand.Parameters.AddWithValue("@AllPower", staff.AllPower);
                            insertCommand.Parameters.AddWithValue("@LoginTime", staff.LoginTime);
                            insertCommand.Parameters.AddWithValue("@LAmount", staff.LAmount);
                            insertCommand.Parameters.AddWithValue("@CTime", staff.CTime);
                            insertCommand.Parameters.AddWithValue("@MTime", staff.MTime);
                            insertCommand.Parameters.AddWithValue("@MSNo", staff.MSNo);

                            await connection.OpenAsync();
                            int rowsAffected = await insertCommand.ExecuteNonQueryAsync();

                            connection.Close();

                            if (rowsAffected == 0)
                            {
                                return BadRequest();
                            }
                            return Ok();
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
        /// ※修改
        /// </summary>
        [HttpPatch("{STNo}")]
        public async Task<IActionResult> UpdateStaff(int STNo, [FromBody] StaffDTO staff)
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
                        Sql = @"UPDATE Staff SET
                        STName = @STName,
                        LoginName = @LoginName,
                        LoginPasswd = @LoginPasswd,
                        Tel = @Tel,
                        EMail = @EMail,
                        State = @State,
                        AllPower = @AllPower,
                        LoginTime = @LoginTime,
                        LAmount = @LAmount,
                        CTime = @CTime,
                        MTime = @MTime,
                        MSNo = @MSNo
                        WHERE STNo = @STNo";

                        using (SqlCommand command = new(Sql, connection))
                        {
                            command.Parameters.AddWithValue("@STNo", STNo);
                            command.Parameters.AddWithValue("@STName", staff.STName);
                            command.Parameters.AddWithValue("@LoginName", staff.LoginName);
                            command.Parameters.AddWithValue("@LoginPasswd", staff.LoginPasswd);
                            command.Parameters.AddWithValue("@Tel", staff.Tel);
                            command.Parameters.AddWithValue("@EMail", staff.EMail);
                            command.Parameters.AddWithValue("@State", staff.State);
                            command.Parameters.AddWithValue("@AllPower", staff.AllPower);
                            command.Parameters.AddWithValue("@LoginTime", staff.LoginTime);
                            command.Parameters.AddWithValue("@LAmount", staff.LAmount);
                            command.Parameters.AddWithValue("@CTime", staff.CTime);
                            command.Parameters.AddWithValue("@MTime", staff.MTime);
                            command.Parameters.AddWithValue("@MSNo", staff.MSNo);

                            await connection.OpenAsync();
                            int result = await command.ExecuteNonQueryAsync();

                            connection.Close();

                            if (result > 0)
                            {
                                return Ok();
                            }
                            else
                            {
                                return NotFound();
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
        [EnableCors("CorsPolicy")]
        [HttpPut("{STNo}")]
        public async Task<IActionResult> EditStaffInfo(int STNo, [FromBody] StaffInfoDTO staffInfoDto)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = @"UPDATE Staff SET
                                    STName = @STName,
                                    Tel = @Tel,
                                    AllPower = @AllPower,
                                    EMail = @EMail,
                                    MTime = GETDATE()
                                    WHERE STNo = @STNo";
                        command.Parameters.AddWithValue("@STName", staffInfoDto.STName);
                        command.Parameters.AddWithValue("@Tel", staffInfoDto.Tel);
                        command.Parameters.AddWithValue("@AllPower", staffInfoDto.AllPower);
                        command.Parameters.AddWithValue("@EMail", staffInfoDto.EMail);
                        command.Parameters.AddWithValue("@STNo", STNo);

                        await connection.OpenAsync();
                        int result = await command.ExecuteNonQueryAsync();

                        connection.Close();

                        if (result > 0)
                        {
                            return Ok();
                        }
                        else
                        {
                            return NotFound();
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
        /// ※修改密碼
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpPost("{STNo}/{newLoginPasswd}")]
        public async Task<IActionResult> EditLoginPasswd(string STNo, string newLoginPasswd)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            try
            {
                string IdentityName = HttpContext.User.Identity?.Name ?? "";
                MgrUseredItemPower UserUsedPower = await _VerifyHelper.VerifyJwtMgr(ItemID, IdentityName.ToString()); // 將這裡寫項目編號

                if (UserUsedPower.MgrUserPowerList?.Count > 0 && UserUsedPower.MgrUserPowerList?.ToList()[0].MgrPU == 1) // 哪個權限開放便能操作
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE Staff SET LoginPasswd = @LoginPasswd WHERE STNo = @STNo";
                        var newPassword = EandD_Reply.StaffSet(newLoginPasswd, 1, 1);
                        command.Parameters.AddWithValue("@LoginPasswd", newPassword);
                        command.Parameters.AddWithValue("@STNo", STNo);

                        await connection.OpenAsync();
                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("LoginPasswd updated successfully");
                        }
                        else
                        {
                            return NotFound("Staff not found");
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
        [EnableCors("CorsPolicy")]
        [HttpDelete("batch")]
        public async Task<IActionResult> DeleteStaffBatch([FromQuery(Name = "STNo")] List<int> STNos)
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
                        Sql = "UPDATE Staff SET State = 1 WHERE STNo IN ({0})";
                        string parameterNames = string.Join(", ", STNos.Select((_, index) => "@STNo" + index));
                        // 使用 String.Format 將子句中的參數名稱動態替換。避免 SQL 注入攻擊。
                        string formattedSql = string.Format(Sql, parameterNames);

                        using (SqlCommand deleteCommand = new(formattedSql, connection))
                        {
                            for (int i = 0; i < STNos.Count; i++)
                            {
                                deleteCommand.Parameters.AddWithValue("@STNo" + i, STNos[i]);
                            }

                            await connection.OpenAsync();
                            int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();

                            connection.Close();

                            if (rowsAffected == 0)
                            {
                                return NotFound();
                            }

                            return NoContent();
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
    }
}

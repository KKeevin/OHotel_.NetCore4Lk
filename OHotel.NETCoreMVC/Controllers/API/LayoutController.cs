using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using DataView = System.Data.DataView;
using DataColumn = System.Data.DataColumn;
using DataRowView = System.Data.DataRowView;

namespace OHotel.NETCoreMVC.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class LayoutController : ControllerBase
    {
        private readonly IDbFunction _IDbFunction;
        private readonly IConfiguration _Configuration;

        public LayoutController(IDbFunction dbFunction, IConfiguration configuration)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
        }

        private bool IsSqlite => string.Equals(_Configuration["DatabaseProvider"] ?? "", "Sqlite", StringComparison.OrdinalIgnoreCase);

        /// <summary>※查詢所有 ManageClass</summary>
        [EnableCors("CorsPolicy")]
        [HttpGet]
        public IActionResult GetManageClass()
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            if (IsSqlite)
            {
                _IDbFunction.DbConnect(connStr ?? "");
                if (!_IDbFunction.SelectDbDataView("SELECT * FROM ManageClass WHERE State = 0 ORDER BY MCNo ASC", "ManageClass"))
                {
                    _IDbFunction.DbClose();
                    return Ok(new List<IDictionary<string, object>>());
                }
                var results = DataViewToList(_IDbFunction.SqlDataView);
                _IDbFunction.DbClose();
                return Ok(results);
            }
            return RunSqlServer(async (conn) =>
            {
                using var cmd = new SqlCommand("SELECT * FROM ManageClass WHERE State = 0 ORDER BY ManageClass.[Order] ASC", conn);
                using var reader = await cmd.ExecuteReaderAsync();
                return Ok(ReaderToList(reader));
            });
        }

        /// <summary>※獲取登入的會員名稱</summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public IActionResult GetSTNameBySTNo(int STNo)
        {
            if (IsSqlite)
            {
                var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                _IDbFunction.DbConnect(connStr ?? "");
                var ok = _IDbFunction.SelectDbDataViewWithParams("SELECT STName FROM Staff WHERE STNo = @STNo", "Staff", new Dictionary<string, object> { ["@STNo"] = STNo });
                _IDbFunction.DbClose();
                if (ok && _IDbFunction.SqlDataView.Count > 0)
                    return Ok(_IDbFunction.SqlDataView[0]["STName"]?.ToString() ?? "Unknown");
                return NotFound("Unknown");
            }
            return RunSqlServerSync((conn) =>
            {
                using var cmd = new SqlCommand("SELECT STName FROM Staff WHERE STNo = @STNo", conn);
                cmd.Parameters.AddWithValue("@STNo", STNo);
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? Ok(reader.GetString(0)) : NotFound("Unknown");
            });
        }

        /// <summary>※取得 SIDEBAR 選單（依權限）</summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public IActionResult GetManageClassAndItemsBySTNo(int STNo)
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            if (IsSqlite)
            {
                _IDbFunction.DbConnect(connStr ?? "");
                var allPowerOk = _IDbFunction.SelectDbDataViewWithParams("SELECT AllPower FROM Staff WHERE STNo = @STNo", "Staff", new Dictionary<string, object> { ["@STNo"] = STNo });
                var allPower = (allPowerOk && _IDbFunction.SqlDataView.Count > 0) ? Convert.ToInt32(_IDbFunction.SqlDataView[0]["AllPower"]) : 0;
                string sql;
                bool ok;
                if (allPower == 0)
                {
                    sql = @"SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
WHERE MC.State = 0 AND MI.State = 0 AND MI.MINo IN (SELECT MINo FROM StaffPower WHERE STNo = @STNo AND PV = 1)
ORDER BY MC.MCNo ASC, MI.MINo ASC";
                    ok = _IDbFunction.SelectDbDataViewWithParams(sql, "Menu", new Dictionary<string, object> { ["@STNo"] = STNo });
                }
                else
                {
                    sql = @"SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
WHERE MC.State = 0 AND MI.State = 0
ORDER BY MC.MCNo ASC, MI.MINo ASC";
                    ok = _IDbFunction.SelectDbDataView(sql, "Menu");
                }
                var results = new Dictionary<string, List<IDictionary<string, object>>>();
                if (ok && _IDbFunction.SqlDataView.Count > 0)
                {
                    foreach (DataRowView row in _IDbFunction.SqlDataView)
                    {
                        var mcName = row["MCName"]?.ToString() ?? "";
                        if (!results.ContainsKey(mcName)) results[mcName] = new List<IDictionary<string, object>>();
                        var dict = new Dictionary<string, object>();
                        var tbl = _IDbFunction.SqlDataView.Table;
                        if (tbl != null)
                            foreach (DataColumn col in tbl.Columns)
                            dict[col.ColumnName] = row[col.ColumnName] ?? DBNull.Value;
                        results[mcName].Add(dict);
                    }
                }
                _IDbFunction.DbClose();
                return Ok(results);
            }
            return RunSqlServer(async (conn) =>
            {
                using var checkCmd = new SqlCommand("SELECT AllPower FROM Staff WHERE STNo = @STNo", conn);
                checkCmd.Parameters.AddWithValue("@STNo", STNo);
                var allPower = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                string sql = allPower == 0
                    ? @"SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
WHERE MC.State = 0 AND MC.MCNo IN (SELECT MCNo FROM ManageItem WHERE MINo IN (SELECT MINo FROM StaffPower WHERE STNo = @STNo AND PV = 1) AND State = 0)
AND MI.State = 0 AND MI.MINo IN (SELECT MINo FROM StaffPower WHERE STNo = @STNo AND PV = 1)
ORDER BY MC.[Order] ASC"
                    : @"SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
WHERE MC.State = 0 AND MI.State = 0
ORDER BY MC.[Order] ASC";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@STNo", STNo);
                using var reader = await cmd.ExecuteReaderAsync();
                var results = new Dictionary<string, List<IDictionary<string, object>>>();
                while (await reader.ReadAsync())
                {
                    var mcName = reader.GetString(reader.GetOrdinal("MCName"));
                    if (!results.ContainsKey(mcName)) results[mcName] = new List<IDictionary<string, object>>();
                    results[mcName].Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue));
                }
                return Ok(results);
            });
        }

        /// <summary>※透過編號查詢 ManageClass</summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("MCNo/{MCNo}")]
        public IActionResult GetManageClass_ByMCNo(int MCNo)
        {
            if (IsSqlite)
            {
                var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                _IDbFunction.DbConnect(connStr ?? "");
                var ok = _IDbFunction.SelectDbDataViewWithParams("SELECT * FROM ManageClass WHERE MCNo = @MCNo", "MC", new Dictionary<string, object> { ["@MCNo"] = MCNo });
                _IDbFunction.DbClose();
                return Ok(ok && _IDbFunction.SqlDataView.Count > 0 ? DataViewToList(_IDbFunction.SqlDataView) : new List<IDictionary<string, object>>());
            }
            return RunSqlServer(async (conn) =>
            {
                using var cmd = new SqlCommand("SELECT * FROM ManageClass WHERE MCNo = @MCNo", conn);
                cmd.Parameters.AddWithValue("@MCNo", MCNo);
                using var reader = await cmd.ExecuteReaderAsync();
                var list = ReaderToList(reader);
                return Ok(list);
            });
        }

        private static List<IDictionary<string, object>> DataViewToList(DataView dv)
        {
            var list = new List<IDictionary<string, object>>();
            var table = dv?.Table;
            if (table == null) return list;
            for (int i = 0; i < dv!.Count; i++)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                    dict[col.ColumnName] = dv[i][col.ColumnName] ?? DBNull.Value;
                list.Add(dict);
            }
            return list;
        }

        private static List<IDictionary<string, object>> ReaderToList(SqlDataReader reader)
        {
            var list = new List<IDictionary<string, object>>();
            while (reader.Read())
                list.Add(Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => reader.GetValue(i)));
            return list;
        }

        private IActionResult RunSqlServer(Func<SqlConnection, Task<IActionResult>> fn)
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using var conn = new SqlConnection(connStr);
            conn.Open();
            return fn(conn).GetAwaiter().GetResult();
        }

        private IActionResult RunSqlServerSync(Func<SqlConnection, IActionResult> fn)
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using var conn = new SqlConnection(connStr);
            conn.Open();
            return fn(conn);
        }

        private readonly string storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Happy");

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("未上傳檔案");

                const long maxFileSize = 10 * 1024 * 1024; // 10 MB
                if (file.Length > maxFileSize)
                    return BadRequest("檔案大小不可超過 10 MB");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx" };
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    return BadRequest($"僅允許上傳以下格式: {string.Join(", ", allowedExtensions)}");

                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(storagePath, fileName);
                Directory.CreateDirectory(storagePath);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(fileName);
            }
            catch (Exception ex)
            {
                // 處理例外
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }




    }
}

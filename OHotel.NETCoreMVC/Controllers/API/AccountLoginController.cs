using EasyCLib.NET.Sdk;
using OHotelCLib.Alenher;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using OHotel.NETCoreMVC.Models;
using OHotel.NETCoreMVC.Helper;
using OHotel.NETCoreMVC.Data;

namespace OHotel.NETCoreMVC.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountLoginController : ControllerBase
    {
        private readonly IDbFunction _IDbFunction;
        private readonly IConfiguration _Configuration;
        private readonly IWebHostEnvironment _Env;
        private readonly ILogger<AccountLoginController> _logger;
        private string Sql { get; set; } = "";

        public AccountLoginController(IDbFunction dbFunction, IConfiguration configuration, IWebHostEnvironment env, ILogger<AccountLoginController> logger)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
            _Env = env;
            _logger = logger;
        }

        private string SignKey => _Configuration.GetSection("JwtSettings:SignKey").Value ?? throw new InvalidOperationException("JwtSettings:SignKey not configured.");
        private string Issuer => _Configuration.GetSection("JwtSettings:Issuer").Value ?? "JWT.JXINFO";

        [EnableCors("CorsPolicy")]
        [HttpPost]
        public IActionResult Login([FromForm] string? LoginName, [FromForm] string? LoginPasswd)
        {
            if (string.IsNullOrWhiteSpace(LoginName))
                return BadRequest(new { message = "帳號為必填" });
            if (string.IsNullOrWhiteSpace(LoginPasswd))
                return BadRequest(new { message = "密碼為必填" });
            if (LoginName.Length > 50)
                return BadRequest(new { message = "帳號長度不可超過 50 字元" });

            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            if (string.IsNullOrEmpty(connStr))
                return BadRequest("Database not configured");

            try
            {
                _IDbFunction.DbConnect(connStr);
                if (ValidateUser(LoginName, LoginPasswd))
                {
                    int STNo = GetSTNoByLoginName(LoginName);
                    _IDbFunction.DbClose();
                    _logger.LogInformation("登入成功: {LoginName}", LoginName);
                    return Ok(GetToken(STNo));
                }
                _logger.LogWarning("登入失敗: {LoginName}", LoginName);
                // 開發模式：回傳除錯資訊
                if (_Env.IsDevelopment())
                {
                    var enc = EandD_Reply.StaffSet(LoginPasswd ?? "", 1, 1);
                    _IDbFunction.SelectDbDataViewWithParams("SELECT STNo, LoginName, LoginPasswd FROM Staff WHERE LoginName = @LoginName", "Staff", new Dictionary<string, object> { ["@LoginName"] = LoginName ?? "" });
                    var cnt = _IDbFunction.SqlDataView.Count;
                    var dbPwd = cnt > 0 ? _IDbFunction.SqlDataView[0]["LoginPasswd"]?.ToString() : "(無)";
                    var match = dbPwd == enc ? "是" : "否";
                    return BadRequest(new { message = "Invalid credentials", debug = new { staffFound = cnt, encLen = enc?.Length ?? 0, dbPwdLen = dbPwd?.Length ?? 0, pwdMatch = match } });
                }
            }
            catch (Exception ex) when (_Env.IsDevelopment())
            {
                _IDbFunction.DbClose();
                return BadRequest(new { message = "Invalid credentials", debug = new { error = ex.Message } });
            }
            finally { _IDbFunction.DbClose(); }

            return BadRequest("Invalid credentials");
        }
        [HttpPost]
        public IActionResult GetOriginPWD([FromForm] string EncodePasswd) 
        { // 密碼轉回正
            var Password = EandD_Reply.StaffSet(EncodePasswd, 2, 1);

            return Ok(Password);
        }

        /// <summary>
        /// 診斷：檢查資料庫連線與 Staff 資料（開發除錯用）
        /// </summary>
        [HttpGet]
        public IActionResult Diagnostics()
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            var dbPath = connStr?.Contains("ohotel.db") == true ? connStr.Replace("Data Source=", "").Trim() : "";
            var exists = !string.IsNullOrEmpty(dbPath) && global::System.IO.File.Exists(dbPath);
            try
            {
                _IDbFunction.DbConnect(connStr ?? "");
                _IDbFunction.SelectDbDataView("SELECT STNo, LoginName, STName, LENGTH(LoginPasswd) as PwdLen FROM Staff", "Staff");
                var rows = new List<object>();
                for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
                {
                    var r = _IDbFunction.SqlDataView[i];
                    rows.Add(new { STNo = r["STNo"], LoginName = r["LoginName"]?.ToString(), STName = r["STName"]?.ToString(), PwdLen = r["PwdLen"] });
                }
                _IDbFunction.DbClose();
                return Ok(new { connStr = connStr?.Length > 20 ? "***" + connStr[^20..] : "***", dbPath, exists, staffCount = rows.Count, staff = rows });
            }
            catch (Exception ex)
            {
                _IDbFunction.DbClose();
                return Ok(new { connStr = "***", dbPath, exists, error = ex.Message });
            }
        }

        /// <summary>
        /// 重設 admin 密碼為 admin123（開發除錯用）
        /// </summary>
        [HttpPost]
        public IActionResult ResetAdminPassword()
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            if (string.IsNullOrEmpty(connStr)) return BadRequest("Database not configured");
            try
            {
                _IDbFunction.DbConnect(connStr);
                var enc = EandD_Reply.StaffSet("admin123", 1, 1).Replace("'", "''");
                var sql = $"UPDATE Staff SET LoginPasswd = '{enc}' WHERE LoginName = 'admin'";
                var ok = _IDbFunction.AlterDb(sql);
                _IDbFunction.DbClose();
                return ok ? Ok(new { message = "密碼已重設為 admin123" }) : StatusCode(500, "更新失敗");
            }
            catch (Exception ex)
            {
                _IDbFunction.DbClose();
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 刪除 admin 後重新建立（開發除錯用，確保使用正確資料庫）
        /// </summary>
        [HttpPost]
        public IActionResult RecreateAdmin()
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            if (string.IsNullOrEmpty(connStr)) return BadRequest("Database not configured");
            try
            {
                _IDbFunction.DbConnect(connStr);
                _IDbFunction.AlterDb("DELETE FROM Staff WHERE LoginName = 'admin'");
                var enc = EandD_Reply.StaffSet("admin123", 1, 1).Replace("'", "''");
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var ins = $"INSERT INTO Staff (STName, LoginName, LoginPasswd, Tel, EMail, State, AllPower, LoginTime, LAmount, CTime, MTime, MSNo) VALUES ('系統管理員', 'admin', '{enc}', '', '', 0, 1, '{now}', 0, '{now}', '{now}', 0)";
                var ok = _IDbFunction.AlterDb(ins);
                _IDbFunction.DbClose();
                return ok ? Ok(new { message = "admin 已重新建立", loginName = "admin", password = "admin123" }) : StatusCode(500, "新增失敗");
            }
            catch (Exception ex)
            {
                _IDbFunction.DbClose();
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// 建立首個管理員帳號（僅在 Staff 表為空時可用）
        /// 帳號: admin / 密碼需至少 8 字元且含英文與數字
        /// </summary>
        [HttpPost]
        public IActionResult CreateFirstAdmin([FromForm] string? password = null)
        {
            var pwd = password ?? "admin123";
            var (isValid, errorMsg) = PasswordPolicy.Validate(pwd);
            if (!isValid)
                return BadRequest(new { message = errorMsg });
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel")
                ?? throw new InvalidOperationException("SQLCD_Read_OHotel not configured.");
            var dbProvider = _Configuration["DatabaseProvider"] ?? "";

            try
            {
                _IDbFunction.DbConnect(connStr);

                // SQLite: 建立 Staff 表與選單相關表（若不存在）
                if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    var createTableSql = @"
CREATE TABLE IF NOT EXISTS Staff (
    STNo INTEGER PRIMARY KEY AUTOINCREMENT,
    STName TEXT,
    LoginName TEXT,
    LoginPasswd TEXT,
    Tel TEXT,
    EMail TEXT,
    State INTEGER DEFAULT 0,
    AllPower INTEGER DEFAULT 0,
    LoginTime TEXT,
    LAmount INTEGER DEFAULT 0,
    CTime TEXT,
    MTime TEXT,
    MSNo INTEGER DEFAULT 0
)";
                    if (!_IDbFunction.AlterDb(createTableSql))
                        return StatusCode(500, "無法建立 Staff 資料表");

                    _IDbFunction.DbClose();
                    SqliteDbInitializer.EnsureTablesAndSeed(_IDbFunction, connStr);
                    _IDbFunction.DbConnect(connStr);
                }

                // 檢查是否已有 Staff
                if (!_IDbFunction.SelectDbDataView("SELECT COUNT(*) as cnt FROM Staff", "Staff"))
                    return StatusCode(500, "無法查詢 Staff 資料表");

                if (_IDbFunction.SqlDataView.Count > 0)
                {
                    var count = Convert.ToInt32(_IDbFunction.SqlDataView[0]["cnt"]);
                    if (count > 0)
                    {
                        _IDbFunction.DbClose();
                        return BadRequest("已有管理員帳號，無法重複建立");
                    }
                }

                // 建立首個管理員
                var plainPwd = pwd;
                var encryptedPwd = EandD_Reply.StaffSet(plainPwd, 1, 1);
                var escapedPwd = encryptedPwd.Replace("'", "''");
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var insertSql = dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
                    ? $@"INSERT INTO Staff (STName, LoginName, LoginPasswd, Tel, EMail, State, AllPower, LoginTime, LAmount, CTime, MTime, MSNo)
VALUES ('系統管理員', 'admin', '{escapedPwd}', '', '', 0, 1, '{now}', 0, '{now}', '{now}', 0)"
                    : $@"INSERT INTO Staff (STName, LoginName, LoginPasswd, Tel, EMail, State, AllPower, LoginTime, LAmount, CTime, MTime, MSNo)
VALUES (N'系統管理員', N'admin', N'{escapedPwd}', N'', N'', 0, 1, '{now}', 0, '{now}', '{now}', 0)";

                if (!_IDbFunction.AlterDb(insertSql))
                {
                    _IDbFunction.DbClose();
                    return StatusCode(500, "無法新增管理員");
                }

                _IDbFunction.DbClose();
                return Ok(new { 
                    success = true, 
                    message = "管理員已建立",
                    loginName = "admin",
                    password = plainPwd,
                    hint = "請至 /Sys/Login 登入"
                });
            }
            catch (Exception ex)
            {
                _IDbFunction.DbClose();
                return StatusCode(500, $"建立失敗: {ex.Message}");
            }
        }
        private bool ValidateUser(string loginName, string loginPasswd)
        {
            var encryptedPwd = EandD_Reply.StaffSet(loginPasswd, 1, 1);
            var sql = "SELECT COUNT(*) as cnt FROM Staff WHERE LoginName = @LoginName AND LoginPasswd = @LoginPasswd";
            var prms = new Dictionary<string, object> { ["@LoginName"] = loginName, ["@LoginPasswd"] = encryptedPwd };
            if (!_IDbFunction.SelectDbDataViewWithParams(sql, "Staff", prms) || _IDbFunction.SqlDataView.Count == 0)
                return false;
            return Convert.ToInt32(_IDbFunction.SqlDataView[0]["cnt"]) > 0;
        }

        private int GetSTNoByLoginName(string loginName)
        {
            var sql = "SELECT STNo FROM Staff WHERE LoginName = @LoginName";
            var prms = new Dictionary<string, object> { ["@LoginName"] = loginName };
            if (!_IDbFunction.SelectDbDataViewWithParams(sql, "Staff", prms) || _IDbFunction.SqlDataView.Count == 0)
                return 0;
            var val = _IDbFunction.SqlDataView[0]["STNo"];
            return (val != null && val != DBNull.Value) ? Convert.ToInt32(val) : 0;
        }
        private string GetToken(int userId)
        {
            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, userId.ToString(), "Admin", Issuer,null),
                new (JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim("rolesId", "Admin")
            };

            // 聲明驗證
            var claimsIdentity = new ClaimsIdentity(claims);
            // 對稱安全密鑰
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SignKey));
            // 簽署
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer, //發行者
                Subject = claimsIdentity, //聲明驗證
                Expires = DateTime.UtcNow.AddMinutes(30), //時效
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var token = tokenHandler.WriteToken(securityToken);

            return token;
        }
        /// <summary>
        /// 驗證取得資料;Header Authorization 必須加入 Bearer {AccessToken}
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            // sub 存 STNo，NameClaimType 已設為 "sub"，Identity.Name 應為 STNo
            string stNo = HttpContext.User.Identity?.Name
                ?? HttpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? "";
            string TokenId = HttpContext.User.Claims.FirstOrDefault(p => p.Type == "jti")?.Value ?? "";
            string RolesId = HttpContext.User.Claims.FirstOrDefault(m => m.Type == "rolesId")?.Value ?? "";
            return Ok(new { STNo = stNo, tokenId = TokenId, rolesId = RolesId });
        }
        /// <summary>
        /// ※這支參考與舊版 OHOTEL 搬運過來改版
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public IActionResult GetSTNoPG(int STNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                Sql = @"
                        SELECT ManageItem.MINo, ManageClass.MCName, ManageItem.ItemName,
                                ManageItem.PowerView, ManageItem.PowerAdd, ManageItem.PowerDel,
                                ManageItem.PowerUpdate, ManageItem.PowerGrant, ManageItem.Power1, ManageItem.Power2
                        FROM ManageItem
                        INNER JOIN StaffPower ON StaffPower.MINo = ManageItem.MINo
                        INNER JOIN ManageClass ON ManageItem.MCNo = ManageClass.MCNo
                        WHERE ManageItem.State = 0 AND ManageClass.State = 0
                            AND StaffPower.PG = 1 AND StaffPower.STNo = @STNo
                        ORDER BY ManageClass.MCNo ASC, ManageItem.MINo ASC";

                using (SqlCommand selectStaffPowerCommand = new(Sql, connection))
                {
                    selectStaffPowerCommand.Parameters.AddWithValue("@STNo", STNo);

                    using (SqlDataReader reader = selectStaffPowerCommand.ExecuteReader())
                    {
                        List<StaffPowerPG> staffPowerList = new List<StaffPowerPG>();

                        while (reader.Read())
                        {
                            Models.StaffPowerPG staffPower = new StaffPowerPG()
                            {
                                MINo = Convert.ToInt32(reader["MINo"]),
                                MCName = reader["MCName"].ToString(),
                                ItemName = reader["ItemName"].ToString(),
                                PowerView = Convert.ToInt32(reader["PowerView"]),
                                PowerAdd = Convert.ToInt32(reader["PowerAdd"]),
                                PowerDel = Convert.ToInt32(reader["PowerDel"]),
                                PowerUpdate = Convert.ToInt32(reader["PowerUpdate"]),
                                PowerGrant = Convert.ToInt32(reader["PowerGrant"]),
                                Power1 = reader["Power1"].ToString(),
                                Power2 = reader["Power2"].ToString()
                            };

                            staffPowerList.Add(staffPower);
                        }
                        return Ok(staffPowerList);
                    }
                }
            }
        }
        /// <summary>
        /// ※獲取登入的會員名稱
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public IActionResult GetSTNameBySTNo(int STNo)
        {
            try
            {
                string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    Sql = "SELECT STName FROM Staff WHERE STNo = @STNo";
                    using (SqlCommand command = new SqlCommand(Sql, connection))
                    {
                        command.Parameters.AddWithValue("@STNo", STNo);

                        connection.Open();
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string STName = reader.GetString(reader.GetOrdinal("STName"));
                                return Ok(STName);
                            }
                            else
                            {
                                return NotFound("Unknown");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while retrieving STName: " + ex.Message);
                return StatusCode(500, "An error occurred while retrieving STName");
            }
        }

        /// <summary>
        /// ※獲取登入的會員各種權限
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public IActionResult GetStaffPermissions(int STNo)
        {
            var connStr = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            var isSqlite = string.Equals(_Configuration["DatabaseProvider"] ?? "", "Sqlite", StringComparison.OrdinalIgnoreCase);

            if (isSqlite)
            {
                try
                {
                    _IDbFunction.DbConnect(connStr ?? "");
                    var allPowerOk = _IDbFunction.SelectDbDataViewWithParams("SELECT AllPower FROM Staff WHERE STNo = @STNo", "Staff", new Dictionary<string, object> { ["@STNo"] = STNo });
                    if (!allPowerOk || _IDbFunction.SqlDataView.Count == 0)
                    {
                        _IDbFunction.DbClose();
                        return NotFound("Staff not found");
                    }
                    var apVal = _IDbFunction.SqlDataView[0]["AllPower"];
                    var allPower = (apVal == null || apVal == DBNull.Value) ? 0 : Convert.ToInt32(apVal);
                    var permissions = new List<StaffPowerPermissions>();

                    if (allPower == 1)
                    {
                        _IDbFunction.SelectDbDataView("SELECT MINo, PowerView, PowerAdd, PowerDel, PowerUpdate, PowerGrant, Power1, Power2 FROM ManageItem", "ManageItem");
                        for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
                        {
                            var r = _IDbFunction.SqlDataView[i];
                            permissions.Add(new StaffPowerPermissions
                            {
                                MINo = Convert.ToInt32(r["MINo"]),
                                PV = Convert.ToInt32(r["PowerView"] ?? 0),
                                PA = Convert.ToInt32(r["PowerAdd"] ?? 0),
                                PD = Convert.ToInt32(r["PowerDel"] ?? 0),
                                PU = Convert.ToInt32(r["PowerUpdate"] ?? 0),
                                PG = Convert.ToInt32(r["PowerGrant"] ?? 0),
                                P1 = string.IsNullOrEmpty(Convert.ToString(r["Power1"])) ? 0 : 1,
                                P2 = string.IsNullOrEmpty(Convert.ToString(r["Power2"])) ? 0 : 1,
                            });
                        }
                    }
                    else
                    {
                        _IDbFunction.SelectDbDataViewWithParams("SELECT MINo, PV, PA, PD, PU, PG, P1, P2 FROM StaffPower WHERE STNo = @STNo", "StaffPower", new Dictionary<string, object> { ["@STNo"] = STNo });
                        for (int i = 0; i < _IDbFunction.SqlDataView.Count; i++)
                        {
                            var r = _IDbFunction.SqlDataView[i];
                            permissions.Add(new StaffPowerPermissions
                            {
                                MINo = Convert.ToInt32(r["MINo"]),
                                PV = Convert.ToInt32(r["PV"] ?? 0),
                                PA = Convert.ToInt32(r["PA"] ?? 0),
                                PD = Convert.ToInt32(r["PD"] ?? 0),
                                PU = Convert.ToInt32(r["PU"] ?? 0),
                                PG = Convert.ToInt32(r["PG"] ?? 0),
                                P1 = Convert.ToInt32(r["P1"] ?? 0),
                                P2 = Convert.ToInt32(r["P2"] ?? 0),
                            });
                        }
                    }
                    _IDbFunction.DbClose();
                    return Ok(permissions);
                }
                catch (Exception ex)
                {
                    _IDbFunction.DbClose();
                    _logger.LogError(ex, "GetStaffPermissions Sqlite 失敗");
                    return StatusCode(500, "An error occurred while retrieving staff permissions");
                }
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();

                    Sql = "SELECT AllPower FROM Staff WHERE STNo = @STNo";
                    SqlCommand staffCommand = new SqlCommand(Sql, connection);
                    staffCommand.Parameters.AddWithValue("@STNo", STNo);
                    object allPowerResult = staffCommand.ExecuteScalar();

                    if (allPowerResult == null || allPowerResult == DBNull.Value)
                    {
                        return NotFound("Staff not found");
                    }

                    bool hasAllPower = Convert.ToInt32(allPowerResult) == 1;

                    if (hasAllPower)
                    {
                        Sql = "SELECT MINo, PowerView, PowerAdd, PowerDel, PowerUpdate, PowerGrant, Power1, Power2 FROM ManageItem";
                        SqlCommand manageItemCommand = new SqlCommand(Sql, connection);
                        SqlDataReader reader = manageItemCommand.ExecuteReader();

                        List<StaffPowerPermissions> permissions = new List<StaffPowerPermissions>();

                        while (reader.Read())
                        {
                            StaffPowerPermissions permission = new StaffPowerPermissions
                            {
                                MINo = Convert.ToInt32(reader["MINo"]),
                                PV = Convert.ToInt32(reader["PowerView"]),
                                PA = Convert.ToInt32(reader["PowerAdd"]),
                                PD = Convert.ToInt32(reader["PowerDel"]),
                                PU = Convert.ToInt32(reader["PowerUpdate"]),
                                PG = Convert.ToInt32(reader["PowerGrant"]),
                                P1 = string.IsNullOrEmpty(Convert.ToString(reader["Power1"])) ? 0 : 1,
                                P2 = string.IsNullOrEmpty(Convert.ToString(reader["Power2"])) ? 0 : 1,
                            };

                            permissions.Add(permission);
                        }

                        reader.Close();
                        connection.Close();

                        return Ok(permissions);
                    }
                    else
                    {
                        Sql = "SELECT MINo, PV, PA, PD, PU, PG, P1, P2 FROM StaffPower WHERE STNo = @STNo";
                        SqlCommand staffPowerCommand = new SqlCommand(Sql, connection);
                        staffPowerCommand.Parameters.AddWithValue("@STNo", STNo);
                        SqlDataReader reader = staffPowerCommand.ExecuteReader();

                        List<StaffPowerPermissions> permissions = new List<StaffPowerPermissions>();

                        while (reader.Read())
                        {
                            StaffPowerPermissions permission = new StaffPowerPermissions
                            {
                                MINo = Convert.ToInt32(reader["MINo"]),
                                PV = Convert.ToInt32(reader["PV"]),
                                PA = Convert.ToInt32(reader["PA"]),
                                PD = Convert.ToInt32(reader["PD"]),
                                PU = Convert.ToInt32(reader["PU"]),
                                PG = Convert.ToInt32(reader["PG"]),
                                P1 = Convert.ToInt32(reader["P1"]),
                                P2 = Convert.ToInt32(reader["P2"])
                            };
                            permissions.Add(permission);
                        }
                        reader.Close();
                        connection.Close();

                        return Ok(permissions);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetStaffPermissions 失敗");
                return StatusCode(500, "An error occurred while retrieving staff permissions");
            }
        }
    }
}

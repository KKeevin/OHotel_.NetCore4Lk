using EasyCLib.NET.Sdk;
using OHotelCLib.Alenher;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Data.SqlClient;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using OHotel.NETCoreMVC.Models;

namespace OHotel.NETCoreMVC.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountLoginController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private String Sql { get; set; } = "";
        public AccountLoginController(IDbFunction dbFunction, IConfiguration configuration)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
        }

        private readonly string _signKey = "KsQvD2ROnqFOT6W4"; // 驗證 Key
        private readonly string _issuer = "JWT.JXINFO"; // 發行者

        [EnableCors("CorsPolicy")]
        [HttpPost]
        public IActionResult Login([FromForm] string LoginName, [FromForm] string LoginPasswd)
        {
            // 在此處進行帳號密碼的確認邏輯，例如從資料庫中檢查使用者提供的帳號密碼是否正確
            if (ValidateUser(LoginName, LoginPasswd))
            {
                // 假設確認成功，獲得使用者的編號（STNo）
                int STNo = GetSTNoByLoginName(LoginName);
                // 使用獲得的編號（STNo）呼叫 GetToken API 並獲取 Token
                string token = GetToken(STNo);

                // 返回 Token
                return Ok(token);
            }
            else
            {
                // 帳號密碼確認失敗，返回錯誤訊息
                return BadRequest("Invalid credentials");
            }
        }
        [HttpPost]
        public IActionResult GetOriginPWD([FromForm] string EncodePasswd) 
        { // 密碼轉回正
            var Password = EandD_Reply.StaffSet(EncodePasswd, 2, 1);

            return Ok(Password);
        }
        private bool ValidateUser(string loginName, string loginPasswd)
        {
            try
            {
                // 假設確認成功，返回 true；否則返回 false
               
                var PasswordInput = EandD_Reply.StaffSet(loginPasswd, 1, 1);

                string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    Sql = "SELECT COUNT(*) FROM Staff WHERE LoginName = @LoginName AND LoginPasswd = @LoginPasswd";
                    SqlCommand command = new SqlCommand(Sql, connection);
                    command.Parameters.AddWithValue("@LoginName", loginName);
                    command.Parameters.AddWithValue("@LoginPasswd", PasswordInput);

                    connection.Open();
                    int count = (int)command.ExecuteScalar();
                    connection.Close();

                    return count > 0;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error connecting to the database: {ex.Message}");
                return false;
            }
        }
        private int GetSTNoByLoginName(string loginName)
        {
            int stNo = 0;
            try
            {
                string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    Sql = "SELECT STNo FROM Staff WHERE LoginName = @LoginName";
                    SqlCommand command = new SqlCommand(Sql, connection);
                    command.Parameters.AddWithValue("@LoginName", loginName);

                    connection.Open();
                    object result = command.ExecuteScalar();
                    connection.Close();

                    if (result != null && result != DBNull.Value)
                    {
                        stNo = Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while retrieving STNo: " + ex.Message);
            }
            return stNo;
        }
        private string GetToken(int userId)
        {
            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, userId.ToString(), "Admin", _issuer,null),
                new (JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim("rolesId", "Admin")
            };

            // 宣稱驗證
            var claimsIdentity = new ClaimsIdentity(claims);
            // 對稱安全密鑰
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signKey));
            // 簽署
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _issuer, //發行者
                Subject = claimsIdentity, //宣稱驗證
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
            string IdentityName = HttpContext.User.Identity?.Name ?? "";
            string TokenId = HttpContext.User.Claims.FirstOrDefault(p => p.Type == "jti")?.Value ?? "";
            string RolesId = HttpContext.User.Claims.FirstOrDefault(m => m.Type == "rolesId")?.Value ?? "";
            return Ok(new { STNo = IdentityName, tokenId = TokenId, rolesId = RolesId });
        }
        /// <summary>
        /// ※這支參考宇舊版OHOTEL搬運過來改版
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
            try
            {
                string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // 檢查 Staff 表的 AllPower 值
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
                        // AllPower 為 1，返回 ManageItem 表中所有權限設置
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
                        // AllPower 為 0，根據 STNo 查詢 StaffPower 表中的權限設置
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
                Console.WriteLine("An error occurred while retrieving staff permissions: " + ex.Message);
                return StatusCode(500, "An error occurred while retrieving staff permissions");
            }
        }
    }
}

using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;

namespace OHotel.NETCoreMVC.Controllers.API
{
    //[Route("api/[controller]")]
    // 以下是命名路徑位置: (例如) Values/GetClass 而完整的話長得像是這樣: https:// localhost:44367/api/EHotelFood/GetEHotel_Food 的樣子
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class LayoutController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private String Sql { get; set; } = "";
        public LayoutController(IDbFunction dbFunction, IConfiguration configuration)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
        }

        /// <summary>
        /// ※查詢所有資料
        /// 230503 Added by KEVINN （Origin from 【EHotelFoodController】）
        /// </summary>
        [EnableCors("CorsPolicy")] // 加入跨網權限設定
        [HttpGet]
        public async Task<IActionResult> GetManageClass()
        { // 使用SqlReader
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
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

        /// <summary>
        /// ※獲取登入的會員資訊
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
                    string query = "SELECT STName FROM Staff WHERE STNo = @STNo";
                    using (SqlCommand command = new SqlCommand(query, connection))
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
        /// ※取得SIDEBAR標題 這個後續要獨立出來
        /// 230510 Created by KEVINN
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("{STNo}")]
        public async Task<IActionResult> GetManageClassAndItemsBySTNo(int STNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql;
                bool allPowerEqualsZero;

                // Check the value of AllPower in the Staff table
                using (SqlCommand checkCommand = new SqlCommand("SELECT AllPower FROM Staff WHERE STNo = @STNo", connection))
                {
                    checkCommand.Parameters.AddWithValue("@STNo", STNo);
                    await connection.OpenAsync();
                    var allPowerResult = await checkCommand.ExecuteScalarAsync();
                    allPowerEqualsZero = Convert.ToInt32(allPowerResult) == 0;
                }

                if (allPowerEqualsZero)
                {
                    sql = @"
                SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
                FROM ManageClass MC
                INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
                WHERE MC.State = 0
                    AND MC.MCNo IN (
                        SELECT MCNo
                        FROM ManageItem
                        WHERE ManageItem.MINo IN (
                            SELECT MINo
                            FROM StaffPower
                            WHERE STNo = @STNo AND PV = 1
                        ) AND ManageItem.State = 0
                    )
                    AND MI.State = 0
                    AND MI.MINo IN (
                        SELECT MINo
                        FROM StaffPower
                        WHERE STNo = @STNo AND PV = 1
                    )
                ORDER BY MC.[Order] ASC";
                }
                else
                {
                    sql = @"
                SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName
                FROM ManageClass MC
                INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo
                WHERE MC.State = 0
                    AND MI.State = 0
                ORDER BY MC.[Order] ASC";
                }

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@STNo", STNo);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        Dictionary<string, List<IDictionary<string, object>>> results = new Dictionary<string, List<IDictionary<string, object>>>();
                        while (await reader.ReadAsync())
                        {
                            string MCName = reader.GetString(reader.GetOrdinal("MCName"));
                            if (!results.ContainsKey(MCName))
                            {
                                results[MCName] = new List<IDictionary<string, object>>();
                            }
                            var result = Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue);
                            results[MCName].Add(result);
                        }
                        return Ok(results);
                    }
                }
            }
        }

        /// <summary>
        /// ※透過編號查詢
        /// 230508 Added by KEVINN （Origin from 【EHotelFoodController】）
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpGet("MCNo/{MCNo}")]
        public async Task<IActionResult> GetManageClass_ByMCNo(int MCNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
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

        private readonly string storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Happy");

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                string fileName = Guid.NewGuid().ToString(); // 使用唯一标识符作为文件名

                // 获取文件扩展名
                string fileExtension = Path.GetExtension(file.FileName);

                // 拼接文件名和扩展名
                fileName += fileExtension;

                string filePath = Path.Combine(storagePath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(fileName); // 返回文件名或文件路径作为响应
            }
            catch (Exception ex)
            {
                // 处理异常
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }




    }
}

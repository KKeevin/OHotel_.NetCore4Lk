using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using System.Data;
using System.Data.SqlClient;
using OHotel.NETCoreMVC.DTO;
using Microsoft.AspNetCore.Authorization;

namespace OHotel.NETCoreMVC.Controllers.API
{
    //[Route("api/[controller]")]
    // 以下是命名路徑位置: (例如) Values/GetClass 而完整的話長得像是這樣: https:// localhost:44367/api/EHotelFood/GetEHotel_Food 的樣子
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class EHotelFoodController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }

        private IEncryptDecrypt _EncryptDecrypt;
        private String Sql { get; set; } = "";
        public EHotelFoodController(IDbFunction dbFunction, IConfiguration configuration, IEncryptDecrypt encryptDecrypt)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
            _EncryptDecrypt = encryptDecrypt;
        }

        /// <summary>
        /// 230420 Created by KEVINN: 【查詢】
        /// </summary>
        [EnableCors("CorsPolicy")] // Required for this path.
        [HttpGet]
        public async Task<IActionResult> GetEHotel_Food()
        { // 使用SqlReader
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql = "SELECT * FROM EHotel_Food WHERE State = 0";
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
        /// 220421 Created by KEVINN: 【搜尋:食物名稱】
        /// </summary>
        [HttpGet("Search")]
        public async Task<IActionResult> SearchEHotel_Food(string foodName)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql = "SELECT * FROM EHotel_Food WHERE State = 0 AND FoodName LIKE '%' + @FoodName + '%'";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@FoodName", foodName);
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
        /// 220421 Created by KEVINN: 【搜尋:食物種類】
        /// </summary>
        [HttpGet("categories/{categories}")]
        public async Task<IActionResult> GetEHotel_Food_ByCategories(string categories)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql = @"SELECT * FROM EHotel_Food WHERE State = 0 AND Categories = @Categories";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Categories", categories);
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
        /// 230420 Created by KEVINN: 【分頁效果】
        /// 230502 Created by KEVINN: 【獲得總頁數、目前頁數、共幾筆資料】
        /// 230503 Created by KEVINN: 【自由選擇資料表以及依照排序】
        /// </summary> 
        [EnableCors("CorsPolicy")]
        [HttpGet]
        public async Task<IActionResult> GetEHotel_Food_Paged(int page = 1, int pageSize = 5, string selectFrom = "EHotel_Food", string orderBy = "FoodNo ASC")
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                // 獲得目前頁數
                string dataSql = $"SELECT * FROM {selectFrom} WHERE State = 0 ORDER BY {orderBy} OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
                using (SqlCommand dataCommand = new SqlCommand(dataSql, connection))
                {
                    await connection.OpenAsync();
                    using (SqlDataReader dataReader = await dataCommand.ExecuteReaderAsync())
                    {
                        List<IDictionary<string, object>> results = new List<IDictionary<string, object>>();
                        while (await dataReader.ReadAsync())
                        {
                            var result = Enumerable.Range(0, dataReader.FieldCount).ToDictionary(dataReader.GetName, dataReader.GetValue);
                            results.Add(result);
                        }

                        // 獲得總頁數
                        string countSql = $"SELECT COUNT(*) FROM {selectFrom} WHERE State = 0";
                        using (SqlCommand countCommand = new SqlCommand(countSql, connection))
                        {
                            int totalCount = (int)await countCommand.ExecuteScalarAsync();
                            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                            // 創建個 paging 物件，把東西丟進去
                            var paging = new { TotalPages = totalPages, CurrentPage = page, TotalCount = totalCount };
                            var resultWithPaging = new { Paging = paging, Data = results };
                            return Ok(resultWithPaging);
                        }
                    }
                }
            }
        }



        /// <summary>
        /// 230420 Created by KEVINN: 【新增】
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddEHotel_Food([FromBody] EHotel_FoodDTO food)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string insertSql = @"
            INSERT INTO EHotel_Food (
            FoodName,
            FoodContnet,
            FoodPrice,
            FoodStock,
            Categories,
            FoodOderCount,
            FoodIsSell,
            [Order],
            State)
            VALUES (
            @FoodName,
            @FoodContnet,
            @FoodPrice,
            @FoodStock,
            @Categories,
            @FoodOderCount,
            @FoodIsSell,
            @Order,
            0)";

                using (SqlCommand insertCommand = new SqlCommand(insertSql, connection))
                {
                    insertCommand.Parameters.AddWithValue("@FoodName", food.FoodName);
                    insertCommand.Parameters.Add("@FoodContnet", SqlDbType.NVarChar).Value = food.FoodContnet;
                    insertCommand.Parameters.AddWithValue("@FoodPrice", food.FoodPrice);
                    insertCommand.Parameters.AddWithValue("@FoodStock", food.FoodStock);
                    insertCommand.Parameters.AddWithValue("@Categories", food.Categories);
                    insertCommand.Parameters.Add("@FoodOderCount", SqlDbType.Int).Value = food.FoodOderCount;
                    insertCommand.Parameters.AddWithValue("@FoodIsSell", food.FoodIsSell);
                    insertCommand.Parameters.AddWithValue("@Order", food.Order);

                    await connection.OpenAsync();
                    int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return BadRequest();
                    }
                    return Ok();
                }
            }
        }

        /// <summary>
        /// 230420 Created by KEVINN: 【修改】
        /// </summary>
        [HttpPut("{foodNo}")]
        public async Task<IActionResult> UpdateEHotel_Food(int foodNo, [FromBody] EHotel_FoodDTO food)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql = @"UPDATE EHotel_Food SET
                       FoodName = @FoodName,
                       FoodContnet = @FoodContnet,
                       FoodPrice = @FoodPrice,
                       FoodStock = @FoodStock,
                       Categories = @Categories,
                       FoodOderCount = @FoodOderCount,
                       FoodIsSell = @FoodIsSell,
                       [Order] = @Order
                       WHERE FoodNo = @FoodNo";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@FoodNo", foodNo);
                    command.Parameters.AddWithValue("@FoodName", food.FoodName);
                    command.Parameters.AddWithValue("@FoodContnet", food.FoodContnet);
                    command.Parameters.AddWithValue("@FoodPrice", food.FoodPrice);
                    command.Parameters.AddWithValue("@FoodStock", food.FoodStock);
                    command.Parameters.AddWithValue("@Categories", food.Categories);
                    command.Parameters.AddWithValue("@FoodOderCount", food.FoodOderCount);
                    command.Parameters.AddWithValue("@FoodIsSell", food.FoodIsSell);
                    command.Parameters.AddWithValue("@Order", food.Order);
                    await connection.OpenAsync();
                    int result = await command.ExecuteNonQueryAsync();
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

        /// <summary>
        /// 230420 Created by KEVINN: 【刪除】 ※指示修改State而已
        /// </summary>
        [HttpDelete("{FoodNo}")]
        public async Task<IActionResult> DeleteEHotel_Food(int FoodNo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string deleteSql = "UPDATE EHotel_Food SET State = 1 WHERE FoodNo = @FoodNo";
                using (SqlCommand deleteCommand = new SqlCommand(deleteSql, connection))
                {
                    deleteCommand.Parameters.AddWithValue("@FoodNo", FoodNo);
                    await connection.OpenAsync();
                    int rowsAffected = await deleteCommand.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        return NotFound();
                    }
                    return NoContent();
                }
            }
        }


    }
}

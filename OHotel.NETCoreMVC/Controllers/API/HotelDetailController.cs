using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using OHotel.NETCoreMVC.DTO;
using Microsoft.AspNetCore.Authorization;

namespace OHotel.NETCoreMVC.Controllers.API
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class HotelDetailController : ControllerBase
    {
        private IDbFunction _IDbFunction;
        public IConfiguration _Configuration { get; set; }
        private String Sql { get; set; } = "";
        public HotelDetailController(IDbFunction dbFunction, IConfiguration configuration)
        {
            _IDbFunction = dbFunction;
            _Configuration = configuration;
        }

        /// <summary>
        /// ※查詢所有資料
        /// </summary>
        [EnableCors("CorsPolicy")] // 加入跨網權限設定
        [HttpGet]
        public async Task<IActionResult> GetHotelInfo()
        { // 使用SqlReader
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new(connectionString))
            {
                Sql = "SELECT top 1 * FROM EHotel_Info order by HINo ASC";
                using (SqlCommand command = new(Sql, connection))
                {
                    await connection.OpenAsync();  // 這邊用了CommandBehavior.CloseConnection，它會在關閉 SqlDataReader 時同時關閉資料庫連線
                    using (SqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection))
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

        /// <summary>
        /// ※修改 
        /// </summary>
        [EnableCors("CorsPolicy")]
        [HttpPatch("{HINo}")]
        public async Task<IActionResult> UpdateEHotelInfo(int HINo, [FromBody] EHotelInfoDTO eHotelInfo)
        {
            string connectionString = _Configuration.GetConnectionString("SQLCD_Read_OHotel");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string sql = @"UPDATE EHotel_Info SET
            HANo = @HANo,
            HTNo = @HTNo,
            HName = @HName,
            HTel = @HTel,
            HEmail = @HEmail,
            HAddr = @HAddr,
            Citizen = @Citizen,
            HSearchWD = @HSearchWD,
            HDescription = @HDescription,
            HLongitude = @HLongitude,
            HLatitude = @HLatitude,
            HLineID = @HLineID,
            HLineAt = @HLineAt,
            HFB = @HFB,
            TWStayNo = @TWStayNo,
            CheckinTime = @CheckinTime,
            ReserveTime = @ReserveTime,
            CheckoutTime = @CheckoutTime,
            FormalOpen = @FormalOpen,
            Decoration = @Decoration,
            Deeds = @Deeds,
            RoomAdvert = @RoomAdvert,
            HBrief = @HBrief,
            CloseRemind = @CloseRemind,
            Note = @Note,
            TrafficAdvert = @TrafficAdvert,
            MTime = @MTime,
            MSNo = @MSNo
            WHERE HINo = @HINo";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@HINo", HINo);
                    command.Parameters.AddWithValue("@HANo", eHotelInfo.HANo);
                    command.Parameters.AddWithValue("@HTNo", eHotelInfo.HTNo);
                    command.Parameters.AddWithValue("@HName", eHotelInfo.HName);
                    command.Parameters.AddWithValue("@HTel", eHotelInfo.HTel);
                    command.Parameters.AddWithValue("@HEmail", eHotelInfo.HEmail);
                    command.Parameters.AddWithValue("@HAddr", eHotelInfo.HAddr);
                    command.Parameters.AddWithValue("@Citizen", eHotelInfo.Citizen);
                    command.Parameters.AddWithValue("@HSearchWD", eHotelInfo.HSearchWD);
                    command.Parameters.AddWithValue("@HDescription", eHotelInfo.HDescription);
                    command.Parameters.AddWithValue("@HLongitude", eHotelInfo.HLongitude);
                    command.Parameters.AddWithValue("@HLatitude", eHotelInfo.HLatitude);
                    command.Parameters.AddWithValue("@HLineID", eHotelInfo.HLineID);
                    command.Parameters.AddWithValue("@HLineAt", eHotelInfo.HLineAt);
                    command.Parameters.AddWithValue("@HFB", eHotelInfo.HFB);
                    command.Parameters.AddWithValue("@TWStayNo", eHotelInfo.TWStayNo);
                    command.Parameters.AddWithValue("@CheckinTime", eHotelInfo.CheckinTime);
                    command.Parameters.AddWithValue("@ReserveTime", eHotelInfo.ReserveTime);
                    command.Parameters.AddWithValue("@CheckoutTime", eHotelInfo.CheckoutTime);
                    command.Parameters.AddWithValue("@FormalOpen", eHotelInfo.FormalOpen);
                    command.Parameters.AddWithValue("@Decoration", eHotelInfo.Decoration);
                    command.Parameters.AddWithValue("@Deeds", eHotelInfo.Deeds);
                    command.Parameters.AddWithValue("@RoomAdvert", eHotelInfo.RoomAdvert);
                    command.Parameters.AddWithValue("@HBrief", eHotelInfo.HBrief);
                    command.Parameters.AddWithValue("@CloseRemind", eHotelInfo.CloseRemind);
                    command.Parameters.AddWithValue("@Note", eHotelInfo.Note);
                    command.Parameters.AddWithValue("@TrafficAdvert", eHotelInfo.TrafficAdvert);
                    command.Parameters.AddWithValue("@MTime", eHotelInfo.MTime);
                    command.Parameters.AddWithValue("@MSNo", eHotelInfo.MSNo);

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



    }
}

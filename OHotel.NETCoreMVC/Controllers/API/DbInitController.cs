using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Mvc;
using OHotel.NETCoreMVC.Data;

namespace OHotel.NETCoreMVC.Controllers.API;

/// <summary>資料庫初始化 API（僅 Sqlite，建立選單表與種子資料）</summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class DbInitController : ControllerBase
{
    private readonly IDbFunction _db;
    private readonly IConfiguration _config;

    public DbInitController(IDbFunction db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 診斷：檢查選單相關資料（無需登入，供除錯用）
    /// </summary>
    [HttpGet]
    public IActionResult Diagnostics()
    {
        var dbProvider = _config["DatabaseProvider"] ?? "";
        var connStr = _config.GetConnectionString("SQLCD_Read_OHotel");
        if (string.IsNullOrEmpty(connStr))
            return Ok(new { error = "Database not configured", dbProvider });

        if (!string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            return Ok(new { message = "此診斷僅適用 Sqlite", dbProvider });

        try
        {
            _db.DbConnect(connStr);
            var result = new Dictionary<string, object> { ["dbProvider"] = dbProvider };

            // Staff
            if (_db.SelectDbDataView("SELECT STNo, LoginName, STName, AllPower FROM Staff WHERE State = 0", "Staff"))
            {
                var staff = new List<object>();
                for (int i = 0; i < _db.SqlDataView.Count; i++)
                {
                    var r = _db.SqlDataView[i];
                    staff.Add(new { STNo = r["STNo"], LoginName = r["LoginName"]?.ToString(), STName = r["STName"]?.ToString(), AllPower = r["AllPower"] });
                }
                result["staff"] = staff;
                result["staffCount"] = staff.Count;
            }
            else
                result["staff"] = "查詢失敗";

            // ManageClass
            if (_db.SelectDbDataView("SELECT COUNT(*) as cnt FROM ManageClass", "ManageClass") && _db.SqlDataView.Count > 0)
            {
                var cntVal = _db.SqlDataView[0]["cnt"] ?? _db.SqlDataView[0][0];
                result["manageClassCount"] = Convert.ToInt32(cntVal ?? 0);
            }
            else
                result["manageClassCount"] = -1;

            // ManageItem
            if (_db.SelectDbDataView("SELECT COUNT(*) as cnt FROM ManageItem", "ManageItem") && _db.SqlDataView.Count > 0)
            {
                var cntVal = _db.SqlDataView[0]["cnt"] ?? _db.SqlDataView[0][0];
                result["manageItemCount"] = Convert.ToInt32(cntVal ?? 0);
            }
            else
                result["manageItemCount"] = -1;

            // 模擬 GetManageClassAndItemsBySTNo(1) 的結果
            var stNo = 1;
            var allPowerOk = _db.SelectDbDataViewWithParams("SELECT AllPower FROM Staff WHERE STNo = @STNo", "Staff", new Dictionary<string, object> { ["@STNo"] = stNo });
            var allPower = (allPowerOk && _db.SqlDataView.Count > 0) ? Convert.ToInt32(_db.SqlDataView[0]["AllPower"]) : -1;
            result["adminAllPower"] = allPower;
            result["testSTNo"] = stNo;

            // 模擬 GetStaffPermissions(1) 回傳（供 Class 頁面除錯）
            var permList = new List<object>();
            if (allPower == 1)
            {
                if (_db.SelectDbDataView("SELECT MINo, PowerView, PowerAdd, PowerDel, PowerUpdate, PowerGrant FROM ManageItem WHERE State = 0", "MI"))
                {
                    for (int i = 0; i < _db.SqlDataView.Count; i++)
                    {
                        var r = _db.SqlDataView[i];
                        permList.Add(new { MINo = Convert.ToInt32(r["MINo"]), PV = Convert.ToInt32(r["PowerView"] ?? 0), PA = Convert.ToInt32(r["PowerAdd"] ?? 0), PD = Convert.ToInt32(r["PowerDel"] ?? 0), PU = Convert.ToInt32(r["PowerUpdate"] ?? 0), PG = Convert.ToInt32(r["PowerGrant"] ?? 0) });
                    }
                }
            }
            else
            {
                if (_db.SelectDbDataViewWithParams("SELECT MINo, PV, PA, PD, PU, PG FROM StaffPower WHERE STNo = @STNo", "SP", new Dictionary<string, object> { ["@STNo"] = stNo }))
                {
                    for (int i = 0; i < _db.SqlDataView.Count; i++)
                    {
                        var r = _db.SqlDataView[i];
                        permList.Add(new { MINo = Convert.ToInt32(r["MINo"]), PV = Convert.ToInt32(r["PV"] ?? 0), PA = Convert.ToInt32(r["PA"] ?? 0), PD = Convert.ToInt32(r["PD"] ?? 0), PU = Convert.ToInt32(r["PU"] ?? 0), PG = Convert.ToInt32(r["PG"] ?? 0) });
                    }
                }
            }
            result["permissionsForSTNo1"] = permList;

            var menuSql = allPower == 1
                ? "SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo WHERE MC.State = 0 AND MI.State = 0 ORDER BY MC.MCNo ASC, MI.MINo ASC"
                : "SELECT MC.MCNo, MC.MCName, MC.MCIcon, MC.MCXtrol, MI.MIAction, MI.ItemName FROM ManageClass MC INNER JOIN ManageItem MI ON MC.MCNo = MI.MCNo WHERE MC.State = 0 AND MI.State = 0 AND MI.MINo IN (SELECT MINo FROM StaffPower WHERE STNo = @STNo AND PV = 1) ORDER BY MC.MCNo ASC, MI.MINo ASC";
            var menuParams = allPower == 1 ? new Dictionary<string, object>() : new Dictionary<string, object> { ["@STNo"] = stNo };
            var menuOk = allPower == 1
                ? _db.SelectDbDataView(menuSql, "Menu")
                : _db.SelectDbDataViewWithParams(menuSql, "Menu", menuParams);
            result["menuQueryOk"] = menuOk;
            result["menuRowCount"] = menuOk ? _db.SqlDataView.Count : 0;
            if (!menuOk && !string.IsNullOrEmpty(_db.LastError))
                result["menuError"] = _db.LastError;

            _db.DbClose();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _db.DbClose();
            return Ok(new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// 初始化 Sqlite 選單表與種子資料（若已存在則跳過）
    /// 適用於：已有 admin 但登入後看不到選單的情況
    /// </summary>
    [HttpPost]
    public IActionResult SeedSqliteMenu()
    {
        var dbProvider = _config["DatabaseProvider"] ?? "";
        if (!string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "此 API 僅適用於 Sqlite" });

        var connStr = _config.GetConnectionString("SQLCD_Read_OHotel");
        if (string.IsNullOrEmpty(connStr))
            return BadRequest("Database not configured");

        try
        {
            SqliteDbInitializer.EnsureTablesAndSeed(_db, connStr);
            return Ok(new { success = true, message = "選單表與種子資料已初始化，請重新整理後台頁面" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

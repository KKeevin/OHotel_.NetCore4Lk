using EasyCLib.NET.Sdk;

namespace OHotel.NETCoreMVC.Data;

/// <summary>Sqlite 資料庫初始化：建立後台選單相關資料表與種子資料</summary>
public static class SqliteDbInitializer
{
    public static bool EnsureTablesAndSeed(IDbFunction db, string connStr)
    {
        db.DbConnect(connStr);
        try
        {
            // ManageClass: 選單主類別
            db.AlterDb(@"
CREATE TABLE IF NOT EXISTS ManageClass (
    MCNo INTEGER PRIMARY KEY AUTOINCREMENT,
    MCName TEXT,
    MCIcon TEXT,
    MCXtrol TEXT,
    [Order] INTEGER DEFAULT 0,
    State INTEGER DEFAULT 0,
    MSNo INTEGER DEFAULT 0
)");

            // ManageItem: 選單項目
            db.AlterDb(@"
CREATE TABLE IF NOT EXISTS ManageItem (
    MINo INTEGER PRIMARY KEY AUTOINCREMENT,
    MCNo INTEGER NOT NULL,
    ItemName TEXT,
    MIAction TEXT,
    PowerView INTEGER DEFAULT 1,
    PowerAdd INTEGER DEFAULT 1,
    PowerDel INTEGER DEFAULT 1,
    PowerUpdate INTEGER DEFAULT 1,
    PowerGrant INTEGER DEFAULT 1,
    Power1 TEXT,
    Power2 TEXT,
    [Order] INTEGER DEFAULT 0,
    State INTEGER DEFAULT 0,
    MSNo INTEGER DEFAULT 0,
    FOREIGN KEY (MCNo) REFERENCES ManageClass(MCNo)
)");

            // StaffPower: 人員權限（STNo, MINo 對應 Staff 與 ManageItem）
            db.AlterDb(@"
CREATE TABLE IF NOT EXISTS StaffPower (
    SPNo INTEGER PRIMARY KEY AUTOINCREMENT,
    STNo INTEGER NOT NULL,
    MINo INTEGER NOT NULL,
    PV INTEGER DEFAULT 0,
    PA INTEGER DEFAULT 0,
    PD INTEGER DEFAULT 0,
    PU INTEGER DEFAULT 0,
    PG INTEGER DEFAULT 0,
    P1 INTEGER DEFAULT 0,
    P2 INTEGER DEFAULT 0,
    FOREIGN KEY (STNo) REFERENCES Staff(STNo),
    FOREIGN KEY (MINo) REFERENCES ManageItem(MINo)
)");

            // 種子資料：若 ManageClass 為空則插入
            if (db.SelectDbDataView("SELECT COUNT(*) as cnt FROM ManageClass", "ManageClass") && db.SqlDataView.Count > 0)
            {
                var cntVal = db.SqlDataView[0]["cnt"] ?? db.SqlDataView[0][0];
                var cnt = Convert.ToInt32(cntVal ?? 0);
                if (cnt == 0)
                {
                    db.AlterDb(@"INSERT INTO ManageClass (MCName, MCIcon, MCXtrol, [Order], State, MSNo) VALUES 
('系統管理', 'fa fa-cog', 'System', 1, 0, 0),
('網站設定', 'fa fa-globe', 'Website', 2, 0, 0)");

                    db.SelectDbDataView("SELECT MCNo FROM ManageClass ORDER BY MCNo", "MC");
                    var mcSystem = db.SqlDataView.Count > 0 ? Convert.ToInt32(db.SqlDataView[0]["MCNo"]) : 1;
                    var mcWebsite = db.SqlDataView.Count > 1 ? Convert.ToInt32(db.SqlDataView[1]["MCNo"]) : 2;

                    db.AlterDb($@"INSERT INTO ManageItem (MCNo, ItemName, MIAction, PowerView, PowerAdd, PowerDel, PowerUpdate, PowerGrant, [Order], State, MSNo) VALUES 
({mcSystem}, '類別管理', 'Class', 1, 1, 1, 1, 1, 1, 0, 0),
({mcSystem}, '項目管理', 'Item', 1, 1, 1, 1, 1, 2, 0, 0),
({mcSystem}, '人員管理', 'Staff', 1, 1, 1, 1, 1, 3, 0, 0),
({mcWebsite}, '飯店資訊', 'Info', 1, 1, 0, 1, 0, 1, 0, 0)");
                }
            }
            return true;
        }
        finally
        {
            db.DbClose();
        }
    }
}

# Sqlite 後台使用說明

## 首次設定

### 1. 建立管理員與選單

若尚未有管理員，請呼叫：

```
POST /api/AccountLogin/CreateFirstAdmin
```

或

```
POST /api/account-login/create-first-admin
```

此 API 會自動：
- 建立 `Staff` 表
- 建立 `ManageClass`、`ManageItem`、`StaffPower` 表
- 插入選單種子資料（系統管理、網站設定）
- 建立 admin 帳號（密碼：admin123）

### 2. 已有 admin 但登入後看不到選單

若您之前已建立 admin，但選單表尚未建立，請呼叫：

```
POST /api/DbInit/SeedSqliteMenu
```

此 API 會建立選單相關表並插入種子資料，完成後重新整理後台頁面即可。

## 選單項目

| 主類別   | 項目     | 路徑                |
|----------|----------|---------------------|
| 系統管理 | 類別管理 | /Sys/System/Class   |
| 系統管理 | 項目管理 | /Sys/System/Item    |
| 系統管理 | 人員管理 | /Sys/System/Staff   |
| 網站設定 | 飯店資訊 | /Sys/Website/Info   |

## 注意事項

- **類別管理、項目管理、人員管理** 的 CRUD 功能目前仍使用 SQL Server 語法，若使用 Sqlite 可能無法正常操作。側邊選單與首頁可正常顯示。
- **飯店資訊** 的查詢已支援 Sqlite，修改功能僅支援 SQL Server。
- 若需完整 CRUD 功能，建議使用 SQL Server。

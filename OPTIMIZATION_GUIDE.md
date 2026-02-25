# OHotel .NET Core 專案優化指南

本文件列出專案可改進項目，依優先級排序。

---

## ✅ 已實作項目

| 項目 | 說明 |
|------|------|
| **全域例外處理** | `Middleware/ExceptionHandlingMiddleware.cs` 統一捕獲未處理例外，回傳 JSON 格式錯誤 |
| **Health Check** | `/health` 端點供負載平衡器或監控系統檢查服務狀態 |
| **CorsPolicy 命名** | 已定義 `CorsPolicy`，供 `[EnableCors("CorsPolicy")]` 使用 |
| **API 回應格式** | `Models/ApiResponse.cs`、`Models/ApiErrorResponse.cs` 統一成功/錯誤回應結構 |
| **VerifyHelper SQL 注入修正** | 已改用參數化查詢 `SelectDbDataViewWithParams` |
| **API 輸入驗證** | 登入 API 驗證帳號/密碼必填與長度 |
| **檔案上傳驗證** | 副檔名白名單、10MB 大小限制 |
| **密碼強度政策** | `PasswordPolicy.Validate`：至少 8 字元、含英文與數字 |
| **結構化日誌** | AccountLoginController 登入成功/失敗紀錄 |
| **HotelDetailController Sqlite** | GetHotelInfo 支援 Sqlite，UpdateEHotelInfo 僅 SQL Server |
| **Sqlite 後台選單** | 建立 ManageClass、ManageItem、StaffPower 表與種子資料 |
| **LayoutController Sqlite** | GetManageClass、GetSTNameBySTNo、GetManageClassAndItemsBySTNo 支援 Sqlite |
| **DbInit/SeedSqliteMenu** | 已有 admin 但無選單時，可 POST 此 API 初始化選單表 |

---

### 2. 後台登入頁面授權保護

`/Sys/Login` 與後台頁面應加上 `[Authorize]` 或 Session 驗證，避免未登入存取。

**建議**：
- 建立 `[RequireLogin]` 或使用 Cookie 驗證
- 登入成功後建立 Session/Cookie，後台頁面檢查登入狀態

---

### 3. API 輸入驗證

API 的 DTO 可加上 `[Required]`、`[StringLength]`、`[Range]` 等 DataAnnotations。

**範例**：
```csharp
public class LoginRequest
{
    [Required(ErrorMessage = "帳號為必填")]
    [StringLength(50)]
    public string LoginName { get; set; } = "";
    
    [Required(ErrorMessage = "密碼為必填")]
    public string LoginPasswd { get; set; } = "";
}
```

---

### 4. 敏感設定使用 User Secrets / 環境變數

`appsettings.json` 中的連線字串、JWT SignKey 不應提交至版控。

**建議**：
- 開發環境：使用 `dotnet user-secrets`
- 正式環境：使用環境變數或 Azure Key Vault

---

## 🟡 中優先級建議

### 5. 結構化日誌

在關鍵流程（登入、API 呼叫、錯誤）加入 `ILogger` 紀錄。

**範例**：
```csharp
_logger.LogInformation("使用者 {LoginName} 登入成功", loginName);
_logger.LogWarning("登入失敗: {LoginName}", loginName);
```

---

### 6. API 版本控制

若未來會有多版 API，可加入 `Microsoft.AspNetCore.Mvc.Versioning`。

---

### 7. 速率限制 (Rate Limiting)

防止暴力破解與 DDoS，可加入 `Microsoft.AspNetCore.RateLimiting`。

**範例**：
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
    });
});
```

---

### 8. HotelDetailController 資料庫相容性

`HotelDetailController` 使用 `SqlConnection` 直接連線，僅支援 SQL Server。若使用 Sqlite，需改為 `IDbFunction` 或條件分支。

---

### 9. Swagger 正式環境存取控制

正式環境若需保留 Swagger，應限制僅管理員可存取。

---

## 🟢 低優先級 / 功能擴充

### 10. 單元測試

為核心邏輯（登入、權限、加密）撰寫單元測試。

---

### 11. 回應快取

對不常變動的 API（如飯店資訊、房型列表）加入 `[ResponseCache]`。

---

### 12. 檔案上傳驗證

`LayoutController` 上傳檔案時，可驗證副檔名白名單、檔案大小上限、MIME 類型。

---

### 13. 密碼強度政策

建立管理員時可檢查密碼長度、複雜度。

---

### 14. 操作稽核紀錄

紀錄誰在何時做了哪些操作（新增/修改/刪除），便於追蹤與稽核。

---

## 檔案結構建議

```
OHotel.NETCoreMVC/
├── Middleware/          # 中介軟體（已新增 ExceptionHandlingMiddleware）
├── Models/              # 模型與 DTO（已新增 ApiResponse、ApiErrorResponse）
├── Services/            # 可考慮抽離業務邏輯（如 AuthService、HotelService）
├── Filters/             # 可加入 ActionFilter 做統一驗證或日誌
└── Validators/          # FluentValidation 驗證器（可選）
```

---

## 參考資源

- [ASP.NET Core 安全性最佳做法](https://learn.microsoft.com/aspnet/core/security/)
- [ASP.NET Core 健康檢查](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [JWT 最佳實踐](https://datatracker.ietf.org/doc/html/rfc8725)

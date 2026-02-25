namespace OHotel.NETCoreMVC.Models;

/// <summary>API 錯誤回應統一格式</summary>
public class ApiErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int StatusCode { get; set; }
    /// <summary>僅在開發環境回傳</summary>
    public string? Detail { get; set; }
}

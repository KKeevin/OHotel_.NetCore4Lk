namespace OHotel.NETCoreMVC.Models;

/// <summary>API 成功回應統一格式</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
}

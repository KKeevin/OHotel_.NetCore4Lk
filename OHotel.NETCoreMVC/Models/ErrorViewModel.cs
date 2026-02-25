namespace OHotel.NETCoreMVC.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public int? StatusCode { get; set; }
        public bool IsDevelopment { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string StatusMessage => StatusCode switch
        {
            404 => "找不到您要的頁面",
            500 => "伺服器發生錯誤，請稍後再試",
            _ => "處理您的請求時發生錯誤"
        };
    }
}
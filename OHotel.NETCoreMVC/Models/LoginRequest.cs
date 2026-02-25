using System.ComponentModel.DataAnnotations;

namespace OHotel.NETCoreMVC.Models;

/// <summary>登入 API 請求 DTO（含輸入驗證）</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "帳號為必填")]
    [StringLength(50, ErrorMessage = "帳號長度不可超過 50 字元")]
    [Display(Name = "帳號")]
    public string LoginName { get; set; } = "";

    [Required(ErrorMessage = "密碼為必填")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "密碼不可為空")]
    [Display(Name = "密碼")]
    public string LoginPasswd { get; set; } = "";
}

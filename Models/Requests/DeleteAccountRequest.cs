namespace V3.Admin.Backend.Models.Requests;

/// <summary>
/// 刪除帳號請求模型
/// </summary>
/// <remarks>
/// 用於接收客戶端的刪除帳號請求,需要二次確認
/// </remarks>
public class DeleteAccountRequest
{
    /// <summary>
    /// 確認訊息 (必須為 "CONFIRM")
    /// </summary>
    /// <remarks>
    /// 防止誤刪操作,使用者必須輸入 "CONFIRM" 才能刪除帳號
    /// </remarks>
    public string Confirmation { get; set; } = string.Empty;
}

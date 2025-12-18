namespace V3.Admin.Backend.Services.Interfaces;

/// <summary>
/// Dropbox Sign API 服務介面
/// </summary>
/// <remarks>
/// 封裝線上簽名邀請寄送、狀態查詢、重新發送等操作
/// </remarks>
public interface IDropboxSignService
{
    /// <summary>
    /// 發送線上簽名邀請
    /// </summary>
    /// <param name="recipientEmail">收件者 Email</param>
    /// <param name="documentName">文件名稱</param>
    /// <param name="pdfBytes">要簽名的 PDF 位元組</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Dropbox Sign 請求 ID</returns>
    Task<string> SendSignatureInvitationAsync(
        string recipientEmail,
        string documentName,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// 重新發送線上簽名邀請
    /// </summary>
    /// <param name="requestId">Dropbox Sign 請求 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ResendSignatureInvitationAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢線上簽名狀態
    /// </summary>
    /// <param name="requestId">Dropbox Sign 請求 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>狀態字串</returns>
    Task<string> GetSignatureStatusAsync(string requestId, CancellationToken cancellationToken = default);
}

using V3.Admin.Backend.Services.Interfaces;

namespace V3.Admin.Backend.Tests.Helpers;

/// <summary>
/// 測試用 DropboxSignService 替身，避免整合測試依賴外部 Dropbox Sign API。
/// </summary>
public class FakeDropboxSignService : IDropboxSignService
{
    public Task<string> SendSignatureInvitationAsync(
        string recipientEmail,
        string documentName,
        byte[] pdfBytes,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("recipientEmail 不可為空", nameof(recipientEmail));
        }

        if (string.IsNullOrWhiteSpace(documentName))
        {
            throw new ArgumentException("documentName 不可為空", nameof(documentName));
        }

        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            throw new ArgumentException("pdfBytes 不可為空", nameof(pdfBytes));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult($"fake_{Guid.NewGuid():N}");
    }

    public Task ResendSignatureInvitationAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("requestId 不可為空", nameof(requestId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    public Task<string> GetSignatureStatusAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("requestId 不可為空", nameof(requestId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult("PENDING");
    }
}

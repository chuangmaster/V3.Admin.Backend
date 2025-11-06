using FluentValidation;
using V3.Admin.Backend.Models.Requests;

namespace V3.Admin.Backend.Validators;

/// <summary>
/// 查詢稽核日誌請求驗證器
/// </summary>
public class QueryAuditLogRequestValidator : AbstractValidator<QueryAuditLogRequest>
{
    /// <summary>
    /// 初始化查詢稽核日誌請求驗證器
    /// </summary>
    public QueryAuditLogRequestValidator()
    {
        // 起始時間不能晚於結束時間
        RuleFor(x => x.StartTime)
            .Must(
                (request, startTime) =>
                {
                    if (!startTime.HasValue || !request.EndTime.HasValue)
                    {
                        return true;
                    }
                    return startTime.Value <= request.EndTime.Value;
                }
            )
            .WithMessage("起始時間不能晚於結束時間");

        // 結束時間不能早於起始時間
        RuleFor(x => x.EndTime)
            .Must(
                (request, endTime) =>
                {
                    if (!endTime.HasValue || !request.StartTime.HasValue)
                    {
                        return true;
                    }
                    return endTime.Value >= request.StartTime.Value;
                }
            )
            .WithMessage("結束時間不能早於起始時間");

        // 時間範圍不能超過 90 天
        RuleFor(x => x)
            .Must(request =>
            {
                if (!request.StartTime.HasValue || !request.EndTime.HasValue)
                {
                    return true;
                }
                var daysDifference = (request.EndTime.Value - request.StartTime.Value).TotalDays;
                return daysDifference <= 90;
            })
            .WithMessage("查詢時間範圍不能超過 90 天");

        // 頁碼必須 >= 1
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).WithMessage("頁碼必須 >= 1");

        // 每頁筆數必須 >= 1 且 <= 100
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("每頁筆數必須 >= 1")
            .LessThanOrEqualTo(100)
            .WithMessage("每頁筆數不能超過 100");

        // 操作類型有效值驗證
        RuleFor(x => x.OperationType)
            .Must(type => string.IsNullOrEmpty(type) || IsValidOperationType(type))
            .WithMessage("操作類型必須是：create, update, delete");

        // 目標類型有效值驗證
        RuleFor(x => x.TargetType)
            .Must(type => string.IsNullOrEmpty(type) || IsValidTargetType(type))
            .WithMessage("目標類型必須是：permission, role, role_permission, user_role");
    }

    /// <summary>
    /// 驗證操作類型是否有效
    /// </summary>
    private static bool IsValidOperationType(string operationType)
    {
        return operationType switch
        {
            "create" or "update" or "delete" => true,
            _ => false,
        };
    }

    /// <summary>
    /// 驗證目標類型是否有效
    /// </summary>
    private static bool IsValidTargetType(string targetType)
    {
        return targetType switch
        {
            "permission" or "role" or "role_permission" or "user_role" => true,
            _ => false,
        };
    }
}

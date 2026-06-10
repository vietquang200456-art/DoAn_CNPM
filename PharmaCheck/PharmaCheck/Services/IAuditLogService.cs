using System.Threading.Tasks;

namespace PharmaCheck.Services;

public interface IAuditLogService
{
    // Hàm ghi log bất đồng bộ
    Task LogAsync(string message, string actionType, string? username);
}
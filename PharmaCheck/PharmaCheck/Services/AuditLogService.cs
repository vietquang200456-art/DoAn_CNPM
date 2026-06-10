using System;
using System.Threading.Tasks;
using PharmaCheck.Data;
using PharmaCheck.Models;

namespace PharmaCheck.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    // Inject DbContext vào Service để làm việc với Database
    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string message, string actionType, string? username)
    {
        var log = new AuditLog
        {
            Message = message,
            ActionType = actionType, // "Create", "Edit", "Delete", "Status", v.v.
            PerformedBy = username ?? "Hệ thống",
            CreatedAt = DateTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
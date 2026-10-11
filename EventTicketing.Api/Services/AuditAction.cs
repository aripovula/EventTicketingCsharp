namespace EventTicketing.Api.Services;

/// <summary>The action names written to audit_logs.</summary>
public static class AuditAction
{
    public const string EventCreated = "EventCreated";
    public const string EventUpdated = "EventUpdated";
    public const string EventDeleted = "EventDeleted";
    public const string TicketBooked = "TicketBooked";
    public const string UserLoggedIn = "UserLoggedIn";
    public const string UserLoginFailed = "UserLoginFailed";
}

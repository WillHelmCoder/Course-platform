namespace Pow.Domain.Enums;

/// <summary>
/// Who can send messages to the channel inbox.
/// </summary>
public enum InboxVisibility
{
    /// <summary>Inbox disabled - only SuperAdmin can send messages</summary>
    Disabled = 0,

    /// <summary>Only channel subscribers can send messages</summary>
    SubscribersOnly = 1,

    /// <summary>Everyone can send messages (public)</summary>
    Everyone = 2
}

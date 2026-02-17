namespace Pow.Domain.Enums;

/// <summary>
/// Content access levels - who can view the content.
/// Plan-specific access is controlled at the Course level, not Content level.
/// </summary>
public enum ContentAccessLevel
{
    /// <summary>Public - anyone can view, no login required</summary>
    Public = 0,

    /// <summary>LoggedIn - any authenticated user can view</summary>
    LoggedIn = 1,

    /// <summary>AllSubscribers - any subscriber of the channel (any plan)</summary>
    AllSubscribers = 2
}

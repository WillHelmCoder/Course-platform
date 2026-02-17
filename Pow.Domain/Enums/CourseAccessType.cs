namespace Pow.Domain.Enums;

/// <summary>
/// Course access types - defines who gets FREE access.
/// </summary>
public enum CourseAccessType
{
    /// <summary>Free course - accessible to everyone</summary>
    Free = 0,

    /// <summary>Subscribers only - requires any channel subscription</summary>
    SubscribersOnly = 1,

    /// <summary>Specific plan - requires a specific subscription plan or higher</summary>
    SpecificPlan = 2
}

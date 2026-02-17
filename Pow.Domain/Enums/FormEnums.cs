namespace Pow.Domain.Enums;

/// <summary>
/// Type of form - Survey (no scoring) or Quiz (with scoring)
/// </summary>
public enum FormType
{
    /// <summary>Survey - no scoring, just collecting answers</summary>
    Survey = 0,

    /// <summary>Quiz - with scoring, can have pass/fail</summary>
    Quiz = 1
}

/// <summary>
/// Type of question
/// </summary>
public enum QuestionType
{
    /// <summary>Open text answer</summary>
    OpenText = 0,

    /// <summary>Single choice (radio buttons)</summary>
    SingleChoice = 1,

    /// <summary>Multiple choice (checkboxes)</summary>
    MultipleChoice = 2
}

/// <summary>
/// When the form is triggered/shown to users
/// </summary>
public enum FormTrigger
{
    /// <summary>Manual - creator sends link or embeds</summary>
    Manual = 0,

    /// <summary>After user registration to the channel</summary>
    AfterRegistration = 1,

    /// <summary>After completing a specific course</summary>
    AfterCourseComplete = 2
}

/// <summary>
/// Status of a user's form response
/// </summary>
public enum FormResponseStatus
{
    /// <summary>User started but hasn't completed</summary>
    InProgress = 0,

    /// <summary>User completed the form</summary>
    Completed = 1
}

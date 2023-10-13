namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class TaskManagementTask
{
    public string Id { get; set; }

    public string Title { get; set; }

    public string? Reminder { get; set; }

    public string? Due { get; set; }

    public bool IsCompleted { get; set; }

    public TaskManagementTask(string id, string title, string? reminder = null, string? due = null, bool isCompleted = false)
    {
        Id = id;
        Title = title;
        Reminder = reminder;
        Due = due;
        IsCompleted = isCompleted;
    }
}

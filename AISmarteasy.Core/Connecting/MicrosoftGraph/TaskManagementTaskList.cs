namespace AISmarteasy.Core.Connecting.MicrosoftGraph;

public class TaskManagementTaskList
{
    public string Id { get; set; }

    public string Name { get; set; }

    public TaskManagementTaskList(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

using System.Globalization;
using AISmarteasy.Core.Function;
using Microsoft.Graph;
using TaskStatus = Microsoft.Graph.TaskStatus;


namespace AISmarteasy.Core.Connector.MicrosoftGraph;

public class MicrosoftToDoConnector : ITaskManagementConnector
{
    private readonly GraphServiceClient _graphServiceClient;

    public MicrosoftToDoConnector(GraphServiceClient graphServiceClient)
    {
        _graphServiceClient = graphServiceClient;
    }

    public async Task<TaskManagementTaskList?> GetDefaultTaskListAsync(CancellationToken cancellationToken = default)
    {
        ITodoListsCollectionPage lists = await _graphServiceClient.Me
            .Todo.Lists
            .Request().GetAsync(cancellationToken).ConfigureAwait(false);

        TodoTaskList? result = lists.SingleOrDefault(list => list.WellknownListName == WellknownListName.DefaultList);

        while (result == null && lists.Count != 0 && lists.NextPageRequest != null)
        {
            lists = await lists.NextPageRequest.GetAsync(cancellationToken).ConfigureAwait(false);
            result = lists.SingleOrDefault(list => list.WellknownListName == WellknownListName.DefaultList);
        }

        if (result == null)
        {
            throw new SKException("Could not find default task list.");
        }

        return new TaskManagementTaskList(result.Id, result.DisplayName);
    }

    public async Task<IEnumerable<TaskManagementTaskList>> GetTaskListsAsync(CancellationToken cancellationToken = default)
    {
        ITodoListsCollectionPage lists = await _graphServiceClient.Me
            .Todo.Lists
            .Request().GetAsync(cancellationToken).ConfigureAwait(false);

        List<TodoTaskList> taskLists = lists.ToList();

        while (lists.Count != 0 && lists.NextPageRequest != null)
        {
            lists = await lists.NextPageRequest.GetAsync(cancellationToken).ConfigureAwait(false);
            taskLists.AddRange(lists.ToList());
        }

        return taskLists.Select(list => new TaskManagementTaskList(id: list.Id, name: list.DisplayName));
    }

    public async Task<IEnumerable<TaskManagementTask>> GetTasksAsync(string listId, bool includeCompleted, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(listId, nameof(listId));

        string filterValue = string.Empty;
        if (!includeCompleted)
        {
            filterValue = "status ne 'completed'";
        }

        ITodoTaskListTasksCollectionPage tasksPage = await _graphServiceClient.Me
            .Todo.Lists[listId]
            .Tasks.Request().Filter(filterValue).GetAsync(cancellationToken).ConfigureAwait(false);

        List<TodoTask> tasks = tasksPage.ToList();

        while (tasksPage.Count != 0 && tasksPage.NextPageRequest != null)
        {
            tasksPage = await tasksPage.NextPageRequest.GetAsync(cancellationToken).ConfigureAwait(false);
            tasks.AddRange(tasksPage.ToList());
        }

        return tasks.Select(task => new TaskManagementTask(
            id: task.Id,
            title: task.Title,
            reminder: task.ReminderDateTime?.DateTime,
            due: task.DueDateTime?.DateTime,
            isCompleted: task.Status == TaskStatus.Completed));
    }

    public async Task<TaskManagementTask> AddTaskAsync(string listId, TaskManagementTask task, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(listId, nameof(listId));
        Verify.NotNull(task, nameof(task));

        return ToTaskListTask(await _graphServiceClient.Me
            .Todo.Lists[listId]
            .Tasks
            .Request().AddAsync(FromTaskListTask(task), cancellationToken).ConfigureAwait(false));
    }

    public Task DeleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default)
    {
        Verify.NotNullOrWhitespace(listId, nameof(listId));
        Verify.NotNullOrWhitespace(taskId, nameof(taskId));

        return _graphServiceClient.Me
            .Todo.Lists[listId]
            .Tasks[taskId]
            .Request().DeleteAsync(cancellationToken);
    }

    private static TodoTask FromTaskListTask(TaskManagementTask task)
    {
        Verify.NotNull(task, nameof(task));

        return new TodoTask()
        {
            Title = task.Title,
            ReminderDateTime = task.Reminder == null
                ? null
                : DateTimeTimeZone.FromDateTimeOffset(DateTimeOffset.Parse(task.Reminder, CultureInfo.InvariantCulture.DateTimeFormat)),
            DueDateTime = task.Due == null
                ? null
                : DateTimeTimeZone.FromDateTimeOffset(DateTimeOffset.Parse(task.Due, CultureInfo.InvariantCulture.DateTimeFormat)),
            Status = task.IsCompleted ? TaskStatus.Completed : TaskStatus.NotStarted
        };
    }

    private static TaskManagementTask ToTaskListTask(TodoTask task)
    {
        Verify.NotNull(task, nameof(task));

        return new TaskManagementTask(
            id: task.Id,
            title: task.Title,
            reminder: task.ReminderDateTime?.DateTime,
            due: task.DueDateTime?.DateTime,
            isCompleted: task.Status == TaskStatus.Completed);
    }
}

using System.ComponentModel;
using System.Text.Json;
using AISmarteasy.Core.Connecting;
using AISmarteasy.Core.Connecting.MicrosoftGraph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.PluginFunction.NativeSkill;


public sealed class TaskListSkill
{
    public static class Parameters
    {
        public const string Reminder = "reminder";
        public const string IncludeCompleted = "includeCompleted";
    }

    private readonly ITaskManagementConnector _connector;
    private readonly ILogger _logger;

    public TaskListSkill()
    {

    }

    public TaskListSkill(ITaskManagementConnector connector, ILoggerFactory? loggerFactory = null)
    {
        Verify.NotNull(connector, nameof(connector));

        _connector = connector;
        _logger = loggerFactory is not null ? loggerFactory.CreateLogger(typeof(TaskListSkill)) : NullLogger.Instance;
    }

    public static DateTimeOffset GetNextDayOfWeek(DayOfWeek dayOfWeek, TimeSpan timeOfDay)
    {
        DateTimeOffset today = new(DateTime.Today);
        int nextDayOfWeekOffset = dayOfWeek - today.DayOfWeek;
        if (nextDayOfWeekOffset <= 0)
        {
            nextDayOfWeekOffset += 7;
        }

        DateTimeOffset nextDayOfWeek = today.AddDays(nextDayOfWeekOffset);
        DateTimeOffset nextDayOfWeekAtTimeOfDay = nextDayOfWeek.Add(timeOfDay);

        return nextDayOfWeekAtTimeOfDay;
    }

    [SKFunction, Description("Add a task to a task list with an optional reminder.")]
    public async Task AddTaskAsync(
        [Description("Title of the task.")] string title,
        [Description("Reminder for the task in DateTimeOffset (optional)")] string? reminder = null,
        CancellationToken cancellationToken = default)
    {
        TaskManagementTaskList? defaultTaskList = await this._connector.GetDefaultTaskListAsync(cancellationToken).ConfigureAwait(false);
        if (defaultTaskList == null)
        {
            throw new InvalidOperationException("No default task list found.");
        }

        TaskManagementTask task = new(
            id: Guid.NewGuid().ToString(),
            title: title,
            reminder: reminder);

        _logger.LogTrace("Adding task '{0}' to task list '{1}'", task.Title, defaultTaskList.Name);

        await _connector.AddTaskAsync(defaultTaskList.Id, task, cancellationToken).ConfigureAwait(false);
    }

    [SKFunction, Description("Get tasks from the default task list.")]
    public async Task<string> GetDefaultTasksAsync(
        [Description("Whether to include completed tasks (optional)")] string includeCompleted = "false",
        CancellationToken cancellationToken = default)
    {
        TaskManagementTaskList? defaultTaskList = await _connector.GetDefaultTaskListAsync(cancellationToken).ConfigureAwait(false);
        if (defaultTaskList == null)
        {
            throw new InvalidOperationException("No default task list found.");
        }

        if (!bool.TryParse(includeCompleted, out bool includeCompletedValue))
        {
            _logger.LogWarning("Invalid value for '{0}' variable: '{1}'", nameof(includeCompleted), includeCompleted);
        }

        IEnumerable<TaskManagementTask> tasks = await _connector.GetTasksAsync(defaultTaskList.Id, includeCompletedValue, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(tasks);
    }
}

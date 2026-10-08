using SkillSnap.Client.Models;

namespace SkillSnap.Client.Services;

public sealed class UserSessionService
{
    public string? UserId { get; private set; }

    public string? Role { get; private set; }

    public Project? CurrentProject { get; private set; }

    public bool IsEditingProject { get; private set; }

    public event Action? StateChanged;

    public void SetUser(string? userId, string? role)
    {
        UserId = userId;
        Role = role;
        CurrentProject = null;
        IsEditingProject = false;
        StateChanged?.Invoke();
    }

    public void SetProject(Project? project, bool isEditing = false)
    {
        CurrentProject = project;
        IsEditingProject = project is not null && isEditing;
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        UserId = null;
        Role = null;
        CurrentProject = null;
        IsEditingProject = false;
        StateChanged?.Invoke();
    }
}

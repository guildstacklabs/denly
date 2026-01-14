namespace Denly.Services;

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}

public record ToastMessage(string Message, ToastType Type);

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

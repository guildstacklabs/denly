namespace Denly.Services;

public interface IToastService
{
    event EventHandler<ToastEventArgs>? OnShow;
    void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000);
    void Success(string message, int durationMs = 3000);
    void Error(string message, int durationMs = 4000);
    void Info(string message, int durationMs = 3000);
}

public enum ToastType
{
    Info,
    Success,
    Error
}

public class ToastEventArgs : EventArgs
{
    public string Message { get; }
    public ToastType Type { get; }
    public int DurationMs { get; }

    public ToastEventArgs(string message, ToastType type, int durationMs)
    {
        Message = message;
        Type = type;
        DurationMs = durationMs;
    }
}

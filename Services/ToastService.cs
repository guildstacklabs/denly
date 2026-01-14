namespace Denly.Services;

public class ToastService : IToastService
{
    public event EventHandler<ToastEventArgs>? OnShow;

    public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        OnShow?.Invoke(this, new ToastEventArgs(message, type, durationMs));
    }

    public void Success(string message, int durationMs = 3000)
    {
        Show(message, ToastType.Success, durationMs);
    }

    public void Error(string message, int durationMs = 4000)
    {
        Show(message, ToastType.Error, durationMs);
    }

    public void Info(string message, int durationMs = 3000)
    {
        Show(message, ToastType.Info, durationMs);
    }
}

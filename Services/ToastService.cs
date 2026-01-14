namespace Denly.Services;

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message)
    {
        OnShow?.Invoke(new ToastMessage(message, ToastType.Success));
    }

    public void ShowError(string message)
    {
        OnShow?.Invoke(new ToastMessage(message, ToastType.Error));
    }

    public void ShowWarning(string message)
    {
        OnShow?.Invoke(new ToastMessage(message, ToastType.Warning));
    }

    public void ShowInfo(string message)
    {
        OnShow?.Invoke(new ToastMessage(message, ToastType.Info));
    }
}

namespace TokenIDP.Server;

public class LoadService
{
    public event Action OnShowLoader;
    public event Action OnHideLoader;

    // Method for pages to call
    public void ShowLoader() => OnShowLoader?.Invoke();
    public void HideLoader() => OnHideLoader?.Invoke();
}


namespace Denly.Services;

public interface ISafeAreaService
{
    int Top { get; }
    int Bottom { get; }
    int Left { get; }
    int Right { get; }
}

#if ANDROID
public class AndroidSafeAreaService : ISafeAreaService
{
    public int Top => SafeAreaInsets.Top;
    public int Bottom => SafeAreaInsets.Bottom;
    public int Left => SafeAreaInsets.Left;
    public int Right => SafeAreaInsets.Right;
}
#else
public class DefaultSafeAreaService : ISafeAreaService
{
    // Return 0 - iOS uses native CSS env() via fallback pattern
    public int Top => 0;
    public int Bottom => 0;
    public int Left => 0;
    public int Right => 0;
}
#endif

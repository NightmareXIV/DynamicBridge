namespace DynamicBridge;
public struct FolderItem
{
    public Action Action;

    public FolderItem(Action action) : this()
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
}

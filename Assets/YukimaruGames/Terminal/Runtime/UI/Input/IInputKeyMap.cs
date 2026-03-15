namespace YukimaruGames.Terminal.UI.Input
{
    public interface IInputKeyMap<out T>
    {
        T GetKey(Trigger action);
    }
}

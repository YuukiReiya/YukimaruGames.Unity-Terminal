namespace YukimaruGames.Terminal.Composition
{
    public interface ITerminalOptions
    {
        ITerminalInput Input { get; }

        int BufferSize { get; }
        string Prompt { get; }
        string BootupCommand { get; }
        bool IsButtonVisible { get; }
        bool IsButtonReverse { get; }
    }
}

namespace YukimaruGames.Terminal.SharedKernel.Constants
{
    public static class Constants
    {
        public static class ColorPalette
        {
            public const string Error = nameof(MessageType.Error);
            public const string Assert = nameof(MessageType.Assert);
            public const string Warning = nameof(MessageType.Warning);
            public const string Message = nameof(MessageType.Message);
            public const string Exception = nameof(MessageType.Exception);
            public const string Entry = nameof(MessageType.Entry);
            public const string System = nameof(MessageType.System);

            public const string Cursor = nameof(UnityEngine.GUI.skin.settings.cursorColor);
            public const string Selection = nameof(UnityEngine.GUI.skin.settings.selectionColor);
        }
    }
}

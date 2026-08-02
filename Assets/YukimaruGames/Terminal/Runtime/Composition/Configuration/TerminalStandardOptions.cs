using System;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;

namespace YukimaruGames.Terminal.Composition
{
    [Serializable]
    public sealed class TerminalStandardOptions : ITerminalOptions
    {
        [Header("Input Settings")]
        [SerializeReference, SerializeInterface]
        private ITerminalInput _input = new TerminalStandardInput();

        [Header("Command Settings")]
        [SerializeField] private int _bufferSize = 256;
        [SerializeField] private string _prompt = "$";
        [SerializeField] private string _bootupCommand;

        [Header("UI Controls")]
        [SerializeField] private bool _buttonVisible;
        [SerializeField] private bool _buttonReverse;

        public ITerminalInput Input => _input ?? new TerminalNullInput();

        public int BufferSize => _bufferSize;
        public string Prompt => _prompt;
        public string BootupCommand => _bootupCommand;
        public bool IsButtonVisible => _buttonVisible;
        public bool IsButtonReverse => _buttonReverse;
    }
}

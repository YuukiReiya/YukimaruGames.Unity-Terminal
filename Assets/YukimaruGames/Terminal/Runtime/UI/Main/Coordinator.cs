using System;
using UnityEngine;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.UI.Core;
using YukimaruGames.Terminal.UI.Input;
using YukimaruGames.Terminal.UI.Launcher;
using YukimaruGames.Terminal.UI.Log;
using YukimaruGames.Terminal.UI.Window;

namespace YukimaruGames.Terminal.UI.Main
{
    public sealed class Coordinator : IDisposable
    {
        private readonly IMainView _view;
        private readonly IScrollConfigurator _scrollConfigurator;
        private readonly IWindowPresenter _windowPresenter;
        private readonly IInputPresenter _inputPresenter;
        private readonly ILogPresenter _logPresenter;
        private readonly ISubmitPresenter _submitPresenter;
        private readonly ILauncherPresenter _launcherPresenter;
        private readonly IEventListener _eventListener;

        private readonly ITerminalService _service;
        private const string BootUpMessage = "Welcome to Runtime YukimaruGames.CLI!\n(c) Independent Developer. All rights reserved.\nType your command below.";

        /// <summary>
        /// 表示されているか.
        /// </summary>
        private bool IsVisible =>
            _windowPresenter.State is WindowState.Open && !_windowPresenter.IsAnimating;
        
        public Coordinator(
            ITerminalService service,
            IMainView view,
            IScrollConfigurator scrollConfigurator,
            IWindowPresenter windowPresenter,
            IInputPresenter inputPresenter,
            ILogPresenter logPresenter,
            ISubmitPresenter submitPresenter,
            ILauncherPresenter launcherPresenter,
            IEventListener eventListener
        )
        {
            _service = service;
            _view = view;
            _scrollConfigurator = scrollConfigurator;
            _windowPresenter = windowPresenter;
            _inputPresenter = inputPresenter;
            _logPresenter = logPresenter;
            _submitPresenter = submitPresenter;
            _launcherPresenter = launcherPresenter;
            _eventListener = eventListener;

            RegisterEvents();

            service.SystemMessage(BootUpMessage);
        }

        private void RegisterEvents()
        {
            _submitPresenter.OnExecuteTriggered += OnExecuteTriggered;
            _launcherPresenter.OnOpenTriggered += OnOpenTriggered;
            _launcherPresenter.OnCloseTriggered += OnCloseTriggered;
            
            _eventListener.OnOpenTriggered += OnOpenTriggered;
            _eventListener.OnCloseTriggered += OnCloseTriggered;
            _eventListener.OnExecuteTriggered += OnExecuteTriggered;
            _eventListener.OnPreviousHistoryTriggered += OnPreviousHistoryTriggered;
            _eventListener.OnNextHistoryTriggered += OnNextHistoryTriggered;
            _eventListener.OnAutocompleteTriggered += OnAutocompleteTriggered;
            _eventListener.OnFocusTriggered += OnFocusTriggered;

            _view.OnScreenSizeChanged += OnScreenSizeChanged;
            _view.OnLogCopiedTriggered += OnLogCopiedTriggered;
        }

        private void UnregisterEvents()
        {
            _submitPresenter.OnExecuteTriggered -= OnExecuteTriggered;
            _launcherPresenter.OnOpenTriggered -= OnOpenTriggered;
            _launcherPresenter.OnCloseTriggered -= OnCloseTriggered;
            
            _eventListener.OnOpenTriggered -= OnOpenTriggered;
            _eventListener.OnCloseTriggered -= OnCloseTriggered;
            _eventListener.OnExecuteTriggered -= OnExecuteTriggered;
            _eventListener.OnPreviousHistoryTriggered -= OnPreviousHistoryTriggered;
            _eventListener.OnNextHistoryTriggered -= OnNextHistoryTriggered;
            _eventListener.OnAutocompleteTriggered -= OnAutocompleteTriggered;
            _eventListener.OnFocusTriggered -= OnFocusTriggered;
            
            _view.OnScreenSizeChanged -= OnScreenSizeChanged;
            _view.OnLogCopiedTriggered -= OnLogCopiedTriggered;
        }
        
        private void OnOpenTriggered()
        {
            if (_inputPresenter.IsImeComposing) return;
            
            _windowPresenter.Open();
            _inputPresenter.SetFocus(true);
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnCloseTriggered()
        {
            if (_inputPresenter.IsImeComposing) return;
            
            _windowPresenter.Close();
            _inputPresenter.SetFocus(false);
        }

        private void OnExecuteTriggered()
        {
            if (!IsVisible) return;
            
            // IMEの文字列入力における変換中であれば早期リターン.
            if (_inputPresenter.IsImeComposing) return;
            
            _service.Execute(_inputPresenter.InputText);

            _inputPresenter.SetInputField(string.Empty);
            _inputPresenter.SetFocus(true);
            _inputPresenter.SetMoveCursorToEnd();
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnPreviousHistoryTriggered()
        {
            if (!IsVisible) return;
            
            _inputPresenter.SetInputField(_service.PrevHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnNextHistoryTriggered()
        {
            if (!IsVisible) return;
            
            _inputPresenter.SetInputField(_service.NextHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnAutocompleteTriggered()
        {
            if (!IsVisible) return;
            
            var completionResults = _service.Autocomplete(_inputPresenter.InputText);
            var length = completionResults?.Length ?? 0;
            
            switch (length)
            {
                case 1:
                    _inputPresenter.SetInputField(completionResults![0]);
                    _inputPresenter.SetFocus(true);
                    _inputPresenter.SetMoveCursorToEnd();
                    break;
                case > 1:
                    const string separator = "    ";
                    _service.SystemMessage(string.Join(separator, completionResults!));
                    _inputPresenter.SetMoveCursorToEnd();
                    break;
            }
            
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnFocusTriggered()
        {
            if (!IsVisible) return;

            _inputPresenter.SetFocus(true);
        }
        
        private void OnScreenSizeChanged(Vector2Int size)
        {
            _windowPresenter.Refresh();
            _scrollConfigurator.ScrollToEnd();
        }

        private void OnLogCopiedTriggered(string copiedText)
        {
            GUIUtility.systemCopyBuffer = copiedText;
        }
        
        public void Dispose()
        {
            UnregisterEvents();
        }
    }
}

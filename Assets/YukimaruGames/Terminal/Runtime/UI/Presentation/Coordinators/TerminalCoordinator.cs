using System;
using UnityEngine;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Window;
using YukimaruGames.Terminal.Presentation.Presenters.Window;

namespace YukimaruGames.Terminal.Presentation.Coordinators
{
    public sealed class TerminalCoordinator : IDisposable
    {
        private readonly ITerminalWindow _view;
        private readonly IAnimationDataProvider _animationDataProvider;
        private readonly IScrollMutator _scrollMutator;
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
            _animationDataProvider.State is WindowState.Open && !_windowPresenter.IsAnimating;
        
        public TerminalCoordinator(
            ITerminalService service,
            ITerminalWindow view,
            IScrollMutator scrollMutator,
            IAnimationDataProvider animationDataProvider,
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
            _scrollMutator = scrollMutator;
            _animationDataProvider = animationDataProvider;
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
            _scrollMutator.ScrollToEnd();
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
            _scrollMutator.ScrollToEnd();
        }

        private void OnPreviousHistoryTriggered()
        {
            if (!IsVisible) return;
            
            _inputPresenter.SetInputField(_service.PrevHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollMutator.ScrollToEnd();
        }

        private void OnNextHistoryTriggered()
        {
            if (!IsVisible) return;
            
            _inputPresenter.SetInputField(_service.NextHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollMutator.ScrollToEnd();
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
            
            _scrollMutator.ScrollToEnd();
        }

        private void OnFocusTriggered()
        {
            if (!IsVisible) return;

            _inputPresenter.SetFocus(true);
        }
        
        private void OnScreenSizeChanged(Vector2Int size)
        {
            _windowPresenter.Refresh();
            _scrollMutator.ScrollToEnd();
        }

        private void OnLogCopiedTriggered(string copiedText)
        {
            GUIUtility.systemCopyBuffer = copiedText;
        }
        
        void IDisposable.Dispose()
        {
            UnregisterEvents();
        }
    }
}

using System;
using System.Threading;
using UnityEngine;
using YukimaruGames.Terminal.Application.Interfaces;
using YukimaruGames.Terminal.Presentation.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors.Window;
using YukimaruGames.Terminal.Presentation.Interfaces.Coordinators;
using YukimaruGames.Terminal.Presentation.Interfaces.Events;
using YukimaruGames.Terminal.Presentation.Interfaces.Presenters;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Presentation.Coordinators
{
    public sealed class TerminalCoordinator : IDisposable
    {
        private readonly ITerminalGUI _gui;
        private readonly IWindowAnimationProvider _windowAnimationProvider;
        private readonly IScrollMutator _scrollMutator;
        private readonly IWindowPresenter _windowPresenter;
        private readonly IInputPresenter _inputPresenter;
        private readonly ILogPresenter _logPresenter;
        private readonly ISubmitPresenter _submitPresenter;
        private readonly ILauncherPresenter _launcherPresenter;
        private readonly IEventListener _eventListener;
        private readonly IWindowFocusInputGuard _windowFocusInputGuard;

        private readonly ITerminalService _service;
        private const string BootUpMessage = "Welcome to Runtime YukimaruGames.CLI!\n(c) Independent Developer. All rights reserved.\nType your command below.";

        private readonly CancellationTokenSource _destroyCancellationToken = new();

        /// <summary>
        /// <see cref="_windowFocusInputGuard"/>の表示区間ハンドル(未表示時はnull).
        /// </summary>
        private IDisposable _windowFocusInputGuardHandle;

        private bool _disposed;
        
        /// <summary>
        /// 表示されているか.
        /// </summary>
        private bool IsVisible =>
            _windowAnimationProvider.State is WindowState.Open && !_windowPresenter.IsAnimating;
        
        public TerminalCoordinator(
            ITerminalService service,
            ITerminalGUI gui,
            IScrollMutator scrollMutator,
            IWindowAnimationProvider windowAnimationProvider,
            IWindowPresenter windowPresenter,
            IInputPresenter inputPresenter,
            ILogPresenter logPresenter,
            ISubmitPresenter submitPresenter,
            ILauncherPresenter launcherPresenter,
            IEventListener eventListener,
            IWindowFocusInputGuard windowFocusInputGuard
        )
        {
            _service = service;
            _gui = gui;
            _scrollMutator = scrollMutator;
            _windowAnimationProvider = windowAnimationProvider;
            _windowPresenter = windowPresenter;
            _inputPresenter = inputPresenter;
            _logPresenter = logPresenter;
            _submitPresenter = submitPresenter;
            _launcherPresenter = launcherPresenter;
            _eventListener = eventListener;
            _windowFocusInputGuard = windowFocusInputGuard ?? NullWindowFocusInputGuard.Instance;

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
            _eventListener.OnCancelTriggered += OnCancelTriggered;
            _eventListener.OnPreviousHistoryTriggered += OnPreviousHistoryTriggered;
            _eventListener.OnNextHistoryTriggered += OnNextHistoryTriggered;
            _eventListener.OnAutocompleteTriggered += OnAutocompleteTriggered;
            _eventListener.OnFocusTriggered += OnFocusTriggered;

            _gui.OnScreenSizeChanged += OnScreenSizeChanged;
            _gui.OnLogCopiedTriggered += OnLogCopiedTriggered;
        }

        private void UnregisterEvents()
        {
            _submitPresenter.OnExecuteTriggered -= OnExecuteTriggered;
            _launcherPresenter.OnOpenTriggered -= OnOpenTriggered;
            _launcherPresenter.OnCloseTriggered -= OnCloseTriggered;
            
            _eventListener.OnOpenTriggered -= OnOpenTriggered;
            _eventListener.OnCloseTriggered -= OnCloseTriggered;
            _eventListener.OnExecuteTriggered -= OnExecuteTriggered;
            _eventListener.OnCancelTriggered -= OnCancelTriggered;
            _eventListener.OnPreviousHistoryTriggered -= OnPreviousHistoryTriggered;
            _eventListener.OnNextHistoryTriggered -= OnNextHistoryTriggered;
            _eventListener.OnAutocompleteTriggered -= OnAutocompleteTriggered;
            _eventListener.OnFocusTriggered -= OnFocusTriggered;
            
            _gui.OnScreenSizeChanged -= OnScreenSizeChanged;
            _gui.OnLogCopiedTriggered -= OnLogCopiedTriggered;
        }
        
        private void OnOpenTriggered()
        {
            if (_inputPresenter.IsImeComposing) return;

            _windowPresenter.Open();
            _inputPresenter.SetFocus(true);
            _scrollMutator.ScrollToEnd();

            EnterWindowFocusInputGuard();
        }

        private void OnCloseTriggered()
        {
            // IME変換中でも常に閉じられるようにする.
            // IsImeComposingガードで閉じる操作までブロックすると、compositionStringが
            // 何らかの理由でクリアされないまま残った場合にウィンドウが永久に閉じられなく
            // なってしまうため(Open/Executeとは異なりCloseは誤発火の実害が小さい一方、
            // 閉じられなくなるスタック状態の方がUXとして深刻).
            _windowPresenter.Close();
            _inputPresenter.SetFocus(false);

            ExitWindowFocusInputGuard();
        }

        private void EnterWindowFocusInputGuard()
        {
            if (_windowFocusInputGuardHandle != null) return;
            _windowFocusInputGuardHandle = _windowFocusInputGuard.BeginScope();
        }

        private void ExitWindowFocusInputGuard()
        {
            _windowFocusInputGuardHandle?.Dispose();
            _windowFocusInputGuardHandle = null;
        }

        private void OnExecuteTriggered()
        {
            if (!IsVisible) return;

            // IMEの文字列入力における変換中であればスキップ.
            if (_inputPresenter.IsImeComposing) return;

            // 処理の実行中であればスキップ.
            if (_service.IsExecuting) return;

            var inputText = _inputPresenter.InputText;

            _inputPresenter.SetInputField(string.Empty);
            _inputPresenter.SetFocus(true);
            _inputPresenter.SetMoveCursorToEnd();
            _scrollMutator.ScrollToEnd();

            ExecuteAsync(inputText);
        }

        /// <summary>
        /// コマンドの実行を待機し、完了するまで入力を受け付けないようにする.
        /// </summary>
        /// <remarks>
        /// 以前はFire-And-Forgetで実行結果を待たずに次の入力が可能だったため、
        /// 実行完了まで<see cref="IInputPresenter.IsEditable"/>を明示的にロックする.
        /// </remarks>
        private async void ExecuteAsync(string inputText)
        {
            _inputPresenter.IsEditable = false;

            try
            {
                await _service.ExecuteAsync(inputText, _destroyCancellationToken.Token);
            }
            catch (OperationCanceledException)
            {
                // キャンセル(ウィンドウ破棄含む)は正常系として扱う.
            }
            finally
            {
                if (!_disposed)
                {
                    _inputPresenter.IsEditable = true;
                }

                _scrollMutator.ScrollToEnd();
            }
        }

        private void OnCancelTriggered()
        {
            if (!IsVisible) return;

            // 実行中かどうかの分岐はITerminalService.Interrupt()の内部に閉じ込める。
            // モード入力待ち(非実行中)のCtrl+Cもここで弾かず、常にInterrupt()へ届ける.
            _service.Interrupt();
            _scrollMutator.ScrollToEnd();
        }

        private void OnPreviousHistoryTriggered()
        {
            if (!IsVisible) return;
            if (_service.IsExecuting) return;

            _inputPresenter.SetInputField(_service.PrevHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollMutator.ScrollToEnd();
        }

        private void OnNextHistoryTriggered()
        {
            if (!IsVisible) return;
            if (_service.IsExecuting) return;

            _inputPresenter.SetInputField(_service.NextHistory());
            _inputPresenter.SetMoveCursorToEnd();
            _scrollMutator.ScrollToEnd();
        }

        private void OnAutocompleteTriggered()
        {
            if (!IsVisible) return;
            if (_service.IsExecuting) return;

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
            if (_disposed) return;
            _disposed = true;

            UnregisterEvents();

            // ウィンドウが開いたままDisposeされた場合に備え、ガードの区間を必ず終了させる.
            ExitWindowFocusInputGuard();

            _destroyCancellationToken.Cancel();
            _destroyCancellationToken.Dispose();
        }
    }
}

using System;
using UnityEngine;
using YukimaruGames.Terminal.Presentation.Constants;
using YukimaruGames.Terminal.Presentation.Contracts;
using YukimaruGames.Terminal.Adapters.IMGUI.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Accessors;
using YukimaruGames.Terminal.Presentation.Interfaces.Renderers;
using YukimaruGames.Terminal.Presentation.Models.Input;
using YukimaruGames.Terminal.Presentation.Models.Window;

namespace YukimaruGames.Terminal.Adapters.IMGUI.Renderers
{
    /// <summary>
    /// IMGUIによる入力欄の描画と、入力イベントの通知を行う.
    /// </summary>
    public sealed class InputRenderer : IInputRenderer, IInputProvider, IPreRenderer
    {
        private readonly IScrollMutator _scrollMutator;
        private readonly IGUIStyleProvider _styleProvider;
        private readonly IColorPaletteProvider _colorPaletteProvider;
        private readonly CursorView _cursorView;

        private bool _isCurrentlyFocused;
        private bool _isMoveCursorToEnd;

        /// <summary>
        /// 直近で<see cref="OnFocusControlChanged"/>へ通知したフォーカス状態.
        /// </summary>
        /// <remarks>
        /// <see cref="Focus"/>(命令チャンネル)は外部からの命令を一度きり適用して
        /// <see cref="WindowFocus.None"/>へ戻すだけで、物理クリック等でIMGUI側が自発的に
        /// フォーカスを失った場合を一切通知しない(#154のフォローアップ)。<see cref="_isCurrentlyFocused"/>
        /// を毎フレーム比較し、命令経由か自発的な変化かを問わず実際の状態変化を通知する.
        /// </remarks>
        private bool _lastNotifiedFocused;

        /// <summary>
        /// フォーカスを戻した結果の全選択を解除する必要があるか.
        /// </summary>
        /// <remarks>
        /// IMGUIのTextFieldはフォーカスを得た瞬間に<b>テキスト全体を選択する</b>。
        /// フォーカスを戻す処理を入れている都合上、そのままではTabやEnterのたびに
        /// 入力中の文字列が選択状態になり、次の1文字で全消えする(#16)。
        /// 選択を解除してキャレットを末尾へ戻すため、描画側へ持ち越す.
        /// </remarks>
        private bool _selectionResetRequested;
        private string _inputField;
        private WindowFocus _focus = WindowFocus.None;
        private bool _isImeComposing;

        private const string ControlName = "COMMAND_INPUT_CONTROL";

        public event Action<string> OnInputTextChanged;
        public event Action<WindowFocus> OnFocusControlChanged;
        public event Action<bool> OnMoveCursorToEndTriggerChanged;
        public event Action<bool> OnImeComposingStateChanged;

        private WindowFocus Focus
        {
            get => _focus;
            set
            {
                if (_focus == value) return;

                _focus = value;
                OnFocusControlChanged?.Invoke(value);
            }
        }

        private bool IsMoveCursorToEndTrigger
        {
            get => _isMoveCursorToEnd;
            set
            {
                if (_isMoveCursorToEnd == value) return;

                _isMoveCursorToEnd = value;
                OnMoveCursorToEndTriggerChanged?.Invoke(value);
            }
        }

        private bool IsImeComposing
        {
            set
            {
                if (_isImeComposing == value) return;
                _isImeComposing = value;
                OnImeComposingStateChanged?.Invoke(value);
            }
        }

        public string InputText
        {
            get => _inputField;
            private set
            {
                if (_inputField == value) return;

                _inputField = value;
                OnInputTextChanged?.Invoke(value);
            }
        }

        public InputRenderer(
            IScrollMutator scrollMutator,
            IGUIStyleProvider styleProvider,
            IColorPaletteProvider colorPaletteProvider,
            CursorView cursorView)
        {
            _scrollMutator = scrollMutator;
            _styleProvider = styleProvider;
            _colorPaletteProvider = colorPaletteProvider;
            _cursorView = cursorView;
        }

        void IPreRenderer.PreRender()
        {
            var evt = Event.current;
            if (UsedInputEvent(evt.type))
            {
                // Tabキー入力されると他のTextFieldにフォーカスが移ってしまうためフォーカスをコントロールする.
                // Tab/EnterはIMGUIのTextField側で処理されず素通しされるため、
                // ネイティブのフォーカス巡回へ流れて入力欄からフォーカスが外れる。
                // 外れたフォーカスを戻すと、TextFieldはフォーカス取得時にテキスト全体を
                // 選択する(TextEditor.OnFocus → SelectAll。IMGUIにこれを無効化するAPIは無い)ため、
                // 入力中の文字列が選択状態になる(#16)。描画前に食ってしまえばフォーカスが
                // 外れず、選択も起きない。
                // ターミナル側のTab=補完 / Enter=実行はUpdate駆動の別経路
                // (IKeyboardInputHandler)で判定するため、ここで止めても機能しなくならない。
                // UIToolkit版が同じ理由でTrickleDownにより先取りしているのと同じ方針.
                if (IsFocusTraversalKey(evt)) evt.Use();

                // 入力テキストの折り返しを考慮しキー入力がされたらスクロール位置を終端へ補正する.
                _scrollMutator.ScrollToEnd();
            }
        }

        /// <summary>
        /// フォーカス巡回を引き起こすキーか.
        /// </summary>
        /// <remarks>
        /// keyCodeだけでなく文字も見る。IMGUIはTabや改行を文字としても
        /// 配信するため、片方だけだとすり抜ける.
        /// </remarks>
        private static bool IsFocusTraversalKey(Event evt) =>
            evt.keyCode is KeyCode.Tab or KeyCode.Return or KeyCode.KeypadEnter
            || evt.character is '\t' or '\n' or '\r';

        public void Render(InputRenderData data)
        {
            UnityEngine.GUI.SetNextControlName(ControlName);
            _isCurrentlyFocused = UnityEngine.GUI.GetNameOfFocusedControl() == ControlName;

            // 命令(Focus)経由かどうかに関わらず、実際のフォーカス状態が変化していれば通知する。
            // 物理クリックで入力欄の外へフォーカスが移った場合、命令チャンネルは何も変化しないため
            // ここで検知しないと呼び出し側(InputPresenter.IsFocused)が古い状態のまま固定される(#154).
            // Layout/Repaint等、1フレームに複数回呼ばれるパスがあるため、通知はRepaintの時だけに絞る.
            if (Event.current.type == EventType.Repaint && _isCurrentlyFocused != _lastNotifiedFocused)
            {
                _lastNotifiedFocused = _isCurrentlyFocused;
                OnFocusControlChanged?.Invoke(_isCurrentlyFocused ? WindowFocus.Apply : WindowFocus.Release);
            }

            var cursorColor = UnityEngine.GUI.skin.settings.cursorColor;
            var selectionColor = UnityEngine.GUI.skin.settings.selectionColor;
            var cursorFlashSpeed = UnityEngine.GUI.skin.settings.cursorFlashSpeed;

            var nextCursorColor = _colorPaletteProvider[Definitions.ThemeLabel.Cursor];
            if (_cursorView is { IsVisible: false })
            {
                nextCursorColor.a = 0f;
            }

            try
            {
                UnityEngine.GUI.skin.settings.cursorColor = nextCursorColor;
                UnityEngine.GUI.skin.settings.selectionColor = _colorPaletteProvider[Definitions.ThemeLabel.Selection];
                // カーソルの点滅はCursorPresenter/CursorViewが管理するため、ネイティブの点滅は無効化する.
                UnityEngine.GUI.skin.settings.cursorFlashSpeed = 0f;

                InputText = GUILayout.TextField(data.InputText, _styleProvider.GetStyle());
                SendImeComposingState();
            }
            finally
            {
                UnityEngine.GUI.skin.settings.cursorColor = cursorColor;
                UnityEngine.GUI.skin.settings.selectionColor = selectionColor;
                UnityEngine.GUI.skin.settings.cursorFlashSpeed = cursorFlashSpeed;
            }

            _focus = data.Focus;

            // キー入力でフォーカスを戻した場合も、選択解除のためにキャレットを末尾へ動かす。
            // dataの値で上書きすると、その要求が消えてしまう.
            _isMoveCursorToEnd = data.IsMoveCursorToEnd || _selectionResetRequested;

            FocusControlIfNeeded();
            CursorToEnd();
        }

        public void SetMoveCursorToEnd() => _isMoveCursorToEnd = true;

        private void FocusControlIfNeeded()
        {
            if (Focus is WindowFocus.None) return;

            switch (Focus)
            {
                case WindowFocus.Apply:
                    if (!_isCurrentlyFocused)
                    {
                        // フォーカスを得た次の描画でTextFieldが全選択するため、その解除を予約する(#16).
                        UnityEngine.GUI.FocusControl(ControlName);
                        _selectionResetRequested = true;
                    }

                    break;
                case WindowFocus.Release:
                    if (_isCurrentlyFocused)
                    {
                        UnityEngine.GUI.FocusControl(null);
                    }

                    break;
            }

            UnityEngine.GUI.changed = true;
            Focus = WindowFocus.None;
        }

        /// <summary>
        /// キャレットを末尾へ移し、選択状態を解除する.
        /// </summary>
        /// <remarks>
        /// <b>フォーカスの判定は描画後に行うこと。</b>描画前の値では、そのフレームで
        /// フォーカスを得た場合(=まさに全選択が起きた場合)を取りこぼす。
        /// フォーカス名で判定するのは、<see cref="GUIUtility.GetStateObject"/>が
        /// IDに対応する状態を持たない場合でもnullではなく<b>空のTextEditorを新規生成して返す</b>
        /// ためで、対象がズレていても例外にならず静かに空振りするのを避ける.
        /// </remarks>
        private void CursorToEnd()
        {
            if (!IsMoveCursorToEndTrigger) return;

            // レイアウト計算中(Layoutパス)はTextEditorのpositionが未確定で、GUILayoutが割り当てる前の
            // 1x1のダミー矩形になっている。その状態でキャレットを動かすと「1x1の枠に末尾を収める」
            // 計算になり、scrollOffsetが桁違いの値へ飛んで文字が表示範囲の外へ消える
            // (実測: 8文字の入力に対しscrollOffset.y=182.52)。矢印キーで直るのは、矩形が確定した
            // 状態で再計算されるため。矩形が確定するRepaintパスでのみ行う(#16).
            if (Event.current.type != EventType.Repaint) return;

            // IME変換中にキャレットを動かすと変換途中の状態を壊すため触らない.
            if (_isImeComposing) return;

            if (UnityEngine.GUI.GetNameOfFocusedControl() != ControlName) return;

            if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is not TextEditor textEditor)
            {
                return;
            }

            // MoveTextEndはキャレットと選択の開始位置を揃えるため、これで全選択が解除される.
            textEditor.MoveTextEnd();

            // キャレットを動かしただけでは表示のスクロール位置は追従しない。オートコンプリートで
            // 入力欄の幅を超える文字列に置き換わったとき、表示が前の位置に取り残されて
            // 文字が見えなくなる(矢印キーを押すと追従して見えるようになる)。
            // showCursorを立てると、次の描画でキャレットが見える位置までスクロールされる
            // (旧revealCursorはUnity 6で非推奨。showCursorが後継).
            textEditor.showCursor = true;

            UnityEngine.GUI.changed = true;
            IsMoveCursorToEndTrigger = false;
            _selectionResetRequested = false;
        }

        private void SendImeComposingState()
        {
            IsImeComposing = !string.IsNullOrEmpty(UnityEngine.Input.compositionString);
        }

        private static bool UsedInputEvent(EventType type) => type switch
        {
            EventType.KeyDown or EventType.KeyUp => true,
            EventType.MouseDown or EventType.MouseUp => true,
            EventType.MouseMove or EventType.MouseDrag => true,
            EventType.Used => true,
            _ => false
        };
    }
}
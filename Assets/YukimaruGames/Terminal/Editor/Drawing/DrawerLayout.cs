using System;
using UnityEditor;
using UnityEngine;

namespace YukimaruGames.Terminal.Editor
{
    /// <summary>
    /// <see cref="PropertyDrawer"/>を矩形ベースで組み立てるためのカーソル.
    /// </summary>
    /// <remarks>
    /// <see cref="PropertyDrawer.OnGUI"/>の中で<c>EditorGUILayout</c>を使うと、描画は与えられた
    /// <c>position</c>ではなく現在のレイアウトカーソルへ流れる。その結果、Drawerが確保した位置は
    /// 空白になり、中身はInspectorの末尾へ出てしまう(#147)。
    /// <para>
    /// このクラスは「高さを測るだけ」と「実際に描く」を同じコードで行うためのもの。
    /// <see cref="IsDrawing"/>が<c>false</c>のときは<c>EditorGUI</c>を呼ばず位置だけを進めるため、
    /// <see cref="PropertyDrawer.GetPropertyHeight"/>と<see cref="PropertyDrawer.OnGUI"/>で
    /// <b>同じ組み立て処理を共有できる</b>(高さの二重管理を避けられる).
    /// </para>
    /// </remarks>
    internal sealed class DrawerLayout
    {
        /// <summary>ボックス(<see cref="EditorStyles.helpBox"/>)の内側に取る余白(px).</summary>
        private const float BoxPadding = 4f;

        /// <summary>幅が分からないときに使う既定幅(px).</summary>
        private const float FallbackWidth = 300f;

        /// <summary>
        /// 高さの計算だけを行うための矩形を作る.
        /// </summary>
        /// <remarks>
        /// <b><see cref="EditorGUIUtility.currentViewWidth"/>を使ってはならない。</b>
        /// <see cref="PropertyDrawer.GetPropertyHeight"/>は<c>OnGUI</c>の外から呼ばれることがあり、
        /// このプロパティはそこで例外になる(実測で確認)。直前の描画で受け取った幅を渡すこと。
        /// まだ一度も描いていない場合は既定幅で代用し、最初の描画以降は実際の幅で測り直される.
        /// </remarks>
        /// <param name="lastKnownWidth">直前の描画で受け取った幅(px)。未描画なら0</param>
        internal static Rect MeasureRect(float lastKnownWidth) =>
            new(0f, 0f, lastKnownWidth > 0f ? lastKnownWidth : FallbackWidth, 0f);

        private readonly bool _isDrawing;
        private readonly float _x;
        private readonly float _width;
        private readonly float _startY;
        private float _y;

        /// <param name="position">組み立てを始める矩形(高さは使わない)</param>
        /// <param name="isDrawing"><c>false</c>なら描画せず高さの計算だけを行う</param>
        internal DrawerLayout(Rect position, bool isDrawing)
        {
            _isDrawing = isDrawing;
            _x = position.x;
            _width = position.width;
            _startY = position.y;
            _y = position.y;
        }

        /// <summary>実際に描画するか(<c>false</c>なら高さの計算のみ).</summary>
        internal bool IsDrawing => _isDrawing;

        /// <summary>組み立てた内容の高さ(px).</summary>
        internal float Height => _y - _startY;

        /// <summary>指定した高さの矩形を確保し、カーソルを進める.</summary>
        internal Rect Next(float height)
        {
            var rect = new Rect(_x, _y, _width, height);
            _y += height + EditorGUIUtility.standardVerticalSpacing;

            return rect;
        }

        /// <summary>1行ぶんの矩形を確保する.</summary>
        internal Rect NextLine() => Next(EditorGUIUtility.singleLineHeight);

        /// <summary>カーソルだけを進める(セクション間の余白).</summary>
        internal void Space(float amount) => _y += amount;

        /// <summary>ラベルを描く.</summary>
        internal void Label(GUIContent label, GUIStyle style)
        {
            var rect = NextLine();
            if (_isDrawing) EditorGUI.LabelField(rect, label, style);
        }

        /// <inheritdoc cref="Label(GUIContent, GUIStyle)"/>
        internal void Label(string label, GUIStyle style) => Label(new GUIContent(label), style);

        /// <summary>プロパティを描く(子要素の展開状態に応じた高さを確保する).</summary>
        internal void PropertyField(SerializedProperty property, GUIContent label = null, bool includeChildren = true)
        {
            if (property == null) return;

            var height = label == null
                ? EditorGUI.GetPropertyHeight(property, includeChildren)
                : EditorGUI.GetPropertyHeight(property, label, includeChildren);
            var rect = Next(height);

            if (!_isDrawing) return;

            if (label == null) EditorGUI.PropertyField(rect, property, includeChildren);
            else EditorGUI.PropertyField(rect, property, label, includeChildren);
        }

        /// <summary>スライダーを描く.</summary>
        internal void Slider(SerializedProperty property, float min, float max, GUIContent label = null)
        {
            if (property == null) return;

            var rect = NextLine();
            if (!_isDrawing) return;

            if (label == null) EditorGUI.Slider(rect, property, min, max);
            else EditorGUI.Slider(rect, property, min, max, label);
        }

        /// <summary>
        /// スライダーと、その右側に置くボタンを描く.
        /// </summary>
        /// <returns>ボタンが押されたら<c>true</c>(計算のみの場合は常に<c>false</c>).</returns>
        internal bool SliderWithButton(
            SerializedProperty property, float min, float max, GUIContent label, string buttonText, float buttonWidth)
        {
            if (property == null) return false;

            var rect = NextLine();
            if (!_isDrawing) return false;

            var sliderRect = new Rect(rect.x, rect.y, rect.width - buttonWidth - Spacing, rect.height);
            var buttonRect = new Rect(rect.xMax - buttonWidth, rect.y, buttonWidth, rect.height);

            EditorGUI.Slider(sliderRect, property, min, max, label);

            return GUI.Button(buttonRect, buttonText, EditorStyles.miniButton);
        }

        /// <summary>列挙体のポップアップを描く.</summary>
        internal void EnumPopup(SerializedProperty property, GUIContent label, GUIContent[] displayedOptions, GUIStyle style)
        {
            if (property == null) return;

            var rect = NextLine();
            if (!_isDrawing) return;

            property.enumValueIndex = EditorGUI.Popup(rect, label, property.enumValueIndex, displayedOptions, style);
        }

        /// <summary>左寄せのトグルを描く.</summary>
        internal void ToggleLeft(SerializedProperty property, GUIContent label)
        {
            if (property == null) return;

            var rect = NextLine();
            if (!_isDrawing) return;

            property.boolValue = EditorGUI.ToggleLeft(rect, label, property.boolValue);
        }

        /// <summary>ヘルプボックスを描く.</summary>
        internal void HelpBox(string message, MessageType type)
        {
            var content = EditorGUIUtility.TrTextContentWithIcon(message, type);
            var height = Mathf.Max(
                EditorStyles.helpBox.CalcHeight(content, _width), EditorGUIUtility.singleLineHeight * 2f);
            var rect = Next(height);

            if (_isDrawing) EditorGUI.HelpBox(rect, message, type);
        }

        /// <summary>ツールバー(タブ)を描く.</summary>
        /// <returns>選択されたインデックス(計算のみの場合は<paramref name="selected"/>をそのまま返す).</returns>
        internal int Toolbar(int selected, string[] texts, GUIStyle style, float height)
        {
            var rect = Next(height);

            return _isDrawing ? GUI.Toolbar(rect, selected, texts, style) : selected;
        }

        /// <summary>
        /// 内容をボックス(<see cref="EditorStyles.helpBox"/>)で囲んで描く.
        /// </summary>
        /// <remarks>
        /// ボックスの背景は中身より先に描く必要があるが、その高さは中身を組み立てるまで分からない。
        /// そのため<paramref name="body"/>を「計算のみ」で一度呼んで高さを求めてから、背景を描き、
        /// 同じ<paramref name="body"/>で中身を描く(組み立てが1箇所なので両者はずれない).
        /// </remarks>
        internal void BoxedGroup(Action<DrawerLayout> body)
        {
            var contentWidth = _width - BoxPadding * 2f;

            var measurer = new DrawerLayout(new Rect(0f, 0f, contentWidth, 0f), false);
            body(measurer);

            var box = Next(measurer.Height + BoxPadding * 2f);
            if (_isDrawing) GUI.Box(box, GUIContent.none, EditorStyles.helpBox);

            var content = new DrawerLayout(
                new Rect(box.x + BoxPadding, box.y + BoxPadding, contentWidth, 0f), _isDrawing);
            body(content);
        }

        private static float Spacing => EditorGUIUtility.standardVerticalSpacing;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YukimaruGames.Terminal.Adapters.CommandLine
{
    /// <summary>
    /// Unityのリッチテキストタグを、外部ターミナル向けのANSIエスケープへ変換する.
    /// </summary>
    /// <remarks>
    /// コマンドの出力に色を付ける手段としてUnityのリッチテキストタグを採用しているが(#156)、
    /// CLIバックエンドは外部ターミナルへ素のテキストを流すため、そのままではタグが文字として見える。
    /// <para>
    /// 認識できないタグは<b>そのまま出力する</b>。Unityのリッチテキストが未知のタグを文字として
    /// 描画する挙動に合わせるためで、これにより<c>List&lt;int&gt;</c>のような例外メッセージが
    /// 各バックエンドと同じ見た目になる.
    /// </para>
    /// </remarks>
    internal static class RichTextAnsiConverter
    {
        private const string Reset = "\x1b[0m";
        private const string BoldOn = "\x1b[1m";
        private const string BoldOff = "\x1b[22m";
        private const string ItalicOn = "\x1b[3m";
        private const string ItalicOff = "\x1b[23m";
        private const string DefaultForeground = "\x1b[39m";

        /// <summary>ANSIに対応物が無く、本文だけ残して取り除くタグ.</summary>
        private static readonly string[] DroppedTags = { "size", "material", "quad" };

        /// <summary>
        /// Unityが解釈する色名。<c>&lt;color=red&gt;</c>のような指定に対応するために持つ.
        /// </summary>
        private static readonly Dictionary<string, (byte R, byte G, byte B)> NamedColors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["aqua"] = (0x00, 0xFF, 0xFF),
                ["black"] = (0x00, 0x00, 0x00),
                ["blue"] = (0x00, 0x00, 0xFF),
                ["brown"] = (0xA5, 0x2A, 0x2A),
                ["cyan"] = (0x00, 0xFF, 0xFF),
                ["darkblue"] = (0x00, 0x00, 0xA0),
                ["fuchsia"] = (0xFF, 0x00, 0xFF),
                ["green"] = (0x00, 0x80, 0x00),
                ["grey"] = (0x80, 0x80, 0x80),
                ["gray"] = (0x80, 0x80, 0x80),
                ["lightblue"] = (0xAD, 0xD8, 0xE6),
                ["lime"] = (0x00, 0xFF, 0x00),
                ["magenta"] = (0xFF, 0x00, 0xFF),
                ["maroon"] = (0x80, 0x00, 0x00),
                ["navy"] = (0x00, 0x00, 0x80),
                ["olive"] = (0x80, 0x80, 0x00),
                ["orange"] = (0xFF, 0xA5, 0x00),
                ["purple"] = (0x80, 0x00, 0x80),
                ["red"] = (0xFF, 0x00, 0x00),
                ["silver"] = (0xC0, 0xC0, 0xC0),
                ["teal"] = (0x00, 0x80, 0x80),
                ["white"] = (0xFF, 0xFF, 0xFF),
                ["yellow"] = (0xFF, 0xFF, 0x00),
            };

        /// <summary>
        /// タグをANSIエスケープへ変換する.
        /// </summary>
        /// <param name="text">変換元。<c>null</c>は空文字列として扱う.</param>
        /// <param name="colored">
        /// <c>false</c>ならエスケープを出力せず、タグを取り除いた本文だけを返す
        /// (色に対応しない端末やパイプへ流す場合).
        /// </param>
        internal static string Convert(string text, bool colored = true)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.IndexOf('<') < 0) return text;

            var builder = new StringBuilder(text.Length);
            var colorStack = new Stack<string>();
            var styled = false;
            var index = 0;

            while (index < text.Length)
            {
                var open = text.IndexOf('<', index);
                if (open < 0)
                {
                    builder.Append(text, index, text.Length - index);
                    break;
                }

                builder.Append(text, index, open - index);

                var close = text.IndexOf('>', open + 1);
                if (close < 0)
                {
                    // 閉じない '<' は本文として扱う.
                    builder.Append(text, open, text.Length - open);
                    break;
                }

                var tag = text.Substring(open + 1, close - open - 1);
                if (TryConvertTag(tag, colored, colorStack, out var replacement))
                {
                    builder.Append(replacement);
                    if (replacement.Length > 0) styled = true;
                }
                else
                {
                    // 未知のタグはそのまま出力する(Unityの挙動に合わせる).
                    builder.Append(text, open, close - open + 1);
                }

                index = close + 1;
            }

            // 色が次の行へ漏れないよう、装飾を使った行は必ずリセットして終わる.
            if (styled) builder.Append(Reset);

            return builder.ToString();
        }

        /// <summary>
        /// タグ1つをANSIへ変換する。認識できない場合は<c>false</c>を返す.
        /// </summary>
        private static bool TryConvertTag(string tag, bool colored, Stack<string> colorStack, out string replacement)
        {
            replacement = string.Empty;
            if (tag.Length == 0) return false;

            var isClosing = tag[0] == '/';
            var body = isClosing ? tag[1..] : tag;
            var equalsIndex = body.IndexOf('=');

            // タグ名は '=' または空白まで。<quad size=5 ...> のように空白区切りの属性を持つ
            // タグがあるため、'=' だけで切ると名前を取り違える.
            var nameEnd = body.Length;
            for (var i = 0; i < body.Length; i++)
            {
                if (body[i] != '=' && !char.IsWhiteSpace(body[i])) continue;

                nameEnd = i;
                break;
            }

            var name = body[..nameEnd];

            if (name.Equals("color", StringComparison.OrdinalIgnoreCase))
            {
                return TryConvertColor(isClosing, equalsIndex < 0 ? null : body[(equalsIndex + 1)..], colored, colorStack, out replacement);
            }

            if (name.Equals("b", StringComparison.OrdinalIgnoreCase))
            {
                replacement = colored ? (isClosing ? BoldOff : BoldOn) : string.Empty;
                return true;
            }

            if (name.Equals("i", StringComparison.OrdinalIgnoreCase))
            {
                replacement = colored ? (isClosing ? ItalicOff : ItalicOn) : string.Empty;
                return true;
            }

            foreach (var dropped in DroppedTags)
            {
                if (!name.Equals(dropped, StringComparison.OrdinalIgnoreCase)) continue;

                // ANSIに対応物が無いため、タグだけ取り除いて本文は残す.
                replacement = string.Empty;
                return true;
            }

            return false;
        }

        /// <summary>
        /// <c>&lt;color&gt;</c>を変換する.
        /// </summary>
        /// <remarks>
        /// ANSIには入れ子の概念が無いため、開きタグでスタックへ積み、閉じタグで<b>外側の色を再送出</b>する。
        /// スタックが空になったら既定の前景色へ戻す.
        /// </remarks>
        private static bool TryConvertColor(bool isClosing, string value, bool colored, Stack<string> colorStack, out string replacement)
        {
            replacement = string.Empty;

            if (isClosing)
            {
                if (colorStack.Count > 0) colorStack.Pop();
                if (!colored) return true;

                replacement = colorStack.Count > 0 ? colorStack.Peek() : DefaultForeground;
                return true;
            }

            if (!TryParseColor(value, out var r, out var g, out var b)) return false;

            var sequence = string.Format(CultureInfo.InvariantCulture, "\x1b[38;5;{0}m", ToXterm256(r, g, b));
            colorStack.Push(sequence);

            if (colored) replacement = sequence;
            return true;
        }

        /// <summary>
        /// RGBを xterm-256 のカラー番号へ落とす.
        /// </summary>
        /// <remarks>
        /// 24bitカラー(<c>38;2;R;G;B</c>)は対応していない端末があり、macOS標準のTerminal.appが該当する。
        /// 非対応端末では引数の解釈がずれ、背景が塗られる等の化け方をする(実機で確認)。
        /// 256色(<c>38;5;N</c>)はほぼ全ての端末が解釈できるため、そちらへ寄せる。
        /// <para>
        /// 無彩色はグレースケールランプ(232-255)へ、それ以外は6x6x6のカラーキューブ(16-231)へ
        /// 最も近い番号を割り当てる.
        /// </para>
        /// </remarks>
        private static int ToXterm256(byte r, byte g, byte b)
        {
            if (r == g && g == b)
            {
                if (r < 8) return 16;
                if (r > 248) return 231;

                return 232 + (r - 8) * 24 / 247;
            }

            return 16
                   + 36 * Quantize(r)
                   + 6 * Quantize(g)
                   + Quantize(b);
        }

        /// <summary>8bitの成分を、カラーキューブの0-5へ丸める.</summary>
        private static int Quantize(byte component) => (component * 5 + 127) / 255;

        /// <summary>
        /// 色指定を解釈する。<c>#RGB</c> / <c>#RRGGBB</c> / <c>#RRGGBBAA</c> と色名に対応する.
        /// </summary>
        /// <remarks>アルファは端末で表現できないため無視する.</remarks>
        private static bool TryParseColor(string value, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            if (string.IsNullOrEmpty(value)) return false;

            var text = value.Trim().Trim('"');

            if (text[0] != '#')
            {
                if (!NamedColors.TryGetValue(text, out var named)) return false;

                (r, g, b) = named;
                return true;
            }

            var hex = text[1..];
            switch (hex.Length)
            {
                case 3:
                case 4:
                    return TryParseHex(hex[..1], out r) && TryParseHex(hex[1..2], out g) && TryParseHex(hex[2..3], out b);
                case 6:
                case 8:
                    return TryParseHex(hex[..2], out r) && TryParseHex(hex[2..4], out g) && TryParseHex(hex[4..6], out b);
                default:
                    return false;
            }
        }

        /// <summary>16進の1桁または2桁を8bit値へ変換する.</summary>
        /// <remarks>1桁の場合は<c>#RGB</c>記法として桁を複製する(<c>f</c> → <c>ff</c>).</remarks>
        private static bool TryParseHex(string hex, out byte value)
        {
            var normalized = hex.Length == 1 ? new string(hex[0], 2) : hex;

            return byte.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}

using System;
using System.Threading.Tasks;
using UnityEngine;
using YukimaruGames.Terminal.Composition.Shared;
using YukimaruGames.Terminal.Domain.Services;

namespace YukimaruGames.Terminal.Composition
{
    /// <summary>
    /// 全<see cref="IInstaller"/>実装に共通する構築フローをまとめた基底クラス.
    /// </summary>
    /// <remarks>
    /// Install()の骨格(Domain構築 → コマンド登録 → <see cref="BuildBackend"/> → Scope構築、
    /// および失敗時のCleanUp)と、全バックエンドで共通の設定<see cref="ITerminalOptions"/>を
    /// ここに集約する。Domain構築とScope組み立ての実体は<see cref="DomainBuilder"/> /
    /// <see cref="ScopeBuilder"/>にあり、本クラスは順序と後始末だけを持つ。
    ///
    /// 出力先(UIバックエンド、外部ターミナル等)ごとの差分は<see cref="BuildBackend"/>のみ。
    /// 描画を持つバックエンドは、さらに<see cref="RenderingInstallerBase"/>を継承して
    /// 描画コンテキストの構築だけを実装する(#145)。
    ///
    /// 派生クラスの型名・名前空間・アセンブリは変更しないこと。<c>SerializeReference</c>は
    /// シーン/プレハブへ{class, ns, asm}を保存するため、これらが変わると既存シーンの
    /// Installer参照が壊れる(基底クラスを挟む・階層内でフィールドの宣言クラスが変わるだけなら
    /// 影響しない).
    /// </remarks>
    [Serializable]
    public abstract class InstallerBase : IInstaller
    {
        [SerializeReference, SerializeInterface]
        private ITerminalOptions _options = new ImmediateModeOptions();

        /// <summary>
        /// 各種オプション.
        /// </summary>
        /// <remarks>
        /// 既定値を差し替えたい派生クラスは、自身のコンストラクタから設定する
        /// (C#はコンストラクタ本体が基底のフィールド初期化子より後に実行されるため確実に上書きできる).
        /// </remarks>
        protected ITerminalOptions Options
        {
            get => _options;
            set => _options = value;
        }

        /// <summary>
        /// 既定モード。<see cref="DomainBuilder"/>が生成し、<see cref="SyncOptions"/>で
        /// プロンプト文字列の再適用対象になる.
        /// </summary>
        protected NormalMode Mode { get; private set; }

        TerminalRuntimeScope IInstaller.Install()
        {
            // Null Object Pattern: 意図的な null は Null 実装にフォールバック
            var options = _options ?? CreateFallbackOptions();

            DomainContext domainContext = default;
            BackendContext backendContext = default;

            try
            {
                domainContext = DomainBuilder.Build(options);
                Mode = domainContext.Mode;
                DomainBuilder.RegisterCommands(in domainContext);
                backendContext = BuildBackend(options, in domainContext);
                return ScopeBuilder.Build(in domainContext, in backendContext);
            }
            catch (Exception)
            {
                ScopeBuilder.CleanUp(domainContext.Components);
                ScopeBuilder.CleanUp(backendContext.Components);
                ClearReferences();
                throw;
            }
        }

        void IInstaller.Uninstall(TerminalRuntimeScope scope)
        {
            try
            {
                (scope as IDisposable)?.Dispose();
            }
            finally
            {
                ClearReferences();
            }
        }

        async ValueTask IInstaller.UninstallAsync(TerminalRuntimeScope scope)
        {
            try
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    (scope as IDisposable)?.Dispose();
                }
            }
            finally
            {
                ClearReferences();
            }
        }

        void IInstaller.Resolve(TerminalRuntimeScope scope)
        {
            if (scope == null) return;

            SyncOptions(_options ?? CreateFallbackOptions());
            OnResolve();
        }

        /// <summary>
        /// 出力先(UIバックエンド、外部ターミナル等)を構築する.
        /// </summary>
        /// <remarks>
        /// 返した<see cref="BackendContext.Components"/>は<see cref="ScopeBuilder"/>が
        /// 更新対象・破棄対象へ振り分ける。描画を持たないバックエンドは
        /// <see cref="BackendContext.GUI"/>にnull、<see cref="BackendContext.View"/>に
        /// <see cref="NullTerminalView"/>を設定すること.
        /// </remarks>
        protected abstract BackendContext BuildBackend(ITerminalOptions options, in DomainContext domain);

        /// <summary>
        /// <see cref="Options"/>が意図的にnullにされていた場合のフォールバックを返す.
        /// </summary>
        protected virtual ITerminalOptions CreateFallbackOptions() => new NullOptions();

        /// <summary>
        /// 各種オプションを実行時インスタンスへ再適用する.
        /// </summary>
        protected virtual void SyncOptions(ITerminalOptions options)
        {
            if (Mode != null)
            {
                Mode.Prompt = options.Prompt;
            }
        }

        /// <summary>
        /// <see cref="IInstaller.Resolve"/>時、<see cref="SyncOptions"/>の後に呼ばれる.
        /// </summary>
        /// <remarks>
        /// テーマやアニメーションのように、派生クラス側が持つ設定の再適用に使う.
        /// </remarks>
        protected virtual void OnResolve()
        {
        }

        /// <summary>
        /// 保持している実行時インスタンスの参照を解放する.
        /// </summary>
        /// <remarks>
        /// 破棄そのものは<see cref="TerminalRuntimeScope"/>(Components経由のDispose)が行う。
        /// ここでの責務は参照を残さないことであり、派生クラスは自身が保持する参照をクリアしたうえで
        /// 基底実装を呼ぶこと.
        /// </remarks>
        protected virtual void ClearReferences()
        {
            Mode = null;
        }
    }
}

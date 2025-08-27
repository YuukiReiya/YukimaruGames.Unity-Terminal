using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Application.Model;

namespace YukimaruGames.Terminal.UI.Presentation.Model
{
    public sealed class TerminalLogPresenter : ITerminalLogPresenter, IDisposable
    {
        private readonly ITerminalService _service;
        
        /// <summary>
        /// ログの描画データを構築する上で必要なDto.
        /// </summary>
        /// <remarks>
        /// TerminalService.Logsのプロパティを介してデータを取得するとアクセスの度にMappingによるコストが掛かるためキャッシュの役割も兼ねる.
        /// </remarks>
        private readonly List<LogRenderData> _logs;
        
        /// <summary>
        /// ログの描画データのキャッシュ.
        /// </summary>
        private TerminalLogRenderData _cachedLogRenderData;

        public TerminalLogPresenter(ITerminalService service)
        {
            _service = service;
            _logs = new List<LogRenderData>(service.LogBufferSize);
            _logs.AddRange(service.Logs);
            _cachedLogRenderData = new TerminalLogRenderData(_service.Logs);

            _service.OnLogUpdated += HandleLogUpdated;
            _service.OnLogAdded += HandleLogAdded;
            _service.OnLogRemoved += HandleLogRemoved;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <p>OnGUIを介して毎フレーム呼び出される想定のため、ここでは new せずにキャッシュを返す。</p>
        /// <p>キャッシュは更新</p>
        /// </remarks>
        TerminalLogRenderData ITerminalLogRenderDataProvider.GetRenderData() => _cachedLogRenderData;

        public void Dispose()
        {
            _service.OnLogUpdated -= HandleLogUpdated;
            _service.OnLogAdded -= HandleLogAdded;
            _service.OnLogRemoved -= HandleLogRemoved;
        }

        private void HandleLogUpdated() => _cachedLogRenderData = new TerminalLogRenderData(_logs);
        
        private void HandleLogAdded(LogRenderData[] renderDataArray)
        {
            _logs.AddRange(renderDataArray);
        }

        private void HandleLogRemoved(LogRenderData[] renderDataArray)
        {
            _logs.RemoveAll(renderDataArray.Contains);
        }
    }
}

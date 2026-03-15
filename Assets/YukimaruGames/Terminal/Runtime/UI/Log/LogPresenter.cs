using System;
using System.Collections.Generic;
using System.Linq;
using YukimaruGames.Terminal.Application;
using YukimaruGames.Terminal.Application.Dto;

namespace YukimaruGames.Terminal.UI.Log
{
    public sealed class LogPresenter : ILogPresenter, IDisposable
    {
        private readonly ITerminalService _service;
        
        /// <summary>
        /// ログの描画データを構築する上で必要なDto.
        /// </summary>
        /// <remarks>
        /// TerminalService.Logsのプロパティを介してデータを取得するとアクセスの度にMappingによるコストが掛かるためキャッシュの役割も兼ねる.
        /// </remarks>
        private readonly List<LogEntry> _logs;
        
        /// <summary>
        /// ログの描画データのキャッシュ.
        /// </summary>
        private LogRenderData _cachedLogRenderData;

        public LogPresenter(ITerminalService service)
        {
            _service = service;
            _logs = new List<LogEntry>(service.LogBufferSize);
            _logs.AddRange(service.Logs);
            _cachedLogRenderData = new LogRenderData(_service.Logs);

            _service.OnLogUpdated += HandleLogUpdated;
            _service.OnLogAdded += HandleLogAdded;
            _service.OnLogRemoved += HandleLogRemoved;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <p>OnGUIを介して毎フレーム呼び出される想定のため、ここでは new せずにキャッシュを返す。</p>
        /// <p>キャッシュは更新</p>
        /// </remarks>
        LogRenderData ILogRenderDataProvider.GetRenderData() => _cachedLogRenderData;

        public void Dispose()
        {
            _service.OnLogUpdated -= HandleLogUpdated;
            _service.OnLogAdded -= HandleLogAdded;
            _service.OnLogRemoved -= HandleLogRemoved;
        }

        private void HandleLogUpdated() => _cachedLogRenderData = new LogRenderData(_logs);
        
        private void HandleLogAdded(LogEntry[] renderDataArray)
        {
            _logs.AddRange(renderDataArray);
        }

        private void HandleLogRemoved(LogEntry[] renderDataArray)
        {
            _logs.RemoveAll(renderDataArray.Contains);
        }
    }
}

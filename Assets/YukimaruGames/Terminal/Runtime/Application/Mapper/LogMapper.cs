using System;
using YukimaruGames.Terminal.Application.Model;
using YukimaruGames.Terminal.Domain.Model;

namespace YukimaruGames.Terminal.Application.Mapper
{
    public static class LogMapper
    {
        public static LogRenderData Mapping(CommandLog log)
        {
            return new LogRenderData(
                log.Id,
                log.MessageType,
                log.Timestamp,
                log.Message);
        }

        public static LogRenderData[] Mapping(CommandLog[] logs)
        {
            var size = logs is { Length: > 0 } ? logs.Length : 0;
            if (size == 0)
            {
                return Array.Empty<LogRenderData>();
            }

            var array = new LogRenderData[size];
            for (var i = 0; i < size; i++)
            {
                array[i] = Mapping(logs[i]);
            }

            return array;
        }
    }
}

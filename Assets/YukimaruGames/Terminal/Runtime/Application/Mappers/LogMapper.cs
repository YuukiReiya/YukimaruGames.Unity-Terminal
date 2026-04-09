using System;
using YukimaruGames.Terminal.Application.Models;
using YukimaruGames.Terminal.Domain.Abstractions.Models.Entities;

namespace YukimaruGames.Terminal.Application.Mappers
{
    public static class LogMapper
    {
        public static LogEntry Mapping(CommandLog log)
        {
            return new LogEntry(
                log.Id,
                log.MessageType,
                log.Timestamp,
                log.Message);
        }

        public static LogEntry[] Mapping(CommandLog[] logs)
        {
            var size = logs is { Length: > 0 } ? logs.Length : 0;
            if (size == 0)
            {
                return Array.Empty<LogEntry>();
            }

            var array = new LogEntry[size];
            for (var i = 0; i < size; i++)
            {
                array[i] = Mapping(logs[i]);
            }

            return array;
        }
    }
}

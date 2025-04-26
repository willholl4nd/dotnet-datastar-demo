using System.Collections.Concurrent;

namespace dotnet_html_sortable_table.Services
{
    public class SessionQueueStore
    {
        private readonly ConcurrentDictionary<string, BlockingCollection<string>> _queues = new();

        public BlockingCollection<string> GetOrCreate(string sessionId)
        {
            return _queues.GetOrAdd(sessionId, _ => new BlockingCollection<string>());
        }

        public bool TryGet(string sessionId, out BlockingCollection<string>? queue)
        {
            return _queues.TryGetValue(sessionId, out queue);
        }

        public void Remove(string sessionId)
        {
            _queues.TryRemove(sessionId, out _);
        }
    }

}

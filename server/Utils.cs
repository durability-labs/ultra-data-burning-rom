
namespace UltraDataBurningROM.Server
{
    public static class Utils
    {
        public static long ToUnixTimestamp(DateTime utc)
        {
            return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
        }

        public static string[] SplitTagString(string tags)
        {
            return tags
                .Replace(",", " ")
                .Replace(".", " ")
                .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class CapMap<TKey, TValue> where TKey : notnull
    {
        private readonly int cap = 10000;
        private readonly Dictionary<TKey, TValue> _map = new Dictionary<TKey, TValue>();

        public void Add(TKey key, TValue dictionary)
        {
            if (_map.Count > cap) _map.Clear();
            _map.Add(key, dictionary);
        }

        public TValue Get(TKey key)
        {
            return _map[key];
        }

        public void Set(TKey key, TValue value)
        {
            if (_map.Count > cap) _map.Clear();
            _map[key] = value;
        }

        public bool ContainsKey(TKey key)
        {
            return _map.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue? maybeValue)
        {
            return _map.TryGetValue(key, out maybeValue);
        }

        public void ClearKey(TKey key)
        {
            if (_map.ContainsKey(key))
            {
                _map.Remove(key);
            }
        }
    }
}

namespace Helios.Connect.Api;

internal sealed class ExpiringDeliveryReplayCache
{
    private readonly int _capacity;
    private readonly TimeSpan _timeToLive;
    private readonly long _sweepIntervalTicks;
    private readonly object _gate = new();
    private readonly Dictionary<string, long> _entries = new(StringComparer.Ordinal);
    private long _nextSweepAtTicks;

    internal ExpiringDeliveryReplayCache(int capacity, TimeSpan timeToLive)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (timeToLive <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeToLive));
        _capacity = capacity;
        _timeToLive = timeToLive;
        _sweepIntervalTicks = Math.Min(timeToLive.Ticks, TimeSpan.TicksPerMinute);
    }

    internal bool TryRegister(string key, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("A replay key is required.", nameof(key));
        var nowTicks = now.UtcDateTime.Ticks;

        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var existingExpiry) && existingExpiry > nowTicks)
                return false;

            _entries.Remove(key);
            if (nowTicks >= _nextSweepAtTicks || _entries.Count >= _capacity)
            {
                RemoveExpired(nowTicks);
                _nextSweepAtTicks = nowTicks + _sweepIntervalTicks;
            }

            if (_entries.Count >= _capacity)
            {
                string? oldestKey = null;
                var oldestExpiry = long.MaxValue;
                foreach (var entry in _entries)
                {
                    if (entry.Value >= oldestExpiry) continue;
                    oldestKey = entry.Key;
                    oldestExpiry = entry.Value;
                }
                if (oldestKey is not null) _entries.Remove(oldestKey);
            }

            _entries[key] = nowTicks + _timeToLive.Ticks;
            return true;
        }
    }

    private void RemoveExpired(long nowTicks)
    {
        foreach (var key in _entries.Where(entry => entry.Value <= nowTicks).Select(entry => entry.Key).ToArray())
            _entries.Remove(key);
    }
}

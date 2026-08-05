namespace NpcLocator.Multiplayer;

internal sealed class RequestRateLimiter
{
    private readonly Dictionary<long, Queue<DateTimeOffset>> requestsByPlayer = new();

    public bool TryAcquire(long playerId, int maxRequestsPerSecond)
    {
        int limit = Math.Clamp(maxRequestsPerSecond, 1, 20);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset cutoff = now.AddSeconds(-1);

        if (!this.requestsByPlayer.TryGetValue(playerId, out Queue<DateTimeOffset>? requests))
        {
            requests = new Queue<DateTimeOffset>();
            this.requestsByPlayer[playerId] = requests;
        }

        while (requests.Count > 0 && requests.Peek() <= cutoff)
            requests.Dequeue();

        if (requests.Count >= limit)
            return false;

        requests.Enqueue(now);
        return true;
    }

    public void Forget(long playerId) => this.requestsByPlayer.Remove(playerId);

    public void Clear() => this.requestsByPlayer.Clear();
}

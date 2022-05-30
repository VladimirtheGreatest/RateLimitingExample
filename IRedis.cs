using StackExchange.Redis;

namespace RateLimitingExample
{
    public interface IRedis
    {
        ConnectionMultiplexer Connection { get; }
    }
}
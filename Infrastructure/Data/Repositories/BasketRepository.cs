using System.Text.Json;
using Core.Entities;
using Core.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Data.Repositories
{
    public class RedisBasketRepository : IBasketRepository
    {
        private readonly IConnectionMultiplexer _redisConnection;

        public RedisBasketRepository(IConnectionMultiplexer redisConnection)
        {
            _redisConnection = redisConnection;
        }

        public async Task<Basket> GetBasketAsync(string basketId)
        {
            if (_redisConnection == null) return null;

            var database = _redisConnection.GetDatabase();
            var basketData = await database.StringGetAsync(GetRedisKey(basketId));

            return basketData.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<Basket>(basketData);
        }

        public async Task<Basket> UpdateBasketAsync(Basket basket)
        {
            if (_redisConnection == null) return basket;

            var database = _redisConnection.GetDatabase();
            await database.StringSetAsync(GetRedisKey(basket.Id), JsonSerializer.Serialize(basket));

            return basket;
        }

        public async Task<bool> DeleteBasketAsync(string basketId)
        {
            if (_redisConnection == null) return true;

            var database = _redisConnection.GetDatabase();
            return await database.KeyDeleteAsync(GetRedisKey(basketId));
        }

        private static string GetRedisKey(string basketId) => $"Basket:{basketId}";
    }
}
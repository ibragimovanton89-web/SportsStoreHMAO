using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace SportsStore.Infrastructure.Customers;

/// <summary>Лимит чувствительных действий по хешу нормализованной почты, дополняющий HTTP-лимит по IP.</summary>
public sealed class AccountEmailThrottle : IDisposable
{
    /// <summary>Ограниченный кэш; ключи не содержат открытой почты.</summary>
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10000 });
    /// <summary>Процессная соль исключает перебор почтовых адресов по ключам памяти.</summary>
    private readonly byte[] salt = RandomNumberGenerator.GetBytes(32);
    /// <summary>Синхронизирует проверку и увеличение счётчика.</summary>
    private readonly object gate = new();
    /// <summary>Разрешает до трёх запросов одного назначения на адрес за 15 минут.</summary>
    public bool Allow(string normalizedEmail, string purpose, int limit = 3)
    {
        var key = Convert.ToHexString(HMACSHA256.HashData(salt, Encoding.UTF8.GetBytes(purpose + ":" + normalizedEmail)));
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var counter = cache.Get<(int Count, DateTimeOffset End)?>(key) ?? (0, now.AddMinutes(15));
            if (counter.Count >= limit) return false;
            cache.Set(key, (counter.Count + 1, counter.End), new MemoryCacheEntryOptions { AbsoluteExpiration = counter.End, Size = 1 });
            return true;
        }
    }
    /// <summary>Освобождает ограниченный кэш при остановке приложения.</summary>
    public void Dispose() => cache.Dispose();
}

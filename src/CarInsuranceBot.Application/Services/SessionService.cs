using CarInsuranceBot.Application.Interfaces;
using CarInsuranceBot.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CarInsuranceBot.Application.Services;

public class SessionService(IMemoryCache _cache) : ISessionService
{
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

    public UserSession GetOrCreate(long chatId)
    {
        if (_cache.TryGetValue(chatId, out UserSession ? session) && session is not null)
            return session;

        session = new UserSession { ChatId = chatId };

        _cache.Set(chatId, session, new MemoryCacheEntryOptions
        {
            SlidingExpiration = SessionTtl
        });

        return session;
    }
    public void Update(UserSession session)
    {
        session.UpdatedAt = DateTime.UtcNow;

        _cache.Set(session.ChatId, session, new MemoryCacheEntryOptions
        {
            SlidingExpiration = SessionTtl
        });
    }
    public void Remove(long chatId)
    {
        _cache.Remove(chatId);
    }
}
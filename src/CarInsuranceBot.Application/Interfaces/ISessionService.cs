using CarInsuranceBot.Domain.Models;

namespace CarInsuranceBot.Application.Interfaces;

// A contract for working with user sessions.
public interface ISessionService
{
    //Returns existing session or creates new one.
    UserSession GetOrCreate(long chatId);

    //Saves the changed session to the cache.
    void Update(UserSession session);

    //Deletes the session when the dialogue is complete.
    void Remove(long chatId);
}

using System;

namespace TicketManagementSystem
{
    public interface IUserRepository : IDisposable
    {
        User GetAccountManager();
        User GetUser(string username);
    }
}
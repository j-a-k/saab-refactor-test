using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using TicketManagementSystem;

namespace TickManagementsystemTests
{


    [TestFixture]
    public class TicketServiceTests
    {
        [Test]
        public void CreateTicketThrowsIfTorDescInvalid()
        {
            var target = new TicketService();

            Assert.Throws<InvalidTicketException>(() => target.CreateTicket(null, Priority.Low, "foo", "bar", DateTime.Now, false), "t is null");
            Assert.Throws<InvalidTicketException>(() => target.CreateTicket("", Priority.Low, "foo", "bar", DateTime.Now, false), "t is empty");
            Assert.Throws<InvalidTicketException>(() => target.CreateTicket("foo", Priority.Low, "bar", null,  DateTime.Now, false), "desc is null");
            Assert.Throws<InvalidTicketException>(() => target.CreateTicket("foo", Priority.Low, "bar", "", DateTime.Now, false), "desc is empty");

        }

        private class UserRepositoryMock : IUserRepository, IDisposable
        {
            public void Dispose()
            {
                //nothing to dispose
            }

            public User GetAccountManager()
            {
                return new User()
                {
                    Username = "TestManager",
                    FirstName = "TMfirst",
                    LastName = "TMLast"
                };
            }

            public User GetUser(string username)
            {
                if (username == "TestUser")
                {
                    return new User()
                    {
                        Username = "TestUser",
                        FirstName = "TUfirst",
                        LastName = "TULast"
                    };
                }
                return null;
            }
        }

        [Test]
        public void CreateTicketThrowsIfUnkownUser()
        {
            var target = new TicketService();
            target.userRepositoryCreator = () => new UserRepositoryMock();

            Assert.Throws<UnknownUserException>(() => target.CreateTicket("foo", Priority.Low, null, "bar", DateTime.Now, false), "null user");
            Assert.Throws<UnknownUserException>(() => target.CreateTicket("foo", Priority.Low, "NotAUser", "bar", DateTime.Now, false), "unkown user user");

        }
    }
}

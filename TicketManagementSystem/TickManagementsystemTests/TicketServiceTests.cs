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
    }
}

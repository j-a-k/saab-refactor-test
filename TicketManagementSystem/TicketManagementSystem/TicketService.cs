using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using EmailService;

namespace TicketManagementSystem
{
    public class TicketService
    {
        /// <summary>
        /// property to allow test UserRepository to be injected nstead of real one
        /// </summary>
        public Func<IUserRepository> UserRepositoryCreator { get; set; }

        /// <summary>
        /// property to allow test IEmailService to be injected nstead of real one
        /// </summary>
        public Func<IEmailService> EmailServiceCreator { get; set; }

        public Func<Ticket, int> RegisterTicket { get; set; }

        /// <summary>
        /// Constructor, used as is in program.cs so can't be changed to insert dependencies here
        /// </summary>
        public TicketService()
        {
            UserRepositoryCreator = () => new UserRepository();
            EmailServiceCreator = () => new EmailServiceProxy();
            RegisterTicket = TicketRepository.CreateTicket;
        }

        /// <summary>
        /// Create a Ticket
        /// </summary>
        /// <param name="t">title, must not be null or emtpy</param>
        /// <param name="p">priority</param>
        /// <param name="assignedTo">username to assign to, must be valid</param>
        /// <param name="desc">description, must not be null or emtpy</param>
        /// <param name="d"></param>
        /// <param name="isPayingCustomer"></param>
        /// <returns>id for ticket as registered in the TicketRepository</returns>
        public int CreateTicket(string t, Priority p, string assignedTo, string desc, DateTime d, bool isPayingCustomer)
        {
            CheckIfTitleAndDescriptionAreValid(t, desc);

            User user = GetUserOrThrow(assignedTo);

            p = RaisePriorityIfNeeded(p, t, d);

            EmailIfHighPriority(p, t, assignedTo);

            (double price, User accountManager) = SetPriceAndAccountManager(isPayingCustomer, p);

            var ticket = new Ticket()
            {
                Title = t,
                AssignedUser = user,
                Priority = p,
                Description = desc,
                Created = d,
                PriceDollars = price,
                AccountManager = accountManager
            };

            var id = RegisterTicket(ticket);

            // Return the id
            return id;
        }

        private (double price, User accountManager) SetPriceAndAccountManager(bool isPayingCustomer, Priority p)
        {
            double price = 0;
            User accountManager = null;
            if (isPayingCustomer)
            {
                // Only paid customers have an account manager.
                accountManager = UserRepositoryCreator.Invoke().GetAccountManager();
                if (p == Priority.High)
                {
                    price = 100;
                }
                else
                {
                    price = 50;
                }
            }
            return (price, accountManager);
        }

        private void EmailIfHighPriority(Priority priority, string title, string assignedTo)
        {
            if (priority == Priority.High)
            {
                var emailService = EmailServiceCreator.Invoke();
                emailService.SendEmailToAdministrator(title, assignedTo);
            }
        }

        private static Priority RaisePriorityIfNeeded(Priority priority, string title, DateTime dateTime)
        {
            if (dateTime < DateTime.UtcNow - TimeSpan.FromHours(1) || title.Contains("Crash") || title.Contains("Important") || title.Contains("Failure"))
            {
                if (priority == Priority.Low)
                {
                    return Priority.Medium;
                }
                else if (priority == Priority.Medium)
                {
                    return Priority.High;
                }
            }
            return priority;
        }

        private User GetUserOrThrow(string assignedTo)
        {
            User user = null;
            using (var ur = UserRepositoryCreator.Invoke())
            {
                if (assignedTo != null)
                {
                    user = ur.GetUser(assignedTo);
                }
            }

            if (user == null)
            {
                throw new UnknownUserException("User " + assignedTo + " not found");
            }

            return user;
        }

        private static void CheckIfTitleAndDescriptionAreValid(string title, string description)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
            {
                throw new InvalidTicketException("Title or description were null");
            }
        }

        public void AssignTicket(int id, string username)
        {
            User user = null;
            using (var ur = new UserRepository())
            {
                if (username != null)
                {
                    user = ur.GetUser(username);
                }
            }

            if (user == null)
            {
                throw new UnknownUserException("User not found");
            }

            var ticket = TicketRepository.GetTicket(id);

            if (ticket == null)
            {
                throw new ApplicationException("No ticket found for id " + id);
            }

            ticket.AssignedUser = user;

            TicketRepository.UpdateTicket(ticket);
        }

        private void WriteTicketToFile(Ticket ticket)
        {
            var ticketJson = JsonSerializer.Serialize(ticket);
            File.WriteAllText(Path.Combine(Path.GetTempPath(), $"ticket_{ticket.Id}.json"), ticketJson);
        }
    }

    public enum Priority
    {
        High,
        Medium,
        Low
    }
}

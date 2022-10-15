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


        /// <summary>
        /// Constructor, used as is in program.cs so can't be changed to insert dependencies here
        /// </summary>
        public TicketService()
        {
            UserRepositoryCreator = () => new UserRepository();
            EmailServiceCreator = () => new EmailServiceProxy();
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
        /// <exception cref="UnknownUserException">if username is not valid</exception>
        /// <exception cref="InvalidTicketException">if title or description empty or null</exception>
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

            //could remove dependency on this in a similar way but its a light class so leaving it for now
            var id = TicketRepository.CreateTicket(ticket);

            // Return the id
            return id;
        }

        /// <summary>
        /// Assign a user to a ticket by username and ticket id
        /// </summary>
        /// <param name="id">of the ticket to assign</param>
        /// <param name="username">of the user to assign to the ticket</param>
        /// <exception cref="ApplicationException">if ticket id is not valid</exception>
        /// <exception cref="UnknownUserException">if username is not valid</exception>
        public void AssignTicket(int id, string username)
        {
            User user = GetUserOrThrow(username);

            var ticket = TicketRepository.GetTicket(id);

            if (ticket == null)
            {
                throw new ApplicationException("No ticket found for id " + id);
            }

            ticket.AssignedUser = user;

            TicketRepository.UpdateTicket(ticket);
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

        private User GetUserOrThrow(string username)
        {
            User user = null;
            using (var ur = UserRepositoryCreator.Invoke())
            {
                if (username != null)
                {
                    user = ur.GetUser(username);
                }
            }

            if (user == null)
            {
                throw new UnknownUserException("User " + username + " not found");
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

    }

    public enum Priority
    {
        High,
        Medium,
        Low
    }
}

using System;
using System.Threading.Tasks;
using ArrangeMarriage.Application.Interfaces;

namespace ArrangeMarriage.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        public Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Simulation of email dispatch
            Console.WriteLine($"[SIMULATED EMAIL] To: {toEmail} | Subject: {subject}");
            Console.WriteLine($"Body: {body}");
            return Task.CompletedTask;
        }

        public Task SendSmsAsync(string toPhoneNumber, string message)
        {
            // Simulation of SMS/WhatsApp dispatch
            Console.WriteLine($"[SIMULATED SMS] To: {toPhoneNumber} | Message: {message}");
            return Task.CompletedTask;
        }
    }
}

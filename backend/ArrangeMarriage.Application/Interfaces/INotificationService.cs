using System.Threading.Tasks;

namespace ArrangeMarriage.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendSmsAsync(string toPhoneNumber, string message);
    }
}

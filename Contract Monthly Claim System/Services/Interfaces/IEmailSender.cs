namespace Contract_Monthly_Claim_System.Services.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailAsync(string email, string subject, string htmlMessage, string plainTextMessage);
    }
}

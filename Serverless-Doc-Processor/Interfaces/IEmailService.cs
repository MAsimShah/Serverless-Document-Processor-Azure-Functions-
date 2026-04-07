namespace Serverless_Doc_Processor.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    }
}

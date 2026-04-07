using SendGrid;
using SendGrid.Helpers.Mail;
using Serverless_Doc_Processor.Interfaces;

namespace Serverless_Doc_Processor.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;

        public SendGridEmailService()
        {
            _apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY")!;
            _fromEmail = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL")!;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient email cannot be empty.");

            var client = new SendGridClient(_apiKey);

            var from = new EmailAddress(_fromEmail);
            var toEmail = new EmailAddress(to);

            var msg = isHtml
                ? MailHelper.CreateSingleEmail(from, toEmail, subject, plainTextContent: null, htmlContent: body)
                : MailHelper.CreateSingleEmail(from, toEmail, subject, plainTextContent: body, htmlContent: null);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Body.ReadAsStringAsync();
                throw new Exception($"Failed to send email: {error}");
            }
        }
    }
}
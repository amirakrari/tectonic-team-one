using System.Net;
using System.Net.Mail;
using ExpenseWatch.Api.Models;

namespace ExpenseWatch.Api.Services;

public sealed class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
{
    public bool Send(Notification notification)
    {
        try
        {
            var host = configuration["Email:Host"];
            var from = configuration["Email:From"];
            var recipient = configuration["Email:Recipient"];
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)
                || string.IsNullOrWhiteSpace(recipient))
            {
                throw new InvalidOperationException("Demo SMTP settings are missing.");
            }

            using var client = new SmtpClient(host, configuration.GetValue<int>("Email:Port"))
            {
                EnableSsl = configuration.GetValue<bool>("Email:EnableSsl"),
                Timeout = 10_000,
                UseDefaultCredentials = false
            };
            var username = Environment.GetEnvironmentVariable("Email__Username");
            var password = Environment.GetEnvironmentVariable("Email__Password");
            if (!string.IsNullOrEmpty(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage(from, recipient)
            {
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = false
            };
            client.Send(message);
            logger.LogInformation("Notification {Id} ({Kind}) accepted by SMTP.",
                notification.Id, notification.Kind);
            return true;
        }
        catch (Exception error) when (error is SmtpException or InvalidOperationException
            or ArgumentException or FormatException)
        {
            logger.LogWarning("Notification {Id} ({Kind}) delivery failed.",
                notification.Id, notification.Kind);
            return false;
        }
    }
}

using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using ExpenseWatch.Api.Models.Domain;

namespace ExpenseWatch.Api.Services;

public sealed class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger,
    IHttpClientFactory clients)
{
    public bool Send(Notification notification)
    {
        try
        {
            var from = configuration["Email:From"];
            var recipient = notification.RecipientAddress;
            if (string.IsNullOrWhiteSpace(from)
                || string.IsNullOrWhiteSpace(recipient))
            {
                throw new InvalidOperationException("Demo email addresses are missing.");
            }

            var mailpitUrl = configuration["Email:MailpitUrl"];
            if (!string.IsNullOrWhiteSpace(mailpitUrl))
            {
                using var http = clients.CreateClient("Mailpit");
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    mailpitUrl.TrimEnd('/') + "/api/v1/send")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new
                    {
                        from = new { email = from },
                        to = new[] { new { email = recipient } },
                        subject = notification.Subject,
                        text = notification.Body
                    }), Encoding.UTF8, "application/json")
                };
                using var response = http.Send(request);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Notification {Id} ({Kind}) accepted by Mailpit HTTP.",
                    notification.Id, notification.ConditionId);
                return true;
            }

            var host = configuration["Email:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Demo SMTP host is missing.");
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
                notification.Id, notification.ConditionId);
            return true;
        }
        catch (Exception error) when (error is SmtpException or InvalidOperationException
            or ArgumentException or FormatException or HttpRequestException or OperationCanceledException)
        {
            logger.LogWarning("Notification {Id} ({Kind}) delivery failed.",
                notification.Id, notification.ConditionId);
            return false;
        }
    }
}

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RestaurantBooking.Application.Abstractions;

namespace RestaurantBooking.Infrastructure.Email;

public class SmtpEmailService(IOptions<SmtpSettings> options) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var s = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(s.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(s.Host, s.Port, SecureSocketOptions.Auto, ct);
        if (!string.IsNullOrEmpty(s.User))
            await client.AuthenticateAsync(s.User, s.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}

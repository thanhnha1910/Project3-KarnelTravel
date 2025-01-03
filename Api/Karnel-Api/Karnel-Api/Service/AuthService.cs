using System.Security.Cryptography;
using System.Text;
using Karnel_Api.Data;
using Karnel_Api.DTO;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Karnel_Api.Service;

public class AuthService
{
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;

    public AuthService(DatabaseContext context, IConfiguration configuration, JwtService jwtService)
    {
        _context = context;
        _configuration = configuration;
        _jwtService = jwtService;
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public async Task<bool> RegisterUserAsync(RegisterDTO registerDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        if (user != null)
        {
            throw new Exception($"Email {registerDto.Email} already exists");
        }
        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            throw new Exception("Passwords do not match.");
        }
        var verificationToken = Guid.NewGuid().ToString();
        var hashedPassword = HashPassword(registerDto.Password);
        var newUser = new User
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            Password = hashedPassword,
            Role = registerDto.Role,
            IsEmailConfirmed = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow,
            Status = true
        };
        await _context.Users.AddAsync(newUser);
        await _context.SaveChangesAsync();
        var verificationLink = $"http://localhost:5173/verify-email?token={verificationToken}";
        var emailBody = $@"
                    <h2>Welcome to Our Application!</h2>
                    <p>Please click the link below to verify your email address:</p>
                    <a href='{verificationLink}'>Verify Email</a>
                    <p>This link will expire in 24 hours.</p>";

        await SendEmailAsync(registerDto.Email, "Email Verification", emailBody);
        return true;
    }
    private bool VerifyPassword(string password, string hashedPassword)
    {
        var hashedInputPassword = HashPassword(password);
        return hashedInputPassword == hashedPassword;
    }
    public async Task<string> Login(LoginDTO loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null)
        {
            throw new Exception($"Email {loginDto.Email} not found");
        }
        if (!user.IsEmailConfirmed)
        {
            throw new Exception("Please verify your email before logging in");
        }

        if (!VerifyPassword(loginDto.Password, user.Password))
        {
            throw new Exception("Invalid password");
        }
        var token = _jwtService.GenerateToken(user);
        return token;
    }
    public async Task<bool> VerifyEmail(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.EmailVerificationToken == token &&
            u.EmailVerificationTokenExpires > DateTime.UtcNow);

        if (user == null)
        {
            throw new Exception("Invalid or expired verification token");
        }

        user.IsEmailConfirmed = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;

        await _context.SaveChangesAsync();
        return true;
    }


    public async Task SendEmailAsync(string email, string subject, string body)
    {
        var smtpHost = _configuration["SmtpSettings:Host"];
        var smtpPort = int.Parse(_configuration["SmtpSettings:Port"]!);
        var smtpUsername = _configuration["SmtpSettings:Username"];
        var smtpPassword = _configuration["SmtpSettings:Password"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("KarnelApi", smtpUsername));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = subject;
        message.Body = new TextPart("html")
        {
            Text = body
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
    private string GenerateResetToken()
    {
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return "E" + new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task RequestResetPassword(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        if (user.IsEmailConfirmed == false)
        {
            throw new Exception($"please confirm your email before trying to reset your password");
        }

        var resetToken = GenerateResetToken(); // Generates something like "E3105"
        user.ResetPasswordToken = resetToken;
        user.ResetPasswordTokenExpires = DateTime.UtcNow.AddMinutes(15); // Token expires in 15 minutes

        await _context.SaveChangesAsync();

        var emailBody = $@"
                <h2>Password Reset Request</h2>
                <p>Your password reset code is: <strong>{resetToken}</strong></p>
                <p>This code will expire in 15 minutes.</p>
                <p>If you did not request this, please ignore this email.</p>";

        await SendEmailAsync(email, "Password Reset Request", emailBody);
    }

    public async Task ResetPassword(string token, ResetPassword resetPassword)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new Exception("Invalid reset token");
        }

        if (resetPassword.NewPassword != resetPassword.ConfirmPassword)
        {
            throw new Exception("Passwords do not match");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.ResetPasswordToken == token &&
            u.ResetPasswordTokenExpires > DateTime.UtcNow);

        if (user == null)
        {
            throw new Exception("Invalid or expired reset token");
        }

        user.Password = HashPassword(resetPassword.NewPassword);
        user.ResetPasswordToken = null;
        user.ResetPasswordTokenExpires = null;

        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<User>> GetAll()
    {
        var user = await _context.Users.ToListAsync();
        return user;
    }

}
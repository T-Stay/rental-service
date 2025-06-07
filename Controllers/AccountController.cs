using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalService.Models;
using RentalService.Data;
using System.Threading.Tasks;
using System;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Net;

namespace RentalService.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string name, string role, string? returnUrl = null)
        {
            User user;
            var userRoleEnum = UserRoleHelper.FromIdentityRoleString(role);
            if (userRoleEnum == UserRole.Host)
            {
                user = new RentalService.Models.Host
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    AvatarUrl = "",
                    Role = userRoleEnum,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PhoneNumber = "",
                    Buildings = new List<Building>()
                };
            }
            else
            {
                user = new Customer
                {
                    UserName = email,
                    Email = email,
                    Name = name,
                    AvatarUrl = "",
                    Role = userRoleEnum,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PhoneNumber = "",
                    Favorites = new List<Favorite>(),
                    BookingRequests = new List<BookingRequest>(),
                    ViewAppointments = new List<ViewAppointment>(),
                    Reviews = new List<Review>(),
                    Notifications = new List<Notification>()
                };
            }
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Add initial contact info (email)
                var dbUser = await _userManager.FindByEmailAsync(user.Email);
                if (dbUser != null)
                {
                    var contact = new ContactInformation
                    {
                        Id = Guid.NewGuid(),
                        UserId = dbUser.Id,
                        Type = ContactType.Email,
                        Data = dbUser.Email ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ContactInformations.Add(contact);
                    await _context.SaveChangesAsync();
                }
                var identityRole = UserRoleHelper.ToIdentityRoleString(userRoleEnum);
                await _userManager.AddToRoleAsync(user, identityRole);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Scheme) ?? string.Empty;
                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(callbackUrl))
                {
                    // Improved email content for better deliverability
                    var emailBody = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Welcome to Trọ Tốt!</h2>
        <p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
        <p>Thank you for registering. Please confirm your account by clicking the button below:</p>
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='background:#007bff;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Confirm Email</a>
        </p>
        <p>If you did not register, please ignore this email.</p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
                    SendEmail(user.Email, "Confirm your email - Rental Service", emailBody);
                }
                ViewBag.Message = "Registration successful! Please check your email to confirm your account.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return View();
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();
            var result = await _userManager.ConfirmEmailAsync(user, code);
            ViewBag.Message = result.Succeeded ? "Email confirmed!" : "Error confirming email.";
            return View();
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var identity = new System.Security.Claims.ClaimsIdentity("Identity.Application");
                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()));
                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? user.Email ?? ""));
                if (roles.Count > 0)
                {
                    identity.AddClaim(new System.Security.Claims.Claim("role", roles[0]));
                    identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roles[0]));
                }
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                await _signInManager.SignOutAsync();
                await HttpContext.SignInAsync("Identity.Application", principal);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                // if host then redirect to host dashboard
                if (roles.Contains("host"))
                {
                    return RedirectToAction("Index", "HostDashboard");
                }
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal user not found or not confirmed
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { code }, protocol: Request.Scheme) ?? string.Empty;
            if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(callbackUrl))
            {
                // Improved email content for better deliverability
                var emailBody = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Password Reset Request</h2>
        <p>Hi {System.Net.WebUtility.HtmlEncode(user.Name)},</p>
        <p>We received a request to reset your password. Click the button below to reset it:</p>
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' style='background:#28a745;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Reset Password</a>
        </p>
        <p>If you did not request a password reset, you can safely ignore this email.</p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
                SendEmail(user.Email, "Reset your password - Rental Service", emailBody);
            }
            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string code = "") => View(model: code);

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string password, string code)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");
            var result = await _userManager.ResetPasswordAsync(user, code, password);
            if (result.Succeeded)
                return RedirectToAction("ResetPasswordConfirmation");
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        private void SendEmail(string to, string subject, string html)
        {
            Task.Run(() =>
            {
                try
                {
                    var host = Environment.GetEnvironmentVariable("SMTP_HOST");
                    var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
                    if (string.IsNullOrEmpty(portStr))
                    {
                        throw new InvalidOperationException("SMTP_PORT environment variable is not set.");
                    }
                    var port = int.Parse(portStr);
                    var user = Environment.GetEnvironmentVariable("SMTP_USER");
                    var pass = Environment.GetEnvironmentVariable("SMTP_PASS");
                    var from = Environment.GetEnvironmentVariable("SMTP_FROM");

                    // log SMTP configuration
                    Console.WriteLine($"SMTP Host: {host}, Port: {port}, From: {from}");
                    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(from))
                    {
                        throw new InvalidOperationException("SMTP configuration is not set properly.");
                    }

                    var client = new SmtpClient(host, port)
                    {
                        Credentials = new NetworkCredential(user, pass),
                        EnableSsl = true
                    };

                    var mail = new MailMessage(from, to, subject, html)
                    {
                        IsBodyHtml = true
                    };
                    mail.Priority = MailPriority.High;
                    mail.Headers.Add("X-Priority", "1");
                    mail.Headers.Add("X-MSMail-Priority", "High");
                    mail.Headers.Add("Importance", "high");

                    client.Send(mail);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email:");
                    Console.WriteLine(ex);
                    // Optionally log ex.StackTrace or log it somewhere else
                }
            });
        }
        public IActionResult AccessDenied() => View();
    }
}

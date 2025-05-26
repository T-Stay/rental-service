using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RentalService.Models;
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

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, RoleManager<IdentityRole<Guid>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string name)
        {
            var user = new User
            {
                UserName = email,
                Email = email,
                Name = name,
                AvatarUrl = "",
                Role = UserRole.RegisteredCustomer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                PhoneNumber = ""
            };
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "customer");
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Scheme) ?? string.Empty;
                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(callbackUrl))
                {
                    SendEmail(user.Email, "Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                }
                ViewBag.Message = "Registration successful! Please check your email to confirm your account.";
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
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(email, password, false, false);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");
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
            if (user == null )
            {
                // Don't reveal user not found or not confirmed
                return RedirectToAction("ForgotPasswordConfirmation");
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { code }, protocol: Request.Scheme) ?? string.Empty;
            if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(callbackUrl))
            {
                SendEmail(user.Email, "Reset Password", $"Reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
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

                    client.Send(mail);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email:");
                    Console.WriteLine(ex.Message);
                    // Optionally log ex.StackTrace or log it somewhere else
                }
            });
        }
        public IActionResult AccessDenied() => View();
    }
}

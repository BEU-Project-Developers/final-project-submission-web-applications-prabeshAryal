using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MusicApp.Models;
using MusicApp.Models.DTOs;
using MusicApp.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MusicApp.Data;
using Microsoft.JSInterop;
using System.Text.Json;
using MusicApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace MusicApp.Controllers
{
    public class AccountController : BaseAppController
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService, ApiService apiService, ILogger<AccountController> logger)
            : base(apiService, logger)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                var error = "Please fix the validation errors.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }

            try
            {
                var success = await _authService.LoginAsync(model.UserIdentifier, model.Password, model.RememberMe);
                if (success)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true });

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    // Redirect to dashboard after login
                    return RedirectToAction("Dashboard", "Account");
                }

                var error = "Invalid login attempt. Please check your username/email and password.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {UserIdentifier}", model.UserIdentifier);
                var error = "An error occurred during login. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                var error = "Please fix the validation errors.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }

            try
            {
                var success = await _authService.RegisterAsync(model);
                
                if (success)
                {
                    var message = "Registration successful! Please sign in.";
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = message });
                    }
                    TempData["SuccessMessage"] = message;
                    return RedirectToAction(nameof(Login));
                }
                
                var error = "Registration failed. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Email}", model.Email);
                var error = "An error occurred during registration. Please try again.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ErrorMessage"] = error;
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction(nameof(Login));
            }

            return View();
        }
    }
}
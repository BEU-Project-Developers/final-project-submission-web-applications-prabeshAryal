using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MusicApp.Services;
using MusicApp.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApiService _apiService;

        public UsersController(ApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _apiService.GetAsync<List<UserDto>>("api/Users");
                return View(users ?? new List<UserDto>());
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading users: {ex.Message}";
                return View(new List<UserDto>());
            }
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _apiService.GetAsync<UserDto>($"api/Users/{id}");
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error loading user details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _apiService.DeleteAsync($"api/Users/{id}");
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/GetUsers - API endpoint for AJAX calls
        [HttpGet]
        public async Task<JsonResult> GetUsers()
        {
            try
            {
                var users = await _apiService.GetAsync<List<UserDto>>("api/Users");
                return Json(new { success = true, data = users ?? new List<UserDto>() });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

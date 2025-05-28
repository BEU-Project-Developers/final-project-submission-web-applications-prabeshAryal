using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MusicApp.Services;
using MusicApp.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : BaseAppController
    {
        public UsersController(ApiService apiService, ILogger<UsersController> logger)
            : base(apiService, logger)
        {
        }        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await SafeApiCall(
                async () => await _apiService.GetAsync<List<UserDto>>("api/Users"),
                new List<UserDto>(),
                GetStandardErrorMessage("load", "users"),
                "UsersController.Index"
            );

            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            return await SafeApiAction(
                async () =>
                {
                    var user = await _apiService.GetAsync<UserDto>($"api/Users/{id}");
                    if (user == null)
                    {
                        return NotFound();
                    }
                    return View(user);
                },
                () => {
                    SetErrorMessage(GetStandardErrorMessage("load", "user details"));
                    return RedirectToAction(nameof(Index));
                },
                GetStandardErrorMessage("load", "user details"),
                $"UsersController.Details for ID {id}"
            );
        }        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await SafeApiCall(
                async () =>
                {
                    await _apiService.DeleteAsync($"api/Users/{id}");
                    SetSuccessMessage("User deleted successfully.");
                    return true;
                },
                false,
                GetStandardErrorMessage("delete", "user"),
                $"UsersController.DeleteConfirmed for ID {id}"
            );
            
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/GetUsers - API endpoint for AJAX calls
        [HttpGet]
        public async Task<JsonResult> GetUsers()
        {
            var users = await SafeApiCall(
                async () => await _apiService.GetAsync<List<UserDto>>("api/Users"),
                new List<UserDto>(),
                "Unable to load users",
                "UsersController.GetUsers AJAX"
            );

            return Json(new { success = users != null, data = users ?? new List<UserDto>() });
        }
    }
}

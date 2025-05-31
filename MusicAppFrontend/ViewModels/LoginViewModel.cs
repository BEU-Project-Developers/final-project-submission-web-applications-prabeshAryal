using System.ComponentModel.DataAnnotations;

namespace MusicApp.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or Email is required")]
        [Display(Name = "Username or Email")]
<<<<<<< HEAD
        public string Identifier { get; set; }
=======
        public string UsernameOrEmail { get; set; }
>>>>>>> main

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
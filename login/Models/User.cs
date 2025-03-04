using System;
using System.ComponentModel.DataAnnotations;

namespace login.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username harus diisi")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string Email { get; set; }

        public string Role { get; set; } // Menambahkan properti Role

        public DateTime CreatedDate { get; set; }
    }

    // Model untuk form login
    public class LoginModel
    {
        [Required(ErrorMessage = "Username harus diisi")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password harus diisi")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
using System;

namespace ZenTask.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }  // Adăugat pentru stocare salt
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public string ProfileImagePath { get; set; }
        public string ThemePreference { get; set; } // Light/Dark

        public User()
        {
            CreatedDate = DateTime.Now;
            LastLoginDate = DateTime.Now;
            ThemePreference = "Light"; // Default theme
        }

        // Proprietate calculată pentru numele complet
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
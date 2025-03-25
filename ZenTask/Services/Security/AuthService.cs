using System;
using System.Security.Cryptography;
using System.Text;
// Using explicit namespace to avoid Task ambiguity
using System.Threading.Tasks;
using ZenTask.Models;
using ZenTask.Services.Data;

namespace ZenTask.Services.Security
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private User _currentUser;

        public User CurrentUser => _currentUser;
        public bool IsAuthenticated => _currentUser != null;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async System.Threading.Tasks.Task<bool> RegisterAsync(string username, string email, string password, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                // Check if username exists (run this in parallel with email check)
                var usernameTask = _userRepository.GetByUsernameAsync(username);
                var emailTask = _userRepository.GetByEmailAsync(email);

                // Wait for both checks to complete
                await System.Threading.Tasks.Task.WhenAll(usernameTask, emailTask);

                // Check results
                var existingUser = await usernameTask;
                if (existingUser != null)
                {
                    return false; // Username already taken
                }

                existingUser = await emailTask;
                if (existingUser != null)
                {
                    return false; // Email already taken
                }

                // Create password hash with salt (CPU intensive, do it in parallel)
                var salt = await System.Threading.Tasks.Task.Run(() => GenerateSalt());
                var passwordHash = await System.Threading.Tasks.Task.Run(() => HashPassword(password, salt));

                // Create new user
                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = Convert.ToBase64String(passwordHash),
                    PasswordSalt = Convert.ToBase64String(salt),
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedDate = DateTime.Now,
                    LastLoginDate = DateTime.Now,
                    ThemePreference = "Light"
                };

                var userId = await _userRepository.InsertAsync(user);
                return userId > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null)
                {
                    return false; // User not found
                }

                // Verify password (CPU intensive, do it in parallel)
                var salt = Convert.FromBase64String(user.PasswordSalt);
                var storedHash = Convert.FromBase64String(user.PasswordHash);

                bool passwordValid = await System.Threading.Tasks.Task.Run(() => VerifyPassword(password, salt, storedHash));
                if (!passwordValid)
                {
                    return false; // Incorrect password
                }

                // Update last login date
                await _userRepository.UpdateLastLoginAsync(user.Id);

                // Set current user
                _currentUser = user;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void Logout()
        {
            _currentUser = null;
        }

        private byte[] GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[16];
                rng.GetBytes(salt);
                return salt;
            }
        }

        private byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256 bits
            }
        }

        private bool VerifyPassword(string password, byte[] salt, byte[] storedHash)
        {
            var computedHash = HashPassword(password, salt);

            // Compare hashes
            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != storedHash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
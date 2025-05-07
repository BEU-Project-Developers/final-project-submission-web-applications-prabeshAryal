using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MusicAppBackend.Data;
using MusicAppBackend.Models;
using MusicAppBackend.DTOs;

namespace MusicAppBackend.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, UserDTO? User, string? Token, string? RefreshToken)> LoginAsync(LoginDTO login);
        Task<(bool Success, string Message, UserDTO? User)> RegisterAsync(RegisterDTO register);
        Task<bool> AssignRoleAsync(int userId, string roleName);
        Task<string> GenerateJwtTokenAsync(User user);
        Task<string> GenerateRefreshTokenAsync(int userId);
        Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task<bool> RevokeTokenAsync(string refreshToken);
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }

    public class AuthService : IAuthService
    {
        private readonly MusicDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(MusicDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message, UserDTO? User, string? Token, string? RefreshToken)> LoginAsync(LoginDTO login)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == login.Email);

            if (user == null)
            {
                return (false, "User not found", null, null, null);
            }

            if (!VerifyPassword(login.Password, user.PasswordHash))
            {
                return (false, "Invalid password", null, null, null);
            }

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

            var userDto = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = roles
            };

            return (true, "Login successful", userDto, token, refreshToken);
        }

        public async Task<(bool Success, string Message, UserDTO? User)> RegisterAsync(RegisterDTO register)
        {
            if (await _context.Users.AnyAsync(u => u.Email == register.Email))
            {
                return (false, "Email already exists", null);
            }

            if (await _context.Users.AnyAsync(u => u.Username == register.Username))
            {
                return (false, "Username already exists", null);
            }

            var user = new User
            {
                Email = register.Email,
                Username = register.Username,
                FirstName = register.FirstName,
                LastName = register.LastName,
                PasswordHash = HashPassword(register.Password),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default user role
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (userRole == null)
            {
                // Create user role if it doesn't exist
                userRole = new Role { Name = "User", Description = "Regular user" };
                _context.Roles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id
            });

            await _context.SaveChangesAsync();

            var userDto = new UserDTO
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = new List<string> { "User" }
            };

            return (true, "Registration successful", userDto);
        }

        public async Task<bool> AssignRoleAsync(int userId, string roleName)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                // Create role if it doesn't exist
                role = new Role { Name = roleName, Description = $"{roleName} role" };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

            if (existingUserRole != null) return true; // Already assigned

            _context.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = role.Id
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateJwtTokenAsync(User user)
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "MusicAppSecretKey12345678901234567890"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "MusicAppIssuer",
                audience: _configuration["Jwt:Audience"] ?? "MusicAppAudience",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshTokenAsync(int userId)
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            // Store refresh token in database
            var tokenEntry = new RefreshToken
            {
                Token = refreshToken,
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IssuedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(tokenEntry);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<(bool Success, string? Token, string? RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

            if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow)
            {
                return (false, null, null);
            }

            // Generate new tokens
            var newJwtToken = await GenerateJwtTokenAsync(storedToken.User);
            var newRefreshToken = await GenerateRefreshTokenAsync(storedToken.UserId);

            // Revoke old refresh token
            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();

            return (true, newJwtToken, newRefreshToken);
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null)
            {
                return false;
            }

            storedToken.IsRevoked = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
} 
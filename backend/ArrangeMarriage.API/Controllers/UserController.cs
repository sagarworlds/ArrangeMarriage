using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var createdUser = await _userService.CreateUserAsync(user);
            return Ok(createdUser);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("verify/{id}")]
        public async Task<IActionResult> VerifyUser(Guid id)
        {
            var result = await _userService.VerifyUserAsync(id);
            if (!result) return NotFound();
            return Ok("User verified successfully");
        }

        [HttpGet("unverified")]
        public async Task<IActionResult> GetUnverified()
        {
            var users = await _userService.GetUnverifiedUsersAsync();
            return Ok(users);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);
            if (user == null) return Unauthorized("Invalid email or password");

            // Simple comparison for standard hashes/passwords
            if (user.PasswordHash != request.Password) 
            {
                return Unauthorized("Invalid email or password");
            }

            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes("SUPER_SECRET_KEY_FOR_ARRANGE_MARRIAGE_12345!");
            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature),
                Issuer = "ArrangeMarriageIssuer",
                Audience = "ArrangeMarriageAudience"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString, User = user });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

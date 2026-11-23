using System;
using System.Linq;
using System.Threading.Tasks;
using ArrangeMarriage.API.Dtos;
using ArrangeMarriage.API.Security;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ArrangeMarriage.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Public sign-up must not be able to mint Admin/Staff accounts.
            if (request.Role is UserRole.Admin or UserRole.Staff)
            {
                return BadRequest("This role cannot be self-registered.");
            }

            if (await _userService.GetUserByEmailAsync(request.Email) != null)
            {
                return Conflict("An account with this email already exists.");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                PasswordHash = _passwordHasher.Hash(request.Password)
            };

            var created = await _userService.CreateUserAsync(user);
            return Ok(UserResponse.From(created));
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            if (!User.CanAccessUser(id)) return Forbid();

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(UserResponse.From(user));
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPut("verify/{id}")]
        public async Task<IActionResult> VerifyUser(Guid id)
        {
            var result = await _userService.VerifyUserAsync(id);
            if (!result) return NotFound();
            return Ok("User verified successfully");
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("unverified")]
        public async Task<IActionResult> GetUnverified()
        {
            var users = await _userService.GetUnverifiedUsersAsync();
            return Ok(users.Select(UserResponse.From));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userService.GetUserByEmailAsync(request.Email);

            // Same message for unknown email and wrong password to avoid account enumeration.
            if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt for {Email}", request.Email);
                return Unauthorized("Invalid email or password");
            }

            return Ok(new LoginResponse
            {
                Token = _tokenService.CreateToken(user),
                User = UserResponse.From(user)
            });
        }
    }
}

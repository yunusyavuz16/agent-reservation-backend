using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.DTOs;
using ReservationApi.Models;
using ReservationApi.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
        _configuration = configuration;
    }

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        // Email kontrolü
        var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
        if (userExists != null)
            return Conflict(new { message = "User with this email already exists" });

        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            PhoneNumber = registerDto.PhoneNumber,
            Address = registerDto.Address,
            City = registerDto.City,
            Country = registerDto.Country,
            PostalCode = registerDto.PostalCode,
            RegistrationDate = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("User creation failed: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "User creation failed", errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, "User");

        // Token oluşturma
        var token = await _tokenService.CreateToken(user);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto
        {
            Token = tokenStr,
            Expiration = token.ValidTo,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                Roles = new List<string> { "User" },
                IsActive = user.IsActive
            }
        };
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found", loginDto.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, false, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed: Invalid password for user {Email}", loginDto.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Son giriş tarihini güncelleme
            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Token oluşturma
            var token = await _tokenService.CreateToken(user);
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("Login successful for user {Email}, token issued", loginDto.Email);

            // Kullanıcı rollerini alma
            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                Token = tokenStr,
                Expiration = token.ValidTo,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ProfileImageUrl,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", loginDto.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    // GET: api/Auth/profile
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            ProfileImageUrl = user.ProfileImageUrl,
            Roles = roles.ToList(),
            IsActive = user.IsActive
        };
    }

    // GET: api/Auth/validate-token
    [HttpGet("validate-token")]
    public ActionResult ValidateToken()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided in Authorization header");
                return BadRequest(new { message = "No token provided" });
            }

            _logger.LogInformation("Attempting to validate token: {TokenStart}...", token.Substring(0, Math.Min(10, token.Length)));

            var jwtSecret = _configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                _logger.LogError("JWT:Secret is null or empty in configuration");
                return StatusCode(500, new { message = "Server configuration error" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidAudience = _configuration["JWT:ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            try
            {
                // This will throw an exception if the token is invalid
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // Get token details for debugging
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var tokenInfo = new
                {
                    Valid = true,
                    Issuer = jwtToken.Issuer,
                    Audience = jwtToken.Audiences.FirstOrDefault(),
                    ExpiresAt = jwtToken.ValidTo,
                    IssuedAt = jwtToken.IssuedAt,
                    Claims = jwtToken.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                    TokenId = jwtToken.Id,
                    UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Username = principal.FindFirst(ClaimTypes.Name)?.Value,
                    Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
                };

                _logger.LogInformation("Token validated successfully for user {UserId}", tokenInfo.UserId);
                return Ok(tokenInfo);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("Token expired: {Exception}", ex.Message);
                return BadRequest(new { message = "Token expired", error = ex.Message });
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("Invalid token signature: {Exception}", ex.Message);
                return BadRequest(new { message = "Invalid token signature", error = ex.Message });
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Invalid token: {Exception}", ex.Message);
                return BadRequest(new { message = "Invalid token", error = ex.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Token validation error: {Exception}", ex);
            return StatusCode(500, new { message = "Error validating token", error = ex.Message });
        }
    }

    // PUT: api/Auth/profile
    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult> UpdateProfile(UserProfileUpdateDto profileDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Profil güncelleme
        user.FirstName = profileDto.FirstName;
        user.LastName = profileDto.LastName;
        user.PhoneNumber = profileDto.PhoneNumber;
        user.Address = profileDto.Address;
        user.City = profileDto.City;
        user.Country = profileDto.Country;
        user.PostalCode = profileDto.PostalCode;
        user.Company = profileDto.Company;
        user.JobTitle = profileDto.JobTitle;
        user.DateOfBirth = profileDto.DateOfBirth;
        user.ProfileImageUrl = profileDto.ProfileImageUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = "Profile update failed", errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    // POST: api/Auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordDto passwordDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized(new { message = "User not found" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        var result = await _userManager.ChangePasswordAsync(user, passwordDto.CurrentPassword, passwordDto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = "Password change failed", errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }

    // POST: api/Auth/debug-token
    [HttpPost("debug-token")]
    public ActionResult<object> DebugToken([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "No token provided" });
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);

            var result = new
            {
                Valid = true,
                Issuer = jwt.Issuer,
                Audience = jwt.Audiences.FirstOrDefault(),
                ExpiresAt = jwt.ValidTo,
                IssuedAt = jwt.IssuedAt,
                Claims = jwt.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                TokenId = jwt.Id
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid token", error = ex.Message });
        }
    }
}
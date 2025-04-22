using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.DTOs;
using ReservationApi.Models;
using ReservationApi.Services;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        TokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
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
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, false, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid email or password" });

        // Son giriş tarihini güncelleme
        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Token oluşturma
        var token = await _tokenService.CreateToken(user);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

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
}
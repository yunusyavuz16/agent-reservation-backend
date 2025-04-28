using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ReservationApi.Models;

namespace ReservationApi.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        IConfiguration configuration,
        UserManager<ApplicationUser> userManager,
        ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<JwtSecurityToken> CreateToken(ApplicationUser user)
    {
        _logger.LogInformation("Creating token for user: {UserId}, {UserName}", user.Id, user.UserName);

        try
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                // Add sub claim for better compatibility with JWT standard
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                // Add extra claim with creation time
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var jwtSecret = _configuration["JWT:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                _logger.LogError("JWT:Secret is null or empty in configuration");
                jwtSecret = "YourSuperSecretKeyWith32Characters!!";
            }

            _logger.LogDebug("JWT Secret length: {Length}", jwtSecret.Length);

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

            // Explicitly set issuer and audience for flexibility
            var validIssuer = _configuration["JWT:ValidIssuer"] ?? "http://localhost:5121";
            var validAudience = _configuration["JWT:ValidAudience"] ?? "http://localhost:3000";

            _logger.LogDebug("Token configuration - Issuer: {Issuer}, Audience: {Audience}",
                validIssuer, validAudience);

            var token = new JwtSecurityToken(
                issuer: validIssuer,
                audience: validAudience,
                expires: DateTime.UtcNow.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            _logger.LogInformation("Token created successfully for user {UserId}, expires: {ExpiryTime}",
                user.Id, token.ValidTo);

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token for user {UserId}", user.Id);
            throw;
        }
    }
}
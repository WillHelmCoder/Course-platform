using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Entities;
using Pow.Domain.Enums;
using Pow.Domain.Interfaces;

namespace Pow.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse(false, Message: "Invalid email or password");
        }

        if (!user.IsActive)
        {
            return new AuthResponse(false, Message: "User account is disabled");
        }

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return new AuthResponse(
            true,
            Token: token,
            RefreshToken: refreshToken,
            Expiration: DateTime.UtcNow.AddHours(1),
            User: new UserInfo(user.Id, user.Email, user.Role, user.TenantId, user.Tenant?.Name ?? "")
        );
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse(false, Message: "Email already registered");
        }

        // Get or create default tenant (single-tenant mode)
        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            tenant = new Tenant
            {
                Name = "Default",
                Slug = "default",
                IsActive = true
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }

        // Create user
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TenantId = tenant.Id,
            Role = "User", // Default role, SuperAdmin created by seeder
            IsActive = true,
            PhoneNumber = request.PhoneNumber
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        // Auto-login after registration
        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RegisterCreatorAsync(RegisterCreatorRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse(false, Message: "Email already registered");
        }

        // Get or create default tenant
        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            tenant = new Tenant { Name = "Default", Slug = "default", IsActive = true };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }

        // Verify platform plan exists (if provided)
        Plan? platformPlan = null;
        if (request.PlatformPlanId.HasValue)
        {
            platformPlan = await _db.Plans.FindAsync(request.PlatformPlanId.Value);
            if (platformPlan == null)
            {
                return new AuthResponse(false, Message: "Invalid platform plan");
            }
        }

        // Create user with Admin role
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TenantId = tenant.Id,
            Role = "Admin",
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create channel for the creator
        var channel = new Channel
        {
            TenantId = tenant.Id,
            Name = request.ChannelName,
            Slug = GenerateSlug(request.ChannelName),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Channels.Add(channel);
        await _db.SaveChangesAsync();

        // Add user as channel admin
        var channelAdmin = new ChannelAdmin
        {
            TenantId = tenant.Id,
            ChannelId = channel.Id,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _db.ChannelAdmins.Add(channelAdmin);

        // Subscribe creator to platform plan (only if plan provided - for paid plans, subscription happens after Stripe payment)
        if (platformPlan != null)
        {
            var userPlan = new UserPlan
            {
                UserId = user.Id,
                PlanId = platformPlan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = platformPlan.Price > 0 ? DateTime.UtcNow.AddMonths(1) : null,
                Status = "active"
            };
            _db.UserPlans.Add(userPlan);
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Creator registered: {Email} with channel {Channel}", request.Email, request.ChannelName);

        // Auto-login after registration
        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RegisterSubscriberAsync(RegisterSubscriberRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            return new AuthResponse(false, Message: "Email already registered");
        }

        // Get or create default tenant
        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            tenant = new Tenant { Name = "Default", Slug = "default", IsActive = true };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }

        // Verify channel exists
        var channel = await _db.Channels.FindAsync(request.ChannelId);
        if (channel == null)
        {
            return new AuthResponse(false, Message: "Channel not found");
        }

        // Create user with User role
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            TenantId = tenant.Id,
            Role = "User",
            IsActive = true,
            PhoneNumber = request.PhoneNumber
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Subscribe to channel if plan provided
        if (request.ChannelPlanId.HasValue)
        {
            var channelPlan = await _db.ChannelPlans.FindAsync(request.ChannelPlanId.Value);
            if (channelPlan != null && channelPlan.ChannelId == request.ChannelId)
            {
                var subscription = new ChannelSubscription
                {
                    TenantId = tenant.Id,
                    UserId = user.Id,
                    ChannelId = request.ChannelId,
                    ChannelPlanId = channelPlan.Id,
                    SubscribedAt = DateTime.UtcNow,
                    ExpiresAt = channelPlan.Price > 0 ? DateTime.UtcNow.AddMonths(1) : null,
                    Status = SubscriptionStatus.Active,
                    PricePaid = channelPlan.Price,
                    Currency = channelPlan.Currency
                };
                _db.ChannelSubscriptions.Add(subscription);
                await _db.SaveChangesAsync();

                _logger.LogInformation("User {Email} subscribed to channel {Channel} with plan {Plan}",
                    request.Email, channel.Name, channelPlan.Name);
            }
        }

        // Auto-login after registration
        return await LoginAsync(new LoginRequest(request.Email, request.Password));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.Token);
        if (principal == null)
        {
            return new AuthResponse(false, Message: "Invalid token");
        }

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
        {
            return new AuthResponse(false, Message: "Invalid refresh token");
        }

        var newToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return new AuthResponse(
            true,
            Token: newToken,
            RefreshToken: newRefreshToken,
            Expiration: DateTime.UtcNow.AddHours(1),
            User: new UserInfo(user.Id, user.Email, user.Role, user.TenantId, user.Tenant?.Name ?? "")
        );
    }

    public async Task<AuthResponse> LogoutAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
        }
        return new AuthResponse(true, Message: "Logged out successfully");
    }

    public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            // Don't reveal if email exists
            return new AuthResponse(true, Message: "If email exists, reset instructions will be sent");
        }

        // TODO: Generate reset token and send email
        _logger.LogInformation("Password reset requested for {Email}", request.Email);

        return new AuthResponse(true, Message: "If email exists, reset instructions will be sent");
    }

    public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        // TODO: Validate reset token
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return new AuthResponse(false, Message: "Invalid request");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        return new AuthResponse(true, Message: "Password reset successfully");
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DefaultSecretKey123!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("TenantId", user.TenantId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "DefaultSecretKey123!");
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("'", "")
            + "-" + Guid.NewGuid().ToString()[..8];
    }
}

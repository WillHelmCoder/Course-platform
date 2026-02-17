using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pow.Api.Data;
using Pow.Domain.DTOs;
using Pow.Domain.Interfaces;

namespace Pow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _db;

    public AuthController(IAuthService authService, AppDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    [HttpGet("check-email")]
    public async Task<ActionResult<bool>> CheckEmailExists([FromQuery] string email)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        return Ok(exists);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("register-creator")]
    public async Task<ActionResult<AuthResponse>> RegisterCreator([FromBody] RegisterCreatorRequest request)
    {
        var result = await _authService.RegisterCreatorAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("register-subscriber")]
    public async Task<ActionResult<AuthResponse>> RegisterSubscriber([FromBody] RegisterSubscriberRequest request)
    {
        var result = await _authService.RegisterSubscriberAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<AuthResponse>> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authService.LogoutAsync(Guid.Parse(userId));
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<AuthResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<AuthResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        return Ok(new UserInfo(
            user.Id,
            user.Email,
            user.Role,
            user.TenantId,
            user.Tenant?.Name ?? "",
            user.DisplayName,
            user.Bio,
            user.ProfilePicture,
            user.Twitter,
            user.LinkedIn,
            user.GitHub,
            user.Instagram,
            user.YouTube,
            user.TikTok,
            user.Website
        ));
    }

    [HttpGet("profile/{userId:guid}")]
    public async Task<ActionResult<UserInfo>> GetPublicProfile(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound();

        return Ok(new UserInfo(
            user.Id,
            user.Email,
            user.Role,
            user.TenantId,
            user.Tenant?.Name ?? "",
            user.DisplayName,
            user.Bio,
            user.ProfilePicture,
            user.Twitter,
            user.LinkedIn,
            user.GitHub,
            user.Instagram,
            user.YouTube,
            user.TikTok,
            user.Website
        ));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserInfo>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        // Update profile fields
        user.DisplayName = request.DisplayName;
        user.Bio = request.Bio;
        user.ProfilePicture = request.ProfilePicture;
        user.Twitter = request.Twitter;
        user.LinkedIn = request.LinkedIn;
        user.GitHub = request.GitHub;
        user.Instagram = request.Instagram;
        user.YouTube = request.YouTube;
        user.TikTok = request.TikTok;
        user.Website = request.Website;

        await _db.SaveChangesAsync();

        return Ok(new UserInfo(
            user.Id,
            user.Email,
            user.Role,
            user.TenantId,
            user.Tenant?.Name ?? "",
            user.DisplayName,
            user.Bio,
            user.ProfilePicture,
            user.Twitter,
            user.LinkedIn,
            user.GitHub,
            user.Instagram,
            user.YouTube,
            user.TikTok,
            user.Website
        ));
    }

    [Authorize]
    [HttpDelete("delete-account")]
    public async Task<ActionResult> DeleteAccount()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound();

        // Delete related data
        var subscriptions = await _db.ChannelSubscriptions.Where(s => s.UserId == userId).ToListAsync();
        _db.ChannelSubscriptions.RemoveRange(subscriptions);

        var purchases = await _db.CoursePurchases.Where(p => p.UserId == userId).ToListAsync();
        _db.CoursePurchases.RemoveRange(purchases);

        var userPlans = await _db.UserPlans.Where(p => p.UserId == userId).ToListAsync();
        _db.UserPlans.RemoveRange(userPlans);

        var userRoles = await _db.UserRoles.Where(r => r.UserId == userId).ToListAsync();
        _db.UserRoles.RemoveRange(userRoles);

        // Remove from channel admins
        var channelAdmins = await _db.ChannelAdmins.Where(ca => ca.UserId == userId).ToListAsync();
        _db.ChannelAdmins.RemoveRange(channelAdmins);

        // Delete the user
        _db.Users.Remove(user);

        await _db.SaveChangesAsync();

        return Ok(new { message = "Account deleted successfully" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr))
            return Unauthorized();

        var userId = Guid.Parse(userIdStr);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound();

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect" });

        // Validate new password
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "New password must be at least 6 characters" });

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }
}

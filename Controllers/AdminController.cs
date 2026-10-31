using EcoEast.API.Data;
using EcoEast.API.Dtos;
using EcoEast.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    EcoEastDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var userList = await users.Users.ToListAsync();
        var result = new List<object>();

        foreach (var u in userList)
        {
            var userRoles = await users.GetRolesAsync(u);
            var person = u.PersonId.HasValue
                ? await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.PersonID == u.PersonId.Value)
                : null;

            result.Add(new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.PhoneNumber,
                FirstName = person?.FIRST_NAME,
                LastName = person?.LAST_NAME,
                Roles = userRoles
            });
        }

        return Ok(result);
    }

    [HttpPost("users/assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        var user = await users.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "User not found." });

        if (!await roles.RoleExistsAsync(dto.RoleName))
            return BadRequest(new { message = $"Role '{dto.RoleName}' does not exist." });

        var result = await users.AddToRoleAsync(user, dto.RoleName);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = $"Role '{dto.RoleName}' assigned to user {user.UserName} successfully." });
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaffAccounts()
    {
        var staff = await (from u in db.Users
                           join p in db.People on u.PersonId equals p.PersonID
                           where p.ROLE == "Admin" || p.ROLE == "Manager" || p.ROLE == "FieldWorker"
                           select new
                           {
                               id = u.Id,
                               username = u.UserName,
                               email = u.Email,
                               role = p.ROLE,
                               firstName = p.FIRST_NAME,
                               lastName = p.LAST_NAME,
                               phone = p.PHONE
                           }).ToListAsync();

        return Ok(staff);
    }

    [HttpPost("staff/role")]
    public async Task<IActionResult> UpdateUserRole([FromBody] UpdateRoleDto dto)
    {
        var allowedRoles = new[] { "Admin", "Manager", "FieldWorker", "Resident" };
        var newRole = allowedRoles.FirstOrDefault(x => string.Equals(x, dto.NewRole, StringComparison.OrdinalIgnoreCase));
        if (newRole == null)
            return BadRequest(new { message = "Invalid role." });

        var user = await users.FindByIdAsync(dto.UserId);
        if (user == null) return NotFound(new { message = "User not found." });
        if (!await roles.RoleExistsAsync(newRole))
            return BadRequest(new { message = $"Role '{newRole}' does not exist." });

        var currentRoles = await users.GetRolesAsync(user);
        var removeResult = await users.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return BadRequest(new { errors = removeResult.Errors.Select(e => e.Description) });

        var addResult = await users.AddToRoleAsync(user, newRole);
        if (!addResult.Succeeded)
            return BadRequest(new { errors = addResult.Errors.Select(e => e.Description) });

        var person = user.PersonId.HasValue
            ? await db.People.FirstOrDefaultAsync(p => p.PersonID == user.PersonId.Value)
            : null;
        if (person != null)
        {
            person.ROLE = newRole;
            await db.SaveChangesAsync();
        }

        return Ok(new { message = "Staff role updated successfully." });
    }

    [HttpGet("audit-logs")]
    public IActionResult GetAuditLogs()
    {
        var logs = new[]
        {
            new { Id = 1, Timestamp = DateTime.UtcNow.AddMinutes(-15), User = "admin_thabo", Action = "USER_ROLE_UPDATE", Details = "Promoted user to FieldWorker role." },
            new { Id = 2, Timestamp = DateTime.UtcNow.AddHours(-1), User = "system", Action = "DB_BACKUP_SUCCESS", Details = "Automated nightly backup completed." },
            new { Id = 3, Timestamp = DateTime.UtcNow.AddHours(-3), User = "manager_sara", Action = "SERVICE_REQUEST_APPROVE", Details = "Approved service request #104." }
        };

        return Ok(logs);
    }

    [HttpGet("system-health")]
    public async Task<IActionResult> GetSystemHealth()
    {
        var databaseConnected = await db.Database.CanConnectAsync();
        return Ok(new
        {
            status = databaseConnected ? "Healthy" : "Degraded",
            database = databaseConnected ? "Connected (SQL Server)" : "Unavailable",
            serverTime = DateTime.UtcNow
        });
    }
}

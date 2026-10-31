using EcoEast.API.Data;
using EcoEast.API.Dtos;
using EcoEast.API.Models;
using EcoEast.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> rolesManager,
    EcoEastDbContext db,
    JwtTokenService tokens) : ControllerBase
{
    [HttpPost("register-resident")]
    public async Task<IActionResult> Register(RegisterResidentDto dto)
    {
        if (await users.FindByNameAsync(dto.UserName) != null)
            return Conflict(new { message = "Username already exists." });

        if (await users.FindByEmailAsync(dto.Email) != null)
            return Conflict(new { message = "Email already exists." });

        if (dto.LocationId.HasValue && !await db.Locations.AnyAsync(x => x.LOCATION_ID == dto.LocationId))
            return BadRequest(new { message = "Invalid location ID selected." });

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var person = new Person
            {
                FIRST_NAME = dto.FirstName.Trim(),
                LAST_NAME = dto.LastName.Trim(),
                USERNAME = dto.UserName.Trim(),
                EMAIL = dto.Email.Trim(),
                PHONE = dto.Phone,
                LOCATION_ID = dto.LocationId,
                ROLE = "Resident"
            };

            db.People.Add(person);
            await db.SaveChangesAsync();

            db.Residents.Add(new Resident { RESIDENT_ID = person.PersonID });
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = dto.UserName.Trim(),
                Email = dto.Email.Trim(),
                PhoneNumber = dto.Phone,
                PersonId = person.PersonID,
                EmailConfirmed = true
            };

            var result = await users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            if (!await rolesManager.RoleExistsAsync("Resident"))
            {
                var roleResult = await rolesManager.CreateAsync(new IdentityRole("Resident"));
                if (!roleResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return StatusCode(500, new { errors = roleResult.Errors.Select(e => e.Description) });
                }
            }

            var userRoleResult = await users.AddToRoleAsync(user, "Resident");
            if (!userRoleResult.Succeeded)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { errors = userRoleResult.Errors.Select(e => e.Description) });
            }

            await tx.CommitAsync();
            return Ok(new { message = "Resident account created successfully.", personId = person.PersonID });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Failed to create account.", error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await users.FindByNameAsync(dto.UserName) ?? await users.FindByEmailAsync(dto.UserName);

        if (user == null || !await users.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid username/email or password." });

        if (await users.IsLockedOutAsync(user))
            return Unauthorized(new { message = "Account is locked out. Please contact administrator." });

        var userRoles = await users.GetRolesAsync(user);
        var person = user.PersonId.HasValue
            ? await db.People.AsNoTracking().FirstOrDefaultAsync(x => x.PersonID == user.PersonId.Value)
            : null;

        var token = tokens.Create(user, userRoles);

        return Ok(new
        {
            token,
            user = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.PersonId,
                FirstName = person?.FIRST_NAME ?? user.UserName,
                LastName = person?.LAST_NAME ?? "",
                roles = userRoles
            }
        });
    }

    [HttpGet("seed-admin")]
    public async Task<IActionResult> SeedAdmin()
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (!environment.IsDevelopment())
            return NotFound();

        const string userName = "admin_thabo";
        const string email = "thabo.admin@ecoeast.co.za";

        var existing = await users.FindByNameAsync(userName);
        if (existing != null)
            return Ok(new { message = $"Admin user '{userName}' already exists in the database." });

        if (await users.FindByEmailAsync(email) != null)
            return Conflict(new { message = $"Email '{email}' is already registered." });

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            var person = new Person
            {
                FIRST_NAME = "Thabo",
                LAST_NAME = "Molefe",
                USERNAME = userName,
                EMAIL = email,
                PHONE = "0723456789",
                ROLE = "Admin"
            };

            db.People.Add(person);
            await db.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                PhoneNumber = "0723456789",
                PersonId = person.PersonID,
                EmailConfirmed = true
            };

            var result = await users.CreateAsync(user, "AdminPass2026!");
            if (!result.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            if (!await rolesManager.RoleExistsAsync("Admin"))
            {
                var roleResult = await rolesManager.CreateAsync(new IdentityRole("Admin"));
                if (!roleResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description) });
                }
            }

            var assignment = await users.AddToRoleAsync(user, "Admin");
            if (!assignment.Succeeded)
            {
                await tx.RollbackAsync();
                return BadRequest(new { errors = assignment.Errors.Select(e => e.Description) });
            }

            await tx.CommitAsync();
            return Ok(new { message = "Admin user created successfully!" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Failed to create admin user.", error = ex.Message });
        }
    }
}
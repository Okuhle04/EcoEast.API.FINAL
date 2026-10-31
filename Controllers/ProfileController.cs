using EcoEast.API.Data;
using EcoEast.API.Dtos;
using EcoEast.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(
    UserManager<ApplicationUser> users,
    EcoEastDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var person = user.PersonId.HasValue
            ? await db.People.AsNoTracking().FirstOrDefaultAsync(p => p.PersonID == user.PersonId.Value)
            : null;

        var roles = await users.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            FirstName = person?.FIRST_NAME,
            LastName = person?.LAST_NAME,
            LocationId = person?.LOCATION_ID,
            Roles = roles
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await users.GetUserAsync(User);
        if (user == null) return Unauthorized();

        user.PhoneNumber = dto.Phone;
        var userResult = await users.UpdateAsync(user);
        if (!userResult.Succeeded)
            return BadRequest(new { errors = userResult.Errors.Select(e => e.Description) });

        if (user.PersonId.HasValue)
        {
            var person = await db.People.FindAsync(user.PersonId.Value);
            if (person != null)
            {
                person.FIRST_NAME = dto.FirstName.Trim();
                person.LAST_NAME = dto.LastName.Trim();
                person.PHONE = dto.Phone;
                if (dto.LocationId.HasValue) person.LOCATION_ID = dto.LocationId;

                await db.SaveChangesAsync();
            }
        }

        return Ok(new { message = "Profile updated successfully." });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/public")]
public class PublicContentController : ControllerBase
{
    [HttpGet("dropoff-points")]
    public IActionResult GetDropoffPoints()
    {
        var points = new[]
        {
            new { Id = 1, Name = "East London Central Recycling Hub", Address = "12 Oxford Street, East London", Hours = "Mon-Fri: 08:00 - 17:00", Materials = "Paper, Plastic, Glass, E-Waste" },
            new { Id = 2, Name = "Beacon Bay Eco Depot", Address = "45 Quenera Drive, Beacon Bay", Hours = "Mon-Sat: 08:00 - 15:00", Materials = "Cardboard, Metal, Organic Waste" },
            new { Id = 3, Name = "Nahoon Green Drop-off", Address = "8 Beach Road, Nahoon", Hours = "24/7 Self-Service", Materials = "Glass, Plastic Bottles, Cans" }
        };

        return Ok(points);
    }

    [HttpGet("projects")]
    public IActionResult GetProjects()
    {
        var projects = new[]
        {
            new { Id = 1, Title = "Coastal Plastic Recovery Initiative", Description = "Clearing shoreline plastics along the Eastern Cape coastline and upcycling into building materials.", Status = "Active", Progress = 75 },
            new { Id = 2, Title = "Private Estate Composting Network", Description = "Establishing decentralized organic waste composting hubs for private housing complexes.", Status = "In Planning", Progress = 30 },
            new { Id = 3, Title = "E-Waste Safe Disposal Drive", Description = "Collecting obsolete electronics from private enterprises for certified hazardous component recycling.", Status = "Active", Progress = 90 }
        };

        return Ok(projects);
    }

    [HttpGet("events")]
    public IActionResult GetEvents()
    {
        var events = new[]
        {
            new { Id = 1, Title = "Nahoon Beach Morning Sweep", Date = "2026-11-12 09:00", Location = "Nahoon Mouth Parking Area", Organizer = "EcoEast Operations" },
            new { Id = 2, Title = "Business Park Recycling Workshop", Date = "2026-11-18 14:00", Location = "Gonubie Conference Centre", Organizer = "Private Waste Division" },
            new { Id = 3, Title = "Riverbank Restoration Clean-up", Date = "2026-11-25 08:30", Location = "Buffalo River Bank Sector B", Organizer = "Community Green Team" }
        };

        return Ok(events);
    }

    [HttpGet("notifications")]
    [Authorize]
    public IActionResult GetGeneralNotifications()
    {
        var notifications = new[]
        {
            new { Id = 1, Title = "Schedule Update", Message = "Your local private waste collection schedule has been updated for November.", Date = DateTime.UtcNow.AddDays(-1), IsRead = false },
            new { Id = 2, Title = "Drop-off Reward", Message = "You earned 50 Eco-Points for your recent drop-off at East London Central Hub.", Date = DateTime.UtcNow.AddDays(-3), IsRead = true }
        };

        return Ok(notifications);
    }
}

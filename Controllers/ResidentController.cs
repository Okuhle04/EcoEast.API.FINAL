using System.Security.Claims;
using EcoEast.API.Data;
using EcoEast.API.Dtos;
using EcoEast.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/resident")]
[Authorize(Roles = "Resident")]
public class ResidentController(EcoEastDbContext db) : ControllerBase
{
    private int? PersonId
    {
        get
        {
            var claimVal = User.FindFirstValue("PersonId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimVal, out var id) ? id : null;
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        if (PersonId is null) return Forbid();
        var pid = PersonId.Value;
        var requests = await (from r in db.ServiceRequests join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                              where r.RESIDENT_ID == pid select s.STATUS_NAME).ToListAsync();
        var reports = await db.ReportSubmissions.Where(x => x.RESIDENT_ID == pid).Select(x => x.REVIWED_AT).ToListAsync();
        bool Has(string n, string part) => (n ?? "").Contains(part, StringComparison.OrdinalIgnoreCase);
        var suburbId = await (from p in db.People join l in db.Locations on p.LOCATION_ID equals l.LOCATION_ID
                              where p.PersonID == pid select (int?)l.SUBURB_ID).FirstOrDefaultAsync();
        object? nextCollection = null;
        if (suburbId.HasValue)
        {
            nextCollection = await (from x in db.WasteCollectionSchedules
                                    join a in db.WasteSolutionsSuburbs on x.WS_ID equals a.WS_ID
                                    where x.IS_ACTIVE && a.SUBURB_ID == suburbId.Value
                                    select new { x.COLLECTION_DAY, x.COLLECTION_TYPE, x.COLLECTION_TIME }).FirstOrDefaultAsync();
        }
        return Ok(new
        {
            totalRequests = requests.Count,
            completedRequests = requests.Count(n => Has(n, "complet")),
            inProgressRequests = requests.Count(n => Has(n, "progress")),
            totalReports = reports.Count,
            reportsAwaitingReview = reports.Count(d => d == null),
            nextCollection
        });
    }

    [HttpGet("service-requests")]
    public async Task<IActionResult> MyRequests()
    {
        if (PersonId is null) return Forbid();
        var rows = await (from r in db.ServiceRequests
                          join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                          join t in db.RequestTypes on r.REQUEST_TYPE_ID equals t.REQUEST_TYPE_ID
                          where r.RESIDENT_ID == PersonId.Value
                          orderby r.REQUEST_ID descending
                          select new { r.REQUEST_ID, r.DESCRIPTION, r.SCHEDULED_DATE, Status=s.STATUS_NAME, RequestType=t.REQUEST_NAME }).ToListAsync();
        return Ok(rows);
    }

    [HttpPost("service-requests")]
    public async Task<IActionResult> CreateRequest(CreateServiceRequestDto dto)
    {
        if (PersonId is null || !await db.Residents.AnyAsync(x => x.RESIDENT_ID == PersonId.Value))
            return Forbid();

        if (!await db.WasteSolutionsSuburbs.AnyAsync(x => x.WS_ID == dto.WsId))
            return BadRequest(new { message = "Invalid service area selected." });

        if (!await db.RequestTypes.AnyAsync(x => x.REQUEST_TYPE_ID == dto.RequestTypeId))
            return BadRequest(new { message = "Invalid service request type selected." });

        var initialStatus = await db.Statuses.OrderBy(x => x.STATUS_ID).Select(x => (int?)x.STATUS_ID).FirstOrDefaultAsync();
        if (initialStatus == null)
            return StatusCode(500, new { message = "No request statuses are configured in the system." });

        var entity = new ServiceRequest
        {
            RESIDENT_ID = PersonId.Value,
            WS_ID = dto.WsId,
            REQUEST_TYPE_ID = dto.RequestTypeId,
            STATUS_ID = initialStatus.Value,
            DESCRIPTION = dto.Description?.Trim()
        };

        db.ServiceRequests.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(MyRequests), new { id = entity.REQUEST_ID }, new { message = "Service request created.", requestId = entity.REQUEST_ID });
    }

    [HttpPost("reports")]
    public async Task<IActionResult> CreateReport(CreateReportDto dto)
    {
        if (PersonId is null || !await db.Residents.AnyAsync(x => x.RESIDENT_ID == PersonId.Value))
            return Forbid();

        if (!await db.WasteSolutionsSuburbs.AnyAsync(x => x.WS_ID == dto.WsId))
            return BadRequest(new { message = "Invalid service area selected." });

        if (!await db.ReportCategories.AnyAsync(x => x.REP_CATEGORY_ID == dto.CategoryId))
            return BadRequest(new { message = "Invalid report category selected." });

        if (dto.LocationId.HasValue && !await db.Locations.AnyAsync(x => x.LOCATION_ID == dto.LocationId))
            return BadRequest(new { message = "Invalid location selected." });

        var status = await db.Statuses.OrderBy(x => x.STATUS_ID).Select(x => (int?)x.STATUS_ID).FirstOrDefaultAsync();
        if (status == null)
            return StatusCode(500, new { message = "No report statuses are configured in the system." });

        var report = new ReportSubmission
        {
            RESIDENT_ID = PersonId.Value,
            WS_ID = dto.WsId,
            CATEGORY_ID = dto.CategoryId,
            STATUS_ID = status.Value,
            DESCRIPTION = dto.Description?.Trim(),
            LOCATION_ID = dto.LocationId,
            SUBMISSION_DATE = DateTime.UtcNow
        };

        db.ReportSubmissions.Add(report);
        await db.SaveChangesAsync();

        return Ok(new { message = "Environmental report submitted successfully.", reportId = report.REPORT_ID });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> MyReports()
    {
        if (PersonId is null) return Forbid();
        var rows = await (from r in db.ReportSubmissions
                          join c in db.ReportCategories on r.CATEGORY_ID equals c.REP_CATEGORY_ID
                          join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                          where r.RESIDENT_ID == PersonId.Value
                          orderby r.REPORT_ID descending
                          select new {r.REPORT_ID,r.DESCRIPTION,r.SUBMISSION_DATE,Category=c.CATEGORY_NAME,Status=s.STATUS_NAME}).ToListAsync();
        return Ok(rows);
    }
}

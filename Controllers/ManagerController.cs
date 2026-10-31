using System.Security.Claims;
using EcoEast.API.Data;
using EcoEast.API.Dtos;
using EcoEast.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Roles = "Manager,Admin")]
public class ManagerController(EcoEastDbContext db) : ControllerBase
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
        var totalReports = await db.ReportSubmissions.CountAsync();
        var pendingReports = await (from r in db.ReportSubmissions
                                    join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                                    where s.STATUS_NAME.Contains("Pending") || s.STATUS_NAME.Contains("New")
                                    select r).CountAsync();

        var totalRequests = await db.ServiceRequests.CountAsync();
        var activeSchedules = await db.WasteCollectionSchedules.CountAsync(x => x.IS_ACTIVE);

        return Ok(new
        {
            totalReports,
            pendingReports,
            totalRequests,
            activeSchedules
        });
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetAllReports()
    {
        var reports = await (from r in db.ReportSubmissions
                             join c in db.ReportCategories on r.CATEGORY_ID equals c.REP_CATEGORY_ID
                             join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                             join p in db.People on r.RESIDENT_ID equals p.PersonID
                             orderby r.SUBMISSION_DATE descending
                             select new
                             {
                                 r.REPORT_ID,
                                 r.DESCRIPTION,
                                 r.SUBMISSION_DATE,
                                 r.REVIWED_AT,
                                 Category = c.CATEGORY_NAME,
                                 Status = s.STATUS_NAME,
                                 ResidentName = p.FIRST_NAME + " " + p.LAST_NAME,
                                 ResidentPhone = p.PHONE
                             }).ToListAsync();
        return Ok(reports);
    }

    [HttpPut("reports/{id}/status")]
    public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var report = await db.ReportSubmissions.FindAsync(id);
        if (report == null) return NotFound(new { message = "Report not found." });

        var statusExists = await db.Statuses.AnyAsync(s => s.STATUS_ID == dto.StatusId);
        if (!statusExists) return BadRequest(new { message = "Invalid status ID." });

        report.STATUS_ID = dto.StatusId;
        report.REVIWED_AT = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Ok(new { message = "Report status updated successfully." });
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetAllSchedules()
    {
        var schedules = await (from x in db.WasteCollectionSchedules
                               join a in db.WasteSolutionsSuburbs on x.WS_ID equals a.WS_ID
                               join s in db.Suburbs on a.SUBURB_ID equals s.SUBURB_ID
                               orderby s.SUBURB_NAME, x.COLLECTION_DAY
                               select new
                               {
                                   x.SCHEDULE_ID,
                                   x.COLLECTION_DAY,
                                   x.COLLECTION_TYPE,
                                   x.COLLECTION_TIME,
                                   x.Assigned_Team,
                                   x.IS_ACTIVE,
                                   SuburbsName = s.SUBURB_NAME,
                                   a.WS_ID
                               }).ToListAsync();
        return Ok(schedules);
    }

    [HttpGet("workers")]
    public async Task<IActionResult> GetWorkers()
    {
        var workers = await db.FieldWorkers
            .Where(x => x.Is_Active)
            .OrderBy(x => x.WORKER_ID)
            .Select(x => new { workerId = x.WORKER_ID, teamName = x.Team_Name })
            .ToListAsync();

        return Ok(workers);
    }

    [HttpGet("collection-types")]
    public async Task<IActionResult> GetCollectionTypes()
    {
        return Ok(await db.CollectionTypes
            .OrderBy(x => x.COLLECTION_NAME)
            .Select(x => new { collectionTypeId = x.COLLECTION_ID, name = x.COLLECTION_NAME })
            .ToListAsync());
    }

    [HttpPost("assignments")]
    public async Task<IActionResult> AssignWorker([FromBody] AssignWorkerDto dto)
    {
        if (!TimeSpan.TryParse(dto.CollectionTime, out var collectionTime))
            return BadRequest(new { message = "Invalid collection time. Use HH:mm, for example 09:00." });

        if (!await db.FieldWorkers.AnyAsync(x => x.WORKER_ID == dto.WorkerId && x.Is_Active))
            return BadRequest(new { message = "Invalid or inactive field worker." });

        if (!await db.ServiceRequests.AnyAsync(x => x.REQUEST_ID == dto.RequestId))
            return BadRequest(new { message = "Invalid service request." });

        if (!await db.CollectionTypes.AnyAsync(x => x.COLLECTION_ID == dto.CollectionTypeId))
            return BadRequest(new { message = "Invalid collection type." });

        var assignment = new WasteSchedule
        {
            WORKER_ID = dto.WorkerId,
            REQUEST_ID = dto.RequestId,
            COLLECTION_TYPE_ID = dto.CollectionTypeId,
            COLLECTION_DATE = dto.CollectionDate.Date,
            COLLECTION_TIME = collectionTime
        };

        db.WasteSchedules.Add(assignment);
        await db.SaveChangesAsync();

        return Ok(new { message = "Field worker assigned successfully.", scheduleId = assignment.SCHEDULE_ID });
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto)
    {
        if (!await db.WasteSolutionsSuburbs.AnyAsync(x => x.WS_ID == dto.WsId))
            return BadRequest(new { message = "Invalid service area (WS_ID)." });

        if (!TimeSpan.TryParse(dto.CollectionTime.Trim(), out var collectionTime))
            return BadRequest(new { message = "Invalid collection time." });

        var schedule = new WasteCollectionSchedule
        {
            WS_ID = dto.WsId,
            COLLECTION_DAY = dto.CollectionDay.Trim(),
            COLLECTION_TYPE = dto.CollectionType.Trim(),
            COLLECTION_TIME = collectionTime,
            Assigned_Team = dto.AssignedTeam?.Trim(),
            IS_ACTIVE = true
        };

        db.WasteCollectionSchedules.Add(schedule);
        await db.SaveChangesAsync();

        return Ok(new { message = "Collection schedule created successfully.", scheduleId = schedule.SCHEDULE_ID });
    }

    [HttpPut("schedules/{id}")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] CreateScheduleDto dto)
    {
        var schedule = await db.WasteCollectionSchedules.FindAsync(id);
        if (schedule == null) return NotFound(new { message = "Schedule not found." });

        if (!TimeSpan.TryParse(dto.CollectionTime.Trim(), out var collectionTime))
            return BadRequest(new { message = "Invalid collection time." });

        schedule.COLLECTION_DAY = dto.CollectionDay.Trim();
        schedule.COLLECTION_TYPE = dto.CollectionType.Trim();
        schedule.COLLECTION_TIME = collectionTime;
        schedule.Assigned_Team = dto.AssignedTeam?.Trim();

        await db.SaveChangesAsync();
        return Ok(new { message = "Collection schedule updated successfully." });
    }
}

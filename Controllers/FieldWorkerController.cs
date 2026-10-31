using System.Security.Claims;
using EcoEast.API.Data;
using EcoEast.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoEast.API.Controllers;

[ApiController]
[Route("api/worker")]
[Authorize(Roles = "FieldWorker,Manager,Admin")]
public class FieldWorkerController(EcoEastDbContext db) : ControllerBase
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
        var assignedSchedulesCount = PersonId.HasValue
            ? await db.WasteSchedules.CountAsync(x => x.WORKER_ID == PersonId.Value)
            : 0;

        var pendingRequestsCount = await (from r in db.ServiceRequests
                                          join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                                          where s.STATUS_NAME.Contains("Pending") || s.STATUS_NAME.Contains("Assigned")
                                          select r).CountAsync();

        return Ok(new
        {
            assignedSchedulesCount,
            pendingRequestsCount
        });
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetMySchedules()
    {
        if (PersonId is null) return Forbid();

        var schedules = await (from x in db.WasteSchedules
                               join r in db.ServiceRequests on x.REQUEST_ID equals r.REQUEST_ID
                               join c in db.CollectionTypes on x.COLLECTION_TYPE_ID equals c.COLLECTION_ID
                               where x.WORKER_ID == PersonId.Value
                               orderby x.COLLECTION_DATE
                               select new
                               {
                                   x.SCHEDULE_ID,
                                   x.WORKER_ID,
                                   x.REQUEST_ID,
                                   CollectionDate = x.COLLECTION_DATE,
                                   CollectionTime = x.COLLECTION_TIME,
                                   CollectionType = c.COLLECTION_NAME,
                                   r.DESCRIPTION
                               }).ToListAsync();

        return Ok(schedules);
    }

    [HttpGet("service-requests")]
    public async Task<IActionResult> GetAssignedRequests()
    {
        var requests = await (from r in db.ServiceRequests
                              join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                              join t in db.RequestTypes on r.REQUEST_TYPE_ID equals t.REQUEST_TYPE_ID
                              join p in db.People on r.RESIDENT_ID equals p.PersonID
                              orderby r.REQUEST_ID descending
                              select new
                              {
                                  r.REQUEST_ID,
                                  r.DESCRIPTION,
                                  r.SCHEDULED_DATE,
                                  Status = s.STATUS_NAME,
                                  RequestType = t.REQUEST_NAME,
                                  ResidentName = p.FIRST_NAME + " " + p.LAST_NAME,
                                  ResidentPhone = p.PHONE
                              }).ToListAsync();

        return Ok(requests);
    }

    [HttpPut("service-requests/{id}/status")]
    public async Task<IActionResult> UpdateRequestStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var request = await db.ServiceRequests.FindAsync(id);
        if (request == null) return NotFound(new { message = "Service request not found." });

        if (!await db.Statuses.AnyAsync(s => s.STATUS_ID == dto.StatusId))
            return BadRequest(new { message = "Invalid status ID." });

        request.STATUS_ID = dto.StatusId;
        await db.SaveChangesAsync();

        return Ok(new { message = "Service request status updated successfully." });
    }

    [HttpGet("inspections")]
    public async Task<IActionResult> GetPendingInspections()
    {
        var inspections = await (from r in db.ReportSubmissions
                                 join c in db.ReportCategories on r.CATEGORY_ID equals c.REP_CATEGORY_ID
                                 join s in db.Statuses on r.STATUS_ID equals s.STATUS_ID
                                 join p in db.People on r.RESIDENT_ID equals p.PersonID
                                 where s.STATUS_NAME.Contains("Pending") || s.STATUS_NAME.Contains("New")
                                 orderby r.REPORT_ID descending
                                 select new
                                 {
                                     Id = r.REPORT_ID,
                                     Type = "Environmental Report",
                                     Category = c.CATEGORY_NAME,
                                     Description = r.DESCRIPTION,
                                     ResidentName = p.FIRST_NAME + " " + p.LAST_NAME,
                                     DateSubmitted = r.SUBMISSION_DATE,
                                     Status = s.STATUS_NAME
                                 }).ToListAsync();

        return Ok(inspections);
    }

    [HttpPost("inspections")]
    public async Task<IActionResult> SubmitInspection([FromBody] SubmitInspectionDto dto)
    {
        var report = await db.ReportSubmissions.FindAsync(dto.ReportId);
        if (report == null) return NotFound(new { message = "Report not found." });

        var inspectedStatus = await db.Statuses
            .FirstOrDefaultAsync(s => dto.IsApproved
                ? s.STATUS_NAME.Contains("Inspected") || s.STATUS_NAME.Contains("Completed")
                : s.STATUS_NAME.Contains("Rejected"));

        if (inspectedStatus == null)
            return BadRequest(new { message = "No suitable inspection status is configured." });

        report.STATUS_ID = inspectedStatus.STATUS_ID;
        report.REVIWED_AT = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Inspection result recorded successfully." });
    }

    [HttpPost("completion-proof")]
    public async Task<IActionResult> UploadCompletionProof([FromBody] UploadCompletionProofDto dto)
    {
        var request = await db.ServiceRequests.FindAsync(dto.RequestId);
        if (request == null) return NotFound(new { message = "Service request not found." });

        if (User.IsInRole("FieldWorker"))
        {
            if (PersonId is null || !await db.WasteSchedules.AnyAsync(x => x.REQUEST_ID == dto.RequestId && x.WORKER_ID == PersonId.Value))
                return Forbid();
        }

        var completedStatus = await db.Statuses
            .FirstOrDefaultAsync(s => s.STATUS_NAME.Contains("Completed") || s.STATUS_NAME.Contains("Closed"));

        if (completedStatus == null)
            return BadRequest(new { message = "No completed status is configured." });

        request.STATUS_ID = completedStatus.STATUS_ID;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            request.DESCRIPTION = $"{request.DESCRIPTION} [Completion Proof Notes: {dto.Notes.Trim()}]";

        await db.SaveChangesAsync();

        return Ok(new { message = "Completion proof uploaded and task marked as completed successfully." });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var isFieldWorker = User.IsInRole("FieldWorker");
        var workerId = PersonId;
        var notifications = await (from x in db.WasteSchedules
                                   join r in db.ServiceRequests on x.REQUEST_ID equals r.REQUEST_ID
                                   join t in db.RequestTypes on r.REQUEST_TYPE_ID equals t.REQUEST_TYPE_ID
                                   where !isFieldWorker || (workerId.HasValue && x.WORKER_ID == workerId.Value)
                                   orderby x.SCHEDULE_ID descending
                                   select new
                                   {
                                       id = r.REQUEST_ID,
                                       title = "New Task Assigned: " + t.REQUEST_NAME,
                                       message = r.DESCRIPTION ?? "A service request has been assigned to you.",
                                       date = x.COLLECTION_DATE,
                                       isRead = false
                                   }).Take(10).ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(int id)
    {
        if (!await db.ServiceRequests.AnyAsync(x => x.REQUEST_ID == id))
            return NotFound(new { message = "Notification not found." });

        return Ok(new { message = "Notification marked as read." });
    }
}

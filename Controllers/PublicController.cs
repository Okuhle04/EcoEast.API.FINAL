using EcoEast.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EcoEast.API.Controllers;
[ApiController, Route("api")]
public class PublicController(EcoEastDbContext db) : ControllerBase
{
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        try { var ok=await db.Database.CanConnectAsync(); return Ok(new {api="EcoEast.API",database=ok?"connected":"unavailable"}); }
        catch(Exception ex) { return StatusCode(503,new {api="EcoEast.API",database="error",detail=ex.Message}); }
    }
    [HttpGet("lookups/locations")]
    public async Task<IActionResult> Locations() => Ok(await (from l in db.Locations join s in db.Suburbs on l.SUBURB_ID equals s.SUBURB_ID join c in db.Cities on s.CITY_ID equals c.CITY_ID select new {l.LOCATION_ID,l.STREET_NAME,l.STREET_NUMBER,s.SUBURB_ID,s.SUBURB_NAME,c.CITY_ID,c.CITY_NAME}).ToListAsync());
    [HttpGet("lookups/service-areas")]
    public async Task<IActionResult> ServiceAreas() => Ok(await (from x in db.WasteSolutionsSuburbs join s in db.Suburbs on x.SUBURB_ID equals s.SUBURB_ID join w in db.WasteSolutions on x.WASTE_SOL_ID equals w.WASTE_SOL_ID select new {x.WS_ID,x.SUBURB_ID,s.SUBURB_NAME,x.WASTE_SOL_ID,w.WASTE_SOL_NAME}).ToListAsync());
    [HttpGet("lookups/request-types")]
    public async Task<IActionResult> RequestTypes() => Ok(await db.RequestTypes.AsNoTracking().ToListAsync());
    [HttpGet("lookups/report-categories")]
    public async Task<IActionResult> ReportCategories() => Ok(await db.ReportCategories.AsNoTracking().ToListAsync());
    [HttpGet("lookups/statuses")]
    public async Task<IActionResult> Statuses() => Ok(await db.Statuses.AsNoTracking().ToListAsync());
    [HttpGet("schedules/areas")]
    public async Task<IActionResult> AreaSchedules([FromQuery]int? suburbId)
    {
        var q=from x in db.WasteCollectionSchedules join a in db.WasteSolutionsSuburbs on x.WS_ID equals a.WS_ID join s in db.Suburbs on a.SUBURB_ID equals s.SUBURB_ID where x.IS_ACTIVE select new {x.SCHEDULE_ID,x.COLLECTION_DAY,x.COLLECTION_TYPE,x.COLLECTION_TIME,x.Assigned_Team,a.WS_ID,s.SUBURB_ID,s.SUBURB_NAME};
        if(suburbId.HasValue) q=q.Where(x=>x.SUBURB_ID==suburbId.Value);
        return Ok(await q.OrderBy(x=>x.SUBURB_NAME).ThenBy(x=>x.COLLECTION_DAY).ToListAsync());
    }
    [HttpGet("recycling-guide")]
    public async Task<IActionResult> RecyclingGuide() => Ok(await (from g in db.RecyclingGuides join c in db.ItemCategories on g.ITEM_CATEGORY_ID equals c.ITEM_CATEGORY_ID orderby c.CATEGORY_NAME select new {g.RECYCLING_ID,c.ITEM_CATEGORY_ID,c.CATEGORY_NAME,g.IS_RECYCLABLE,g.DISPOSAL_RULE,g.DATE_POSTED}).ToListAsync());
}

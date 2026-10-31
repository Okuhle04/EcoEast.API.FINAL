using Microsoft.AspNetCore.Identity;
namespace EcoEast.API.Models;

public class ApplicationUser : IdentityUser
{
    public int? PersonId { get; set; }
}
public class Person
{
    public int PersonID { get; set; }
    public int? LOCATION_ID { get; set; }
    public string LAST_NAME { get; set; } = "";
    public string FIRST_NAME { get; set; } = "";
    public string USERNAME { get; set; } = "";
    public string EMAIL { get; set; } = "";
    public string? PHONE { get; set; }
    public string? GENDER { get; set; }
    public string? ROLE { get; set; }
}
public class Resident { public int RESIDENT_ID { get; set; } }
public class Manager { public int MANAGER_ID { get; set; } public int WASTE_SOL_ID { get; set; } }
public class FieldWorker { public int WORKER_ID { get; set; } public int WASTE_SOL_ID { get; set; } public string? Team_Name { get; set; } public bool Is_Active { get; set; } }
public class Location { public int LOCATION_ID { get; set; } public string? STREET_NAME { get; set; } public string? STREET_NUMBER { get; set; } public int SUBURB_ID { get; set; } public string? LOCATION_DETAILS { get; set; } }
public class City { public int CITY_ID { get; set; } public string CITY_NAME { get; set; } = ""; }
public class Suburb { public int SUBURB_ID { get; set; } public string SUBURB_NAME { get; set; } = ""; public int CITY_ID { get; set; } }
public class WasteSolution { public int WASTE_SOL_ID { get; set; } public string WASTE_SOL_NAME { get; set; } = ""; public string? CONTACT_EMAIL { get; set; } public string? CELLPHONE_NUMBER { get; set; } }
public class WasteSolutionsSuburb { public int WS_ID { get; set; } public int WASTE_SOL_ID { get; set; } public int SUBURB_ID { get; set; } }
public class Status { public int STATUS_ID { get; set; } public string STATUS_NAME { get; set; } = ""; }
public class RequestType { public int REQUEST_TYPE_ID { get; set; } public string REQUEST_NAME { get; set; } = ""; }
public class ServiceRequest { public int REQUEST_ID { get; set; } public int RESIDENT_ID { get; set; } public int WS_ID { get; set; } public int REQUEST_TYPE_ID { get; set; } public int STATUS_ID { get; set; } public string? DESCRIPTION { get; set; } public DateTime? SCHEDULED_DATE { get; set; } }
public class CollectionType { public int COLLECTION_ID { get; set; } public string COLLECTION_NAME { get; set; } = ""; public string? COLLECTION_DESCRIPTION { get; set; } }
public class WasteSchedule { public int SCHEDULE_ID { get; set; } public int WORKER_ID { get; set; } public int REQUEST_ID { get; set; } public int COLLECTION_TYPE_ID { get; set; } public DateTime COLLECTION_DATE { get; set; } public TimeSpan COLLECTION_TIME { get; set; } }
public class ReportCategory { public int REP_CATEGORY_ID { get; set; } public string CATEGORY_NAME { get; set; } = ""; }
public class ReportSubmission { public int REPORT_ID { get; set; } public int RESIDENT_ID { get; set; } public int WS_ID { get; set; } public int CATEGORY_ID { get; set; } public int STATUS_ID { get; set; } public string? DESCRIPTION { get; set; } public DateTime? REVIWED_AT { get; set; } public DateTime SUBMISSION_DATE { get; set; } public int? LOCATION_ID { get; set; } }
public class WasteCollectionSchedule { public int SCHEDULE_ID { get; set; } public int WS_ID { get; set; } public string COLLECTION_DAY { get; set; } = ""; public string COLLECTION_TYPE { get; set; } = ""; public TimeSpan? COLLECTION_TIME { get; set; } public bool IS_ACTIVE { get; set; } public DateTime CREATED_AT { get; set; } public string? Assigned_Team { get; set; } }
public class ItemCategory { public int ITEM_CATEGORY_ID { get; set; } public string CATEGORY_NAME { get; set; } = ""; }
public class RecyclingGuide { public int RECYCLING_ID { get; set; } public int ITEM_CATEGORY_ID { get; set; } public bool IS_RECYCLABLE { get; set; } public int MANAGER_ID { get; set; } public string? DISPOSAL_RULE { get; set; } public DateTime? DATE_POSTED { get; set; } }

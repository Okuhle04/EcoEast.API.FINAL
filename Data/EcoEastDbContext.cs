using EcoEast.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace EcoEast.API.Data;

public class EcoEastDbContext : IdentityDbContext<ApplicationUser>
{
    public EcoEastDbContext(DbContextOptions<EcoEastDbContext> options) : base(options) { }
    public DbSet<Person> People => Set<Person>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<FieldWorker> FieldWorkers => Set<FieldWorker>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Suburb> Suburbs => Set<Suburb>();
    public DbSet<WasteSolution> WasteSolutions => Set<WasteSolution>();
    public DbSet<WasteSolutionsSuburb> WasteSolutionsSuburbs => Set<WasteSolutionsSuburb>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<CollectionType> CollectionTypes => Set<CollectionType>();
    public DbSet<WasteSchedule> WasteSchedules => Set<WasteSchedule>();
    public DbSet<ReportCategory> ReportCategories => Set<ReportCategory>();
    public DbSet<ReportSubmission> ReportSubmissions => Set<ReportSubmission>();
    public DbSet<WasteCollectionSchedule> WasteCollectionSchedules => Set<WasteCollectionSchedule>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<RecyclingGuide> RecyclingGuides => Set<RecyclingGuide>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ApplicationUser>().ToTable("AspNetUsers").Property(x => x.PersonId).HasColumnName("PersonId");
        b.Entity<Person>().ToTable("PERSON").HasKey(x => x.PersonID);
        b.Entity<Person>().Property(x => x.PersonID).HasColumnName("PersonID");
        b.Entity<Resident>().ToTable("RESIDENT").HasKey(x => x.RESIDENT_ID);
        b.Entity<Manager>().ToTable("MANAGERS").HasKey(x => x.MANAGER_ID);
        b.Entity<FieldWorker>().ToTable("FIELD_WORKERS").HasKey(x => x.WORKER_ID);
        b.Entity<Location>().ToTable("LOCATIONS").HasKey(x => x.LOCATION_ID);
        b.Entity<City>().ToTable("CITIES").HasKey(x => x.CITY_ID);
        b.Entity<Suburb>().ToTable("SUBURBS").HasKey(x => x.SUBURB_ID);
        b.Entity<WasteSolution>().ToTable("WASTE_SOLUTIONS").HasKey(x => x.WASTE_SOL_ID);
        b.Entity<WasteSolutionsSuburb>().ToTable("WASTESOLUTIONS_SUBURBS").HasKey(x => x.WS_ID);
        b.Entity<Status>().ToTable("STATUSES").HasKey(x => x.STATUS_ID);
        b.Entity<RequestType>().ToTable("REQUEST_TYPES").HasKey(x => x.REQUEST_TYPE_ID);
        b.Entity<ServiceRequest>().ToTable("SERVICE_REQUESTS").HasKey(x => x.REQUEST_ID);
        b.Entity<CollectionType>().ToTable("COLLECTION_TYPES").HasKey(x => x.COLLECTION_ID);
        b.Entity<WasteSchedule>().ToTable("WASTE_SCHEDULES").HasKey(x => x.SCHEDULE_ID);
        b.Entity<ReportCategory>().ToTable("REPORT_CATEGORIES").HasKey(x => x.REP_CATEGORY_ID);
        b.Entity<ReportSubmission>().ToTable("REPORT_SUBMISSIONS").HasKey(x => x.REPORT_ID);
        b.Entity<WasteCollectionSchedule>().ToTable("WASTE_COLLECTION_SCHEDULES").HasKey(x => x.SCHEDULE_ID);
        b.Entity<ItemCategory>().ToTable("ITEM_CATEGORIES").HasKey(x => x.ITEM_CATEGORY_ID);
        b.Entity<RecyclingGuide>().ToTable("RECYCLING_GUIDES").HasKey(x => x.RECYCLING_ID);
        b.Entity<WasteCollectionSchedule>().Property(x => x.CREATED_AT).HasDefaultValueSql("GETDATE()");
        b.Entity<FieldWorker>().Property(x => x.Is_Active).HasColumnName("Is_Active");
        b.Entity<FieldWorker>().Property(x => x.Team_Name).HasColumnName("Team_Name");
        b.Entity<WasteCollectionSchedule>().Property(x => x.Assigned_Team).HasColumnName("Assigned_Team");
        // PersonId intentionally has no EF relationship: the supplied database has no FK for AspNetUsers.PersonId.
        // Do not call EnsureCreated/Migrate against this existing database.
    }
}

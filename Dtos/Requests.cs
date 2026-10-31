namespace EcoEast.API.Dtos;
public record RegisterResidentDto(string FirstName, string LastName, string UserName, string Email, string Password, string? Phone, int? LocationId);
public record LoginDto(string UserName, string Password);
public record CreateServiceRequestDto(int WsId, int RequestTypeId, string? Description);
public record CreateReportDto(int WsId, int CategoryId, string? Description, int? LocationId);
public record CreateAreaScheduleDto(int WsId, string CollectionDay, string CollectionType, TimeSpan? CollectionTime, string? AssignedTeam);

public class UpdateStatusDto
{
    public int StatusId { get; set; }
}

public class CreateScheduleDto
{
    public int WsId { get; set; }
    public string CollectionDay { get; set; } = string.Empty;
    public string CollectionType { get; set; } = string.Empty;
    public string CollectionTime { get; set; } = string.Empty;
    public string? AssignedTeam { get; set; }
}

public class AssignRoleDto
{
    public string UserId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class UpdateRoleDto
{
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;
}

public class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int? LocationId { get; set; }
}

public class AssignWorkerDto
{
    public int WorkerId { get; set; }
    public int RequestId { get; set; }
    public int CollectionTypeId { get; set; }
    public DateTime CollectionDate { get; set; }
    public string CollectionTime { get; set; } = string.Empty;
}

public class SubmitInspectionDto
{
    public int ReportId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}

public class UploadCompletionProofDto
{
    public int RequestId { get; set; }
    public string? ProofImageUrl { get; set; }
    public string? Notes { get; set; }
}

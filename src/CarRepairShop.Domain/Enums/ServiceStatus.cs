namespace CarRepairShop.Domain.Enums;

public enum ServiceStatus
{
    Received = 1,
    Diagnosing = 2,
    WaitingForApproval = 3,
    Executing = 4,
    Finished = 5,
    Delivered = 6
}

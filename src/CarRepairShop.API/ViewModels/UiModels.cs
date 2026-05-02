using System.ComponentModel.DataAnnotations;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.API.ViewModels;

public class FlashMessageViewModel
{
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}

public class LookupOptionViewModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public class ServicesPageViewModel
{
    public string? Search { get; set; }
    public ServiceStatus? Status { get; set; }
    public bool CanCreateService { get; set; }
    public FlashMessageViewModel? Flash { get; set; }
    public IReadOnlyList<ServiceStatusCountViewModel> StatusCounts { get; set; } = [];
    public IReadOnlyList<ServiceListItemViewModel> Services { get; set; } = [];
}

public class ServiceStatusCountViewModel
{
    public ServiceStatus? Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ServiceListItemViewModel
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string VehicleLabel { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string AssignedEmployee { get; set; } = string.Empty;
    public ServiceStatus Status { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal GrandTotal => PartsTotal + LaborTotal;
    public int JobCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ServiceCreateViewModel
{
    [Required]
    public Guid? CustomerId { get; set; }

    [Required]
    public Guid? VehicleId { get; set; }

    public FlashMessageViewModel? Flash { get; set; }
    public bool CanCreate { get; set; }
    public IReadOnlyList<LookupOptionViewModel> Customers { get; set; } = [];
    public IReadOnlyList<LookupOptionViewModel> Vehicles { get; set; } = [];
}

public class AddServiceItemFormViewModel : IValidatableObject
{
    [Required]
    public Guid? CatalogItemId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => [];
}

public class AddServiceJobFormViewModel
{
    [Required]
    public Guid? CatalogJobId { get; set; }
}

public class ServiceDetailViewModel
{
    public Guid Id { get; set; }
    public ServiceStatus Status { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerTelephone { get; set; } = string.Empty;
    public string VehicleLabel { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string AssignedEmployee { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal PartsTotal { get; set; }
    public decimal LaborTotal { get; set; }
    public decimal GrandTotal => PartsTotal + LaborTotal;
    public bool IsAdmin { get; set; }
    public bool CanEditService { get; set; }
    public bool CanModifyServiceItems { get; set; }
    public FlashMessageViewModel? Flash { get; set; }
    public AddServiceItemFormViewModel ItemForm { get; set; } = new();
    public AddServiceJobFormViewModel JobForm { get; set; } = new();
    public IReadOnlyList<CatalogItemListItemViewModel> CatalogItems { get; set; } = [];
    public IReadOnlyList<CatalogJobListItemViewModel> CatalogJobs { get; set; } = [];
    public IReadOnlyList<ServiceItemRowViewModel> Items { get; set; } = [];
    public IReadOnlyList<ServiceJobRowViewModel> Jobs { get; set; } = [];
    public IReadOnlyList<ServiceHistoryRowViewModel> History { get; set; } = [];
    public bool CanRequestApproval { get; set; }
    public bool CanApprove { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanDispute { get; set; }
}

public class ServiceItemRowViewModel
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public class ServiceJobRowViewModel
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public Guid CatalogJobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public decimal Price { get; set; }
    public string AssignedEmployee { get; set; } = "Unassigned";
    public bool CanAcknowledge { get; set; }
    public bool CanStartProgress { get; set; }
    public bool CanComplete { get; set; }
    public bool CanDelete { get; set; }
}

public class ServiceHistoryRowViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public class CustomersPageViewModel
{
    public FlashMessageViewModel? Flash { get; set; }
    public CustomerListFiltersViewModel Filters { get; set; } = new();
    public CustomerFormViewModel Form { get; set; } = new();
    public IReadOnlyList<CustomerListItemViewModel> Customers { get; set; } = [];
}

public class CustomerListFiltersViewModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PersonalId { get; set; }
    public string? Telephone { get; set; }
}

public class CustomerFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string PersonalId { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Telephone { get; set; } = string.Empty;
}

public class CustomerListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PersonalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
}

public class CustomerDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PersonalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public IReadOnlyList<VehicleListItemViewModel> Vehicles { get; set; } = [];
    public bool CanDelete { get; set; }
}

public class CustomerDetailsModalViewModel
{
    public CustomerFormViewModel Form { get; set; } = new();
    public IReadOnlyList<VehicleListItemViewModel> Vehicles { get; set; } = [];
    public bool CanDelete { get; set; }
}

public class CustomerVehiclesModalViewModel
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public IReadOnlyList<VehicleListItemViewModel> Vehicles { get; set; } = [];
}

public class VehiclesPageViewModel
{
    public FlashMessageViewModel? Flash { get; set; }
    public VehicleListFiltersViewModel Filters { get; set; } = new();
    public VehicleFormViewModel Form { get; set; } = new();
    public IReadOnlyList<VehicleListItemViewModel> Vehicles { get; set; } = [];
    public IReadOnlyList<LookupOptionViewModel> Customers { get; set; } = [];
}

public class VehicleListFiltersViewModel
{
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? LicensePlate { get; set; }
}

public class VehicleFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid? CustomerId { get; set; }

    [Required]
    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Required]
    public string LicensePlate { get; set; } = string.Empty;

    public string? Color { get; set; }
}

public class VehicleListItemViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Color { get; set; }
}

public class VehicleDetailViewModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool CanDelete { get; set; }
}

public class VehicleDetailsModalViewModel
{
    public VehicleFormViewModel Form { get; set; } = new();
    public string CustomerName { get; set; } = string.Empty;
    public bool CanDelete { get; set; }
}

public class CatalogPageViewModel
{
    public FlashMessageViewModel? Flash { get; set; }
    public CatalogListFiltersViewModel Filters { get; set; } = new();
    public CatalogItemFormViewModel Form { get; set; } = new();
    public IReadOnlyList<CatalogItemListItemViewModel> Items { get; set; } = [];
}

public class CatalogListFiltersViewModel
{
    public string? Name { get; set; }
}

public class CatalogItemFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(0, 999999)]
    public int Stock { get; set; }
}

public class CatalogItemListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public class CatalogItemDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool CanDelete { get; set; }
}

public class CatalogItemDetailsModalViewModel
{
    public CatalogItemFormViewModel Form { get; set; } = new();
    public bool CanDelete { get; set; }
}

public class CatalogJobListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class UsersPageViewModel
{
    public FlashMessageViewModel? Flash { get; set; }
    public UserListFiltersViewModel Filters { get; set; } = new();
    public UserFormViewModel Form { get; set; } = new();
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = [];
}

public class UserListFiltersViewModel
{
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class UserFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public UserRole Role { get; set; } = UserRole.Mechanic;
}

public class UserListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
}

public class UserDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public bool CanDeactivate => IsActive;
}

public class UserDetailsModalViewModel
{
    public UserFormViewModel Form { get; set; } = new();
    public bool IsActive { get; set; }
    public bool CanDeactivate => IsActive;
}

public class ChangePasswordViewModel
{
    public FlashMessageViewModel? Flash { get; set; }

    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ServiceJobsPageViewModel
{
    public string? Search { get; set; }
    public JobStatus? Status { get; set; }
    public FlashMessageViewModel? Flash { get; set; }
    public ServiceJobFormViewModel Form { get; set; } = new();
    public IReadOnlyList<ServiceJobListItemViewModel> Jobs { get; set; } = [];
}

public class ServiceJobDetailsModalViewModel
{
    public ServiceJobFormViewModel Form { get; set; } = new();
    public bool CanDelete { get; set; }
}

public class ServiceJobListItemViewModel
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public decimal Price { get; set; }
    public string AssignedEmployee { get; set; } = string.Empty;
    public string ServiceLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
}

public class ServiceJobDetailViewModel
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public string ServiceLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AssignedEmployee { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public FlashMessageViewModel? Flash { get; set; }
    public ServiceJobFormViewModel Form { get; set; } = new();
    public IReadOnlyList<ServiceHistoryRowViewModel> History { get; set; } = [];
    public bool CanAcknowledge { get; set; }
    public bool CanStartProgress { get; set; }
    public bool CanComplete { get; set; }
    public bool CanDelete { get; set; }
}

public class ServiceJobFormViewModel
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Price { get; set; }
}

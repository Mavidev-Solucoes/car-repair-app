using CarRepairShop.API.ViewModels;
using CarRepairShop.Domain.Enums;

namespace CarRepairShop.UnitTests.ViewModels;

public class UiModelsTests
{
    [Fact]
    public void ServiceListItemViewModel_GrandTotal_SumsParts_And_Labor()
    {
        var vm = new ServiceListItemViewModel { PartsTotal = 100m, LaborTotal = 50m };
        Assert.Equal(150m, vm.GrandTotal);
    }

    [Fact]
    public void ServiceDetailViewModel_GrandTotal_SumsParts_And_Labor()
    {
        var vm = new ServiceDetailViewModel { PartsTotal = 200m, LaborTotal = 75m };
        Assert.Equal(275m, vm.GrandTotal);
    }

    [Fact]
    public void ServiceItemRowViewModel_LineTotal_IsPrice_Times_Quantity()
    {
        var vm = new ServiceItemRowViewModel { Price = 10m, Quantity = 3 };
        Assert.Equal(30m, vm.LineTotal);
    }

    [Fact]
    public void UserDetailViewModel_CanDeactivate_True_WhenActive()
    {
        var vm = new UserDetailViewModel { IsActive = true };
        Assert.True(vm.CanDeactivate);
    }

    [Fact]
    public void UserDetailViewModel_CanDeactivate_False_WhenInactive()
    {
        var vm = new UserDetailViewModel { IsActive = false };
        Assert.False(vm.CanDeactivate);
    }

    [Fact]
    public void UserDetailsModalViewModel_CanDeactivate_TrueWhenActive()
    {
        Assert.True(new UserDetailsModalViewModel { IsActive = true }.CanDeactivate);
    }

    [Fact]
    public void UserDetailsModalViewModel_CanDeactivate_FalseWhenInactive()
    {
        Assert.False(new UserDetailsModalViewModel { IsActive = false }.CanDeactivate);
    }

    [Fact]
    public void FlashMessageViewModel_DefaultsToEmpty()
    {
        var vm = new FlashMessageViewModel();
        Assert.Equal(string.Empty, vm.Message);
        Assert.False(vm.IsError);
    }

    [Fact]
    public void LoginViewModel_DefaultsToEmpty()
    {
        var vm = new LoginViewModel();
        Assert.Equal(string.Empty, vm.Email);
        Assert.Equal(string.Empty, vm.Password);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void CustomerFormViewModel_Defaults()
    {
        var vm = new CustomerFormViewModel();
        Assert.Null(vm.Id);
        Assert.Equal(string.Empty, vm.Name);
    }

    [Fact]
    public void VehicleFormViewModel_Defaults()
    {
        var vm = new VehicleFormViewModel();
        Assert.Null(vm.Id);
        Assert.Equal(string.Empty, vm.Brand);
    }

    [Fact]
    public void CatalogItemFormViewModel_Defaults()
    {
        var vm = new CatalogItemFormViewModel();
        Assert.Null(vm.Id);
        Assert.Equal(string.Empty, vm.Name);
    }

    [Fact]
    public void ServiceJobFormViewModel_Defaults()
    {
        var vm = new ServiceJobFormViewModel();
        Assert.Equal(default(Guid), vm.Id);
        Assert.Equal(string.Empty, vm.Name);
    }

    [Fact]
    public void UserFormViewModel_DefaultRole_IsMechanic()
    {
        var vm = new UserFormViewModel();
        Assert.Equal(UserRole.Mechanic, vm.Role);
    }

    [Fact]
    public void AddServiceItemFormViewModel_DefaultQuantity_IsOne()
    {
        var vm = new AddServiceItemFormViewModel();
        Assert.Equal(1, vm.Quantity);
        Assert.Null(vm.CatalogItemId);
    }

    [Fact]
    public void AddServiceItemFormViewModel_Validate_ReturnsEmpty()
    {
        var vm = new AddServiceItemFormViewModel();
        var results = vm.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(vm));
        Assert.Empty(results);
    }

    [Fact]
    public void ServiceJobRowViewModel_Defaults()
    {
        var vm = new ServiceJobRowViewModel();
        Assert.Equal("Unassigned", vm.AssignedEmployee);
    }

    [Fact]
    public void ChangePasswordViewModel_Defaults()
    {
        var vm = new ChangePasswordViewModel();
        Assert.Null(vm.Flash);
        Assert.Equal(string.Empty, vm.CurrentPassword);
        Assert.Equal(string.Empty, vm.NewPassword);
        Assert.Equal(string.Empty, vm.ConfirmPassword);
    }

    [Fact]
    public void ServiceStatusCountViewModel_Defaults()
    {
        var vm = new ServiceStatusCountViewModel();
        Assert.Equal(string.Empty, vm.Label);
        Assert.Equal(0, vm.Count);
        Assert.Null(vm.Status);
    }

    [Fact]
    public void LookupOptionViewModel_Defaults()
    {
        var vm = new LookupOptionViewModel();
        Assert.Equal(default(Guid), vm.Id);
        Assert.Equal(string.Empty, vm.Label);
    }

    [Fact]
    public void ServiceJobDetailViewModel_Defaults()
    {
        var vm = new ServiceJobDetailViewModel();
        Assert.Equal(string.Empty, vm.AssignedEmployee);
        Assert.False(vm.CanAcknowledge);
        Assert.False(vm.CanStartProgress);
        Assert.False(vm.CanComplete);
        Assert.False(vm.CanDelete);
    }
}

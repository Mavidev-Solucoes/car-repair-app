using CarRepairShop.API.ViewModels;

namespace CarRepairShop.UnitTests.Services.API;

public class UiModelsTests
{
    [Fact]
    public void ServiceListItemViewModel_GrandTotal_IsSumOfPartsAndLabor()
    {
        var vm = new ServiceListItemViewModel { PartsTotal = 100m, LaborTotal = 50m };
        Assert.Equal(150m, vm.GrandTotal);
    }

    [Fact]
    public void ServiceDetailViewModel_GrandTotal_IsSumOfPartsAndLabor()
    {
        var vm = new ServiceDetailViewModel { PartsTotal = 200m, LaborTotal = 75m };
        Assert.Equal(275m, vm.GrandTotal);
    }

    [Fact]
    public void ServiceItemRowViewModel_LineTotal_IsPriceTimesQuantity()
    {
        var vm = new ServiceItemRowViewModel { Price = 10m, Quantity = 3 };
        Assert.Equal(30m, vm.LineTotal);
    }

    [Fact]
    public void UserDetailViewModel_CanDeactivate_TrueWhenActive()
    {
        var vm = new UserDetailViewModel { IsActive = true };
        Assert.True(vm.CanDeactivate);
    }

    [Fact]
    public void UserDetailViewModel_CanDeactivate_FalseWhenInactive()
    {
        var vm = new UserDetailViewModel { IsActive = false };
        Assert.False(vm.CanDeactivate);
    }

    [Fact]
    public void UserDetailsModalViewModel_CanDeactivate_TrueWhenActive()
    {
        var vm = new UserDetailsModalViewModel { IsActive = true };
        Assert.True(vm.CanDeactivate);
    }

    [Fact]
    public void UserDetailsModalViewModel_CanDeactivate_FalseWhenInactive()
    {
        var vm = new UserDetailsModalViewModel { IsActive = false };
        Assert.False(vm.CanDeactivate);
    }

    [Fact]
    public void FlashMessageViewModel_DefaultsToNotError()
    {
        var vm = new FlashMessageViewModel { Message = "Hello" };
        Assert.False(vm.IsError);
    }

    [Fact]
    public void LookupOptionViewModel_Properties_Work()
    {
        var id = Guid.NewGuid();
        var vm = new LookupOptionViewModel { Id = id, Label = "Option A" };
        Assert.Equal(id, vm.Id);
        Assert.Equal("Option A", vm.Label);
    }

    [Fact]
    public void AddServiceItemFormViewModel_Validate_ReturnsEmpty()
    {
        var vm = new AddServiceItemFormViewModel { CatalogItemId = Guid.NewGuid(), Quantity = 2 };
        var results = vm.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(vm));
        Assert.Empty(results);
    }

    [Fact]
    public void ServiceJobDetailViewModel_CanAcknowledge_WhenSet()
    {
        var vm = new ServiceJobDetailViewModel { CanAcknowledge = true };
        Assert.True(vm.CanAcknowledge);
        Assert.False(vm.CanStartProgress);
        Assert.False(vm.CanComplete);
    }

    [Fact]
    public void ServiceJobDetailViewModel_CanStartProgress_WhenSet()
    {
        var vm = new ServiceJobDetailViewModel { CanStartProgress = true };
        Assert.True(vm.CanStartProgress);
        Assert.False(vm.CanAcknowledge);
    }

    [Fact]
    public void ServiceJobDetailViewModel_CanComplete_WhenSet()
    {
        var vm = new ServiceJobDetailViewModel { CanComplete = true };
        Assert.True(vm.CanComplete);
        Assert.False(vm.CanAcknowledge);
        Assert.False(vm.CanStartProgress);
    }

    [Fact]
    public void ServiceJobDetailViewModel_DefaultsHaveNoActions()
    {
        var vm = new ServiceJobDetailViewModel();
        Assert.False(vm.CanAcknowledge);
        Assert.False(vm.CanStartProgress);
        Assert.False(vm.CanComplete);
    }
}

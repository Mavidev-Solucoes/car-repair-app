using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class VehiclesUiControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private VehiclesUiController CreateController(bool htmxRequest = false)
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, htmxRequest: htmxRequest);
        return new VehiclesUiController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static VehicleDto MakeVehicle(string brand = "Ford") =>
        new(Guid.NewGuid(), Guid.NewGuid(), brand, "Focus", 2022, "ABC1234", "Black", DateTime.UtcNow);

    private static CustomerDto MakeCustomer(string name = "Alice") =>
        new(Guid.NewGuid(), name, "12345678901", "alice@example.com", "11987654321", true, DateTime.UtcNow);

    private void SetupPagedVehicles(IEnumerable<VehicleDto>? vehicles = null)
    {
        vehicles ??= Array.Empty<VehicleDto>();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<VehicleDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<VehicleDto>(vehicles.ToList(), vehicles.Count(), 1, 100));
    }

    private void SetupPagedCustomers(IEnumerable<CustomerDto>? customers = null)
    {
        customers ??= Array.Empty<CustomerDto>();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(customers.ToList(), customers.Count(), 1, 100));
    }

    [Fact]
    public async Task Index_ReturnsView_WithVehiclesAndCustomers()
    {
        SetupPagedVehicles(new[] { MakeVehicle() });
        SetupPagedCustomers(new[] { MakeCustomer() });
        var controller = CreateController();

        var result = await controller.Index(new VehicleListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<VehiclesPageViewModel>(view.Model);
        Assert.Single(model.Vehicles);
        Assert.Single(model.Customers);
    }

    [Fact]
    public async Task Details_ReturnsPartialView()
    {
        var vehicle = MakeVehicle();
        var customer = MakeCustomer();
        _apiClientMock.Setup(c => c.GetAsync<VehicleDto>(
                It.Is<string>(s => s.Contains(vehicle.Id.ToString())), Ct))
            .ReturnsAsync(vehicle);
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.Is<string>(s => s.Contains("/api/customers")), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto> { customer with { Id = vehicle.CustomerId } }, 1, 1, 200));
        var controller = CreateController();

        var result = await controller.Details(vehicle.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_VehicleDetailsModalContent", pv.ViewName);
        var model = Assert.IsType<VehicleDetailsModalViewModel>(pv.Model);
        Assert.Equal(vehicle.Id, model.Form.Id);
    }

    [Fact]
    public async Task Save_InvalidModel_ReturnsIndex()
    {
        SetupPagedVehicles();
        SetupPagedCustomers();
        var controller = CreateController();
        controller.ModelState.AddModelError("Brand", "Required");

        var form = new VehicleFormViewModel { Brand = "" };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task Save_Success_Redirects()
    {
        var vehicle = MakeVehicle();
        _apiClientMock.Setup(c => c.PostAsync<object, VehicleDto>(
                "/api/vehicles", It.IsAny<object>(), Ct))
            .ReturnsAsync(vehicle);
        var controller = CreateController();

        var form = new VehicleFormViewModel
        {
            CustomerId = Guid.NewGuid(), Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Save(form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/vehicles", redirect.Url);
    }

    [Fact]
    public async Task Save_ApiException_WithErrors_ReturnsIndex()
    {
        SetupPagedVehicles();
        SetupPagedCustomers();
        _apiClientMock.Setup(c => c.PostAsync<object, VehicleDto>(
                "/api/vehicles", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Duplicate plate", 409,
                new Dictionary<string, string[]> { { "LicensePlate", new[] { "Already exists." } } }));
        var controller = CreateController();

        var form = new VehicleFormViewModel
        {
            CustomerId = Guid.NewGuid(), Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task Save_ApiException_NoErrors_AddsGlobalError()
    {
        SetupPagedVehicles();
        SetupPagedCustomers();
        _apiClientMock.Setup(c => c.PostAsync<object, VehicleDto>(
                "/api/vehicles", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Server error", 500));
        var controller = CreateController();

        var form = new VehicleFormViewModel
        {
            CustomerId = Guid.NewGuid(), Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Save(form, Ct);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Update_InvalidModel_ReturnsPartial()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = MakeCustomer();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto> { customer with { Id = customerId } }, 1, 1, 200));
        var controller = CreateController();
        controller.ModelState.AddModelError("Brand", "Required");

        var form = new VehicleFormViewModel { CustomerId = customerId };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_VehicleDetailsModalContent", pv.ViewName);
        Assert.Equal(id, form.Id);
    }

    [Fact]
    public async Task Update_Success_NonHtmx_Redirects()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: false);

        var form = new VehicleFormViewModel
        {
            CustomerId = customerId, Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Update(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/vehicles", redirect.Url);
    }

    [Fact]
    public async Task Update_Success_HtmxRequest_ReturnsEmpty()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: true);

        var form = new VehicleFormViewModel
        {
            CustomerId = customerId, Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Update(id, form, Ct);

        Assert.IsType<EmptyResult>(result);
    }

    [Fact]
    public async Task Update_ApiException_ReturnsPartialWithCustomer()
    {
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customer = MakeCustomer();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var form = new VehicleFormViewModel
        {
            CustomerId = customerId, Brand = "Ford", Model = "Focus",
            Year = 2022, LicensePlate = "ABC1234"
        };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_VehicleDetailsModalContent", pv.ViewName);
    }

    [Fact]
    public async Task Delete_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Delete(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/vehicles", redirect.Url);
    }

    [Fact]
    public async Task Delete_ApiException_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var result = await controller.Delete(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/vehicles", redirect.Url);
    }
}

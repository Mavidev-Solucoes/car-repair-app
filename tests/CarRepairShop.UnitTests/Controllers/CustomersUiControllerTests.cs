using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class CustomersUiControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private CustomersUiController CreateController(string role = "Admin", bool htmxRequest = false)
    {
        var user = UiControllerTestHelper.CreateUser(role);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, htmxRequest: htmxRequest);
        return new CustomersUiController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static CustomerDto MakeCustomer(string name = "Alice") =>
        new(Guid.NewGuid(), name, "12345678901", "alice@example.com", "11987654321", true, DateTime.UtcNow);

    private static VehicleDto MakeVehicle(Guid customerId) =>
        new(Guid.NewGuid(), customerId, "Ford", "Focus", 2020, "ABC1234", "Blue", DateTime.UtcNow);

    private void SetupPagedCustomers(IEnumerable<CustomerDto>? customers = null)
    {
        customers ??= Array.Empty<CustomerDto>();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(customers.ToList(), customers.Count(), 1, 100));
    }

    [Fact]
    public async Task Index_ReturnsView_WithCustomersList()
    {
        SetupPagedCustomers(new[] { MakeCustomer() });
        var controller = CreateController();

        var result = await controller.Index(new CustomerListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CustomersPageViewModel>(view.Model);
        Assert.Single(model.Customers);
    }

    [Fact]
    public async Task Details_ReturnsPartialView_WithModal()
    {
        var customer = MakeCustomer();
        var vehicles = new List<VehicleDto> { MakeVehicle(customer.Id) };

        _apiClientMock.Setup(c => c.GetAsync<CustomerDto>(
                It.Is<string>(s => s.Contains(customer.Id.ToString())), Ct))
            .ReturnsAsync(customer);
        _apiClientMock.Setup(c => c.GetAsync<List<VehicleDto>>(
                It.Is<string>(s => s.Contains("customer")), Ct))
            .ReturnsAsync(vehicles);

        var controller = CreateController();
        var result = await controller.Details(customer.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CustomerDetailsModalContent", pv.ViewName);
        var model = Assert.IsType<CustomerDetailsModalViewModel>(pv.Model);
        Assert.Equal(customer.Id, model.Form.Id);
        Assert.Single(model.Vehicles);
    }

    [Fact]
    public async Task Save_InvalidModel_ReturnsIndex()
    {
        SetupPagedCustomers();
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new CustomerFormViewModel { Name = "" };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.Null(form.Id);
    }

    [Fact]
    public async Task Save_Success_Redirects()
    {
        var customer = MakeCustomer();
        _apiClientMock.Setup(c => c.PostAsync<object, CustomerDto>(
                "/api/customers", It.IsAny<object>(), Ct))
            .ReturnsAsync(customer);
        var controller = CreateController();

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Save(form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/customers", redirect.Url);
    }

    [Fact]
    public async Task Save_ApiException_ReturnsIndexWithErrors()
    {
        SetupPagedCustomers();
        _apiClientMock.Setup(c => c.PostAsync<object, CustomerDto>(
                "/api/customers", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Duplicate", 409,
                new Dictionary<string, string[]> { { "PersonalId", new[] { "Already exists." } } }));
        var controller = CreateController();

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task Save_ApiException_NoErrors_AddsGlobalError()
    {
        SetupPagedCustomers();
        _apiClientMock.Setup(c => c.PostAsync<object, CustomerDto>(
                "/api/customers", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Server error", 500));
        var controller = CreateController();

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Save(form, Ct);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Update_InvalidModel_ReturnsPartialView()
    {
        var id = Guid.NewGuid();
        var vehicles = new List<VehicleDto> { MakeVehicle(id) };
        _apiClientMock.Setup(c => c.GetAsync<List<VehicleDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(vehicles);
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new CustomerFormViewModel { Name = "" };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CustomerDetailsModalContent", pv.ViewName);
        Assert.Equal(id, form.Id);
    }

    [Fact]
    public async Task Update_Success_NonHtmx_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: false);

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Update(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/customers", redirect.Url);
    }

    [Fact]
    public async Task Update_Success_HtmxRequest_ReturnsEmpty()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: true);

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Update(id, form, Ct);

        Assert.IsType<EmptyResult>(result);
    }

    [Fact]
    public async Task Update_ApiException_ReturnsPartialWithVehicles()
    {
        var id = Guid.NewGuid();
        var vehicles = new List<VehicleDto> { MakeVehicle(id) };
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        _apiClientMock.Setup(c => c.GetAsync<List<VehicleDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(vehicles);
        var controller = CreateController();

        var form = new CustomerFormViewModel
        {
            Name = "Alice", PersonalId = "12345678901",
            Email = "alice@example.com", Telephone = "11987654321"
        };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CustomerDetailsModalContent", pv.ViewName);
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
        Assert.Equal("/ui/customers", redirect.Url);
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
        Assert.Equal("/ui/customers", redirect.Url);
    }
}

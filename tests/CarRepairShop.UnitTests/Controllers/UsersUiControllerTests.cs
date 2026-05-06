using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class UsersUiControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private UsersUiController CreateController(bool htmxRequest = false)
    {
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, htmxRequest: htmxRequest);
        return new UsersUiController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static readonly string[] EmailTakenError = ["Email taken."];

    private static UserDto MakeUser(string name = "Bob", UserRole role = UserRole.Mechanic) =>
        new(Guid.NewGuid(), name, "bob@example.com", role, true, DateTime.UtcNow);

    private void SetupPagedUsers(IEnumerable<UserDto>? users = null)
    {
        users ??= Array.Empty<UserDto>();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<UserDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<UserDto>(users.ToList(), users.Count(), 1, 100));
    }

    [Fact]
    public async Task Index_ReturnsView_WithUsers()
    {
        SetupPagedUsers(new[] { MakeUser() });
        var controller = CreateController();

        var result = await controller.Index(new UserListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UsersPageViewModel>(view.Model);
        Assert.Single(model.Users);
    }

    [Fact]
    public async Task Details_ReturnsPartialView()
    {
        var user = MakeUser();
        _apiClientMock.Setup(c => c.GetAsync<UserDto>(
                It.Is<string>(s => s.Contains(user.Id.ToString())), Ct))
            .ReturnsAsync(user);
        var controller = CreateController();

        var result = await controller.Details(user.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_UserDetailsModalContent", pv.ViewName);
        var model = Assert.IsType<UserDetailsModalViewModel>(pv.Model);
        Assert.Equal(user.Id, model.Form.Id);
    }

    [Fact]
    public async Task Save_InvalidModel_ReturnsIndex()
    {
        SetupPagedUsers();
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new UserFormViewModel { Name = "" };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task Save_NewUser_WithoutPassword_AddsModelError()
    {
        SetupPagedUsers();
        var controller = CreateController();

        var form = new UserFormViewModel { Name = "Bob", Email = "bob@example.com", Password = null };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.True(controller.ModelState.ContainsKey("Form.Password"));
    }

    [Fact]
    public async Task Save_Success_Redirects()
    {
        var user = MakeUser();
        _apiClientMock.Setup(c => c.PostAsync<object, UserDto>(
                "/api/users", It.IsAny<object>(), Ct))
            .ReturnsAsync(user);
        var controller = CreateController();

        var form = new UserFormViewModel
        {
            Name = "Bob", Email = "bob@example.com", Password = "password123",
            Role = UserRole.Mechanic
        };
        var result = await controller.Save(form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }

    [Fact]
    public async Task Save_ApiException_ReturnsIndex()
    {
        SetupPagedUsers();
        _apiClientMock.Setup(c => c.PostAsync<object, UserDto>(
                "/api/users", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Email taken", 409,
                new Dictionary<string, string[]> { { "Email", EmailTakenError } }));
        var controller = CreateController();

        var form = new UserFormViewModel
        {
            Name = "Bob", Email = "bob@example.com", Password = "password123",
            Role = UserRole.Mechanic
        };
        var result = await controller.Save(form, Ct);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Save_ApiException_NoErrors_AddsGlobalError()
    {
        SetupPagedUsers();
        _apiClientMock.Setup(c => c.PostAsync<object, UserDto>(
                "/api/users", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Server error", 500));
        var controller = CreateController();

        var form = new UserFormViewModel
        {
            Name = "Bob", Email = "bob@example.com", Password = "password123",
            Role = UserRole.Mechanic
        };
        var result = await controller.Save(form, Ct);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Update_InvalidModel_ReturnsPartial()
    {
        var id = Guid.NewGuid();
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new UserFormViewModel { Name = "" };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_UserDetailsModalContent", pv.ViewName);
        Assert.Equal(id, form.Id);
        Assert.Null(form.Password);
    }

    [Fact]
    public async Task Update_Success_NonHtmx_Redirects()
    {
        var id = Guid.NewGuid();
        var userDto = MakeUser();
        _apiClientMock.Setup(c => c.GetAsync<UserDto>(
                It.Is<string>(s => s.Contains(id.ToString())), Ct))
            .ReturnsAsync(userDto);
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: false);

        var form = new UserFormViewModel
        {
            Name = "Bob", Email = "bob@example.com", Role = UserRole.Mechanic
        };
        var result = await controller.Update(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }

    [Fact]
    public async Task Update_Success_HtmxRequest_ReturnsEmpty()
    {
        var id = Guid.NewGuid();
        var userDto = MakeUser();
        _apiClientMock.Setup(c => c.GetAsync<UserDto>(
                It.Is<string>(s => s.Contains(id.ToString())), Ct))
            .ReturnsAsync(userDto);
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: true);

        var form = new UserFormViewModel
        {
            Name = "Bob", Email = "bob@example.com", Role = UserRole.Mechanic
        };
        var result = await controller.Update(id, form, Ct);

        Assert.IsType<EmptyResult>(result);
    }

    [Fact]
    public async Task Update_ApiException_ReturnsPartialWithUser()
    {
        var id = Guid.NewGuid();
        var userDto = MakeUser();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        _apiClientMock.Setup(c => c.GetAsync<UserDto>(
                It.Is<string>(s => s.Contains(id.ToString())), Ct))
            .ReturnsAsync(userDto);
        var controller = CreateController();

        var form = new UserFormViewModel { Name = "Bob", Email = "bob@example.com", Role = UserRole.Mechanic };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_UserDetailsModalContent", pv.ViewName);
    }

    [Fact]
    public async Task Deactivate_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Deactivate(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }

    [Fact]
    public async Task Deactivate_ApiException_RedirectsWithFlash()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var result = await controller.Deactivate(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }

    [Fact]
    public async Task Activate_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PatchAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Activate(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }

    [Fact]
    public async Task Activate_ApiException_RedirectsWithFlash()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PatchAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var result = await controller.Activate(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/users", redirect.Url);
    }
}

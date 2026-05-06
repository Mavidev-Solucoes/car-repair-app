using CarRepairShop.Application.Common.Behaviors;
using CarRepairShop.Application.Common.Exceptions;
using FluentValidation;
using MediatR;
using Moq;

namespace CarRepairShop.UnitTests.Application.Common;

public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    private class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);
        var nextCalled = false;
        var result = await behavior.Handle(
            new TestRequest("Alice"),
            () => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(
            new TestRequest("Alice"),
            () => { nextCalled = true; return Task.FromResult("ok"); },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var ex = await Assert.ThrowsAsync<CarRepairShop.Application.Common.Exceptions.ValidationException>(() =>
            behavior.Handle(
                new TestRequest(""),
                () => Task.FromResult("ok"),
                CancellationToken.None));

        Assert.True(ex.Errors.ContainsKey("Name"));
    }
}

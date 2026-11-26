using Invoicing.Application.Common.Behaviors;
using Invoicing.Application.Common.Exceptions;
using Invoicing.Domain.Abstractions;

namespace Invoicing.Application.Tests.Common;

public class DomainExceptionTranslationBehaviorTests
{
    private readonly DomainExceptionTranslationBehavior<string, string> _behavior = new();

    [Fact]
    public async Task Domain_exception_is_translated_to_business_rule_violation()
    {
        var domainException = new DomainException("some.code", "Rule broken.");

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            _behavior.Handle("request", () => throw domainException, CancellationToken.None));

        Assert.Equal("some.code", ex.Code);
        Assert.Equal("Rule broken.", ex.Message);
        Assert.Same(domainException, ex.InnerException);
    }

    [Fact]
    public async Task Other_exceptions_pass_through_unchanged()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _behavior.Handle("request", () => throw new NotFoundException("missing"), CancellationToken.None));
    }

    [Fact]
    public async Task Successful_response_is_returned_unchanged()
    {
        var result = await _behavior.Handle("request", () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }
}

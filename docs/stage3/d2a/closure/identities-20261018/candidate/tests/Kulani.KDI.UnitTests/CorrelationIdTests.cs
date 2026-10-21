using Kulani.KDI.Api.Http;

namespace Kulani.KDI.UnitTests;

public sealed class CorrelationIdTests
{
    [Theory]
    [InlineData("req-123_abc.test")]
    [InlineData("a")]
    public void Preserves_bounded_ascii_identifier(string supplied) =>
        Assert.Equal(supplied, CorrelationId.Resolve(supplied));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("one,two")]
    [InlineData("line\r\ninjected: value")]
    [InlineData("á")]
    [InlineData("with space")]
    public void Replaces_untrusted_identifier(string? supplied) =>
        Assert.True(Guid.TryParseExact(CorrelationId.Resolve(supplied), "N", out _));

    [Fact]
    public void Enforces_header_size_boundary()
    {
        Assert.Equal(new string('a', 64), CorrelationId.Resolve(new string('a', 64)));
        Assert.NotEqual(new string('a', 65), CorrelationId.Resolve(new string('a', 65)));
    }
}

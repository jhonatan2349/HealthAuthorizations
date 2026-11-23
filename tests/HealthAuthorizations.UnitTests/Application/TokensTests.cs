using HealthAuthorizations.Application.Autorizaciones.Common;
using HealthAuthorizations.Application.Common.Pagination;

namespace HealthAuthorizations.UnitTests.Application;

public sealed class TokensTests
{
    [Theory]
    [InlineData(1L)]
    [InlineData(987654321L)]
    [InlineData(long.MaxValue)]
    public void Cursor_CodificaYDecodificaElMismoId(long id)
    {
        var cursor = Cursor.Encode(id);

        Cursor.TryDecode(cursor, out var decodificado).ShouldBeTrue();
        decodificado.ShouldBe(id);
        cursor.ShouldNotContain(id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-base64!")]
    [InlineData("YWJj")]   // "abc"
    [InlineData("LTE")]    // "-1"
    [InlineData("MA")]     // "0"
    public void Cursor_Invalido_NoSeDecodifica(string? cursor) =>
        Cursor.TryDecode(cursor, out _).ShouldBeFalse();

    [Fact]
    public void VersionToken_CodificaYDecodifica8Bytes()
    {
        byte[] rowVersion = [0, 0, 0, 0, 0, 0, 7, 209];

        var token = VersionToken.Encode(rowVersion);

        VersionToken.TryDecode(token, out var decodificado).ShouldBeTrue();
        decodificado.ShouldBe(rowVersion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("###")]
    [InlineData("AQID")]   // 3 bytes
    public void VersionToken_Invalido_NoSeDecodifica(string? token) =>
        VersionToken.TryDecode(token, out _).ShouldBeFalse();
}

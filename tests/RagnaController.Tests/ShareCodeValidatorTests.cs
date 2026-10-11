using RagnaController.Core;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// UX-013: ShareCodeValidator — mirrors ProfileShareService.ResolveGistId acceptance.
    /// The UI enables the Download button only for input the service will accept,
    /// so a valid-looking code never leads to a guaranteed-fail download (dead-end).
    /// </summary>
    public class ShareCodeValidatorTests
    {
        [Theory]
        [InlineData("GX-A3F9")]          // canonical share code (uppercase prefix + 4 hex)
        [InlineData("AB-0123")]          // minimal valid code
        [InlineData("ZZ-FFFF")]          // all-F hex suffix
        [InlineData("1234567890abcdef1234567890abcdef")]   // raw Gist ID (32 hex)
        [InlineData("a1b2c3d4e5f607182930415263748596")]   // mixed-case Gist ID
        public void Valid_ShareCodes_And_GistIds_AreAccepted(string code)
            => Assert.True(ShareCodeValidator.IsValid(code), $"expected valid: {code}");

        [Theory]
        [InlineData("gx-a3f9")]          // lowercase prefix — service requires uppercase (ResolveGistId ^[A-Z]{2})
        [InlineData("GX-12345")]        // 5-char suffix — too long for the code form
        [InlineData("G-A3F9")]          // single-letter prefix
        [InlineData("GX")]              // no dash / no suffix
        [InlineData("A3F9")]            // hex without prefix
        [InlineData("1234567890abcdef")]// 16 hex — below the 20-char Gist-ID threshold
        public void Invalid_Inputs_AreRejected(string code)
            => Assert.False(ShareCodeValidator.IsValid(code), $"expected invalid: {code}");

        [Theory]
        [InlineData("")]                // empty
        [InlineData("   ")]             // whitespace only
        [InlineData(null!)]             // null
        public void Empty_Or_Null_IsRejected(string? code)
            => Assert.False(ShareCodeValidator.IsValid(code));

        [Fact]
        public void Surrounding_Whitespace_IsTrimmed()
            => Assert.True(ShareCodeValidator.IsValid("  GX-A3F9  "));
    }
}

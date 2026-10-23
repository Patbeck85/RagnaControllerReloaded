using System;
using Xunit;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class UpdateInfoTests
    {
        [Fact]
        public void IsNewerThan_HigherVersion_ReturnsTrue()
        {
            var update = new UpdateInfo { Version = "2.5.1" };
            Assert.True(update.IsNewerThan("2.5.0"));
        }

        [Fact]
        public void IsNewerSameVersion_ReturnsFalse()
        {
            var update = new UpdateInfo { Version = "2.5.0" };
            Assert.False(update.IsNewerThan("2.5.0"));
        }

        [Fact]
        public void IsNewerThan_LowerVersion_ReturnsFalse()
        {
            var update = new UpdateInfo { Version = "2.4.9" };
            Assert.False(update.IsNewerThan("2.5.0"));
        }

        [Fact]
        public void IsNewerThan_MajorVersionJump_ReturnsTrue()
        {
            var update = new UpdateInfo { Version = "3.0.0" };
            Assert.True(update.IsNewerThan("2.5.0"));
        }

        [Fact]
        public void IsNewerThan_WithVPrefix_HandlesCorrectly()
        {
            var update = new UpdateInfo { Version = "v2.5.1" };
            Assert.True(update.IsNewerThan("v2.5.0"));
        }

        [Fact]
        public void IsNewerThan_EmptyCurrentVersion_ReturnsFalse()
        {
            var update = new UpdateInfo { Version = "2.5.0" };
            Assert.False(update.IsNewerThan(""));
        }

        [Fact]
        public void IsNewerThan_EmptyUpdateVersion_ReturnsFalse()
        {
            var update = new UpdateInfo { Version = "" };
            Assert.False(update.IsNewerThan("2.5.0"));
        }

        [Fact]
        public void UpdateInfo_DefaultValues_AreReasonable()
        {
            var update = new UpdateInfo();
            Assert.Equal(string.Empty, update.Version);
            Assert.Equal(string.Empty, update.Title);
            Assert.Equal(string.Empty, update.ReleaseNotes);
            Assert.Equal(string.Empty, update.DownloadUrl);
            Assert.False(update.IsMandatory);
        }
    }
}

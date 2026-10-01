using System;
using System.IO;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public sealed class ModSettingsTests : IDisposable
    {
        private readonly string dir = Directory.CreateTempSubdirectory("rr-ap-settings").FullName;

        private string SettingsPath => Path.Combine(dir, ModSettings.FileName);

        public void Dispose() => Directory.Delete(dir, recursive: true);

        [Fact]
        public void MissingFile_InterceptOff_NoError()
        {
            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.False(settings.InterceptUpgradePurchases);
            Assert.Null(error);
        }

        [Fact]
        public void InterceptEnabled_InterceptOn()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": true}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.True(settings.InterceptUpgradePurchases);
            Assert.Null(error);
        }

        [Fact]
        public void EmptyObject_InterceptOff()
        {
            File.WriteAllText(SettingsPath, "{}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.False(settings.InterceptUpgradePurchases);
        }

        [Fact]
        public void MalformedJson_InterceptOff_ErrorNamesFile()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": tru");

            Assert.False(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.False(settings.InterceptUpgradePurchases);
            Assert.Contains(ModSettings.FileName, error);
        }

        [Fact]
        public void WrongValueType_InterceptOff_Error()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": \"yes\"}");

            Assert.False(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.False(settings.InterceptUpgradePurchases);
            Assert.NotNull(error);
        }

        [Fact]
        public void UnknownKeys_Ignored()
        {
            File.WriteAllText(SettingsPath, "{\"somethingElse\": 42, \"interceptUpgradePurchases\": true}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.True(settings.InterceptUpgradePurchases);
            Assert.Null(error);
        }

        [Fact]
        public void JsonNull_InterceptOff()
        {
            File.WriteAllText(SettingsPath, "null");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.False(settings.InterceptUpgradePurchases);
        }

        [Fact]
        public void ConnectionFields_Load()
        {
            File.WriteAllText(SettingsPath,
                "{\"interceptUpgradePurchases\": true, \"server\": \"localhost:38281\", \"slot\": \"Player\", \"password\": \"hunter2\"}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.Null(error);
            Assert.Equal("localhost:38281", settings.Server);
            Assert.Equal("Player", settings.Slot);
            Assert.Equal("hunter2", settings.PasswordOrNull);
            Assert.True(settings.HasConnection);
        }

        [Theory]
        [InlineData("{\"interceptUpgradePurchases\": true}")]
        [InlineData("{\"interceptUpgradePurchases\": true, \"server\": \"localhost\"}")]
        [InlineData("{\"interceptUpgradePurchases\": true, \"slot\": \"Player\"}")]
        [InlineData("{\"interceptUpgradePurchases\": true, \"server\": \" \", \"slot\": \"Player\"}")]
        [InlineData("{\"interceptUpgradePurchases\": true, \"server\": \"localhost\", \"slot\": \"\"}")]
        public void MissingServerOrSlot_Offline(string json)
        {
            File.WriteAllText(SettingsPath, json);

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.False(settings.HasConnection);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"password\": \"\"}")]
        [InlineData("{\"password\": null}")]
        public void MissingOrEmptyPassword_IsNull(string json)
        {
            File.WriteAllText(SettingsPath, json);

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.Null(settings.PasswordOrNull);
        }

        [Fact]
        public void LogText_NeverContainsPassword()
        {
            var settings = new ModSettings
            {
                InterceptUpgradePurchases = true,
                Server = "localhost:38281",
                Slot = "Player",
                Password = "s3cr3t-Pa55",
            };

            var text = settings.ToString();

            Assert.DoesNotContain("s3cr3t-Pa55", text);
            Assert.Contains("password set", text);
            Assert.Contains("server localhost:38281", text);
            Assert.Contains("slot Player", text);
        }

        [Fact]
        public void LogText_NoPassword()
        {
            Assert.Contains("password none", new ModSettings().ToString());
        }

        [Fact]
        public void DebugEndlessCompleteThreshold_AbsentIsNull()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": true}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.Null(settings.DebugEndlessCompleteThreshold);
        }

        [Fact]
        public void DebugEndlessCompleteThreshold_Loads()
        {
            File.WriteAllText(SettingsPath, "{\"debugEndlessCompleteThreshold\": 3}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.Null(error);
            Assert.Equal(3, settings.DebugEndlessCompleteThreshold);
        }

        [Fact]
        public void LogText_MentionsDebugThresholdOnlyWhenSet()
        {
            Assert.DoesNotContain("threshold", new ModSettings().ToString());
            Assert.Contains("debug endless-complete threshold 3", new ModSettings { DebugEndlessCompleteThreshold = 3 }.ToString());
        }

        [Fact]
        public void NotificationKeysAbsent_Defaults()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": true}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.Equal(10, settings.NotificationSecondsOrDefault);
            Assert.Equal(5, settings.NotificationLimitOrDefault);
        }

        [Fact]
        public void NotificationValues_Load()
        {
            File.WriteAllText(SettingsPath, "{\"notificationSeconds\": 4, \"notificationLimit\": 3}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.Null(error);
            Assert.Equal(4, settings.NotificationSecondsOrDefault);
            Assert.Equal(3, settings.NotificationLimitOrDefault);
        }

        [Fact]
        public void NotificationZero_KeptAndShownAsOff()
        {
            File.WriteAllText(SettingsPath, "{\"notificationSeconds\": 0, \"notificationLimit\": 0}");

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.Equal(0, settings.NotificationSecondsOrDefault);
            Assert.Equal(0, settings.NotificationLimitOrDefault);
            Assert.Contains("notifications off, limit off", settings.ToString());
        }

        [Theory]
        [InlineData("{\"notificationSeconds\": -1, \"notificationLimit\": -1}")]
        [InlineData("{\"notificationSeconds\": null, \"notificationLimit\": null}")]
        public void NotificationNegativeOrNull_Defaults(string json)
        {
            File.WriteAllText(SettingsPath, json);

            Assert.True(ModSettings.TryLoad(SettingsPath, out var settings, out _));
            Assert.Equal(10, settings.NotificationSecondsOrDefault);
            Assert.Equal(5, settings.NotificationLimitOrDefault);
        }

        [Fact]
        public void NotificationNotANumber_Error_Defaults()
        {
            File.WriteAllText(SettingsPath, "{\"interceptUpgradePurchases\": true, \"notificationSeconds\": \"ten\"}");

            Assert.False(ModSettings.TryLoad(SettingsPath, out var settings, out var error));
            Assert.NotNull(error);
            Assert.False(settings.InterceptUpgradePurchases);
            Assert.Equal(10, settings.NotificationSecondsOrDefault);
            Assert.Equal(5, settings.NotificationLimitOrDefault);
        }

        [Fact]
        public void LogText_ContainsNotificationValues()
        {
            Assert.Contains("notifications 10s, limit 5", new ModSettings().ToString());
            Assert.Contains("notifications 4s, limit 3", new ModSettings { NotificationSeconds = 4, NotificationLimit = 3 }.ToString());
        }
    }
}

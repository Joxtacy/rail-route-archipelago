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
    }
}

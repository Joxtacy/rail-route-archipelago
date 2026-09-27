using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class ItemNamesTests
    {
        private const int ExpectedRows = 44;

        private sealed record Row(string Name, long ItemId, string[] GameIds, string Kind);

        /// <summary>
        /// Rows of the "## Item names" table in apworld/README.md: item name, item ID, the backticked
        /// game Ids (comma-separated, "—" for none) and the kind (upgrade, progressive or filler).
        /// </summary>
        private static List<Row> ReadmeRows()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "apworld", "README.md");
            var rows = new List<Row>();
            var inSection = false;
            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inSection = line.StartsWith("## Item names", StringComparison.Ordinal);
                    continue;
                }
                if (!inSection || !line.StartsWith("| ", StringComparison.Ordinal) || line.StartsWith("| Item name", StringComparison.Ordinal))
                {
                    continue;
                }
                var cells = line.Split('|').Select(c => c.Trim()).ToArray();
                var ids = cells[3] == "—"
                    ? Array.Empty<string>()
                    : cells[3].Split(',').Select(id => id.Trim().Trim('`')).ToArray();
                rows.Add(new Row(cells[1], long.Parse(cells[2]), ids, cells[4]));
            }
            return rows;
        }

        [Fact]
        public void Readme_TableParsesAllRows()
        {
            var rows = ReadmeRows();

            Assert.Equal(ExpectedRows, rows.Count);
            Assert.Equal(40, rows.Count(r => r.Kind == "upgrade"));
            Assert.Equal(3, rows.Count(r => r.Kind == "progressive"));
            Assert.Equal(1, rows.Count(r => r.Kind == "filler"));
        }

        [Fact]
        public void ClientTable_MatchesReadme()
        {
            var readme = ReadmeRows();
            var readmeNames = new HashSet<string>(readme.Select(r => r.Name));
            var problems = new List<string>();
            foreach (var row in readme)
            {
                if (row.Kind == "filler")
                {
                    if (!ItemNames.IsFiller(row.Name))
                    {
                        problems.Add(row.Name + ": filler in README, not filler in ItemNames");
                    }
                    continue;
                }
                if (!ItemNames.TryGet(row.Name, out var clientId))
                {
                    problems.Add(row.Name + ": in README as `" + string.Join(", ", row.GameIds) + "`, missing from ItemNames");
                }
                else if (row.GameIds.Length == 0 || clientId != row.GameIds[0])
                {
                    problems.Add(row.Name + ": README `" + string.Join(", ", row.GameIds) + "`, ItemNames `" + clientId + "`");
                }
            }
            foreach (var entry in ItemNames.All)
            {
                if (!readmeNames.Contains(entry.Key))
                {
                    problems.Add(entry.Key + ": in ItemNames as `" + entry.Value + "`, missing from README");
                }
            }
            foreach (var name in ItemNames.FillerNames)
            {
                if (!readmeNames.Contains(name))
                {
                    problems.Add(name + ": filler in ItemNames, missing from README");
                }
            }

            Assert.True(problems.Count == 0, "Item tables differ:\n" + string.Join("\n", problems));
        }

        [Fact]
        public void Readme_GameIdsAreKnownLocations()
        {
            var unknown = ReadmeRows().SelectMany(r => r.GameIds).Where(id => !LocationNames.TryGet(id, out _)).ToList();

            Assert.True(unknown.Count == 0, "README item Ids without a location: " + string.Join(", ", unknown));
        }

        [Theory]
        [InlineData("Progressive Track Speed", "track_speed1")]
        [InlineData("Progressive Station Count", "station_count1")]
        [InlineData("Progressive Contract Offers", "more_offered_contracts1")]
        public void TryGet_ProgressiveItem_MapsToChainStart(string name, string id)
        {
            Assert.True(ItemNames.TryGet(name, out var clientId));
            Assert.Equal(id, clientId);
        }

        [Fact]
        public void TryGet_CustomContracts()
        {
            Assert.True(ItemNames.TryGet("Custom Contracts", out var id));
            Assert.Equal("custom_contracts", id);
        }

        [Fact]
        public void BinaryItems_NamedLikeTheirLocation()
        {
            var problems = new List<string>();
            foreach (var entry in ItemNames.All.Where(e => !e.Key.StartsWith("Progressive ", StringComparison.Ordinal)))
            {
                if (!LocationNames.TryGet(entry.Value, out var location) || location != entry.Key)
                {
                    problems.Add(entry.Key + " → " + entry.Value + " → location " + (location ?? "(none)"));
                }
            }

            Assert.True(problems.Count == 0, "Items not named like their location:\n" + string.Join("\n", problems));
        }

        [Fact]
        public void Filler_IsFillerAndHasNoUpgrade()
        {
            Assert.True(ItemNames.IsFiller("Green XP Bundle"));
            Assert.False(ItemNames.TryGet("Green XP Bundle", out _));
            Assert.False(ItemNames.IsFiller("Autoblocks"));
        }

        [Theory]
        [InlineData("Not An Item")]
        [InlineData("autoblock")]
        [InlineData(null)]
        public void TryGet_Unknown_ReturnsFalse(string name)
        {
            Assert.False(ItemNames.TryGet(name, out var id));
            Assert.Null(id);
            Assert.False(ItemNames.IsFiller(name));
        }
    }
}

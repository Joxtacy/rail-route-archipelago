using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class LocationNamesTests
    {
        private const int ExpectedRows = 50;

        /// <summary>
        /// Rows of the "## Location names" table in apworld/README.md: lines starting with "| `",
        /// first cell the backticked game Id, second cell the location name.
        /// </summary>
        private static List<(string Id, string Name)> ReadmeRows()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "apworld", "README.md");
            var rows = new List<(string, string)>();
            var inSection = false;
            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inSection = line.StartsWith("## Location names", StringComparison.Ordinal);
                    continue;
                }
                if (!inSection || !line.StartsWith("| `", StringComparison.Ordinal))
                {
                    continue;
                }
                var cells = line.Split('|');
                rows.Add((cells[1].Trim().Trim('`'), cells[2].Trim()));
            }
            return rows;
        }

        [Fact]
        public void Readme_TableParsesAllRows()
        {
            Assert.Equal(ExpectedRows, ReadmeRows().Count);
        }

        [Fact]
        public void ClientTable_MatchesReadme()
        {
            var readme = ReadmeRows();
            var readmeIds = new HashSet<string>(readme.Select(r => r.Id));
            var problems = new List<string>();
            foreach (var (id, name) in readme)
            {
                if (!LocationNames.TryGet(id, out var clientName))
                {
                    problems.Add(id + ": in README as \"" + name + "\", missing from LocationNames");
                }
                else if (clientName != name)
                {
                    problems.Add(id + ": README \"" + name + "\", LocationNames \"" + clientName + "\"");
                }
            }
            foreach (var entry in LocationNames.All)
            {
                if (!readmeIds.Contains(entry.Key))
                {
                    problems.Add(entry.Key + ": in LocationNames as \"" + entry.Value + "\", missing from README");
                }
            }

            Assert.True(problems.Count == 0, "Location tables differ:\n" + string.Join("\n", problems));
        }

        [Fact]
        public void TryGet_LevelledSlot()
        {
            Assert.True(LocationNames.TryGet("track_speed1", out var name));
            Assert.Equal("Basic Tracks", name);
        }

        [Theory]
        [InlineData("custom_contracts")]
        [InlineData("custom_contracts_alt")]
        public void TryGet_CustomContractsVariantsShareOneLocation(string id)
        {
            Assert.True(LocationNames.TryGet(id, out var name));
            Assert.Equal("Custom Contracts", name);
        }

        [Theory]
        [InlineData("not_an_upgrade")]
        [InlineData(null)]
        public void TryGet_Unknown_ReturnsFalse(string id)
        {
            Assert.False(LocationNames.TryGet(id, out var name));
            Assert.Null(name);
        }
    }
}

using System.Collections.Generic;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public sealed class NotificationBudgetTests
    {
        private readonly HashSet<string> gone = new HashSet<string>();
        private readonly NotificationBudget<string> budget;

        public NotificationBudgetTests()
        {
            budget = new NotificationBudget<string>(gone.Contains);
        }

        private List<string> AddAll(bool sticky, int limit, params string[] items)
        {
            var evicted = new List<string>();
            foreach (var item in items)
            {
                evicted.AddRange(budget.Add(item, sticky, limit));
            }
            return evicted;
        }

        [Fact]
        public void UnderLimit_NothingEvicted()
        {
            Assert.Empty(AddAll(sticky: false, limit: 5, "a", "b", "c"));
            Assert.Equal(3, budget.Count);
        }

        [Fact]
        public void SevenRoutine_TwoOldestEvictedInOrder()
        {
            var evicted = AddAll(sticky: false, limit: 5, "r1", "r2", "r3", "r4", "r5", "r6", "r7");

            Assert.Equal(new[] { "r1", "r2" }, evicted);
            Assert.Equal(5, budget.Count);
        }

        [Fact]
        public void StickyThenRoutine_OldestRoutineEvicted_StickyKept()
        {
            var evicted = AddAll(sticky: true, limit: 5, "s1", "s2");
            evicted.AddRange(AddAll(sticky: false, limit: 5, "r1", "r2", "r3", "r4", "r5"));

            Assert.Equal(new[] { "r1", "r2" }, evicted);
            Assert.Equal(5, budget.Count);
        }

        [Fact]
        public void SixSticky_NothingEvicted()
        {
            Assert.Empty(AddAll(sticky: true, limit: 5, "s1", "s2", "s3", "s4", "s5", "s6"));
            Assert.Equal(6, budget.Count);
        }

        [Fact]
        public void FiveStickyThenRoutine_NewRoutineEvicted()
        {
            AddAll(sticky: true, limit: 5, "s1", "s2", "s3", "s4", "s5");

            Assert.Equal(new[] { "r1" }, budget.Add("r1", sticky: false, limit: 5));
            Assert.Equal(5, budget.Count);
        }

        [Fact]
        public void LimitZero_NothingEvicted()
        {
            Assert.Empty(AddAll(sticky: false, limit: 0, "r1", "r2", "r3", "r4", "r5", "r6", "r7"));
            Assert.Equal(7, budget.Count);
        }

        [Fact]
        public void GoneEntries_PrunedAndNotCounted()
        {
            AddAll(sticky: false, limit: 5, "r1", "r2", "r3", "r4", "r5");
            gone.Add("r1");
            gone.Add("r3");

            Assert.Empty(budget.Add("r6", sticky: false, limit: 5));
            Assert.Equal(4, budget.Count);
            Assert.Empty(budget.Add("r7", sticky: false, limit: 5));
            Assert.Equal(new[] { "r2" }, budget.Add("r8", sticky: false, limit: 5));
        }

        [Fact]
        public void Clear_Empties()
        {
            AddAll(sticky: true, limit: 5, "s1", "s2");
            budget.Clear();

            Assert.Equal(0, budget.Count);
        }
    }
}

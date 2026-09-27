using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class ItemSyncTests
    {
        [Fact]
        public void FirstFullList_Replaces()
        {
            var sync = new ItemSync();

            Assert.Equal(ItemSyncAction.Replace, sync.Accept(0, 20));
            Assert.Equal(20, sync.Expected);
        }

        [Fact]
        public void EmptyFullList_Replaces()
        {
            var sync = new ItemSync();

            Assert.Equal(ItemSyncAction.Replace, sync.Accept(0, 0));
            Assert.Equal(0, sync.Expected);
        }

        [Fact]
        public void Increment_Appends()
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);

            Assert.Equal(ItemSyncAction.Append, sync.Accept(3, 1));
            Assert.Equal(4, sync.Expected);
            Assert.Equal(ItemSyncAction.Append, sync.Accept(4, 2));
            Assert.Equal(6, sync.Expected);
        }

        [Fact]
        public void Gap_Resyncs_AndKeepsExpected()
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);

            Assert.Equal(ItemSyncAction.Resync, sync.Accept(5, 1));
            Assert.Equal(3, sync.Expected);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(1)]
        public void DuplicateOrOldIndex_Resyncs(int index)
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);

            Assert.Equal(ItemSyncAction.Resync, sync.Accept(index, 1));
            Assert.Equal(3, sync.Expected);
        }

        [Fact]
        public void FullListAfterSync_ResetsExpected()
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);
            sync.Accept(3, 2);
            Assert.Equal(ItemSyncAction.Resync, sync.Accept(9, 1));

            Assert.Equal(ItemSyncAction.Replace, sync.Accept(0, 4));
            Assert.Equal(4, sync.Expected);
            Assert.Equal(ItemSyncAction.Append, sync.Accept(4, 1));
        }

        [Fact]
        public void FirstFullList_NothingKnown()
        {
            var sync = new ItemSync();

            sync.Accept(0, 1);

            Assert.Equal(0, sync.Known);
        }

        [Fact]
        public void FullListAfterSync_KnowsTheItemsAlreadyApplied()
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);
            sync.Accept(3, 2);
            sync.Accept(9, 1);

            sync.Accept(0, 7);

            Assert.Equal(5, sync.Known);
        }

        [Fact]
        public void ShorterFullList_KnownCappedAtLength()
        {
            var sync = new ItemSync();
            sync.Accept(0, 5);

            sync.Accept(0, 2);

            Assert.Equal(2, sync.Known);
        }

        [Fact]
        public void Reset_StartsOver()
        {
            var sync = new ItemSync();
            sync.Accept(0, 3);

            sync.Reset();

            Assert.Equal(0, sync.Expected);
            Assert.Equal(ItemSyncAction.Resync, sync.Accept(3, 1));
        }
    }
}

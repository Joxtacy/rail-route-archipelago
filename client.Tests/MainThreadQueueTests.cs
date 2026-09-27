using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RailRouteArchipelago.Core;
using Xunit;

namespace RailRouteArchipelago.Tests
{
    public class MainThreadQueueTests
    {
        [Fact]
        public void Drain_RunsInFifoOrder()
        {
            var queue = new MainThreadQueue();
            var epoch = queue.NextEpoch();
            var ran = new List<int>();
            for (var i = 0; i < 5; i++)
            {
                var n = i;
                queue.Enqueue(epoch, () => ran.Add(n));
            }

            Assert.Equal(5, queue.Drain(null));
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, ran);
            Assert.Equal(0, queue.Drain(null));
        }

        [Fact]
        public void Drain_DropsStaleEpochs()
        {
            var queue = new MainThreadQueue();
            var old = queue.NextEpoch();
            var ran = new List<string>();
            queue.Enqueue(old, () => ran.Add("old-1"));
            var current = queue.NextEpoch();
            queue.Enqueue(old, () => ran.Add("old-2"));
            queue.Enqueue(current, () => ran.Add("current"));

            Assert.Equal(1, queue.Drain(null));
            Assert.Equal(new[] { "current" }, ran);
        }

        [Fact]
        public void Drain_EpochBumpedByAnAction_DropsTheRest()
        {
            var queue = new MainThreadQueue();
            var epoch = queue.NextEpoch();
            var ran = new List<string>();
            queue.Enqueue(epoch, () => { ran.Add("disconnect"); queue.NextEpoch(); });
            queue.Enqueue(epoch, () => ran.Add("late item"));

            queue.Drain(null);

            Assert.Equal(new[] { "disconnect" }, ran);
        }

        [Fact]
        public void Drain_ExceptionReported_DrainContinues()
        {
            var queue = new MainThreadQueue();
            var epoch = queue.NextEpoch();
            var ran = new List<int>();
            var errors = new List<Exception>();
            queue.Enqueue(epoch, () => ran.Add(1));
            queue.Enqueue(epoch, () => throw new InvalidOperationException("boom"));
            queue.Enqueue(epoch, () => ran.Add(3));

            Assert.Equal(3, queue.Drain(errors.Add));
            Assert.Equal(new[] { 1, 3 }, ran);
            Assert.Equal("boom", Assert.Single(errors).Message);
        }

        [Fact]
        public void Drain_ActionsQueuedDuringDrain_WaitForNextDrain()
        {
            var queue = new MainThreadQueue();
            var epoch = queue.NextEpoch();
            var ran = new List<string>();
            queue.Enqueue(epoch, () => { ran.Add("first"); queue.Enqueue(epoch, () => ran.Add("second")); });

            Assert.Equal(1, queue.Drain(null));
            Assert.Equal(new[] { "first" }, ran);
            Assert.Equal(1, queue.Drain(null));
            Assert.Equal(new[] { "first", "second" }, ran);
        }

        [Fact]
        public async Task ConcurrentEnqueue_LosesNothing()
        {
            const int threads = 8;
            const int perThread = 5000;
            var queue = new MainThreadQueue();
            var epoch = queue.NextEpoch();
            var ran = new List<(int Thread, int Seq)>();

            await Task.WhenAll(Enumerable.Range(0, threads).Select(t => Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++)
                {
                    var seq = i;
                    queue.Enqueue(epoch, () => ran.Add((t, seq)));
                }
            })));
            queue.Drain(null);

            Assert.Equal(threads * perThread, ran.Count);
            // Each thread's own actions keep their order.
            foreach (var group in ran.GroupBy(r => r.Thread))
            {
                Assert.Equal(Enumerable.Range(0, perThread), group.Select(r => r.Seq));
            }
        }
    }
}

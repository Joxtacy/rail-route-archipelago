using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Hands work from network threads to the game's main thread. Actions are enqueued from any thread,
    /// stamped with the session epoch they belong to, and <see cref="Drain"/> runs them on the caller's
    /// thread in FIFO order. Actions from an older epoch are dropped, so a late event from a closed
    /// connection never touches the next level.
    /// </summary>
    public sealed class MainThreadQueue
    {
        private readonly ConcurrentQueue<(int Epoch, Action Action)> queue = new ConcurrentQueue<(int, Action)>();
        private int epoch;

        public int Epoch => Volatile.Read(ref epoch);

        /// <summary>Starts a new epoch (on connect and on disconnect) and returns it.</summary>
        public int NextEpoch() => Interlocked.Increment(ref epoch);

        public void Enqueue(int actionEpoch, Action action)
        {
            if (action != null)
            {
                queue.Enqueue((actionEpoch, action));
            }
        }

        /// <summary>
        /// Runs the actions queued so far whose epoch is current, in order. Actions queued while draining
        /// wait for the next drain. An exception goes to <paramref name="onError"/> and the drain goes on.
        /// Returns the number of actions run.
        /// </summary>
        public int Drain(Action<Exception> onError)
        {
            var ran = 0;
            for (var pending = queue.Count; pending > 0 && queue.TryDequeue(out var entry); pending--)
            {
                // Read per action, so an action that ends the session also drops the rest of its epoch.
                if (entry.Epoch != Epoch)
                {
                    continue;
                }
                ran++;
                try
                {
                    entry.Action();
                }
                catch (Exception e)
                {
                    onError?.Invoke(e);
                }
            }
            return ran;
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// One connection to the Archipelago server, for one loaded Endless level. Network callbacks only
    /// enqueue onto the pump's <see cref="MainThreadQueue"/>, stamped with this session's epoch; the pump
    /// runs them on the main thread. <see cref="Disconnect"/> starts a new epoch, so anything the old
    /// socket still delivers is dropped.
    /// </summary>
    internal sealed class ApSession
    {
        public const string Game = "Rail Route";

        // The Archipelago version the client reports; the 0.6.7 server accepts it.
        private static readonly Version ProtocolVersion = new Version(0, 6, 7);

        private static bool bindingLogged;

        private readonly MainThreadQueue queue;
        private readonly ServerAddress address;
        private readonly string slot;
        private readonly string password;
        private readonly int epoch;
        private readonly ArchipelagoSession session;
        private bool closed;

        // Network thread only. MultiClient raises the packets of one message in order on one thread.
        private bool previousWasConnected;

        public ApSession(MainThreadQueue queue, ServerAddress address, string slot, string password)
        {
            this.queue = queue;
            this.address = address;
            this.slot = slot;
            this.password = password;
            epoch = queue.NextEpoch();
            session = address.IsUri
                ? ArchipelagoSessionFactory.CreateSession(address.ToUri())
                : ArchipelagoSessionFactory.CreateSession(address.Host, address.Port);
            Items = new ItemRouter(this);
        }

        /// <summary>Logged in, and the socket hasn't closed since.</summary>
        public bool IsConnected { get; private set; }

        /// <summary>The slot was generated for another map, so no checks are sent on this connection.</summary>
        public bool ChecksBlocked { get; private set; }

        /// <summary>The loaded level is bound to this session's seed and slot (it matched or bound at login).</summary>
        public bool Bound { get; private set; }

        /// <summary>Checks may be sent: the map matches and the level is bound to this slot.</summary>
        public bool ChecksAllowed => !ChecksBlocked && Bound;

        /// <summary>The slot data's goal is one the client reports (<see cref="GoalState.IsSupportedGoal"/>).</summary>
        public bool GoalSupported { get; private set; }

        public ArchipelagoSession Session => session;

        public ItemRouter Items { get; }

        public string Description => address + " as " + slot;

        public void Start()
        {
            if (!bindingLogged)
            {
                Log.Info(NewtonsoftBinding.Describe());
                bindingLogged = true;
            }
            session.Socket.PacketReceived += OnPacketReceived;
            session.Socket.ErrorReceived += OnErrorReceived;
            session.Socket.SocketClosed += OnSocketClosed;
            Log.Info("Connecting to " + Description);
            Task.Run(ConnectAndLogin);
        }

        /// <summary>Ends the session without blocking: a new epoch, then the socket closes in the background.</summary>
        public void Disconnect()
        {
            if (closed)
            {
                return;
            }
            closed = true;
            IsConnected = false;
            queue.NextEpoch();
            session.Socket.PacketReceived -= OnPacketReceived;
            session.Socket.ErrorReceived -= OnErrorReceived;
            session.Socket.SocketClosed -= OnSocketClosed;
            Log.Info("Disconnecting from " + Description);
            Task.Run(async () =>
            {
                try
                {
                    await session.Socket.DisconnectAsync();
                }
                catch (Exception)
                {
                    // Closing an already broken socket; nothing is left to report to.
                }
            });
        }

        /// <summary>Queues an action from a network thread for this session's epoch.</summary>
        public void Post(Action action) => queue.Enqueue(epoch, action);

        /// <summary>Sends without blocking. A failure is logged, or handed to <paramref name="onFailed"/> on the main thread.</summary>
        public void SendPacket(ArchipelagoPacketBase packet, string what, Action<string> onFailed = null)
        {
            session.Socket.SendPacketAsync(packet).ContinueWith(t =>
            {
                var message = t.Exception?.GetBaseException().Message;
                Post(() =>
                {
                    if (onFailed != null)
                    {
                        onFailed(message);
                    }
                    else
                    {
                        Log.Error("Sending " + what + " failed: " + message);
                    }
                });
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task ConnectAndLogin()
        {
            LoginResult result;
            try
            {
                await session.ConnectAsync();
                result = await session.LoginAsync(Game, slot, ItemsHandlingFlags.AllItems, ProtocolVersion,
                    tags: new string[0], password: password, requestSlotData: true);
            }
            catch (Exception e)
            {
                var message = e.GetBaseException().Message;
                Post(() => OnLoginFailed(new[] { message }));
                return;
            }
            if (result is LoginSuccessful success)
            {
                Post(() => OnLoginSucceeded(success));
            }
            else
            {
                var errors = (result as LoginFailure)?.Errors ?? new string[0];
                Post(() => OnLoginFailed(errors));
            }
        }

        private void OnLoginSucceeded(LoginSuccessful success)
        {
            IsConnected = true;
            Log.Info("Connected to " + Description + " (team " + success.Team + ", slot " + success.Slot + ")");
            Notify.Side("Archipelago connected");
            ChecksBlocked = SlotDataCheck.Run(success.SlotData);
            ReadGoal(success.SlotData);

            var state = SaveStateStore.Current;
            var loggedIn = new SeedBinding(session.RoomState.Seed, session.ConnectionInfo.Team, session.ConnectionInfo.Slot, slot);
            var decision = BindingDecision.Decide(state, loggedIn, !ChecksBlocked);
            switch (decision.Outcome)
            {
                case BindingOutcome.Refuse:
                    Log.Warn("Save refused: " + decision.Reason + " (this save: " + state.Describe() + "; server: " + loggedIn.Key + ")");
                    Notify.Side("This save belongs to another Archipelago seed – disconnected");
                    Items.Drop();
                    Disconnect();
                    return;
                case BindingOutcome.Bind:
                    state.Binding = loggedIn;
                    state.Goal = GoalState.OnBind(state.Goal, GoalWatcher.EndlessCompleteGranted(Ctx.Deps.ResearchController));
                    Bound = true;
                    Log.Info("Save bound to " + loggedIn.Seed + ", slot " + loggedIn.SlotName + " (" + loggedIn.Slot + ")");
                    break;
                case BindingOutcome.Matches:
                    Bound = true;
                    break;
                case BindingOutcome.Skip:
                    Log.Warn("Save not bound: the level's map doesn't match the seed");
                    break;
            }

            CheckSender.ResendAll(this);
            var loadedSave = Ctx.Deps.GameController.LoadedSave;
            if (decision.Outcome == BindingOutcome.Bind && loadedSave != null)
            {
                // Before the held items raise the index: the loaded file's game state hasn't changed.
                SaveStateStore.WriteFor(loadedSave.FileName);
            }
            Items.Begin(state.ReceivedIndex);
            GoalWatcher.OnLogin(this);
        }

        private void ReadGoal(System.Collections.Generic.Dictionary<string, object> slotData)
        {
            object raw = null;
            slotData?.TryGetValue("goal", out raw);
            var goal = raw == null ? null : Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture);
            GoalSupported = GoalState.IsSupportedGoal(goal);
            if (!GoalSupported)
            {
                Log.Warn("Slot goal " + (goal ?? "(missing)") + " isn't supported; the goal is never sent");
            }
        }

        private void OnLoginFailed(string[] errors)
        {
            Log.Error("Connection to " + Description + " failed" + (errors.Length == 0 ? " (no reason given)" : ":"));
            foreach (var error in errors)
            {
                Log.Error("  " + error);
            }
            Notify.Side("Archipelago connection failed – see Player.log");
            Disconnect();
        }

        private void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            if (packet is ReceivedItemsPacket items)
            {
                // The login reply is [Connected, ReceivedItems(0, all items)] when the slot has items.
                var replay = previousWasConnected && items.Index == 0;
                Post(() => Items.OnReceivedItems(items, replay));
            }
            previousWasConnected = packet is ConnectedPacket;
        }

        private void OnErrorReceived(Exception e, string message)
        {
            Post(() =>
            {
                Log.Error("Archipelago socket error: " + message + (e == null ? "" : " (" + e.GetBaseException().Message + ")"));
                // A server that dies without a close handshake only raises this error, never SocketClosed.
                if (IsConnected && !session.Socket.Connected)
                {
                    OnClosed(message);
                }
            });
        }

        private void OnSocketClosed(string reason) => Post(() => OnClosed(reason));

        private void OnClosed(string reason)
        {
            if (!IsConnected)
            {
                return;
            }
            IsConnected = false;
            Log.Warn("Archipelago disconnected from " + Description + (string.IsNullOrEmpty(reason) ? "" : ": " + reason));
            Notify.Side("Archipelago disconnected");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Context;
using RailRouteArchipelago.Core;
using RailRouteArchipelago.Interception;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// Sends bought slots as location checks. A purchase while connected is sent at once; after each login
    /// every bought slot is sent again in one batch, which covers slots bought offline or restored from a
    /// save. Resending a location the server already has is harmless. Only a level bound to the session's slot
    /// sends checks, and every location handed to the server is recorded in the save's state.
    /// </summary>
    internal static class CheckSender
    {
        private const string BlockedMessage = "Checks not sent: the level's map doesn't match the seed";
        private const string UnboundMessage = "Checks not sent: the save isn't bound to the seed";

        /// <summary>Sends one location if a session is connected; otherwise the next resend covers it.</summary>
        public static void Send(string locationName)
        {
            var session = ArchipelagoPump.Session;
            if (session == null || !session.IsConnected)
            {
                return;
            }
            if (!session.ChecksAllowed)
            {
                Log.Warn((session.ChecksBlocked ? BlockedMessage : UnboundMessage) + " (" + locationName + ")");
                return;
            }
            if (TryResolve(session, locationName, out var id))
            {
                Complete(session, new[] { id }, new[] { locationName }, "Check sent: " + locationName + " (" + id + ")");
            }
        }

        public static void ResendAll(ApSession session)
        {
            if (!session.ChecksAllowed)
            {
                Log.Warn((session.ChecksBlocked ? BlockedMessage : UnboundMessage) + " (resend skipped)");
                return;
            }
            var bought = Ctx.Deps.ResearchController.ResearchItems.Where(i => i.Researched).Select(i => i.Id);
            var selection = CheckSelection.Select(bought, EffectState.GrantedIds);
            foreach (var id in selection.UnknownIds)
            {
                Log.Warn("Resend skipped " + id + ": no location name");
            }
            var ids = new List<long>();
            var names = new List<string>();
            foreach (var name in selection.LocationNames)
            {
                if (TryResolve(session, name, out var id))
                {
                    ids.Add(id);
                    names.Add(name);
                }
            }
            if (ids.Count == 0)
            {
                Log.Info("Resent 0 checks");
                return;
            }
            Complete(session, ids.ToArray(), names, "Resent " + ids.Count + " checks: " + string.Join(", ", names));
        }

        private static bool TryResolve(ApSession session, string locationName, out long id)
        {
            var locations = session.Session.Locations;
            id = locations.GetLocationIdFromName(ApSession.Game, locationName);
            if (id < 0)
            {
                Log.Warn("Check not sent: " + locationName + " is not in the datapackage");
                return false;
            }
            if (!locations.AllLocations.Contains(id))
            {
                Log.Warn("Check not sent: " + locationName + " (" + id + ") is not in the seed");
                return false;
            }
            return true;
        }

        private static void Complete(ApSession session, long[] ids, IEnumerable<string> names, string logLine)
        {
            SaveStateStore.Current.SentChecks.UnionWith(names);
            session.Session.Locations.CompleteLocationChecksAsync(ids).ContinueWith(
                t => session.Post(() => Log.Error("Sending checks " + string.Join(", ", ids) + " failed: "
                    + t.Exception?.GetBaseException().Message + ". The next connection resends them.")),
                TaskContinuationOptions.OnlyOnFaulted);
            Log.Info(logLine);
        }
    }
}

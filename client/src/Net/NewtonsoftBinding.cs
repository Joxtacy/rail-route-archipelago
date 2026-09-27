using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Archipelago.MultiClient.Net;
using Newtonsoft.Json;

namespace RailRouteArchipelago.Net
{
    /// <summary>
    /// Archipelago.MultiClient.Net references an unsigned Newtonsoft.Json 11.0.0.0, and the mod ships no
    /// copy of it. Requests for Newtonsoft.Json are answered with the game's loaded 13.0.0.0 assembly, in
    /// case Mono doesn't bind the reference by simple name on its own. Register before any MultiClient type loads.
    /// </summary>
    internal static class NewtonsoftBinding
    {
        private const string SimpleName = "Newtonsoft.Json";

        private static readonly HashSet<string> redirected = new HashSet<string>();
        private static bool registered;

        public static Assembly GameAssembly => typeof(JsonConvert).Assembly;

        public static void Register()
        {
            if (registered)
            {
                return;
            }
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            registered = true;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);
            if (name.Name != SimpleName)
            {
                return null;
            }
            var target = GameAssembly;
            lock (redirected)
            {
                if (redirected.Add(args.Name))
                {
                    Log.Info("Redirected " + args.Name + " (requested by " + (args.RequestingAssembly?.GetName().Name ?? "unknown")
                        + ") to " + target.FullName);
                }
            }
            return target;
        }

        /// <summary>
        /// "MultiClient 6.7.1.0, Newtonsoft.Json bound to …": loads MultiClient and resolves its Newtonsoft.Json
        /// reference the way the runtime does, without connecting. Kept out of line so the caller's JIT doesn't
        /// load MultiClient types before <see cref="Register"/> has run.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Describe()
        {
            var multiClient = typeof(ArchipelagoSessionFactory).Assembly;
            var reference = multiClient.GetReferencedAssemblies().FirstOrDefault(n => n.Name == SimpleName);
            string bound;
            try
            {
                bound = reference == null ? "(no reference)" : Assembly.Load(reference).FullName;
            }
            catch (Exception e)
            {
                bound = "(failed: " + e.GetType().Name + ": " + e.Message + ")";
            }
            var loaded = AppDomain.CurrentDomain.GetAssemblies().Count(a => a.GetName().Name == SimpleName);
            return "MultiClient " + multiClient.GetName().Version + ", Newtonsoft.Json bound to " + bound
                + " (" + loaded + " Newtonsoft.Json assembly(ies) loaded)";
        }

        /// <summary>The spike: creates a session without connecting, which loads MultiClient's types.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LogSpike()
        {
            try
            {
                ArchipelagoSessionFactory.CreateSession("localhost", 38281);
                Log.Info(Describe());
            }
            catch (Exception e)
            {
                Log.Error("Loading Archipelago.MultiClient.Net failed: " + e);
            }
        }
    }
}

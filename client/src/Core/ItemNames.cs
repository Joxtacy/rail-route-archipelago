using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Archipelago item name → the game upgrade Id it grants. Copied from the "Item names" table in
    /// apworld/README.md (the contract with the APWorld); ItemNamesTests keeps the two in sync.
    /// A progressive item maps to its chain's first Id and Custom Contracts to <c>custom_contracts</c>;
    /// the receiver resolves the chain level and the variant the level shows.
    /// </summary>
    public static class ItemNames
    {
        private static readonly Dictionary<string, string> Ids = new Dictionary<string, string>
        {
            { "Autoblocks", "autoblock" },
            { "Auto-accept Trains", "auto_accept" },
            { "Automatic Routing", "automatic_routing" },
            { "Perpetual Circuit", "perpetual_circuit" },
            { "Auto-reverse Trains", "auto_reverse" },
            { "Manual Signal Route Preview", "manual_signal_route_preview" },
            { "Signalling Safety", "manual_signal_security" },
            { "Platform Adjustments", "platform_management" },
            { "Timetable Adjustments", "contract_management" },
            { "Train Alerts", "train_alerts" },
            { "Relay Sensor", "relay_sensor" },
            { "Custom Contracts", "custom_contracts" },
            { "Maintenance Depot", "maintenance_depot" },
            { "Additional Service Capacity", "additional_service_capacity" },
            { "Field Efficiency", "field_efficiency" },
            { "Expanded Service Capacity", "expanded_service_capacity" },
            { "InterCities", "ic_contracts" },
            { "Custom Contract Period", "custom_contract_period" },
            { "Operating Hours", "contract_windows" },
            { "Departure Sensor", "departure_sensor" },
            { "Arrival Sensor", "platform_sensor" },
            { "Waypoints", "waypoint" },
            { "Routing Sensor", "routing_sensor" },
            { "Structural Contracts Manager", "auto_contract_manager_structural" },
            { "Financial Contracts Manager", "auto_contract_manager_financial" },
            { "Regional Contracts Manager", "auto_contract_manager_regional" },
            { "Faster Switches", "switch_speed" },
            { "Regional Trains", "regional_contracts" },
            { "Freights", "onetime_contracts" },
            { "Shunting Commands", "shunting_commands" },
            { "Shunting Track", "shunting_track" },
            { "Shunting Sensor", "shunting_sensor" },
            { "Stabling Sensor", "stabling_sensor" },
            { "Tunnels", "tunnels" },
            { "Urban Transit Contracts", "urban_contracts" },
            { "Loco Coupling", "loco_coupling" },
            { "Advanced Arrival Sensor", "advanced_arrival_sensor" },
            { "Service Automation", "service_automation" },
            { "Regional Trains Stabling", "coach_yard" },
            { "Advanced Routing Sensor", "advanced_routing_sensor" },
            { "Progressive Track Speed", "track_speed1" },
            { "Progressive Station Count", "station_count1" },
            { "Progressive Contract Offers", "more_offered_contracts1" },
        };

        private static readonly HashSet<string> Filler = new HashSet<string>
        {
            "Green XP Bundle",
        };

        public static IReadOnlyDictionary<string, string> All => Ids;

        public static IReadOnlyCollection<string> FillerNames => Filler;

        /// <summary>Looks up the game upgrade Id an item grants; false for filler, unknown or null names.</summary>
        public static bool TryGet(string name, out string id)
        {
            if (name == null)
            {
                id = null;
                return false;
            }
            return Ids.TryGetValue(name, out id);
        }

        public static bool IsFiller(string name) => name != null && Filler.Contains(name);
    }
}

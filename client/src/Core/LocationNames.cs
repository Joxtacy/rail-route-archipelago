using System.Collections.Generic;

namespace RailRouteArchipelago.Core
{
    /// <summary>
    /// Game upgrade Id → Archipelago location name. Copied from the "Location names" table in
    /// apworld/README.md (the contract with the APWorld); LocationNamesTests keeps the two in sync.
    /// Both Custom Contracts variants share one location because the game never shows both.
    /// </summary>
    public static class LocationNames
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "autoblock", "Autoblocks" },
            { "auto_accept", "Auto-accept Trains" },
            { "automatic_routing", "Automatic Routing" },
            { "perpetual_circuit", "Perpetual Circuit" },
            { "auto_reverse", "Auto-reverse Trains" },
            { "manual_signal_route_preview", "Manual Signal Route Preview" },
            { "manual_signal_security", "Signalling Safety" },
            { "platform_management", "Platform Adjustments" },
            { "contract_management", "Timetable Adjustments" },
            { "station_count1", "More Stations" },
            { "train_alerts", "Train Alerts" },
            { "relay_sensor", "Relay Sensor" },
            { "custom_contracts_alt", "Custom Contracts" },
            { "maintenance_depot", "Maintenance Depot" },
            { "additional_service_capacity", "Additional Service Capacity" },
            { "field_efficiency", "Field Efficiency" },
            { "expanded_service_capacity", "Expanded Service Capacity" },
            { "track_speed1", "Basic Tracks" },
            { "ic_contracts", "InterCities" },
            { "more_offered_contracts1", "More Contract Offers" },
            { "custom_contract_period", "Custom Contract Period" },
            { "contract_windows", "Operating Hours" },
            { "station_count2", "Even More Stations" },
            { "departure_sensor", "Departure Sensor" },
            { "platform_sensor", "Arrival Sensor" },
            { "track_speed2", "Advanced Tracks" },
            { "waypoint", "Waypoints" },
            { "routing_sensor", "Routing Sensor" },
            { "auto_contract_manager_structural", "Structural Contracts Manager" },
            { "auto_contract_manager_financial", "Financial Contracts Manager" },
            { "auto_contract_manager_regional", "Regional Contracts Manager" },
            { "more_offered_contracts2", "Even More Contract Offers" },
            { "more_offered_contracts3", "Way More Contract Offers" },
            { "station_count3", "Unlimited Stations" },
            { "switch_speed", "Faster Switches" },
            { "track_speed3", "Corridor Tracks" },
            { "regional_contracts", "Regional Trains" },
            { "onetime_contracts", "Freights" },
            { "shunting_commands", "Shunting Commands" },
            { "shunting_track", "Shunting Track" },
            { "shunting_sensor", "Shunting Sensor" },
            { "stabling_sensor", "Stabling Sensor" },
            { "tunnels", "Tunnels" },
            { "urban_contracts", "Urban Transit Contracts" },
            { "loco_coupling", "Loco Coupling" },
            { "advanced_arrival_sensor", "Advanced Arrival Sensor" },
            { "service_automation", "Service Automation" },
            { "coach_yard", "Regional Trains Stabling" },
            { "advanced_routing_sensor", "Advanced Routing Sensor" },
            { "custom_contracts", "Custom Contracts" },
        };

        public static IReadOnlyDictionary<string, string> All => Names;

        /// <summary>Looks up the location name for a game upgrade Id; false for unknown or null Ids.</summary>
        public static bool TryGet(string id, out string name)
        {
            if (id == null)
            {
                name = null;
                return false;
            }
            return Names.TryGetValue(id, out name);
        }
    }
}

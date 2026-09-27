from itertools import product

from ..options import Map
from .bases import RailRouteTestBase

# One solo seed per supported option combination, each running WorldTestBase's default tests
# (all state reaches everything and beats the game, empty state reaches something, fill is valid).
for _map, _expect_delays, _happy_passengers in product(Map.name_lookup.values(), (False, True), (False, True)):
    _name = f"TestMatrix_{_map}_ed{int(_expect_delays)}_hp{int(_happy_passengers)}"
    globals()[_name] = type(_name, (RailRouteTestBase,), {
        "options": {"map": _map, "expect_delays": _expect_delays, "happy_passengers": _happy_passengers},
    })

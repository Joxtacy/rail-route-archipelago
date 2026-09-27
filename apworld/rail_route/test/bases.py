from test.bases import WorldTestBase

from .. import RailRouteWorld
from ..data import GAME


class RailRouteTestBase(WorldTestBase):
    game = GAME
    world: RailRouteWorld

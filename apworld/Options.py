from dataclasses import dataclass
from Options import Choice, Range, Toggle, DefaultOnToggle, PerGameCommonOptions

class StartingCharacter(Choice):
    """Which character do you want to start with? Random will pick one for you."""
    display_name = "Starting Character"
    option_manager = 0
    option_biologist = 1
    option_chemist = 2
    option_trader = 3
    option_astronomer = 4
    option_big_eater = 5
    option_random = 6
    default = 6

class MilestoneDifficulty(Choice):
    """
    Scales the score required for milestones.
    Easy: 50% score required.
    Normal: 100% score required.
    Hard: 200% score required.
    """
    display_name = "Milestone Difficulty"
    option_easy = 0
    option_normal = 1
    option_hard = 2
    default = 1

class TrapWeight(Range):
    """How many traps (Doom, Earthquake, Roulette) should be in the pool?"""
    display_name = "Trap Weight"
    range_start = 0
    range_end = 100
    default = 20

class PointsValue(Range):
    """Points granted by each '100 Points' item received."""
    display_name = "Points Item Value"
    range_start = 10
    range_end = 1000
    default = 100

@dataclass
class RaccoinOptions(PerGameCommonOptions):
    starting_character: StartingCharacter
    milestone_difficulty: MilestoneDifficulty
    trap_weight: TrapWeight
    points_value: PointsValue
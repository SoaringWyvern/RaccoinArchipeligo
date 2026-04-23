from worlds.generic.Rules import set_rule

def setup_rules(world):
    # 1. Character Drop Checks
    character_logic = {
        "Manager": "Unlock Manager",
        "Biologist": "Unlock Biologist",
        "Chemist": "Unlock Chemist",
        "Trader": "Unlock Trader",
        "Astronomer": "Unlock Astronomer",
        "Big Eater": "Unlock Big Eater",
    }

    start_char = world.starting_char_name 

    for char_prefix, req_item in character_logic.items():
        for i in range(1, 51):
            loc_name = f"{char_prefix} Check {i}"
            loc = world.get_location(loc_name)
            
            if char_prefix == start_char:
                continue 
            else:
                set_rule(loc, lambda state, item=req_item: state.has(item, world.player))

    # 2. Score Milestone Scaling Logic
    character_items = list(character_logic.values())

    for i in range(1, 29):
        loc_name = f"Score Milestone {i}"
        loc = world.get_location(loc_name)

        # Distribute the 28 milestones across the 6 characters
        if i <= 4:
            req_count = 1
        elif i <= 9:
            req_count = 2
        elif i <= 14:
            req_count = 3
        elif i <= 19:
            req_count = 4
        elif i <= 24:
            req_count = 5
        else:
            req_count = 6

        # The Rule: The player must have 'req_count' number of characters unlocked to logically access this milestone.
        set_rule(loc, lambda state, count=req_count: sum(1 for char in character_items if state.has(char, world.player)) >= count)
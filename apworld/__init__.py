from worlds.AutoWorld import World
from worlds.generic.Rules import set_rule
from BaseClasses import Region, Location, Item, ItemClassification

class RaccoinWorld(World):
    """
    RACCOIN is a relaxing coin pusher arcade game. 
    Drop coins, spin the slot machine, and collect prizes!
    """
    game = "RACCOIN" 
    topology_present = False 

    # ITEM DICTIONARY (What you receive)
    item_name_to_id = {
        "100 Points": 80002,
        "Small Coin Tower": 80003,
        "Medium Coin Tower": 80012, 
        "Large Coin Tower": 80013,   
        "Wheel Spin 3": 80004,
        "Wheel Spin 4": 80014, 
        "Wheel Spin 5": 80015,
        "Doom Coin": 80005,
        "Earthquake": 80006,
        "Coin Restock": 80007,
        "Russian Roulette": 80008,
        "Tube Launchers": 80009,
        "Small Gift Rain": 80010,
        "Medium Gift Rain": 80016,
        "Large Gift Rain": 80017,
        "UFO": 80011,
        "Unlock Manager": 80020,
        "Unlock Biologist": 80021,
        "Unlock Chemist": 80022,
        "Unlock Trader": 80023,
        "Unlock Astronomer": 80024,
        "Unlock Big Eater": 80025,

        **{f"Unlock Coin {i}": 80000 + i for i in range(2001, 2124)},
    }
    
    # LOCATION DICTIONARY (Where you check)
    location_name_to_id = {
        # SCORE MILESTONES (IDs 4000 - 4027) ---
        **{f"Score Milestone {i}": 4000 + i - 1 for i in range(1, 29)},

        # MANAGER CHECKS (IDs 81001 - 81050)
        **{f"Manager Check {i}": 81000 + i for i in range(1, 51)},

        # BIOLOGIST CHECKS (IDs 81101 - 81150)
        **{f"Biologist Check {i}": 81100 + i for i in range(1, 51)},

        # CHEMIST CHECKS (IDs 81201 - 81250)
        **{f"Chemist Check {i}": 81200 + i for i in range(1, 51)},

        # TRADER CHECKS (IDs 81301 - 81350)
        **{f"Trader Check {i}": 81300 + i for i in range(1, 51)},

        # ASTRONOMER CHECKS (IDs 81401 - 81450)
        **{f"Astronomer Check {i}": 81400 + i for i in range(1, 51)},

        # BIG EATER CHECKS (IDs 81501 - 81550)
        **{f"Big Eater Check {i}": 81500 + i for i in range(1, 51)},
    }

    def fill_slot_data(self):
        default_milestones = [
            100000, 250000, 500000, 750000, 1000000, 1500000, 2000000, 2500000, 3000000, 4000000,
            5000000, 6000000, 7500000, 10000000, 15000000, 20000000, 25000000, 30000000, 40000000,
            50000000, 60000000, 75000000, 100000000, 150000000, 200000000, 250000000, 500000000, 1000000000
        ]
        
        slot_data = {}
        for i, score in enumerate(default_milestones):
            slot_data[f"milestone_{i+1}"] = score
            
        slot_data["ap_points_value"] = 100
        slot_data["ap_small_tower_coins"] = 100
        slot_data["ap_medium_tower_coins"] = 250
        slot_data["ap_large_tower_coins"] = 500
        
        slot_data["ap_wheel_spin_small"] = 3
        slot_data["ap_wheel_spin_medium"] = 4
        slot_data["ap_wheel_spin_large"] = 5
        
        slot_data["ap_quake_shakes"] = 7
        slot_data["ap_restock_coins"] = 40
        
        slot_data["ap_gift_rain_coins_small"] = 30
        slot_data["ap_gift_rain_coins_medium"] = 40
        slot_data["ap_gift_rain_coins_large"] = 50
        
        slot_data["ap_tube_coins"] = 20
            
        return slot_data

    def create_regions(self):
        menu_region = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu_region)

        for loc_name, loc_id in self.location_name_to_id.items():
            location = Location(self.player, loc_name, loc_id, menu_region)
            menu_region.locations.append(location)

    def create_items(self):
        # 1. Define the characters
        character_items = [
            "Unlock Manager", "Unlock Biologist", "Unlock Chemist", 
            "Unlock Trader", "Unlock Astronomer", "Unlock Big Eater"
        ]
        
        # 2. Pick ONE random character to be the starter
        starting_character = self.multiworld.random.choice(character_items)
        
        # 3. Give the starter to the player immediately!
        self.multiworld.push_precollected(self.create_item(starting_character))

        # 4. Add the remaining 5 characters to the pool
        for char in character_items:
            if char != starting_character:
                self.multiworld.itempool.append(self.create_item(char))

        # 5. Add the 123 Coins to the pool
        for i in range(2001, 2124):
            self.multiworld.itempool.append(self.create_item(f"Unlock Coin {i}"))

        # 6. Fill the rest of the pool (Requires exactly 200 filler/traps to reach 328 total)
        pool_distribution = {
            "100 Points": 30,
            "Small Coin Tower": 20,
            "Medium Coin Tower": 10,
            "Large Coin Tower": 5,
            "Wheel Spin 3": 20,
            "Wheel Spin 4": 10,
            "Wheel Spin 5": 5,
            "Small Gift Rain": 20,
            "Medium Gift Rain": 10,
            "Large Gift Rain": 5,
            "Coin Restock": 10,
            "Tube Launchers": 10,
            "UFO": 10,
            "Earthquake": 10,
            "Doom Coin": 10,
            "Russian Roulette": 15,
        }

        for item_name, count in pool_distribution.items():
            for _ in range(count):
                item = self.create_item(item_name)
                self.multiworld.itempool.append(item)

    def create_item(self, name: str) -> Item:
        item_id = self.item_name_to_id[name]
        
        if name.startswith("Unlock Manager") or name.startswith("Unlock Biologist") or \
           name.startswith("Unlock Chemist") or name.startswith("Unlock Trader") or \
           name.startswith("Unlock Astronomer") or name.startswith("Unlock Big Eater"):
            classification = ItemClassification.progression
        elif name in ["Doom Coin", "Earthquake", "Russian Roulette"]:
            classification = ItemClassification.trap
        elif name in ["100 Points"]:
            classification = ItemClassification.filler
        else:
            classification = ItemClassification.useful # Covers all the Coins!
            
        return Item(name, classification, item_id, self.player)

    def set_rules(self):
        # ---------------------------------------------------------
        # LOCATION LOCKS
        # AP will safely ignore the rule for whichever character 
        # it gave the player as a starter during create_items()
        # ---------------------------------------------------------
        
        # Lock Manager Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Manager Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Manager", self.player))

        # Lock Biologist Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Biologist Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Biologist", self.player))

        # Lock Chemist Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Chemist Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Chemist", self.player))

        # Lock Trader Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Trader Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Trader", self.player))

        # Lock Astronomer Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Astronomer Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Astronomer", self.player))

        # Lock Big Eater Checks
        for i in range(1, 51):
            loc = self.multiworld.get_location(f"Big Eater Check {i}", self.player)
            set_rule(loc, lambda state: state.has("Unlock Big Eater", self.player))
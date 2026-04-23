from worlds.AutoWorld import World
from BaseClasses import Region, Location, Item, ItemClassification
from .Items import item_table, item_name_to_id, get_item_classification, CHARACTER_POOLS
from .Locations import location_name_to_id
from .Rules import setup_rules
from .Options import RaccoinOptions

class RaccoinWorld(World):
    game = "raccoin"
    topology_present = False
    

    options_dataclass = RaccoinOptions 

    item_name_to_id = item_name_to_id
    location_name_to_id = location_name_to_id

    def generate_early(self):
        # 1. Handle Random Character Selection
        if self.options.starting_character == 6:  # 6 is 'Random'
            # Pick a random index between 0 (Manager) and 5 (Big Eater)
            new_char = self.multiworld.random.randint(0, 5)
            self.options.starting_character.value = new_char
            
        # 2. Store the string name for easy access in other functions
        char_list = ["Manager", "Biologist", "Chemist", "Trader", "Astronomer", "Big Eater"]
        self.starting_char_name = char_list[self.options.starting_character.value]

    def create_regions(self):
        menu_region = Region("Menu", self.player, self.multiworld)
        self.multiworld.regions.append(menu_region)
        for loc_name, loc_id in self.location_name_to_id.items():
            menu_region.locations.append(Location(self.player, loc_name, loc_id, menu_region))

    def create_items(self):
        start_char = self.starting_char_name

        # 1. Generate the core coins and characters
        for name, data in item_table.items():
            if name == f"Unlock {start_char}":
                starting_item = self.create_item(name, ItemClassification.progression)
                self.multiworld.push_precollected(starting_item)
                continue
            
            # Skip the filler items for now; we add them dynamically below
            if data.group in ["Event", "Trap", "Filler"]:
                continue
                
            classification = get_item_classification(name, start_char)
            item = self.create_item(name, classification)
            self.multiworld.itempool.append(item)

        # 2. Calculate how many empty locations we have left to fill
        total_locations = len(self.multiworld.get_locations(self.player))
        current_items = len(self.multiworld.itempool)
        deficit = total_locations - current_items

        # 3. Define our filler pools
        traps = ["Doom", "Earthquake", "Russian Roulette"]
        events = ["Points", "Small Tower", "Medium Tower", "Large Tower", 
                  "Wheel Spin Small", "Wheel Spin Medium", "Wheel Spin Large",
                  "Restock", "Tube Launchers", "Small Rain", "Medium Rain", "Large Rain", "UFO"]

        trap_chance = self.options.trap_weight.value

        # 4. Fill the remaining chests
        for _ in range(deficit):
            if self.multiworld.random.randint(1, 100) <= trap_chance:
                filler_name = self.multiworld.random.choice(traps)
            else:
                filler_name = self.multiworld.random.choice(events)
                
            item = self.create_item(filler_name, ItemClassification.filler)
            self.multiworld.itempool.append(item)

    def create_item(self, name: str, classification: ItemClassification = None) -> Item:
        """Helper to create an item with a specific classification."""
        data = item_table[name]
        # Use the dynamic classification passed from create_items, 
        # or default to Useful if called elsewhere.
        cls = classification if classification is not None else ItemClassification.useful
        return Item(name, cls, data.code, self.player)

    def fill_slot_data(self):
        # 1. Handle Milestone Scaling based on YAML Difficulty
        difficulty = self.options.milestone_difficulty
        # Easy = 0.5x, Normal = 1.0x, Hard = 2.0x
        multiplier = 0.5 if difficulty == 0 else (2.0 if difficulty == 2 else 1.0)
        
        default_milestones = [
            100000, 250000, 500000, 750000, 1000000, 1500000, 2000000, 2500000, 3000000, 4000000,
            5000000, 6000000, 7500000, 10000000, 15000000, 20000000, 25000000, 30000000, 40000000,
            50000000, 60000000, 75000000, 100000000, 150000000, 200000000, 250000000, 500000000, 1000000000
        ]
        
        slot_data = {}
        for i, score in enumerate(default_milestones):
            # Scale the goals and send them to the C# mod
            slot_data[f"milestone_{i+1}"] = int(score * multiplier)
            
        # 2. Send other YAML-defined values to the game
        slot_data["ap_points_value"] = self.options.points_value.value
        
        # 3. Static helper values for game events
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

    def set_rules(self):
        setup_rules(self)
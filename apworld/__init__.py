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
        pool = []
        starter_items = set()

        # 1. Handle Starter Pool Logic
        if self.options.starter_pool:
            core_pool = []
            rarity_pools = {"Common": [], "Uncommon": [], "Rare": [], "Epic": []}
            
            # Grab the specific set of coins our starting character is allowed to use
            usable_coins = CHARACTER_POOLS.get(start_char, set())
            
            for name, data in item_table.items():
                if data.group not in ["Character", "Event", "Trap", "Filler"]:
                    
                    if name in usable_coins or data.group == "Core":
                        
                        if data.group == "Core":
                            core_pool.append(name)
                        if hasattr(data, 'rarity') and data.rarity in rarity_pools:
                            rarity_pools[data.rarity].append(name)

            # Pick Core Items first
            self.multiworld.random.shuffle(core_pool)
            for i in range(min(self.options.core_starters.value, len(core_pool))):
                starter_items.add(core_pool[i])

            # Pick Rarity Items (excluding ones already picked by Core)
            counts = {
                "Common": self.options.common_starters.value,
                "Uncommon": self.options.uncommon_starters.value,
                "Rare": self.options.rare_starters.value,
                "Epic": self.options.epic_starters.value
            }

            for rarity, count in counts.items():
                available = [n for n in rarity_pools[rarity] if n not in starter_items]
                self.multiworld.random.shuffle(available)
                for i in range(min(count, len(available))):
                    starter_items.add(available[i])

        # 2. Generate and Push Items
        for name, data in item_table.items():
            
            # A: Precollect the starting character AND any chosen starter coins
            if name == f"Unlock {start_char}" or name in starter_items:
                # Unlocks are progression, starter coins are useful
                cls = ItemClassification.progression if "Unlock" in name else ItemClassification.useful
                item = self.create_item(name, cls)
                self.multiworld.push_precollected(item)
                continue
            
            # B: Skip filler pools (we will calculate these at the end)
            if data.group in ["Event", "Trap", "Filler"]:
                continue
                
            # C: If it's a character unlock that ISN'T our starter, we MUST add it to the pool!
            if data.group == "Character":
                if name != f"Unlock {start_char}":
                    pool.append(self.create_item(name, ItemClassification.progression))
                continue

            # D: Everything else (the remaining coins)
            classification = get_item_classification(name, start_char)
            pool.append(self.create_item(name, classification))

        # 3. Calculate deficit based on actual locations vs pool size
        total_locations = len(self.multiworld.get_locations(self.player))
        deficit = total_locations - len(pool)

        # 4. Filler pools
        traps = ["Doom", "Earthquake", "Russian Roulette"]
        events = ["Points", "Small Tower", "Medium Tower", "Large Tower", 
                  "Wheel Spin Small", "Wheel Spin Medium", "Wheel Spin Large",
                  "Restock", "Tube Launchers", "Small Rain", "Medium Rain", "Large Rain", "UFO"]

        trap_chance = self.options.trap_weight.value

        # 5. Fill the deficit
        for _ in range(deficit):
            if self.multiworld.random.randint(1, 100) <= trap_chance:
                filler_name = self.multiworld.random.choice(traps)
            else:
                filler_name = self.multiworld.random.choice(events)
            
            pool.append(self.create_item(filler_name, ItemClassification.filler))

        # 6. Push the entire completed pool to the multiworld
        self.multiworld.itempool += pool

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
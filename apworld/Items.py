from typing import Dict, Set, NamedTuple, List
from BaseClasses import ItemClassification


class RaccoinItemData(NamedTuple):
    code: int
    group: str

BASE_ID = 80000

# CHARACTER POOLS
# These are used to calculate dynamic rarity (Exclusivity Score)
CHARACTER_POOLS = {
    "Manager": {
        "Chummy Coin", "Square Coin", "Ally Coin", "Equal Coin", "Sumoin", 
        "Workoin", "Factorial Coin", "Percentoin", "Glue Coin", "Bunny Coin", 
        "+1 Coin", "Marsoin", "Chococoin", "Radiation Coin", "Lightning Coin", 
        "Frozen Coin", "Earthquakoin", "Giraffe Coin", "Roulette Coin", 
        "Rocketoin", "Wormhole Coin", "Bomboin", "Tickoin", "Relicoin",
        "Warp Coin", "Dizzy Coin", "Windy Coin", "Glowcoin", "Heavy Coin",
        "Ghostcoin", "Magcoin", "Frostcoin", "Flamecoin", "Mirror Coin",
        "Homing Coin", "Shrinkcoin", "Growcoin", "Shield Coin", "Drill Coin",
        "Stealth Coin", "Spike Coin", "Blast Coin", "Telecoin", "Wormhole Coin",
        "Creditoin", "Bullish Coin", "Antigrav Coin", "Slow Coin", "Fast Coin",
        "Bounce Coin", "Slide Coin", "Jump Coin", "Sticky Coin", "Glass Coin",
        "Lead Coin", "Iron Coin", "Platinum Coin", "Emeraldoin", "Rubyoin",
        "Sapphiroin", "Topazoin", "Diamondoin", "Rainbow Coin", "Prism Coin", "Void Coin"
    },
    "Biologist": {
        "Seedoin", "Wateroin", "Hen Coin", "Wolfoin", "Monkey Coin", 
        "Corncoin", "Stomachoin", "Tigeroin", "Catoin", "Pigeoin", 
        "Frogoin", "Lotusoin", "Foxoin", "Rooster Coin", "Chomp Coin", 
        "Budoin", "Glue Coin", "Lightning Coin", "Bomboin", "Tickoin",
        "Bunny Coin", "Relicoin", "Chococoin", "Radiation Coin", "Boost Coin",
        "Atomicoin", "Multicoin", "JawBreakoin", "Magnetoin", "Sensoroin",
        "Star Coin", "Giraffe Coin", "Roulette Coin", "Lightning Coin",
        "Bubble Gum Coin", "Whirlwind Coin", "Saw Coin", "Wish Pool Coin",
        "Division Coin", "Wormhole Coin", "Creditoin", "Bullish Coin",
        "Rocketoin", "Frozen Coin", "Speakeroin", "Greater Coin", "Cloveroin",
        "Killer Coin", "Earthquakoin", "Richoin", "+1 Coin", "Red Packet Coin",
        "Dogoin", "Bait Coin", "Marsoin", "Cheateroin", "Collapsoin", "Wormhole Coin"
    },
    "Chemist": {
        "Wateroin", "Battery Coin", "Fireball Coin", "Bubble Coin", 
        "TNT Coin", "Snowman Coin", "Palette Coin", "Pinwheel Coin", 
        "Raw Ore Coin", "Glue Coin", "Bomboin", "Tickoin", "Earthquakoin",
        "Frozen Coin", "Fishoin", "Ratoin", "JawBreakoin", "Magnetoin",
        "Sensoroin", "Star Coin", "Bubble Gum Coin", "Saw Coin", "Richoin",
        "Wish Pool Coin", "Division Coin", "Dogoin", "Cheateroin", "Collapsoin",
        "Wormhole Coin", "Wormhole Coin", "Creditoin", "Marsoin", "Lightning Coin"

    },
    "Trader": {
        "Primal Coin", "Jetoin", "Luckcoin", "Drumoin", "Hypnoticoin", 
        "Magicoin", "Blind Boxoin", "Dice Coin", "Slime Coin", "Emoin", 
        "Minion Coin", "Souloin", "Comboin", "Jokeroin", "Glue Coin",
        "Bomboin", "Tickoin", "Bunny Coin", "Relicoin", "Chococoin",
        "Radiation Coin", "Boost Coin", "Atomicoin", "Multicoin", "Star Coin",
        "Bubble Gum Coin", "Saw Coin", "Wish Pool Coin", "Division Coin",
        "Wormhole Coin", "Creditoin", "Bullish Coin", "Rocketoin", "Frozen Coin",
        "Speakeroin", "Greater Coin", "Cloveroin", "Killer Coin", "Richoin",
        "+1 Coin", "Red Packet Coin", "Dogoin", "Bait Coin", "Marsoin",
        "Cheateroin", "Collapsoin", "Wormhole Coin"
    },
    "Astronomer": {
        "Moon Coin", "Sun Coin", "Coinmet", "Meteoroin", "Aeroliteoin", 
        "Mercury Coin", "Venusoin", "Earthoin", "Jupiteroin", "Saturoin", 
        "Uranusoin", "Neptuoin", "Glue Coin", "Bomboin", "Tickoin", 
        "Bunny Coin", "Relicoin", "Chococoin", "Radiation Coin", "Star Coin",
        "Bubble Gum Coin", "Saw Coin", "Wish Pool Coin", "Division Coin",
        "Wormhole Coin", "Creditoin", "Bullish Coin", "Rocketoin", "Frozen Coin",
        "Speakeroin", "Greater Coin", "Cloveroin", "Killer Coin", "Richoin",
        "+1 Coin", "Red Packet Coin", "Dogoin", "Bait Coin", "Marsoin",
        "Cheateroin", "Collapsoin", "Wormhole Coin"
    },
    "Big Eater": {
        "Eggoin", "Mushroin", "Riceoin", "Doughoin", "Greenoin", "Fridgeoin",
        "Ratoin", "Fishoin", "Glue Coin", "Bomboin", "Tickoin", "Relicoin",
        "Chococoin", "Radiation Coin", "Boost Coin", "Atomicoin", "Multicoin",
        "JawBreakoin", "Magnetoin", "Sensoroin", "Star Coin", "Bubble Gum Coin",
        "Saw Coin", "Wish Pool Coin", "Division Coin", "Wormhole Coin",
        "Creditoin", "Bullish Coin", "Rocketoin", "Frozen Coin", "Speakeroin",
        "Greater Coin", "Cloveroin", "Killer Coin", "Richoin", "+1 Coin",
        "Red Packet Coin", "Dogoin", "Bait Coin", "Marsoin", "Cheateroin",
        "Collapsoin", "Wormhole Coin"
    }
}

def get_item_classification(name: str, start_char: str) -> ItemClassification:
    if "Unlock" in name:
        return ItemClassification.progression

    # Count how many characters have access to this coin
    total_character_count = sum(1 for pool in CHARACTER_POOLS.values() if name in pool)
    is_in_my_pool = name in CHARACTER_POOLS.get(start_char, set())

    # Progression: Exclusive to my current character (Rare/Specialist)
    if is_in_my_pool and total_character_count <= 2:
        return ItemClassification.progression
    
    # Useful: Specialist for others or high-utility overlaps
    if total_character_count <= 2:
        return ItemClassification.useful
        
    # Filler: Core coins shared by 3 or more characters
    return ItemClassification.filler

# ITEM GENERATION

def get_item_table() -> Dict[str, RaccoinItemData]:
    """
    Generates the master item_table with verified IDs from the collection dump.
    Groups are categorized by their 'Exclusivity' or 'Type'.
    """
    # Mapping: Name -> (GameID, Group)
    raw_data = {
        # 1000 Series (Base Metals)
        "Copper Coin": (1001, "Metal"),
        "Silver Coin": (1002, "Metal"),
        "Gold Coin": (1003, "Metal"),

        # 2000 Series (Standard & Specialty Coins)
        "Glue Coin": (2001, "Core"),
        "Bomboin": (2002, "Core"),
        "Tickoin": (2003, "Core"),
        "Chummy Coin": (2004, "Manager"),
        "Seedoin": (2005, "Biologist"),
        "Wateroin": (2006, "Shared"),
        "Bunny Coin": (2007, "Core"),
        "Relicoin": (2008, "Core"),
        "Eggoin": (2009, "Big Eater"),
        "Cooinkie": (2010, "Common"),
        "Chococoin": (2011, "Core"),
        "Radiation Coin": (2012, "Core"),
        "Hen Coin": (2013, "Biologist"),
        "Boost Coin": (2014, "Core"),
        "Atomicoin": (2015, "Core"),
        "Multicoin": (2016, "Core"),
        "Square Coin": (2017, "Manager"),
        "JawBreakoin": (2018, "Core"),
        "Fried Chickoin": (2019, "Common"),
        "Magnetoin": (2020, "Core"),
        "Sensoroin": (2021, "Core"),
        "Wolfoin": (2022, "Biologist"),
        "Star Coin": (2023, "Core"),
        "Giraffe Coin": (2024, "Core"),
        "Bananoin": (2025, "Common"),
        "Monkey Coin": (2026, "Biologist"),
        "Moon Coin": (2027, "Astronomer"),
        "Sun Coin": (2028, "Astronomer"),
        "Primal Coin": (2029, "Trader"),
        "Jetoin": (2030, "Trader"),
        "Roulette Coin": (2031, "Core"),
        "Lightning Coin": (2032, "Core"),
        "Battery Coin": (2033, "Chemist"),
        "Ally Coin": (2034, "Manager"),
        "Corncoin": (2035, "Biologist"),
        "Popcorn": (2036, "Common"),
        "Fireball Coin": (2037, "Chemist"),
        "Bubble Coin": (2038, "Chemist"),
        "Bubble Gum Coin": (2039, "Core"),
        "Equal Coin": (2040, "Manager"),
        "Stomachoin": (2041, "Biologist"),
        "Ratoin": (2042, "Shared"),
        "Tigeroin": (2043, "Biologist"),
        "Mushroin": (2044, "Big Eater"),
        "Whirlwind Coin": (2045, "Core"),
        "Poocoin": (2046, "Common"),
        "Saw Coin": (2047, "Core"),
        "Wish Pool Coin": (2048, "Core"),
        "Division Coin": (2049, "Core"),
        "Wormhole Coin": (2050, "Core"),
        "Creditoin": (2051, "Core"),
        "Fishoin": (2052, "Shared"),
        "Catoin": (2053, "Biologist"),
        "Pigeoin": (2054, "Biologist"),
        "Bullish Coin": (2055, "Core"),
        "Rocketoin": (2056, "Core"),
        "Luckcoin": (2057, "Trader"),
        "Frogoin": (2058, "Biologist"),
        "Lotusoin": (2059, "Biologist"),
        "Drumoin": (2060, "Trader"),
        "Frozen Coin": (2061, "Core"),
        "Sumoin": (2062, "Manager"),
        "Speakeroin": (2063, "Core"),
        "1/2 Coin": (2064, "Manager"),
        "Greater Coin": (2065, "Core"),
        "Foxoin": (2066, "Biologist"),
        "Cloveroin": (2067, "Core"),
        "Workoin": (2068, "Manager"),
        "Killer Coin": (2069, "Core"),
        "TNT Coin": (2070, "Chemist"),
        "Earthquakoin": (2071, "Core"),
        "Snowman Coin": (2072, "Chemist"),
        "Palette Coin": (2073, "Chemist"),
        "Hypnoticoin": (2074, "Trader"),
        "Magicoin": (2075, "Trader"),
        "Blind Boxoin": (2076, "Trader"),
        "Dice Coin": (2077, "Trader"),
        "Richoin": (2078, "Core"),
        "+1 Coin": (2079, "Core"),
        "Slime Coin": (2080, "Trader"),
        "Emoin": (2081, "Trader"),
        "Minion Coin": (2082, "Trader"),
        "Red Packet Coin": (2083, "Core"),
        "Factorial Coin": (2084, "Manager"),
        "Percentoin": (2085, "Manager"),
        "Rooster Coin": (2086, "Biologist"),
        "Dogoin": (2087, "Core"),
        "Chomp Coin": (2088, "Biologist"),
        "Bean Coin": (2089, "Common"),
        "Pinwheel Coin": (2090, "Chemist"),
        "Bait Coin": (2091, "Core"),
        "Souloin": (2092, "Trader"),
        "Raw Ore Coin": (2093, "Chemist"),
        "Sandoin": (2094, "Common"),
        "Quartzoin": (2095, "Common"),
        "Amethystoin": (2096, "Common"),
        "Diamondoin": (2097, "Common"),
        "Comboin": (2098, "Trader"),
        "Coinrona": (2099, "Common"),
        "Coinmet": (2100, "Astronomer"),
        "Meteoroin": (2101, "Astronomer"),
        "Aeroliteoin": (2102, "Astronomer"),
        "Mercury Coin": (2103, "Astronomer"),
        "Venusoin": (2104, "Astronomer"),
        "Earthoin": (2105, "Astronomer"),
        "Marsoin": (2106, "Core"),
        "Jupiteroin": (2107, "Astronomer"),
        "Saturoin": (2108, "Astronomer"),
        "Uranusoin": (2109, "Astronomer"),
        "Neptuoin": (2110, "Astronomer"),
        "Dr. Balloin": (2111, "Common"),
        "Coin Alien": (2112, "Common"),
        "Cheateroin": (2113, "Core"),
        "Riceoin": (2114, "Big Eater"),
        "Doughoin": (2115, "Big Eater"),
        "Greenoin": (2116, "Big Eater"),
        "Fridgeoin": (2117, "Big Eater"),
        "Burnt Foodoin": (2118, "Common"),
        "Clayoin": (2119, "Common"),
        "Colored Glazeoin": (2120, "Common"),
        "Budoin": (2121, "Biologist"),
        "Collapsoin": (2122, "Core"),
        "Jokeroin": (2123, "Trader"),

        # 3000 Series (Rotten/Tired Variants)
        "Tired Bunny Coin": (3007, "Variant"),
        "Rotten Chococoin": (3011, "Variant"),
        "Rotten Bananoin": (3025, "Variant"),
        "Rotten Corncoin": (3035, "Variant"),
        "Salted Fishoin": (3052, "Variant"),

        # 5000 Series (Food/Dish Coins)
        "Rice Balloin": (5001, "Food"),
        "Sushioin": (5002, "Food"),
        "Omuriceoin": (5003, "Food"),
        "Mushroin Rice": (5004, "Food"),
        "Rice Pudding": (5005, "Food"),
        "Seven-Herb Porridge": (5006, "Food"),
        "Oyakodoin": (5051, "Food"),
        "Beggar's Chicken Riceoin": (5052, "Food"),
        "Veggie Burgeroin": (5101, "Food"),
        "Chikuwaoin": (5102, "Food"),
        "Egg Puffsoin": (5103, "Food"),
        "Mushroin Pizza": (5104, "Food"),
        "Swiss Rolloin": (5105, "Food"),
        "Clover Fritteroin": (5106, "Food"),
        "Fish Burgeroin": (5151, "Food"),
        "Sour Fish Soupoin": (5202, "Food"),
        "Tanghuluoin": (5205, "Food"),
        "Saladoin": (5206, "Food"),
        "Beggar's Chickeoin": (5207, "Food"),
    }
    
    table = {}
    for name, (game_id, group) in raw_data.items():
        # AP ID calculation logic
        if 1000 <= game_id < 2000:
            ap_id = 81000 + (game_id % 1000)
        elif 2000 <= game_id < 3000:
            ap_id = 82000 + (game_id % 2000)
        elif 3000 <= game_id < 4000:
            ap_id = 83000 + (game_id % 3000)
        elif 5000 <= game_id < 6000:
            ap_id = 85000 + (game_id % 5000)
        else:
            ap_id = BASE_ID + game_id
            
        table[name] = RaccoinItemData(ap_id, group)
        
    table["Unlock Manager"]   = RaccoinItemData(BASE_ID + 900, "Character")
    table["Unlock Biologist"] = RaccoinItemData(BASE_ID + 901, "Character")
    table["Unlock Chemist"]   = RaccoinItemData(BASE_ID + 902, "Character")
    table["Unlock Trader"]    = RaccoinItemData(BASE_ID + 903, "Character")
    table["Unlock Astronomer"] = RaccoinItemData(BASE_ID + 904, "Character")
    table["Unlock Big Eater"]  = RaccoinItemData(BASE_ID + 905, "Character")
    table["Points"] = RaccoinItemData(80002, "Filler")
    table["Small Tower"] = RaccoinItemData(80003, "Event")
    table["Wheel Spin Small"] = RaccoinItemData(80004, "Event")
    table["Doom"] = RaccoinItemData(80005, "Trap")
    table["Earthquake"] = RaccoinItemData(80006, "Trap")
    table["Restock"] = RaccoinItemData(80007, "Event")
    table["Russian Roulette"] = RaccoinItemData(80008, "Trap")
    table["Tube Launchers"] = RaccoinItemData(80009, "Event")
    table["Small Rain"] = RaccoinItemData(80010, "Event")
    table["UFO"] = RaccoinItemData(80011, "Event")
    table["Medium Tower"] = RaccoinItemData(80012, "Event")
    table["Large Tower"] = RaccoinItemData(80013, "Event")
    table["Wheel Spin Medium"] = RaccoinItemData(80014, "Event")
    table["Wheel Spin Large"] = RaccoinItemData(80015, "Event")
    table["Medium Rain"] = RaccoinItemData(80016, "Event")
    table["Large Rain"] = RaccoinItemData(80017, "Event")
    
    return table

item_table = get_item_table()
item_name_to_id = {name: data.code for name, data in item_table.items()}
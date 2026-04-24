from typing import Dict, Set, NamedTuple, List
from BaseClasses import ItemClassification


class RaccoinItemData(NamedTuple):
    code: int
    group: str
    rarity: str

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
    # Character Unlocks are the ONLY items that should be Progression
    if "Unlock" in name:
        return ItemClassification.progression
    
    # Everything else is just a power-up. 
    # Marking them 'useful' or 'filler' prevents circular logic errors.
    item_data = item_table.get(name)
    if item_data and item_data.rarity in ["Epic", "Rare"]:
        return ItemClassification.useful
        
    return ItemClassification.filler

# ITEM GENERATION

def get_item_table() -> Dict[str, RaccoinItemData]:
    """
    Generates the master item_table with verified IDs from the collection dump.
    Groups are categorized by their 'Exclusivity' or 'Type'.
    """
    # Mapping: Name -> (GameID, Group, Rarity)
    raw_data = {
        "Glue Coin": (2001, "Core", "Common"),
        "Bomboin": (2002, "Core", "Rare"),
        "Tickoin": (2003, "Core", "Common"),
        "Chummy Coin": (2004, "Manager", "Common"),
        "Seedoin": (2005, "Biologist", "Rare"),
        "Wateroin": (2006, "Shared", "Common"),
        "Bunny Coin": (2007, "Core", "Common"),
        "Relicoin": (2008, "Core", "Common"),
        "Eggoin": (2009, "Big Eater", "Common"),
        "Chococoin": (2011, "Core", "Uncommon"),
        "Radiation Coin": (2012, "Core", "Uncommon"),
        "Hen Coin": (2013, "Biologist", "Uncommon"),
        "Boost Coin": (2014, "Core", "Uncommon"),
        "Atomicoin": (2015, "Core", "Rare"),
        "Multicoin": (2016, "Core", "Rare"),
        "Square Coin": (2017, "Manager", "Rare"),
        "JawBreakoin": (2018, "Core", "Uncommon"),
        "Magnetoin": (2020, "Core", "Uncommon"),
        "Sensoroin": (2021, "Core", "Uncommon"),
        "Wolfoin": (2022, "Biologist", "Uncommon"),
        "Star Coin": (2023, "Core", "Uncommon"),
        "Giraffe Coin": (2024, "Core", "Rare"),
        "Monkey Coin": (2026, "Biologist", "Uncommon"),
        "Moon Coin": (2027, "Astronomer", "Rare"),
        "Sun Coin": (2028, "Astronomer", "Epic"),
        "Primal Coin": (2029, "Trader", "Uncommon"),
        "Jetoin": (2030, "Trader", "Uncommon"),
        "Roulette Coin": (2031, "Core", "Rare"),
        "Lightning Coin": (2032, "Core", "Uncommon"),
        "Battery Coin": (2033, "Chemist", "Epic"),
        "Ally Coin": (2034, "Manager", "Uncommon"),
        "Corncoin": (2035, "Biologist", "Epic"),
        "Fireball Coin": (2037, "Chemist", "Epic"),
        "Bubble Coin": (2038, "Chemist", "Uncommon"),
        "Bubble Gum Coin": (2039, "Core", "Uncommon"),
        "Equal Coin": (2040, "Manager", "Rare"),
        "Stomachoin": (2041, "Biologist", "Rare"),
        "Ratoin": (2042, "Shared", "Uncommon"),
        "Tigeroin": (2043, "Biologist", "Epic"),
        "Mushroin": (2044, "Big Eater", "Uncommon"),
        "Whirlwind Coin": (2045, "Core", "Rare"),
        "Saw Coin": (2047, "Core", "Rare"),
        "Wish Pool Coin": (2048, "Core", "Epic"),
        "Division Coin": (2049, "Core", "Epic"),
        "Wormhole Coin": (2050, "Core", "Epic"),
        "Creditoin": (2051, "Core", "Uncommon"),
        "Fishoin": (2052, "Shared", "Uncommon"),
        "Catoin": (2053, "Biologist", "Uncommon"),
        "Pigeoin": (2054, "Biologist", "Rare"),
        "Bullish Coin": (2055, "Core", "Rare"),
        "Rocketoin": (2056, "Core", "Rare"),
        "Luckcoin": (2057, "Trader", "Epic"),
        "Frogoin": (2058, "Biologist", "Rare"),
        "Lotusoin": (2059, "Biologist", "Common"),
        "Drumoin": (2060, "Trader", "Epic"),
        "Frozen Coin": (2061, "Core", "Uncommon"),
        "Sumoin": (2062, "Manager", "Epic"),
        "Speakeroin": (2063, "Core", "Rare"),
        "1/2 Coin": (2064, "Manager", "Epic"),
        "Greater Coin": (2065, "Core", "Rare"),
        "Foxoin": (2066, "Biologist", "Epic"),
        "Cloveroin": (2067, "Core", "Common"),
        "Workoin": (2068, "Manager", "Uncommon"),
        "Killer Coin": (2069, "Core", "Epic"),
        "TNT Coin": (2070, "Chemist", "Uncommon"),
        "Earthquakoin": (2071, "Core", "Uncommon"),
        "Snowman Coin": (2072, "Chemist", "Rare"),
        "Palette Coin": (2073, "Chemist", "Rare"),
        "Hypnoticoin": (2074, "Trader", "Uncommon"),
        "Magicoin": (2075, "Trader", "Uncommon"),
        "Blind Boxoin": (2076, "Trader", "Uncommon"),
        "Dice Coin": (2077, "Trader", "Uncommon"),
        "Richoin": (2078, "Core", "Rare"),
        "+1 Coin": (2079, "Core", "Common"),
        "Slime Coin": (2080, "Trader", "Rare"),
        "Emoin": (2081, "Trader", "Rare"),
        "Minion Coin": (2082, "Trader", "Common"),
        "Red Packet Coin": (2083, "Core", "Uncommon"),
        "Factorial Coin": (2084, "Manager", "Epic"),
        "Percentoin": (2085, "Manager", "Uncommon"),
        "Rooster Coin": (2086, "Biologist", "Rare"),
        "Dogoin": (2087, "Core", "Epic"),
        "Chomp Coin": (2088, "Biologist", "Rare"),
        "Pinwheel Coin": (2090, "Chemist", "Epic"),
        "Bait Coin": (2091, "Core", "Uncommon"),
        "Souloin": (2092, "Trader", "Epic"),
        "Raw Ore Coin": (2093, "Chemist", "Uncommon"),
        "Comboin": (2098, "Trader", "Rare"),
        "Coinmet": (2100, "Astronomer", "Rare"),
        "Meteoroin": (2101, "Astronomer", "Uncommon"),
        "Aeroliteoin": (2102, "Astronomer", "Common"),
        "Mercury Coin": (2103, "Astronomer", "Common"),
        "Venusoin": (2104, "Astronomer", "Uncommon"),
        "Earthoin": (2105, "Astronomer", "Rare"),
        "Marsoin": (2106, "Core", "Common"),
        "Jupiteroin": (2107, "Astronomer", "Uncommon"),
        "Saturoin": (2108, "Astronomer", "Epic"),
        "Uranusoin": (2109, "Astronomer", "Rare"),
        "Neptuoin": (2110, "Astronomer", "Epic"),
        "Cheateroin": (2113, "Core", "Epic"),
        "Riceoin": (2114, "Big Eater", "Common"),
        "Doughoin": (2115, "Big Eater", "Uncommon"),
        "Greenoin": (2116, "Big Eater", "Rare"),
        "Fridgeoin": (2117, "Big Eater", "Rare"),
        "Budoin": (2121, "Biologist", "Uncommon"),
        "Collapsoin": (2122, "Core", "Epic"),
        "Jokeroin": (2123, "Trader", "Epic"),
    }
    
    table = {}
    for name, (game_id, group, rarity) in raw_data.items():
        if 2000 <= game_id < 3000:
            ap_id = 82000 + (game_id % 2000)
        else:
            ap_id = 80000 + game_id
            
        table[name] = RaccoinItemData(ap_id, group, rarity)
        
    table["Unlock Manager"]   = RaccoinItemData(BASE_ID + 900, "Character", "None")
    table["Unlock Biologist"] = RaccoinItemData(BASE_ID + 901, "Character", "None")
    table["Unlock Chemist"]   = RaccoinItemData(BASE_ID + 902, "Character", "None")
    table["Unlock Trader"]    = RaccoinItemData(BASE_ID + 903, "Character", "None")
    table["Unlock Astronomer"] = RaccoinItemData(BASE_ID + 904, "Character", "None")
    table["Unlock Big Eater"]  = RaccoinItemData(BASE_ID + 905, "Character", "None")
    table["Points"] = RaccoinItemData(80002, "Filler", "None")
    table["Small Tower"] = RaccoinItemData(80003, "Event", "None")
    table["Wheel Spin Small"] = RaccoinItemData(80004, "Event", "None")
    table["Doom"] = RaccoinItemData(80005, "Trap", "None")
    table["Earthquake"] = RaccoinItemData(80006, "Trap", "None")
    table["Restock"] = RaccoinItemData(80007, "Event", "None")
    table["Russian Roulette"] = RaccoinItemData(80008, "Trap", "None")
    table["Tube Launchers"] = RaccoinItemData(80009, "Event", "None")
    table["Small Rain"] = RaccoinItemData(80010, "Event", "None")
    table["UFO"] = RaccoinItemData(80011, "Event", "None")
    table["Medium Tower"] = RaccoinItemData(80012, "Event", "None")
    table["Large Tower"] = RaccoinItemData(80013, "Event", "None")
    table["Wheel Spin Medium"] = RaccoinItemData(80014, "Event", "None")
    table["Wheel Spin Large"] = RaccoinItemData(80015, "Event", "None")
    table["Medium Rain"] = RaccoinItemData(80016, "Event", "None")
    table["Large Rain"] = RaccoinItemData(80017, "Event", "None")
    
    return table

item_table = get_item_table()
item_name_to_id = {name: data.code for name, data in item_table.items()}
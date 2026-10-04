import json
import os

# Mapping from current Class value to valid ClassToPreset key
CLASS_MAPPING = {
    # Already correct
    "Acolyte": "Acolyte",
    "Archer": "Archer",
    "Mage": "Mage",
    "Thief": "Thief",
    "Swordsman": "Swordsman",
    
    # Melee presets
    "Melee Tank": "Knight",       # Knight, Crusader, Blacksmith, Paladin, Lord Knight, Royal Guard, Rune Knight, Mechanic, Whitesmith
    "Combo Fighter": "Monk",       # Champion, Monk, Sura
    
    # Ranged presets
    "Archer": "Archer",            # Clown (Bard transcendent) -> Bard
    "Ranged DPS": "Hunter",        # Hunter, Ranger, Sniper
    "Support Musician": "Bard",    # Bard, Bard Dancer, Minstrel, Minstrel Wanderer
    "Musician": "Bard",
    "Ranged DPS": "Gunslinger",    # Gunslinger
    "Ranged DPS": "Rebellion",     # Rebellion
    "Archer": "Dancer",            # Dancer, Wanderer (Dancer transcendent)
    
    # Caster presets
    "Merchant": "Alchemist",       # Alchemist, Creator, Genetic
    "Magic DPS": "Wizard",         # High Wizard, Sorcerer, Warlock
    "Magic Support": "Professor",  # Professor
    "Magic DPS": "Sage",           # Sage
    
    # Hybrid presets
    "Melee DPS": "Assassin",       # Assassin, Assassin Cross, Guillotine Cross
    "Thief": "Rogue",              # Rogue
    "Thief": "Stalker",            # Stalker, Shadow Chaser
    "Ninja Class": "Ninja",        # Ninja, Kagerou, Oboro, Kagerou Oboro, Shinkiro Shiranui
    "Taekwon Class": "Taekwon",    # Taekwon, Star Emperor
    "Taekwon Class": "Star Gladiator", # Star Gladiator
    
    # Support presets
    "Support Healer": "Priest",    # Priest, High Priest, Archbishop
    "Soul Linker Class": "Soul Linker", # Soul Linker, Soul Reaper
    
    # Special cases
    "Novice Class": "Super Novice", # Novice, Super Novice
    "Merchant": "Blacksmith",      # Merchant, Blacksmith, Whitesmith
}

# File-specific overrides (filename -> correct Class value)
FILE_OVERRIDES = {
    "alchemist.json": "Alchemist",
    "archbishop.json": "Priest",
    "assassin.json": "Assassin",
    "assassin_cross.json": "Assassin",
    "bard.json": "Bard",
    "bard_dancer.json": "Bard",
    "blacksmith.json": "Blacksmith",
    "champion.json": "Monk",
    "clown.json": "Bard",
    "creator.json": "Alchemist",
    "crusader.json": "Crusader",
    "dancer.json": "Dancer",
    "genetic.json": "Alchemist",
    "guillotine_cross.json": "Assassin",
    "gunslinger.json": "Gunslinger",
    "high_priest.json": "Priest",
    "high_wizard.json": "Wizard",
    "hunter.json": "Hunter",
    "kagerou_oboro.json": "Kagerou",  # or "Oboro" - this file combines both
    "knight.json": "Knight",
    "lord_knight.json": "Knight",
    "mechanic.json": "Blacksmith",
    "merchant.json": "Blacksmith",
    "minstrel.json": "Bard",
    "minstrel_wanderer.json": "Bard",
    "monk.json": "Monk",
    "ninja.json": "Ninja",
    "novice.json": "Swordsman",
    "paladin.json": "Crusader",
    "priest.json": "Priest",
    "professor.json": "Professor",
    "ranger.json": "Hunter",
    "rebellion.json": "Rebellion",
    "rogue.json": "Rogue",
    "royal_guard.json": "Knight",
    "rune_knight.json": "Knight",
    "sage.json": "Sage",
    "shadow_chaser.json": "Stalker",
    "shinkiro_shiranui.json": "Ninja",
    "sniper.json": "Hunter",
    "sorcerer.json": "Wizard",
    "soul_linker.json": "Soul Linker",
    "soul_reaper.json": "Soul Linker",
    "stalker.json": "Stalker",
    "star_emperor.json": "Taekwon",
    "star_gladiator.json": "Star Gladiator",
    "super_novice.json": "Super Novice",
    "sura.json": "Monk",
    "taekwon.json": "Taekwon",
    "wanderer.json": "Dancer",
    "warlock.json": "Wizard",
    "whitesmith.json": "Blacksmith",
    "wizard.json": "Wizard",
}

profiles_dir = "src/RagnaController/DefaultProfiles"

for filename, correct_class in FILE_OVERRIDES.items():
    filepath = os.path.join(profiles_dir, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    old_class = data.get("Class", "")
    data["Class"] = correct_class
    
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"{filename}: Class '{old_class}' -> '{correct_class}'")

print("\nDone! All profile JSON files updated.")
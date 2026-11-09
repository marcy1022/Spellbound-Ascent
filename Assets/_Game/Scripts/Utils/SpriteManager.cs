using System.Collections.Generic;
using UnityEngine;

namespace SpellboundAscent.Utils
{
    public static class SpriteManager
    {
        static readonly Dictionary<string, Sprite> _cache = new();

        static readonly Dictionary<string, string> _enemySpriteMap = new()
        {
            // ACT 1 normal
            {"goblin_scout", "goblin_warrior"},
            {"goblin_warrior", "goblin_warrior"},
            {"fire_sprite", "fire_elemental"},
            {"stone_golem", "crystal_golem"},
            {"shadow_thief", "shadow_assassin"},
            {"moss_crawler", "slime"},
            {"rust_automaton", "dark_knight"},
            {"cave_bat", "skeleton_archer"},
            // ACT 1 elite
            {"fungal_beast", "slime"},
            {"goblin_chieftain", "goblin_warrior"},
            // ACT 2 normal
            {"flame_cultist", "cultist"},
            {"crystal_spider", "crystal_golem"},
            {"shadow_mage", "shadow_assassin"},
            {"bone_knight", "dark_knight"},
            {"plague_rat", "slime"},
            {"arcane_wisp", "fire_elemental"},
            {"blood_golem", "crystal_golem"},
            {"void_walker", "shadow_assassin"},
            // ACT 2 elite
            {"dark_enchantress", "cultist"},
            {"iron_guardian", "dark_knight"},
            // ACT 3 normal
            {"arcane_sentinel", "dark_knight"},
            {"demon_imp", "fire_elemental"},
            {"soul_reaver", "shadow_assassin"},
            {"chaos_mage", "cultist"},
            {"obsidian_golem", "crystal_golem"},
            {"void_stalker", "shadow_assassin"},
            {"infernal_archer", "skeleton_archer"},
            {"corrupted_knight", "dark_knight"},
            // ACT 3 elite
            {"elder_demon", "fire_elemental"},
            {"void_lord", "shadow_assassin"},
        };

        static readonly Dictionary<string, string> _bossSpriteMap = new()
        {
            {"slime_king", "boss_slime_king"},
            {"guardian_treant", "boss_slime_king"},
            {"lich_lord", "boss_lich_king"},
            {"crystal_queen", "boss_lich_king"},
            {"demon_king", "boss_demon_lord"},
            {"void_emperor", "boss_demon_lord"},
        };

        static readonly string[] _fallbackEnemies =
        {
            "goblin_warrior", "skeleton_archer", "slime", "cultist",
            "dark_knight", "fire_elemental", "shadow_assassin", "crystal_golem"
        };

        static readonly string[] _fallbackBosses =
        {
            "boss_slime_king", "boss_lich_king", "boss_demon_lord"
        };

        public static Sprite Load(string resourcePath)
        {
            if (_cache.TryGetValue(resourcePath, out var cached))
                return cached;

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
                _cache[resourcePath] = sprite;
            return sprite;
        }

        public static Sprite GetBackground(int act)
        {
            return Load($"Sprites/Backgrounds/bg_act{Mathf.Clamp(act, 1, 3)}");
        }

        public static Sprite GetEnemy(string enemyId, string enemyType)
        {
            if (enemyType == "boss")
            {
                if (_bossSpriteMap.TryGetValue(enemyId, out var bossSprite))
                    return Load($"Sprites/Enemies/{bossSprite}");
                int hash = Mathf.Abs(enemyId.GetHashCode()) % _fallbackBosses.Length;
                return Load($"Sprites/Enemies/{_fallbackBosses[hash]}");
            }

            if (_enemySpriteMap.TryGetValue(enemyId, out var sprite))
                return Load($"Sprites/Enemies/{sprite}");
            int idx = Mathf.Abs(enemyId.GetHashCode()) % _fallbackEnemies.Length;
            return Load($"Sprites/Enemies/{_fallbackEnemies[idx]}");
        }

        public static Sprite GetMapNode(string nodeType)
        {
            return Load($"Sprites/MapNodes/node_{nodeType.ToLower()}");
        }

        public static Sprite GetCharacter(string className)
        {
            return Load($"Sprites/Characters/{className}");
        }

        static readonly Dictionary<string, int> _relicSpriteMap = new()
        {
            {"burning_heart", 1}, {"iron_ring", 2}, {"energy_crystal", 3},
            {"ancient_tome", 4}, {"ember_amulet", 5}, {"blood_vial", 6},
            {"war_drum", 7}, {"lucky_coin", 8}, {"thorned_bracelet", 9},
            {"phoenix_feather", 10}, {"golden_idol", 11}, {"crown_of_thorns", 12},
            {"sorcerers_stone", 13}, {"dragons_eye", 14}, {"world_tree_seed", 15},
            {"inferno_core", 16}, {"breath_of_flame", 17}, {"ash_crown", 18},
            {"runaway_core", 19}, {"eternal_seed", 20}, {"living_roots", 21},
            {"predator_fang", 22}, {"light_pack", 23}, {"refined_core", 24},
            {"berserker_seal", 25},
            {"temporal_hourglass", 26}, {"echo_pendulum", 27}, {"frozen_moment", 28},
            {"paradox_core", 29}, {"time_dilation_orb", 30}, {"chronal_infinity", 31},
            {"soul_lantern", 32}, {"bone_talisman", 33}, {"vampiric_fang", 34},
            {"plague_censer", 35}, {"death_embrace", 36}, {"necrotic_heart", 37},
        };

        public static Sprite GetRelic(string relicId)
        {
            if (_relicSpriteMap.TryGetValue(relicId, out int idx))
                return Load($"Sprites/Relics/relic_{idx:D2}");
            int hash = Mathf.Abs(relicId.GetHashCode()) % 37 + 1;
            return Load($"Sprites/Relics/relic_{hash:D2}");
        }

        static readonly Dictionary<string, string> _potionSpriteMap = new()
        {
            {"health_potion", "potion_health"},
            {"block_potion", "potion_shield"},
            {"fire_potion", "potion_fire"},
            {"strength_potion", "potion_strength"},
            {"dexterity_potion", "potion_dexterity"},
            {"speed_potion", "potion_speed"},
            {"energy_potion", "potion_energy"},
            {"poison_potion", "potion_poison"},
            {"regen_potion", "potion_mana"},
            {"elixir", "potion_explosive"},
        };

        public static Sprite GetPotion(string potionId)
        {
            if (_potionSpriteMap.TryGetValue(potionId, out var name))
                return Load($"Sprites/Potions/{name}");
            return Load("Sprites/Potions/potion_health");
        }

        public static Sprite GetCardFrame(string cardType)
        {
            return Load($"Sprites/Cards/frame_{cardType}");
        }

        public static Sprite GetCostGem(int cost)
        {
            return Load("Sprites/Cards/cost_gem_1");
        }

        public static Sprite GetStatusEffect(string key)
        {
            return Load($"Sprites/StatusEffects/status_{key.ToLower()}");
        }

        public static Sprite GetUI(string name)
        {
            return Load($"Sprites/UI/{name}");
        }
    }
}

using System.Collections.Generic;
using SpellboundAscent.Card;

namespace SpellboundAscent.Enemy
{
    public class EnemyAI
    {
        public MoveData GetNextMove(EnemyInstance enemy)
        {
            var data = enemy.Data;
            if (data.moves == null || data.moves.Count == 0) return null;

            string moveId = data.pattern switch
            {
                "sequence" => GetSequenceMove(enemy),
                "random" => GetRandomMove(enemy),
                "weighted" => GetWeightedMove(enemy),
                "conditional" => GetConditionalMove(enemy),
                _ => data.moves[0].id
            };

            return data.moves.Find(m => m.id == moveId) ?? data.moves[0];
        }

        string GetSequenceMove(EnemyInstance enemy)
        {
            var seq = enemy.Data.patternData?.sequence;
            if (seq == null || seq.Count == 0) return enemy.Data.moves[0].id;

            int idx = enemy.PatternIndex % seq.Count;
            string moveId = seq[idx];

            if (enemy.Data.patternData.loop || enemy.PatternIndex < seq.Count - 1)
                enemy.PatternIndex++;

            return moveId;
        }

        string GetRandomMove(EnemyInstance enemy)
        {
            var moves = enemy.Data.moves;
            return moves[UnityEngine.Random.Range(0, moves.Count)].id;
        }

        string GetWeightedMove(EnemyInstance enemy)
        {
            var moves = enemy.Data.moves;
            var weights = enemy.Data.patternData?.weights;

            if (weights == null || weights.Count != moves.Count)
                return GetRandomMove(enemy);

            float total = 0f;
            foreach (float w in weights) total += w;

            float roll = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;

            for (int i = 0; i < moves.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return moves[i].id;
            }

            return moves[^1].id;
        }

        string GetConditionalMove(EnemyInstance enemy)
        {
            var conditions = enemy.Data.patternData?.conditions;
            if (conditions != null)
            {
                foreach (var cond in conditions)
                {
                    if (EvaluateCondition(cond, enemy))
                    {
                        enemy.CurrentPhase = cond.then;
                        break;
                    }
                }
            }

            if (enemy.CurrentPhase != null)
            {
                var seq = enemy.Data.patternData?.sequence;
                if (seq != null && seq.Count > 0)
                {
                    int idx = enemy.PatternIndex % seq.Count;
                    enemy.PatternIndex++;
                    return seq[idx];
                }
            }

            return GetSequenceMove(enemy);
        }

        bool EvaluateCondition(ConditionData cond, EnemyInstance enemy)
        {
            return cond.type switch
            {
                "hpBelow" => enemy.HpPercent < cond.value,
                "hpAbove" => enemy.HpPercent > cond.value,
                _ => false
            };
        }

        public void ExecuteMove(MoveData move, EnemyInstance enemy, Battle.BattleState playerState, Battle.StatusEffectSystem statusSystem)
        {
            foreach (var effect in move.effects)
            {
                ExecuteEffect(effect, enemy, playerState, statusSystem);
            }
        }

        void ExecuteEffect(EffectData effect, EnemyInstance enemy, Battle.BattleState playerState, Battle.StatusEffectSystem statusSystem)
        {
            int value = (int)effect.value;
            int strength = enemy.GetStatus(Utils.StatusKeys.Strength);

            switch (effect.type)
            {
                case "damage":
                    int dmg = statusSystem.CalculateDamage(value, strength, enemy.StatusEffects, playerState.StatusEffects);
                    int thornsDmg = HandleThorns(enemy, playerState, statusSystem);
                    playerState.TakeDamage(dmg);
                    break;

                case "block":
                    enemy.Block += value;
                    break;

                case "applyWeak":
                    statusSystem.ApplyStatus(playerState.StatusEffects, Utils.StatusKeys.Weak, value, "Player");
                    break;

                case "applyVulnerable":
                    statusSystem.ApplyStatus(playerState.StatusEffects, Utils.StatusKeys.Vulnerable, value, "Player");
                    break;

                case "applyBurn":
                    statusSystem.ApplyStatus(playerState.StatusEffects, Utils.StatusKeys.Burn, value, "Player");
                    break;

                case "applyStrength":
                    statusSystem.ApplyStatus(enemy.StatusEffects, Utils.StatusKeys.Strength, value, enemy.Data.name);
                    break;

                case "heal":
                    enemy.Heal(value);
                    break;
            }
        }

        int HandleThorns(EnemyInstance enemy, Battle.BattleState playerState, Battle.StatusEffectSystem statusSystem)
        {
            int thorns = statusSystem.GetStatus(playerState.StatusEffects, Utils.StatusKeys.Thorns);
            if (thorns > 0)
            {
                enemy.TakeDamage(thorns);
                return thorns;
            }
            return 0;
        }
    }
}

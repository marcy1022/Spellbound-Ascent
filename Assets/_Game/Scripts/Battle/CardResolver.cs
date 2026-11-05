using System;
using System.Collections.Generic;
using SpellboundAscent.Card;
using SpellboundAscent.Enemy;
using SpellboundAscent.Core;
using SpellboundAscent.Utils;

namespace SpellboundAscent.Battle
{
    public class CardResolver
    {
        StatusEffectSystem _statusSystem;
        Dictionary<string, int> _playerStatus;

        public event Action<EnemyInstance, int> OnDamageDealt;
        public event Action<int> OnBlockGained;
        public event Action<int> OnHealed;
        public event Action<int> OnCardsDrawn;
        public event Action<int> OnEnergyGained;
        public event Action<int> OnGoldGained;

        public CardResolver(StatusEffectSystem statusSystem, Dictionary<string, int> playerStatus)
        {
            _statusSystem = statusSystem;
            _playerStatus = playerStatus;
        }

        public void Resolve(CardInstance card, EnemyInstance target, List<EnemyInstance> allEnemies, RunState run, DeckManager deck)
        {
            int multiHit = 1;
            foreach (var eff in card.ActiveEffects)
            {
                if (eff.type == "multiHit")
                {
                    multiHit = (int)eff.value;
                    break;
                }
            }

            foreach (var effect in card.ActiveEffects)
            {
                ResolveEffect(effect, target, allEnemies, run, deck, multiHit);
            }
        }

        void ResolveEffect(EffectData effect, EnemyInstance target, List<EnemyInstance> allEnemies, RunState run, DeckManager deck, int multiHit)
        {
            int value = (int)effect.value;
            int strength = _statusSystem.GetStatus(_playerStatus, StatusKeys.Strength);
            int dexterity = _statusSystem.GetStatus(_playerStatus, StatusKeys.Dexterity);

            switch (effect.type)
            {
                case "damage":
                    if (target != null)
                    {
                        for (int i = 0; i < multiHit; i++)
                        {
                            int dmg = _statusSystem.CalculateDamage(value, strength, _playerStatus, target.StatusEffects);
                            target.TakeDamage(dmg);
                            OnDamageDealt?.Invoke(target, dmg);
                        }
                    }
                    break;

                case "damageAll":
                    foreach (var enemy in allEnemies)
                    {
                        if (enemy.IsDead) continue;
                        int dmg = _statusSystem.CalculateDamage(value, strength, _playerStatus, enemy.StatusEffects);
                        enemy.TakeDamage(dmg);
                        OnDamageDealt?.Invoke(enemy, dmg);
                    }
                    break;

                case "block":
                    int block = _statusSystem.CalculateBlock(value, dexterity);
                    OnBlockGained?.Invoke(block);
                    break;

                case "draw":
                    deck.DrawCards(value);
                    OnCardsDrawn?.Invoke(value);
                    break;

                case "energy":
                    OnEnergyGained?.Invoke(value);
                    break;

                case "heal":
                    run.Heal(value);
                    OnHealed?.Invoke(value);
                    break;

                case "applyBurn":
                    ApplyToEnemies(StatusKeys.Burn, value, target, allEnemies);
                    break;

                case "applyPoison":
                    ApplyToEnemies(StatusKeys.Poison, value, target, allEnemies);
                    break;

                case "applyWeak":
                    ApplyToEnemies(StatusKeys.Weak, value, target, allEnemies);
                    break;

                case "applyVulnerable":
                    ApplyToEnemies(StatusKeys.Vulnerable, value, target, allEnemies);
                    break;

                case "applyStrength":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Strength, value, "Player");
                    break;

                case "applyDexterity":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Dexterity, value, "Player");
                    break;

                case "applyThorns":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Thorns, value, "Player");
                    break;

                case "applyHaste":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Haste, value, "Player");
                    break;

                case "gainGold":
                    run.AddGold(value);
                    OnGoldGained?.Invoke(value);
                    break;

                case "multiHit":
                    break;

                case "exhaust":
                    break;

                case "upgradeRandom":
                    UpgradeRandomCards(deck, value);
                    break;
            }
        }

        void ApplyToEnemies(string statusKey, int value, EnemyInstance target, List<EnemyInstance> allEnemies)
        {
            if (target != null)
            {
                _statusSystem.ApplyStatus(target.StatusEffects, statusKey, value, target.Data.name);
            }
            else
            {
                foreach (var enemy in allEnemies)
                {
                    if (!enemy.IsDead)
                        _statusSystem.ApplyStatus(enemy.StatusEffects, statusKey, value, enemy.Data.name);
                }
            }
        }

        void UpgradeRandomCards(DeckManager deck, int count)
        {
            var upgradeable = new List<CardInstance>();
            foreach (var c in deck.Hand)
            {
                if (!c.Upgraded) upgradeable.Add(c);
            }

            for (int i = 0; i < count && upgradeable.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, upgradeable.Count);
                upgradeable[idx].Upgrade();
                upgradeable.RemoveAt(idx);
            }
        }
    }
}

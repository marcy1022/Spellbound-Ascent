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
        BattleState _playerState;

        public event Action<EnemyInstance, int> OnDamageDealt;
        public event Action<int> OnBlockGained;
        public event Action<int> OnHealed;
        public event Action<int> OnCardsDrawn;
        public event Action<int> OnEnergyGained;
        public event Action<int> OnGoldGained;
        public event Action<int> OnSelfDamage;
        public event Action<EnemyInstance> OnCcImmune;

        public CardResolver(StatusEffectSystem statusSystem, Dictionary<string, int> playerStatus, BattleState playerState)
        {
            _statusSystem = statusSystem;
            _playerStatus = playerStatus;
            _playerState = playerState;
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
            if (!CheckCondition(effect, target))
                return;

            int value = !string.IsNullOrEmpty(effect.formula)
                ? ResolveFormula(effect.formula, deck)
                : (int)effect.value;

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

                case "gainEmber":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Ember, value, "Player");
                    break;

                case "detonateEmber":
                    {
                        int ember = _statusSystem.GetStatus(_playerStatus, StatusKeys.Ember);
                        if (ember > 0 && target != null)
                        {
                            int detonateDmg = ember * value;
                            int finalDmg = _statusSystem.CalculateDamage(detonateDmg, 0, _playerStatus, target.StatusEffects);
                            target.TakeDamage(finalDmg);
                            OnDamageDealt?.Invoke(target, finalDmg);
                            _playerStatus[StatusKeys.Ember] = 0;
                            _playerStatus.Remove(StatusKeys.Ember);
                        }
                    }
                    break;

                case "detonateEmberAll":
                    {
                        int ember = _statusSystem.GetStatus(_playerStatus, StatusKeys.Ember);
                        if (ember > 0)
                        {
                            int detonateDmg = ember * value;
                            foreach (var enemy in allEnemies)
                            {
                                if (enemy.IsDead) continue;
                                int finalDmg = _statusSystem.CalculateDamage(detonateDmg, 0, _playerStatus, enemy.StatusEffects);
                                enemy.TakeDamage(finalDmg);
                                OnDamageDealt?.Invoke(enemy, finalDmg);
                            }
                            _playerStatus[StatusKeys.Ember] = 0;
                            _playerStatus.Remove(StatusKeys.Ember);
                        }
                    }
                    break;

                case "selfDamage":
                    _playerState?.LoseHp(value);
                    OnSelfDamage?.Invoke(value);
                    break;

                case "applyCombustion":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Combustion, value, "Player");
                    break;

                case "gainGrowth":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Growth, value, "Player");
                    break;

                case "applyRegen":
                    _statusSystem.ApplyStatus(_playerStatus, StatusKeys.Regen, value, "Player");
                    break;

                case "applyRoot":
                    ApplyToEnemies(StatusKeys.Root, value, target, allEnemies);
                    break;

                case "costReduction":
                    break;
            }
        }

        bool CheckCondition(EffectData effect, EnemyInstance target)
        {
            if (string.IsNullOrEmpty(effect.condition)) return true;

            switch (effect.condition)
            {
                case "emberAbove":
                    return _statusSystem.GetStatus(_playerStatus, StatusKeys.Ember) >= (int)effect.conditionValue;
                case "growthAbove":
                    return _statusSystem.GetStatus(_playerStatus, StatusKeys.Growth) >= (int)effect.conditionValue;
                case "targetRooted":
                    return target != null && _statusSystem.GetStatus(target.StatusEffects, StatusKeys.Root) > 0;
                case "targetBurned":
                    return target != null && _statusSystem.GetStatus(target.StatusEffects, StatusKeys.Burn) > 0;
                case "lowHp":
                    return _playerState != null && _playerState.CurrentHp <= _playerState.MaxHp * effect.conditionValue / 100f;
                default:
                    return true;
            }
        }

        int ResolveFormula(string formula, DeckManager deck)
        {
            string expr = formula;
            expr = expr.Replace("{ember}", _statusSystem.GetStatus(_playerStatus, StatusKeys.Ember).ToString());
            expr = expr.Replace("{growth}", _statusSystem.GetStatus(_playerStatus, StatusKeys.Growth).ToString());
            expr = expr.Replace("{strength}", _statusSystem.GetStatus(_playerStatus, StatusKeys.Strength).ToString());
            expr = expr.Replace("{handSize}", (deck?.Hand.Count ?? 0).ToString());
            expr = expr.Replace("{discardPileSize}", (deck?.DiscardPile.Count ?? 0).ToString());

            try
            {
                expr = expr.Trim();
                int mulIdx = expr.IndexOf('*');
                if (mulIdx > 0)
                {
                    int a = int.Parse(expr.Substring(0, mulIdx).Trim());
                    int b = int.Parse(expr.Substring(mulIdx + 1).Trim());
                    return Math.Max(0, a * b);
                }
                int addIdx = expr.IndexOf('+');
                if (addIdx > 0)
                {
                    int a = int.Parse(expr.Substring(0, addIdx).Trim());
                    int b = int.Parse(expr.Substring(addIdx + 1).Trim());
                    return Math.Max(0, a + b);
                }
                return Math.Max(0, int.Parse(expr));
            }
            catch { return 0; }
        }

        void ApplyToEnemies(string statusKey, int value, EnemyInstance target, List<EnemyInstance> allEnemies)
        {
            bool isCC = statusKey == StatusKeys.Root || statusKey == StatusKeys.Weak;

            if (target != null)
            {
                if (isCC && target.CcImmune) { OnCcImmune?.Invoke(target); return; }
                _statusSystem.ApplyStatus(target.StatusEffects, statusKey, value, target.Data.name);
            }
            else
            {
                foreach (var enemy in allEnemies)
                {
                    if (!enemy.IsDead)
                    {
                        if (isCC && enemy.CcImmune) { OnCcImmune?.Invoke(enemy); continue; }
                        _statusSystem.ApplyStatus(enemy.StatusEffects, statusKey, value, enemy.Data.name);
                    }
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

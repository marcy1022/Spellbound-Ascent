using System;
using System.Collections.Generic;
using UnityEngine;
using SpellboundAscent.Card;
using SpellboundAscent.Core;
using SpellboundAscent.Enemy;
using SpellboundAscent.Utils;

namespace SpellboundAscent.Battle
{
    public enum BattlePhase { NotStarted, PlayerTurn, EnemyTurn, Victory, Defeat }

    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        public BattleState PlayerState { get; private set; } = new();
        public DeckManager Deck { get; private set; }
        public List<EnemyInstance> Enemies { get; private set; } = new();
        public BattlePhase Phase { get; private set; } = BattlePhase.NotStarted;
        public int TurnNumber { get; private set; }

        StatusEffectSystem _statusSystem = new();
        CardResolver _cardResolver;
        EnemyAI _enemyAI = new();

        public event Action OnBattleStarted;
        public event Action<BattlePhase> OnPhaseChanged;
        public event Action<CardInstance> OnCardPlayed;
        public event Action<EnemyInstance, MoveData> OnEnemyActed;
        public event Action<bool> OnBattleEnded;
        public event Action<EnemyInstance> OnEnemyDied;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartBattle(List<EnemyData> enemyDatas)
        {
            var run = GameManager.Instance.CurrentRun;
            var balance = GameManager.Instance.Balance;

            PlayerState.Init(run.maxHp, run.currentHp, balance.energy.basePerTurn);

            Deck = new DeckManager(balance.draw.maxHand);
            Deck.InitFromDeck(new List<CardInstance>(run.deck));

            _cardResolver = new CardResolver(_statusSystem, PlayerState.StatusEffects);
            WireCardResolverEvents();

            Enemies.Clear();
            foreach (var data in enemyDatas)
                Enemies.Add(new EnemyInstance(data));

            TurnNumber = 0;
            Phase = BattlePhase.NotStarted;

            TriggerRelics("battleStart");
            OnBattleStarted?.Invoke();

            StartPlayerTurn();
        }

        void StartPlayerTurn()
        {
            TurnNumber++;
            Phase = BattlePhase.PlayerTurn;

            if (GameManager.Instance.Balance.block.resetOnTurnStart)
                PlayerState.ResetBlock();

            PlayerState.ResetEnergy();

            _statusSystem.TickTurnStart(PlayerState.StatusEffects, "Player", null);

            int extraDraw = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Haste);
            int drawCount = GameManager.Instance.Balance.draw.basePerTurn + extraDraw;
            Deck.DrawCards(drawCount);

            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead)
                    enemy.Block = 0;
            }

            TriggerRelics("turnStart");

            OnPhaseChanged?.Invoke(BattlePhase.PlayerTurn);
        }

        public bool TryPlayCard(CardInstance card, EnemyInstance target)
        {
            if (Phase != BattlePhase.PlayerTurn) return false;
            if (card.Cost > PlayerState.Energy) return false;

            if (NeedsTarget(card) && target == null) return false;

            PlayerState.SpendEnergy(card.Cost);
            _cardResolver.Resolve(card, target, Enemies, GameManager.Instance.CurrentRun, Deck);

            if (card.Data.exhaust || card.Data.type == "power")
                Deck.ExhaustCard(card);
            else
                Deck.PlayCard(card);

            OnCardPlayed?.Invoke(card);
            TriggerRelics("onCardPlayed");

            CheckEnemyDeaths();
            if (CheckBattleEnd()) return true;

            return true;
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn) return;

            _statusSystem.TickTurnEnd(PlayerState.StatusEffects, "Player", dmg =>
            {
                PlayerState.TakeDamage(dmg);
            });

            if (CheckPlayerDeath()) return;

            Deck.DiscardHand();
            ExecuteEnemyTurns();
        }

        void ExecuteEnemyTurns()
        {
            SetPhase(BattlePhase.EnemyTurn);

            foreach (var enemy in Enemies)
            {
                if (enemy.IsDead) continue;

                _statusSystem.TickTurnStart(enemy.StatusEffects, enemy.Data.name, null);

                var move = _enemyAI.GetNextMove(enemy);
                if (move != null)
                {
                    _enemyAI.ExecuteMove(move, enemy, PlayerState, _statusSystem);
                    OnEnemyActed?.Invoke(enemy, move);
                }

                _statusSystem.TickTurnEnd(enemy.StatusEffects, enemy.Data.name, dmg =>
                {
                    enemy.TakeDamage(dmg);
                });

                CheckEnemyDeaths();
                if (CheckPlayerDeath()) return;
            }

            if (!CheckBattleEnd())
                StartPlayerTurn();
        }

        void CheckEnemyDeaths()
        {
            foreach (var enemy in Enemies)
            {
                if (enemy.IsDead)
                {
                    OnEnemyDied?.Invoke(enemy);
                    TriggerRelics("onKill");
                }
            }
        }

        bool CheckPlayerDeath()
        {
            if (PlayerState.IsDead)
            {
                EndBattle(false);
                return true;
            }
            return false;
        }

        bool CheckBattleEnd()
        {
            if (PlayerState.IsDead)
            {
                EndBattle(false);
                return true;
            }

            bool allDead = true;
            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead) { allDead = false; break; }
            }

            if (allDead)
            {
                EndBattle(true);
                return true;
            }

            return false;
        }

        void EndBattle(bool victory)
        {
            SetPhase(victory ? BattlePhase.Victory : BattlePhase.Defeat);

            var run = GameManager.Instance.CurrentRun;
            run.currentHp = PlayerState.CurrentHp;

            if (!victory)
                GameManager.Instance.EndRun(false);

            OnBattleEnded?.Invoke(victory);
        }

        void SetPhase(BattlePhase phase)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        bool NeedsTarget(CardInstance card)
        {
            return card.Data.targetType == "single";
        }

        void TriggerRelics(string trigger)
        {
            var run = GameManager.Instance.CurrentRun;
            foreach (var relic in run.relics)
            {
                if (relic.trigger != trigger) continue;

                foreach (var effect in relic.effects)
                {
                    switch (effect.type)
                    {
                        case "block":
                            PlayerState.AddBlock((int)effect.value);
                            break;
                        case "draw":
                            Deck.DrawCards((int)effect.value);
                            break;
                        case "energy":
                            PlayerState.AddEnergy((int)effect.value);
                            break;
                        case "heal":
                            run.Heal((int)effect.value);
                            break;
                        case "applyStrength":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Strength, (int)effect.value, "Player");
                            break;
                    }
                }
            }
        }

        public bool UsePotion(int potionIndex)
        {
            if (Phase != BattlePhase.PlayerTurn) return false;

            var run = GameManager.Instance.CurrentRun;
            if (potionIndex < 0 || potionIndex >= run.potions.Count) return false;

            var potion = run.potions[potionIndex];

            foreach (var effect in potion.effects)
            {
                int value = (int)effect.value;
                switch (effect.type)
                {
                    case "heal":
                        PlayerState.Heal(value);
                        run.currentHp = PlayerState.CurrentHp;
                        break;
                    case "block":
                        PlayerState.AddBlock(value);
                        break;
                    case "damage":
                        foreach (var enemy in Enemies)
                        {
                            if (!enemy.IsDead)
                                enemy.TakeDamage(value);
                        }
                        CheckEnemyDeaths();
                        break;
                    case "applyStrength":
                        _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Strength, value, "Player");
                        break;
                }
            }

            run.potions.RemoveAt(potionIndex);
            OnCardPlayed?.Invoke(null);
            return true;
        }

        void WireCardResolverEvents()
        {
            _cardResolver.OnBlockGained += amount => PlayerState.AddBlock(amount);
            _cardResolver.OnEnergyGained += amount => PlayerState.AddEnergy(amount);
        }
    }
}

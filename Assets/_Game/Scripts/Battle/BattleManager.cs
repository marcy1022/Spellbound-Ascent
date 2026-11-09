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

        int _masterCooldown;
        int _masterMaxCooldown;

        public event Action OnBattleStarted;
        public event Action<BattlePhase> OnPhaseChanged;
        public event Action<CardInstance> OnCardPlayed;
        public event Action<EnemyInstance, MoveData> OnEnemyActed;
        public event Action<bool> OnBattleEnded;
        public event Action<EnemyInstance> OnEnemyDied;
        public event Action<EnemyInstance> OnCcImmune;
        public event Action OnMasterSkillUsed;

        public int MasterCooldown => _masterCooldown;
        public int MasterMaxCooldown => _masterMaxCooldown;
        public bool CanUseMasterSkill => _masterCooldown <= 0 && Phase == BattlePhase.PlayerTurn;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartBattle(List<EnemyData> enemyDatas, List<float> statScales = null, List<bool> ccImmunes = null)
        {
            var run = GameManager.Instance.CurrentRun;
            var balance = GameManager.Instance.Balance;

            PlayerState.Init(run.maxHp, run.currentHp, balance.energy.basePerTurn);

            Deck = new DeckManager(balance.draw.maxHand);
            Deck.InitFromDeck(new List<CardInstance>(run.deck));

            _cardResolver = new CardResolver(_statusSystem, PlayerState.StatusEffects, PlayerState);
            WireCardResolverEvents();

            Enemies.Clear();
            for (int i = 0; i < enemyDatas.Count; i++)
            {
                float scale = statScales != null && i < statScales.Count ? statScales[i] : 1f;
                bool ccImm = ccImmunes != null && i < ccImmunes.Count && ccImmunes[i];
                Enemies.Add(new EnemyInstance(enemyDatas[i], scale, ccImm));
            }

            TurnNumber = 0;
            Phase = BattlePhase.NotStarted;
            _masterMaxCooldown = GameManager.Instance.Balance.masterSkillCooldown;
            _masterCooldown = 0;

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

            int extraDraw = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Haste);

            bool keepHaste = HasRelic("chronal_infinity");
            _statusSystem.TickTurnStart(PlayerState.StatusEffects, "Player", null, heal =>
            {
                PlayerState.Heal(heal);
                GameManager.Instance.CurrentRun.currentHp = PlayerState.CurrentHp;
            }, keepHaste);

            int drawCount = GameManager.Instance.Balance.draw.basePerTurn + extraDraw;
            Deck.DrawCards(drawCount);

            if (_masterCooldown > 0) _masterCooldown--;

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

            if (card.Data.exhaust || card.Data.type == "power")
                TriggerRelics("onExhaust");

            CheckEnemyDeaths();
            if (CheckBattleEnd()) return true;

            return true;
        }

        public void EndPlayerTurn()
        {
            if (Phase != BattlePhase.PlayerTurn) return;

            int growth = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Growth);
            if (growth > 0)
                PlayerState.AddBlock(growth);

            int combustion = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Combustion);
            if (combustion > 0)
            {
                int ember = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Ember);
                if (ember > 0)
                {
                    foreach (var enemy in Enemies)
                    {
                        if (!enemy.IsDead)
                            enemy.TakeDamage(ember * combustion);
                    }
                    CheckEnemyDeaths();
                    if (CheckBattleEnd()) return;
                }
            }

            TriggerRelics("onTurnEnd");

            _statusSystem.TickTurnEnd(PlayerState.StatusEffects, "Player", dmg =>
            {
                PlayerState.TakeDamage(dmg);
            }, HasRelic("eternal_seed"));

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

                enemy.Block = 0;
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
                if (enemy.IsDead && !enemy.DeathHandled)
                {
                    enemy.DeathHandled = true;
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

        bool HasRelic(string relicId)
        {
            foreach (var r in GameManager.Instance.CurrentRun.relics)
                if (r.id == relicId) return true;
            return false;
        }

        void TriggerRelics(string trigger)
        {
            var run = GameManager.Instance.CurrentRun;
            foreach (var relic in run.relics)
            {
                if (relic.trigger != trigger) continue;
                if (!CheckRelicCondition(relic)) continue;

                foreach (var effect in relic.effects)
                {
                    int value = (int)effect.value;
                    switch (effect.type)
                    {
                        case "block":
                            PlayerState.AddBlock(value);
                            break;
                        case "draw":
                            Deck.DrawCards(value);
                            break;
                        case "energy":
                            PlayerState.AddEnergy(value);
                            break;
                        case "heal":
                            run.Heal(value);
                            break;
                        case "applyStrength":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Strength, value, "Player");
                            break;
                        case "applyDexterity":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Dexterity, value, "Player");
                            break;
                        case "applyThorns":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Thorns, value, "Player");
                            break;
                        case "gainEmber":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Ember, value, "Player");
                            break;
                        case "gainGrowth":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Growth, value, "Player");
                            break;
                        case "applyRegen":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Regen, value, "Player");
                            break;
                        case "applyHaste":
                            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Haste, value, "Player");
                            break;
                        case "applyPoison":
                            foreach (var enemy in Enemies)
                            {
                                if (!enemy.IsDead)
                                    _statusSystem.ApplyStatus(enemy.StatusEffects, StatusKeys.Poison, value, enemy.DisplayName);
                            }
                            break;
                        case "damageAllEnemies":
                            foreach (var enemy in Enemies)
                            {
                                if (!enemy.IsDead) enemy.TakeDamage(value);
                            }
                            CheckEnemyDeaths();
                            break;
                        case "exhaustRandom":
                            for (int i = 0; i < value && Deck.DrawPile.Count > 0; i++)
                            {
                                int idx = UnityEngine.Random.Range(0, Deck.DrawPile.Count);
                                var card = Deck.DrawPile[idx];
                                Deck.DrawPile.RemoveAt(idx);
                                Deck.ExhaustPile.Add(card);
                            }
                            break;
                        case "damage":
                            if (Enemies.Count > 0)
                            {
                                var alive = Enemies.FindAll(e => !e.IsDead);
                                if (alive.Count > 0)
                                {
                                    var target = alive[UnityEngine.Random.Range(0, alive.Count)];
                                    target.TakeDamage(value);
                                    CheckEnemyDeaths();
                                }
                            }
                            break;
                    }
                }
            }
        }

        bool CheckRelicCondition(Relic.RelicData relic)
        {
            if (relic.condition == null || string.IsNullOrEmpty(relic.condition.type))
                return true;

            switch (relic.condition.type)
            {
                case "deckSizeBelow":
                    return GameManager.Instance.CurrentRun.deck.Count <= (int)relic.condition.value;
                case "hpBelow":
                    return PlayerState.CurrentHp <= PlayerState.MaxHp * relic.condition.value / 100f;
                case "emberAbove":
                    return _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Ember) >= (int)relic.condition.value;
                default:
                    return true;
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
                    case "applyDexterity":
                        _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Dexterity, value, "Player");
                        break;
                    case "applyRegen":
                        _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Regen, value, "Player");
                        break;
                    case "draw":
                        Deck.DrawCards(value);
                        break;
                    case "energy":
                        PlayerState.AddEnergy(value);
                        break;
                    case "applyPoison":
                        foreach (var enemy in Enemies)
                        {
                            if (!enemy.IsDead)
                                _statusSystem.ApplyStatus(enemy.StatusEffects, StatusKeys.Poison, value, enemy.DisplayName);
                        }
                        break;
                    case "healFull":
                        PlayerState.Heal(PlayerState.MaxHp);
                        run.currentHp = PlayerState.CurrentHp;
                        break;
                }
            }

            run.potions.RemoveAt(potionIndex);
            OnCardPlayed?.Invoke(null);
            return true;
        }

        public bool UseMasterSkill()
        {
            if (!CanUseMasterSkill) return false;

            string playerClass = GameManager.Instance.CurrentRun.playerClass;
            switch (playerClass)
            {
                case "pyromancer":
                    ExecutePyromancerMaster();
                    break;
                case "warden":
                    ExecuteWardenMaster();
                    break;
                case "chronomancer":
                    ExecuteChronomancerMaster();
                    break;
                case "necromancer":
                    ExecuteNecromancerMaster();
                    break;
                default:
                    return false;
            }

            _masterCooldown = _masterMaxCooldown;
            OnMasterSkillUsed?.Invoke();
            CheckEnemyDeaths();
            CheckBattleEnd();
            return true;
        }

        void ExecutePyromancerMaster()
        {
            int ember = _statusSystem.GetStatus(PlayerState.StatusEffects, StatusKeys.Ember);
            if (ember > 0)
            {
                int dmg = ember * 2;
                foreach (var enemy in Enemies)
                {
                    if (!enemy.IsDead)
                        enemy.TakeDamage(dmg);
                }
                PlayerState.StatusEffects.Remove(StatusKeys.Ember);
            }
        }

        void ExecuteWardenMaster()
        {
            int currentBlock = PlayerState.Block;
            PlayerState.AddBlock(currentBlock);
            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Haste, 1, "Player");
        }

        void ExecuteChronomancerMaster()
        {
            if (Deck.DiscardPile.Count > 0)
            {
                int idx = Deck.DiscardPile.Count - 1;
                var card = Deck.DiscardPile[idx];
                Deck.DiscardPile.RemoveAt(idx);
                Deck.Hand.Add(card);
            }
            PlayerState.AddEnergy(1);
        }

        void ExecuteNecromancerMaster()
        {
            int totalPoison = 0;
            foreach (var enemy in Enemies)
            {
                if (!enemy.IsDead)
                {
                    int poison = _statusSystem.GetStatus(enemy.StatusEffects, StatusKeys.Poison);
                    totalPoison += poison;
                }
            }
            int heal = System.Math.Max(1, totalPoison / 2);
            PlayerState.Heal(heal);
            GameManager.Instance.CurrentRun.currentHp = PlayerState.CurrentHp;
            _statusSystem.ApplyStatus(PlayerState.StatusEffects, StatusKeys.Strength, 1, "Player");
        }

        public string GetMasterSkillName()
        {
            string playerClass = GameManager.Instance.CurrentRun.playerClass;
            return playerClass switch
            {
                "pyromancer" => "점화",
                "warden" => "철벽",
                "chronomancer" => "되감기",
                "necromancer" => "영혼 수확",
                _ => "스킬"
            };
        }

        public string GetMasterSkillDesc()
        {
            string playerClass = GameManager.Instance.CurrentRun.playerClass;
            return playerClass switch
            {
                "pyromancer" => "잔불 전부 소비, 잔불×2 전체 데미지",
                "warden" => "현재 블록 2배, 신속 1 획득",
                "chronomancer" => "마지막 사용 카드 핸드로 복귀, 에너지 +1",
                "necromancer" => "적 독 합계의 절반만큼 회복, 힘 +1",
                _ => ""
            };
        }

        void WireCardResolverEvents()
        {
            _cardResolver.OnBlockGained += amount => PlayerState.AddBlock(amount);
            _cardResolver.OnEnergyGained += amount => PlayerState.AddEnergy(amount);
            _cardResolver.OnCcImmune += enemy => OnCcImmune?.Invoke(enemy);
        }
    }
}

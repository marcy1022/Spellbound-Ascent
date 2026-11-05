# Spellbound Ascent — 프로젝트 규칙

## 프로젝트 개요
로그라이크 덱빌더 (Slay the Spire 계열). Unity 2D, 모바일 세로 화면 (1080x1920), 1인 개발 + AI 콘텐츠 양산.

## 기획서
`~/.claude/work/docs/2026-11-05_덱빌더_최종기획서.html` 참조. 모든 수치와 규칙의 원본.

## 폴더 구조
```
Assets/_Game/
├── Data/           # JSON 데이터 (Cards, Enemies, Relics, Events, Balance)
├── Scripts/        # C# 코드
│   ├── Core/       # GameManager, RunState, SaveManager, UnlockManager
│   ├── Battle/     # BattleManager, TurnManager, IntentSystem, StatusEffectSystem
│   ├── Card/       # CardData, CardResolver, DeckManager, HandManager
│   ├── Enemy/      # EnemyData, EnemyAI, PatternExecutor
│   ├── Relic/      # RelicData, RelicManager, RelicTriggerSystem
│   ├── Map/        # MapGenerator, MapNode, MapUI
│   ├── UI/         # 모든 UI 코드 (BattleUI, MapUI, ShopUI, EventUI, etc.)
│   ├── Event/      # EventData, EventManager
│   ├── Potion/     # PotionData, PotionManager
│   ├── Save/       # SaveData, SerializationHelper
│   └── Utils/      # JsonLoader, Extensions, Constants
├── Sprites/        # 이미지 에셋 (Cards, Enemies, Relics, UI, Backgrounds)
├── Prefabs/        # 프리팹
└── Scenes/         # 씬 파일
```

## 코딩 규칙
- **네임스페이스**: `SpellboundAscent.{폴더명}` (예: `SpellboundAscent.Battle`)
- **데이터 로딩**: JSON 파일은 `Resources.Load<TextAsset>()` 사용. ScriptableObject 사용하지 않음.
- **싱글톤**: GameManager, BattleManager 등 매니저 클래스는 `MonoBehaviour` 싱글톤 패턴
- **이벤트**: `System.Action` 또는 `UnityEvent` 사용. 커스텀 이벤트 버스 만들지 않음.
- **UI**: 코드로 생성하지 않음. 프리팹 + 씬에 배치. TextMeshPro 사용.
- **주석**: 기본적으로 작성하지 않음. 비자명한 밸런스 공식이나 물리 규칙만 예외.

## 데이터 규칙
- 모든 게임 콘텐츠(카드, 적, 유물, 이벤트)는 JSON으로 정의
- JSON 파일 위치: `Assets/_Game/Data/{타입}/`
- 파일 단위: 카테고리별 1개 파일 (예: `pyromancer_cards.json`, `act1_enemies.json`)
- ID 네이밍: `{class}_{name}` snake_case (예: `pyro_fireball`, `ward_shield_bash`)
- 밸런스 설정: `Assets/_Game/Data/Balance/balance_config.json`에서 관리

## JSON 스키마 요약

### Card
```json
{
  "id": "string", "name": "string", "nameKo": "string",
  "class": "pyromancer|warden|chronomancer|neutral",
  "type": "attack|skill|power|curse",
  "cost": 0-3, "rarity": "starter|common|uncommon|rare|curse",
  "targetType": "single|all|self|random|none",
  "effects": [{"type": "string", "value": 0}],
  "upgradeEffects": [{"type": "string", "value": 0}],
  "keywords": [], "exhaust": false, "innate": false, "ethereal": false,
  "description": "string", "descriptionKo": "string", "flavorText": "string"
}
```

### Enemy
```json
{
  "id": "string", "name": "string", "nameKo": "string",
  "act": 1-3, "type": "normal|elite|boss",
  "hp": {"min": 0, "max": 0},
  "moves": [{"id": "string", "intent": "attack|defend|buff|debuff|special", "effects": []}],
  "pattern": "sequence|random|weighted|conditional",
  "patternData": {}
}
```

### Relic
```json
{
  "id": "string", "name": "string", "nameKo": "string",
  "rarity": "common|uncommon|rare|boss|shop|event",
  "class": "any|pyromancer|warden|chronomancer",
  "trigger": "battleStart|turnStart|onCardPlayed|onDamaged|onKill|always|conditional",
  "condition": null, "effects": [],
  "description": "string", "descriptionKo": "string", "flavorText": "string"
}
```

## effect.type 목록
damage, damageAll, block, draw, energy, heal, applyBurn, applyPoison, applyThorns, applyStrength, applyDexterity, applyWeak, applyVulnerable, applyHaste, multiHit, exhaust, upgradeRandom, gainGold

## 밸런스 기준
- 1에너지 = 공격 6데미지 / 방어 5블록
- 레어리티 보정: Common x1.0, Uncommon x1.3~1.5, Rare x1.8~2.5
- 보스 HP 고정: Act1 80~100, Act2 150~180, Act3 280~350
- 가시 최대 15스택, 턴 시작 시 -1

## 스킬 라우팅
- 카드 생성 → `/generate-card`
- 적 생성 → `/generate-enemy`
- 밸런스 검증 → `/balance-check`

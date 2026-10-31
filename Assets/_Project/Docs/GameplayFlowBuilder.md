# Генератор уровня — `GameplayFlowBuilder`

Редакторский скрипт `AITools/Editor/GameplayFlowBuilder.cs`. Меню **SibJam → AI Tools → Build Gameplay Flow Scene**.

Собирает сцену `Scenes/GameplayFlow.unity` целиком, одним кликом. Нужен ИИ-агентам (Claude через Unity MCP): так быстрее и надёжнее, чем расставлять сотни объектов по одному. **Геймдизайнерам запускать не нужно.**

## ⚠ Перед запуском

- **Сцена перезаписывается полностью** — ручные правки в ней пропадут. Когда комнаты начнут дорабатывать руками, генератор больше не запускать.
- Только **вне Play Mode**.
- Если открыта изменённая сцена, Unity спросит про сохранение — генератор ждёт ответа.
- Конфиги и материалы в `Configs/Levels` **обновляются на месте**: GUID не меняются, ссылки не ломаются. Ручные правки в них тоже затрутся значениями из генератора.

## Что делает, по шагам

1. **Материалы** — пол, стены, двери, камеры, меч (`Configs/Levels/*.mat`), декор и небо (`Configs/Levels/Materials/*.mat`).
2. **Основа сцены** — содержимое `Tests/Scenes/CamerasTest` копируется в `GameplayFlow` (игрок, камера, свет, HUD, `GameLoop`, `CurrentLocationInfo`). Тестовые локации и `PlayerTeleporter` удаляются.
3. **Меч** — `RaycastEnemyDamager` убирается, на игрока вешается `MeleeWeapon` (выключен), модель меча — в `CameraTarget`.
4. **Отладка** — объект `DebugCheats`.
5. **Враги** — префабы на моделях `Art/Models/Enemies/Mob1Animations.fbx` и `Mob2Animations.fbx` с зацикленным Idle, конфиги со статами тестовых врагов (см. ниже).
6. **SafeRoom** — комната без боя: койка, шкафчики, тела охранников, обломки камер, меч на пьедестале (`WeaponPickup`), табличка с правилами (TextMeshPro, шрифт RubikGlitch). Выход — дверь.
7. **Боевые комнаты** — по таблице `CreateRoomSetups()`. В каждой: пол, стены, двери, общая отделка, тематический декор, `WaveSpawner` с 5 точками, `WavesLoop` с конфигом, 3 камеры + `CameraDestructionGoal`, `LocationLoop`.
8. **Коридоры** — между соседними комнатами: стены, декор безопасной зоны, табличка с названием следующей комнаты, три `PassageZone`, `LocationPassage` (см. [Passages.md](Passages.md)). Первый коридор ждёт подбора меча.
9. **Атмосфера** — сумеречное небо, низкое солнце, туман, трёхцветный ambient.
10. `GameLoop._locations` — боевые локации по порядку. Игрок — в начало SafeRoom.
11. **Игровые системы** — из `Scenes/Game` переносятся `Scope` (`GameLifetimeScope`), `UI` (пауза, настройки, выход), `EventSystem`, `Global Volume`. `TestLifetimeScope` удаляется, игрок добавляется в `autoInjectGameObjects`.
12. **Build Settings** — сцена добавляется (или обновляется).
13. Сохранение сцены и ассетов.

## Что где настраивать

| Что | Где в коде |
|---|---|
| Размеры комнат, стен, проёмов, длина коридора | константы `RoomSize`, `WallHeight`, `WallThickness`, `GapWidth`, `CorridorLength` |
| Список комнат, волны, паузы | `CreateRoomSetups()` — `RoomSetup { Name, Waves, Delay }`, волна = набор `(враг, количество)` |
| Босс | `CreateBossConfig()` — масштаб префаба, хп, урон |
| Точки спавна, позиции камер | массивы в `BuildRoom()` |
| Тема комнаты: название на табличке, цвет подсветки, декор | `CreateRoomSetups()` — `Title`, `Accent`, `Decorate` |
| Декор комнат и коридоров | `GameplayFlowBuilder.Rooms.cs`: `DecorateShell()` (общая отделка), `DecorateWarehouse/Servers/Lab/Generator/Assembly/Core()`, `DecorateSafeRoom()`, `DecorateCorridor()` |
| Пропсы из примитивов, материалы декора | `GameplayFlowBuilder.Props.cs`: `BuildCrate()`, `BuildServerRack()`, `BuildVat()` и т.д., `CreateDecorMaterials()` |
| Враги: модель, масштаб, цвет, статы | `GameplayFlowBuilder.Enemies.cs`: `CreateEnemies()` |
| Небо, солнце, туман | `SetupAtmosphere()` |
| Табличка с правилами SafeRoom | `BuildSafeRoom()`, `BuildNote()` |
| Меч в руке / на подборе | `CreateMeleeWeapon()`, `CreateSword()`, `BuildWeaponPickup()` |
| Триггеры коридора | `BuildCorridor()` |
| Цвета пола, стен, дверей | `CreateMaterials()` |

## Темы комнат

| Комната | Табличка | Цвет | Декор |
|---|---|---|---|
| SafeRoom | ВЫХОД | голубой | койка, шкафчики, тела охранников, обломки камер, меч на пьедестале |
| Light | 01 // СКЛАД | янтарный | ящики, поддоны, стеллажи, бочки |
| Location_1 | 02 // СЕРВЕРНАЯ | голубой | стойки серверов, охладители, пульты |
| Location_2 | 03 // ЛАБОРАТОРИЯ | зелёный | колбы с охранниками, лабораторные столы |
| Location_3 | 04 // ГЕНЕРАТОРНАЯ | оранжевый | генератор, трубы, баки, бетонные укрытия |
| Location_4 | 05 // СБОРОЧНЫЙ ЦЕХ | фиолетовый | конвейер с заготовками, манипуляторы, манекены на стойках |
| Boss | 06 // ЯДРО | красный | колонны, огромный «глаз» на стене |

Общая отделка любой комнаты: плитка пола, кольцо арены, плинтусы, пилястры, световые полосы цвета комнаты, балки с лампами, экраны с графиками, потолок.

**Потолок** (`BuildRoomCeiling()`, в коридорах — `DecorateCorridor()`) на высоте `CeilingHeight` = 4.5, выше стен: щель между верхом стен и потолком закрывает фриз. Плита потолка не заходит в толщу стен с дверями, а над проёмами стоят короба (`DoorHousing`): дверь поднимается на 4.5 и уезжает в них. Солнце внутрь не попадает, комнаты освещают подвесные лампы и световые полосы.

Декор ставится так, чтобы оставались свободными проходы у дверей, точки спавна и места перед камерами наблюдения. Камеры и их позиции не менялись. Коллайдер у пропса один — на корне (`Solid`), детали без коллайдеров.

## Враги

| Префаб (`Prefabs/Enemies`) | Модель | Масштаб | Статы из | Где в волнах |
|---|---|---|---|---|
| `EnemyMelee` | Mob1 | 1 | `TestEnemyConfig` | basic |
| `EnemyGunner` | Mob2 | 1 | `TestEnemy2Config` | medium |
| `EnemyBrute` | Mob1 | 1.35, красный | `TestEnemy3Config` | heavy |
| `Configs/Levels/Boss` | Mob1 | 2.5, тёмно-красный | `BossConfig` | boss |

Конфиги — `Configs/Enemies/Enemy*Config.asset`. Пивот у ног, коллайдер — капсула на корне. `RandomAnimationStart` сдвигает фазу и скорость Idle, чтобы враги не двигались синхронно. В FBX есть клипы Walking, Attack, Shooting, HitWall — пока не используются (у врагов нет ИИ).

Рост сложности волн (`StatsGrowthPerWave`, `ExtraEnemiesPerRepeat`) берётся из значений по умолчанию в `WavesLoopConfig`, см. [Waves.md](Waves.md).

## Что создаёт

```
Configs/Levels/
  <Комната>_Wave_N.asset, <Комната>_WavesLoop.asset   — волны
  Boss.prefab, BossConfig.asset                        — босс
  *.mat                                                — материалы
  Materials/*.mat                                      — материалы декора, врагов, неба
Configs/Enemies/Enemy*Config.asset                     — конфиги врагов
Prefabs/Enemies/Enemy*.prefab                          — префабы врагов
Art/Models/Enemies/*.controller                        — аниматоры (Idle)
Scenes/GameplayFlow.unity
```

Сама сцена при каждой пересборке сильно меняется в git (новые внутренние ID объектов) — это нормально.

## Устройство кода

- Приватные `[SerializeField]`-поля заполняются через `SerializedObject`: хелперы `SetObject`, `SetArray`, `GetArray`. Автосвойства `EnemyConfig` — как `<MaxHealth>k__BackingField`.
- `CreateAsset` — создать ассет или переписать существующий (`CopySerialized`), сохраняя GUID. Материалы правятся отдельно (шейдер и цвет), т.к. `CopySerialized` теряет их теги рендера.
- Код разбит на partial-файлы: основной (сцена, комнаты, коридоры), `.Props` (материалы и пропсы), `.Rooms` (декор и атмосфера), `.Enemies` (враги).
- Примитивы — `CreateBlock` (куб нужного размера, `null` вместо материала = невидимый триггер), точки — `CreatePoint`.

# Генератор уровня — `GameplayFlowBuilder`

Редакторский скрипт `AITools/Editor/GameplayFlowBuilder.cs`. Меню **SibJam → AI Tools → Build Gameplay Flow Scene**.

Собирает сцену `Scenes/GameplayFlow.unity` целиком, одним кликом. Нужен ИИ-агентам (Claude через Unity MCP): так быстрее и надёжнее, чем расставлять сотни объектов по одному. **Геймдизайнерам запускать не нужно.**

## ⚠ Перед запуском

- **Сцена перезаписывается полностью** — ручные правки в ней пропадут. Когда комнаты начнут дорабатывать руками, генератор больше не запускать.
- Только **вне Play Mode**.
- Если открыта изменённая сцена, Unity спросит про сохранение — генератор ждёт ответа.
- Конфиги и материалы в `Configs/Levels` **обновляются на месте**: GUID не меняются, ссылки не ломаются. Ручные правки в них тоже затрутся значениями из генератора.

## Что делает, по шагам

1. **Материалы** — пол, стены, двери, камеры, меч, декор (`Configs/Levels/*.mat`).
2. **Основа сцены** — содержимое `Tests/Scenes/CamerasTest` копируется в `GameplayFlow` (игрок, камера, свет, HUD, `GameLoop`, `CurrentLocationInfo`). Тестовые локации и `PlayerTeleporter` удаляются.
3. **Меч** — `RaycastEnemyDamager` убирается, на игрока вешается `MeleeWeapon` (выключен), модель меча — в `CameraTarget`.
4. **Отладка** — объект `DebugCheats`.
5. **SafeRoom** — комната без боя: декор, `WeaponPickup`, табличка (TextMeshPro, шрифт RubikGlitch). Выход — дверь.
6. **Боевые комнаты** — по таблице `CreateRoomSetups()`. В каждой: пол, стены, двери, `WaveSpawner` с 5 точками, `WavesLoop` с конфигом, 3 камеры + `CameraDestructionGoal`, `LocationLoop`.
7. **Коридоры** — между соседними комнатами: стены, три `PassageZone`, `LocationPassage` (см. [Passages.md](Passages.md)). Первый коридор ждёт подбора меча.
8. `GameLoop._locations` — боевые локации по порядку. Игрок — в начало SafeRoom.
9. **Игровые системы** — из `Scenes/Game` переносятся `Scope` (`GameLifetimeScope`), `UI` (пауза, настройки, выход), `EventSystem`, `Global Volume`. `TestLifetimeScope` удаляется, игрок добавляется в `autoInjectGameObjects`.
10. **Build Settings** — сцена добавляется (или обновляется).
11. Сохранение сцены и ассетов.

## Что где настраивать

| Что | Где в коде |
|---|---|
| Размеры комнат, стен, проёмов, длина коридора | константы `RoomSize`, `WallHeight`, `WallThickness`, `GapWidth`, `CorridorLength` |
| Список комнат, волны, паузы | `CreateRoomSetups()` — `RoomSetup { Name, Waves, Delay }`, волна = набор `(враг, количество)` |
| Босс | `CreateBossConfig()` — масштаб префаба, хп, урон |
| Точки спавна, позиции камер | массивы в `BuildRoom()` |
| Декор и табличка SafeRoom | `BuildSafeRoom()`, `BuildNote()` |
| Меч в руке / на подборе | `CreateMeleeWeapon()`, `CreateSword()`, `BuildWeaponPickup()` |
| Триггеры коридора | `BuildCorridor()` |
| Цвета | `CreateMaterials()` |

Рост сложности волн (`StatsGrowthPerWave`, `ExtraEnemiesPerRepeat`) берётся из значений по умолчанию в `WavesLoopConfig`, см. [Waves.md](Waves.md).

## Что создаёт

```
Configs/Levels/
  <Комната>_Wave_N.asset, <Комната>_WavesLoop.asset   — волны
  Boss.prefab, BossConfig.asset                        — босс
  *.mat                                                — материалы
Scenes/GameplayFlow.unity
```

Сама сцена при каждой пересборке сильно меняется в git (новые внутренние ID объектов) — это нормально.

## Устройство кода

- Приватные `[SerializeField]`-поля заполняются через `SerializedObject`: хелперы `SetObject`, `SetArray`, `GetArray`. Автосвойства `EnemyConfig` — как `<MaxHealth>k__BackingField`.
- `CreateAsset` — создать ассет или переписать существующий (`CopySerialized`), сохраняя GUID. Материалы правятся отдельно (шейдер и цвет), т.к. `CopySerialized` теряет их теги рендера.
- Примитивы — `CreateBlock` (куб нужного размера, `null` вместо материала = невидимый триггер), точки — `CreatePoint`.

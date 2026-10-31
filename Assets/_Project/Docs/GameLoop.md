# Игровой цикл и камеры наблюдения

## Иерархия

```
GameLoop                      — список локаций, какая текущая, переключение
 └─ LocationLoop (на локацию) — связывает волны и цель
     ├─ WavesLoop             — бесконечные волны (см. Waves.md)
     └─ CameraDestructionGoal — сломать все камеры на локации
```

Правило: **команды вниз, события вверх, данные — через read-only `Info`-интерфейсы**.

## Как проходится локация

1. Игрок входит в `LocationTrigger` → `GameLoop.LaunchLocation(локация)` → стартуют волны.
2. Волны идут бесконечно (`RepeatLastWave`).
3. Все камеры сломаны → `WavesLoop.StopSpawning()`. Уже живые враги **не умирают**.
4. Когда добиты все враги — локация `Completed`.
5. `GameLoop` переключает на следующую (`LocationChanged`), `PlayerTeleporter` переносит игрока в её `ArrivalPoint`.
6. Последняя локация завершена → `GameLoop.Completed`.

## Компоненты

| Компонент | Зачем |
|---|---|
| `GameLoop` | `_locations` по порядку. `LaunchLocation(l)` запускает, только если `l` — текущая. События `LocationChanged`, `Completed` |
| `LocationLoop` | поля `_wavesLoop`, `_cameraDestructionGoal`, `_arrivalPoint`. Состояния `Idle → Running → Completed`. Наружу: `Waves` (`IWavesLoopInfo`), `Cameras` (`ICameraDestructionGoalInfo`) |
| `LocationTrigger` | коллайдер `Is Trigger`; запускает свою локацию через `GameLoop` и выключается |
| `PlayerTeleporter` | по `LocationChanged` переносит `CharacterController` игрока в `ArrivalPoint` |
| `SecurityCamera` | ломается после `_hitsToBreak` ударов (`TakeHit()`), событие `Broken`, объект уничтожается |
| `CameraDestructionGoal` | массив `_securityCameras`; события `Changed`, `Completed`; `TotalCount`, `BrokenCount`, `LeftCount` |
| `CurrentLocationInfo` | единственная точка данных для UI: `LocationChanged`, `LocationIndex`, `Waves`, `Cameras` текущей локации |
| `WavesView`, `SecurityCamerasView` | HUD: враги / пауза, камеры. Берут данные из `CurrentLocationInfo` |

## Тест

Сцена `Assets/_Project/Tests/Scenes/CamerasTest`: две локации, по 3 красные камеры на каждой.

- **ЛКМ** — луч из прицела: урон врагу или удар по камере (`RaycastEnemyDamager`, тестовый)
- Курсор лочится автоматически, **Esc** — отпустить, клик — снова залочить
- HUD — оставшиеся враги, пауза, камеры

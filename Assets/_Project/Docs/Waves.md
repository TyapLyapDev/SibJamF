# Волны

Цикл: **волна → пауза → волна → …**

1. Волна спавнит врагов из своего конфига.
2. Когда все враги волны убиты, начинается пауза.
3. После паузы стартует следующая волна.
4. После последней волны:
   - `RepeatLastWave` включён — последняя волна повторяется бесконечно;
   - выключен — цикл завершается (событие `Completed`).

## Состояния (`WavesLoopState`)

`Idle → Wave ⇄ Delay → … → Completed`

| Состояние | Что происходит |
|---|---|
| `Idle` | цикл не запущен |
| `Wave` | идёт волна, ждём убийства её врагов |
| `Delay` | пауза между волнами |
| `WaitingForEnemiesKilled` | спавн остановлен (`StopSpawning` во время волны), ждём, пока добьют оставшихся |
| `Completed` | цикл завершён |

Логика — в `WavesStateMachine` (обычный класс), очерёдность волн и пауз — в `WaveSequence` / `DelaySequence`. `WavesLoop` — тонкая MonoBehaviour-обёртка.

## Конфиги

Создаются через **Create → SibJam**.

| Конфиг | Что внутри |
|---|---|
| `Enemy Config` | префаб врага (с компонентом `Enemy`), хп, скорость, атака |
| `Wave Config` | список врагов: `EnemyConfig` + количество |
| `Waves Loop Config` | список волн с паузой **после** каждой, галка `RepeatLastWave` |

## На сцене

| Компонент | Зачем |
|---|---|
| `WavesLoop` | ведёт цикл. Поля: конфиг цикла и `WaveSpawner` |
| `WaveSpawner` | спавнит врагов в точках `_spawnPoints` (случайная, не равна предыдущей) |
| `WavesTrigger` | на коллайдере с `Is Trigger`. Игрок с тегом `Player` входит — волны стартуют, триггер выключается |
| `LocationTransition` | когда цикл завершился, переносит игрока в точку (старая схема, используется в `WavesTest`) |

Игроку нужен Rigidbody (можно кинематический), иначе триггер не сработает.

## API `WavesLoop`

- `LaunchWaves()` — запустить цикл
- `StopSpawning()` — больше не спавнить: из `Wave` → `WaitingForEnemiesKilled`, из `Delay` → сразу `Completed`
- `CompleteWaves()` — завершить сразу (живые враги остаются)
- `Completed` — цикл завершён
- `IsRunning`, `IsCompleted`
- `Info` (`IWavesLoopInfo`) — только чтение: `State`, `StateChanged`, `WaveIndex`, `AliveEnemiesCount`, `DelayTimeLeft`

## Тест

Сцена `Assets/_Project/Tests/Scenes/WavesTest`: две локации, после трёх волн первой игрок переносится на вторую.

- **Пробел** — 100 урона одному врагу (`SpaceEnemyDamager`)
- Панель слева сверху — состояние циклов (`WavesDebugView`)

Тестовые конфиги и префабы — в `Assets/_Project/Tests/Configs` и `Assets/_Project/Tests/Prefabs`, тестовые скрипты — в `Assets/_Project/Scripts/Features/Waves/Testing`.

См. также [GameLoop.md](GameLoop.md) — как волны встроены в локации.

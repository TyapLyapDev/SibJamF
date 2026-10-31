# Волны

Цикл: **волна → пауза → волна → …**

1. Волна спавнит врагов из своего конфига.
2. Когда все враги волны убиты, начинается пауза.
3. После паузы стартует следующая волна.
4. После последней волны:
   - `RepeatLastWave` включён — последняя волна повторяется бесконечно;
   - выключен — цикл завершается (событие `Completed`).

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
| `LocationTransition` | когда цикл локации завершился, переносит игрока в точку |

Игроку нужен Rigidbody (можно кинематический), иначе триггер не сработает.

## API `WavesLoop`

- `LaunchWaves()` — запустить цикл
- `CompleteWaves()` — завершить досрочно (вызовет `Completed`; живые враги остаются)
- `Completed` — цикл завершён
- `IsRunning`, `IsCompleted`
- `Waves` — текущая волна (`CurrentIndex`, `CurrentWave.AliveCount`)
- `Delays` — пауза (`CurrentIndex`, `TimeLeft`, `IsCompleted`)

## Тест

Сцена `Assets/_Project/Tests/Scenes/WavesTest`: две локации, после трёх волн первой игрок переносится на вторую.

- **Пробел** — 100 урона одному врагу (`SpaceEnemyDamager`)
- Панель слева сверху — состояние циклов (`WavesDebugView`)

Тестовые конфиги и префабы — в `Assets/_Project/Tests/Configs` и `Assets/_Project/Tests/Prefabs`, тестовые скрипты — в `Assets/_Project/Scripts/Waves/Testing`.

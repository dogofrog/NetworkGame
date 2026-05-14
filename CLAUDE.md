# CLAUDE.md — Полный контекст проекта NetworkGame

> Этот файл — единственный источник истины для Claude Code.
> При начале новой сессии: прочитай этот файл целиком, затем прочитай ключевые скрипты из раздела "Архитектура", и сообщи пользователю на чём мы остановились и что делать дальше.

---

## О пользователе (читать первым!)

- **Язык:** всегда отвечать на **русском языке**
- **Опыт Unity:** ограниченный. Плохо разбирается в инспекторе, RectTransform, Anchor Presets, назначении компонентов
- **Правило №1:** всегда давать пошаговые инструкции по работе в Unity Inspector — что создать, как назвать, куда перетащить. Не останавливаться на уровне "добавьте поле в код"
- **Проект для диплома** — ориентация на финальный результат, не на архитектурные паттерны
- **Предпочтения:** простые решения без лишних префабов и ручных настроек. Если решение требует много ручной работы в Unity — предложить более простой вариант
- **Стиль ответов:** коротко и по делу. Не объяснять что код делает — объяснять что делать в Unity

---

## Что это за проект

Unity WebGL-игра (ПК/браузер) — образовательный платформер про сетевые технологии для диплома.

**Суть игры:** игрок управляет персонажем на клеточном поле 8×8, вводя последовательность команд (Up, Down, Left, Right, While). Задача — дойти до целей в правильном порядке, используя ограниченный набор команд.

**Образовательная концепция — уровни:**
- Уровень 0 — туториал (1 цель, нет препятствий, учим базовые команды)
- Уровень 1 — серверная часть и дата-центр (3 цели: база данных, сервер приложений, коммутатор)
- Уровень 2 — сеть провайдера и маршрутизация (3 цели + стены)
- Уровень 3 — последняя миля (3 цели + стены + ямы)

**Метафоры игры:**
- Игрок = пакет данных
- Стены = файрволы / занятые маршруты
- Ямы = потеря пакета (коллизия в сети)
- While = двигаться вдоль кабеля до упора
- Лимит команд = TTL (Time To Live)

**Платформа:** WebGL (ПК/браузер)
**Рендер пайплайн:** URP (Universal Render Pipeline)
**Камера:** Perspective, вид сверху-сбоку ~60° по X

**Сцены:**
- `Assets/Scenes/Level_0.unity` — туториал (1 цель, без препятствий)
- `Assets/Scenes/Level_Test.unity` — тестовая сцена (бывшая Level_1, для экспериментов)

**В проекте нет:** серверной части, базы данных, авторизации, ScriptableObject, JSON — данные уровня задаются через инспектор.

---

## Игровые механики (подробно)

### Команды
- **Up / Down / Left / Right** — шаг на одну клетку
- **While + направление** — двигаться до упора (стена или граница поля)
- Отображаются компактно: `→×5 ↑×2 ⟳↑×1`
- Ввод через **физические 3D кнопки** (не Canvas UI кнопки)

### Цели (чекпоинты)
- Цели нужно пройти строго по порядку
- Правильная цель → клетка красится в красный, ввод очищается, попап с `reachBody`
- Неправильная цель или пустая клетка → блокировка, только Reset
- Яма → блокировка, только Reset
- Последняя цель → попап с `reachBody` → "Уровень пройден!"

### Клик на цель (до достижения)
- Клик на 3D объект цели → попап с `clickBody` (что это такое за устройство)
- Работает через `TargetInfoTrigger.cs` + `OnMouseDown`

### Сброс
- **Reset** (короткое нажатие) — возврат на последний чекпоинт, восстановление лимитов
- **Full Restart** (удержать Reset 3 секунды) — возврат на самое начало

### Стены
- Задаются как рёбра между клетками (`edgeWalls` в инспекторе GridManager)
- `Vertical` = стена между (x,y) и (x+1,y)
- `Horizontal` = стена между (x,y) и (x,y+1)
- При встрече со стеной — персонаж пропускает ход в эту сторону

### Ямы
- Координаты задаются в `GridManager.pits`
- Отображаются через `pitMaterial` (материал клетки)
- При падении — блокировка, только Reset

### Лимиты команд
- Задаются по чекпоинтам: `GameController.checkpointLimits`
- При исчерпании лимита — кнопка отключается (`SetInteractable(false)`)
- Reset восстанавливает лимиты текущего чекпоинта

### Памятка уровня
- **Авто-показ** при старте уровня (`LevelIntroUI.Start()`)
- Повторный вызов кликом по 3D-объекту `button_Game`
- Показывает `Title` и `Body` из `GameController.levelIntro`

---

## Архитектура — все скрипты

### `GameController.cs` — главный контроллер
**Роль:** управляет состоянием игры, целями, чекпоинтами, лимитами команд.

**Ключевые поля (в инспекторе):**
- `start` — стартовая клетка персонажа
- `targetPrefabs` / `targetCells` — список целей и их координаты
- `checkpointLimits` — лимиты команд по чекпоинтам (массив CommandLimits)
- `levelIntro` — `LevelIntroInfo`: `levelId`, `title`, `body` (памятка уровня)
- `checkpointDescriptions` — массив `CheckpointInfo` для каждой цели

**Структура `CheckpointInfo`:**
- `title` — название устройства
- `clickBody` — краткое описание (при клике на объект до достижения)
- `reachBody` — развёрнутое описание (при достижении цели)

**Ключевые методы:**
- `TryConsume(Command cmd)` — попытка потратить лимит команды
- `GetRemaining(Command cmd)` — сколько осталось
- `GetCheckpointInfo(int index)` — вернуть CheckpointInfo по индексу цели
- `BeginRun()` — вызывается перед запуском команд
- `ResetLevel()` — сброс к последнему чекпоинту
- `FullRestart()` — полный сброс к началу

**Enum RunOutcome:** `None`, `ReachedTarget`, `FellIntoPit`, `Mistake`, `AllCompleted`

---

### `GridManager.cs` — поле из клеток
**Роль:** генерирует тайлы, стены, ямы. Предоставляет координатные методы.

**Ключевые поля (в инспекторе):**
- `Width = 8`, `Height = 8`, `CellSize = 1` — размер сетки
- `Origin = (0,0,0)` — начало координат поля
- `tilePrefab` — префаб тайла
- `wallPrefab` — префаб стены
- `edgeWalls` — массив стен (EdgeWall: cell + orientation Vertical/Horizontal)
- `pits` — массив координат ям
- `pitMaterial` — материал для клеток-ям
- `buildInEditMode` — **держать выключенным** (иначе тайлы накапливаются в edit mode)
- `tilesRoot / wallsRoot / propsRoot` — не назначать, создаются автоматически

**ВАЖНО — что убрано из GridManager:**
`goalPrefab`, `startMarkerPrefab`, `start`, `goal` — удалены. Спавн целей и персонажа — только через `GameController`.

**Ключевые методы:**
- `IsBlockedByWall(Vector2Int from, Vector2Int to)`
- `IsPit(Vector2Int cell)`
- `SetCellColor(Vector2Int cell, Color color)` — красит клетку (чекпоинты)
- `ResetTileColors()`
- `CellToWorld(Vector2Int cell)`
- `Build()` — (пере)строить поле

---

### `CommandStation.cs` — контроллер ввода ⭐
**Роль:** центральный мозг для 3D-кнопок. Управляет очередью команд, блокировкой, логами.

**Поля (назначать в инспекторе):**
- `game` — ссылка на `GameController` ← **ОБЯЗАТЕЛЬНО**
- `commandsDisplay` — `TextMeshPro` (3D) для очереди команд
- `logsDisplay` — `TextMeshPro` (3D) для логов
- `commandButtons` — массив `PhysicalButton3D` всех кнопок команд
- `runButton` / `resetButton` — кнопки start и reboot
- `checkpointPopup` — ссылка на `CheckpointPopupUI` ← **ОБЯЗАТЕЛЬНО для попапов**

**Поведение при достижении цели:**
- `ReachedTarget` → показывает `checkpointPopup` с `reachBody`, после закрытия разблокирует ввод
- `AllCompleted` → показывает `checkpointPopup` с `reachBody` последней цели, после закрытия → "Уровень пройден!"

---

### `CheckpointPopupUI.cs` — попап при достижении цели ⭐
**Роль:** показывается автоматически когда игрок достигает цели. Блокирует ввод до закрытия.

**Файл:** `Assets/Scripts/CheckpointPopupUI.cs`

**Поля (назначать в инспекторе):**
- `panel` — GameObject панели
- `titleText` / `bodyText` — TextMeshProUGUI
- `closeButton` — кнопка закрытия

**Важно:** `CheckpointPanel` должен быть **выключен** (inactive) по умолчанию в Hierarchy.

**Публичные:**
- `Show(string title, string body)` — показать попап
- `Hide()` — скрыть и вызвать `OnClosed` коллбэк
- `OnClosed` — `System.Action`, устанавливается из `CommandStation` перед `Show()`

---

### `TargetInfoTrigger.cs` — клик на цель ⭐
**Роль:** вешается на префаб цели. При клике показывает `clickBody` из `checkpointDescriptions`.

**Файл:** `Assets/Scripts/TargetInfoTrigger.cs`

**Как работает:** `OnMouseDown` → `FindObjectOfType<GameController>()` + `FindObjectOfType<CheckpointPopupUI>(true)` → `popup.Show(info.title, info.clickBody)`

**Требования:** на объекте должен быть Collider (не Trigger).

**Примечание:** `FindObjectOfType<CheckpointPopupUI>(true)` — `true` обязателен, иначе не найдёт неактивный объект.

---

### `LevelLoader.cs` — переключение уровней ⭐
**Роль:** загружает сцены по имени или по индексу в Build Settings.

**Файл:** `Assets/Scripts/LevelLoader.cs`

**Публичные методы:**
- `Load(string sceneName)` — загрузить сцену по имени
- `LoadNext()` — следующая сцена по buildIndex
- `Reload()` — перезагрузить текущую сцену

**Использование:** вешается на GameObject `LevelLoader` внутри префаба `LevelButtons`. PhysicalButton3D.onClick → `LevelLoader.Load("Level_0")`.

---

### `LevelButtons` (Prefab) ⭐
**Роль:** набор 3D кнопок навигации по уровням. Общий префаб для всех сцен.

**Файл:** `Assets/Prefabs/LevelButtons.prefab`

**Структура:**
```
LevelButtons (префаб)
├── LevelLoader (GameObject с LevelLoader.cs)
├── LvlBtn_Test (PhysicalButton3D → LevelLoader.Load("Level_Test"))
├── LvlBtn_0    (PhysicalButton3D → LevelLoader.Load("Level_0"))
├── LvlBtn_1    (PhysicalButton3D → LevelLoader.Load("Level_1"))
└── ...
```

**Обновление:** чтобы добавить новый уровень — открой префаб через "Open Prefab", добавь кнопку, сохрани. Изменение применится ко всем сценам автоматически.

---

### `PhysicalButton3D.cs` — универсальная 3D кнопка ⭐
**Роль:** вешается на любой 3D-объект с коллайдером. Анимация нажатия + вызов действия.

**Поля:**
- `pressDepth = 0.08f` — глубина нажатия
- `animSpeed = 20f` — скорость анимации
- `onClick` — UnityEvent при клике
- `onHold` — UnityEvent при удержании
- `holdTime = 3f` — через сколько секунд = удержание

**Требования:** Collider (не Trigger) на объекте.
**Публичный метод:** `SetInteractable(bool)` — блокирует/разблокирует кнопку.

---

### `LevelIntroUI.cs` — попап памятки уровня
**Роль:** авто-показ при старте уровня + повторный вызов через button_Game.

**Поведение:** `Start()` → `Hide()` → `Show()` (если `game.HasLevelIntro()`).

**Поля:** `game`, `panel`, `titleText`, `bodyText`, `closeButton`

---

### `LevelIntroTrigger.cs` — триггер памятки
`OnMouseDown` → `ui.Show()`. Вешается на `button_Game`.

---

### `PlayerAgent.cs` — движение персонажа
- `Run(List<Command> commands)` — запустить выполнение
- `OnRunFinished` — событие по завершении
- `OnCellChanged` — событие при каждом шаге
- `IsBusy` — true пока выполняются команды

---

### `TargetPoint.cs` — компонент цели
`index` — порядковый номер цели. `SetCompleted(bool)` — деактивирует визуально.

---

## Дизайн-концепция сцены

Игрок видит **рабочий стол в мастерской** — вид сверху-сбоку. Фон — синяя чертёжная бумага (blueprint). Всё физическое, 3D. VHS-эффект: scanlines поверх сцены.

### Элементы сцены и их статус

| Элемент | Статус | Описание |
|---|---|---|
| `BackGround` | ✅ | Plane + Table_Mat (синий фон) |
| `gameboy` | ✅ | 3D корпус устройства (Blender FBX) |
| `start` | ✅ | PhysicalButton3D → TriggerRun |
| `reboot` | ✅ | PhysicalButton3D → TriggerReset / TriggerFullRestart |
| `logs_Game` | ✅ | 3D дисплей логов (logsDisplay) |
| `button_Game` | ✅ | Памятка уровня (LevelIntroTrigger) |
| Клавиатура | ✅ | 5 кнопок + экран команд (commandsDisplay) |
| `LevelButtons` | ✅ | Префаб кнопок уровней (LevelLoader) |
| Canvas попапы | ✅ | LevelIntroPanel + CheckpointPanel |
| Провода | ❌ | Blender, соединяют устройства |
| VHS-оверлей | ❌ | Canvas scanlines + TextMeshPro с номером уровня |
| Робот-подсказчик | ❌ | Опционально, в самом конце |

---

## Текущее состояние

### Что сделано (вся логика готова)

**Геймплей:**
- [x] 3D клавиатура + кнопки start/reboot — работают
- [x] Команды Up/Down/Left/Right/While — работают
- [x] Стены, ямы, лимиты команд — работают
- [x] Reset / Full Restart — работают

**Образовательная система (полностью):**
- [x] `LevelIntroUI` — авто-показ при старте + кнопка повторного вызова
- [x] `CheckpointPopupUI` — попап при достижении цели (reachBody), блокирует ввод до закрытия
- [x] `TargetInfoTrigger` — клик на цель показывает clickBody
- [x] `AllCompleted` — сначала попап reachBody, потом "Уровень пройден!"
- [x] `GameController.checkpointDescriptions` — массив с title/clickBody/reachBody на каждую цель

**Уровни и навигация:**
- [x] `LevelLoader.cs` — Load / LoadNext / Reload
- [x] `LevelButtons` (Prefab) — кнопки TEST/0/1/2 с PhysicalButton3D
- [x] Сцены: `Level_0` (туториал), `Level_Test` (тестовая)

**Графика:**
- [x] URP: MSAA 4x, тени, SSAO, Bloom, Vignette, Contrast +25

**Очистка:**
- [x] GridManager — убраны лишние поля (goalPrefab, startMarkerPrefab, start, goal)
- [x] UICommandBuilder — удалён

### Что НЕ сделано

**Следующая сессия начинается с:**
> Дизайн пазлов уровней — расставить цели, стены, ямы на поле. Начать с Level_0 (туториал, 1 цель) и Level_1 (3 цели, без препятствий).

1. **Дизайн пазлов** ← **НАЧАТЬ ОТСЮДА**
   - Level_0: 1 цель, нет стен/ям — придумать интересный маршрут
   - Level_1: 3 цели (БД → Сервер → Коммутатор), нет препятствий
   - Level_2: 3 цели + стены (файрволы)
   - Level_3: 3 цели + стены + ямы (потеря пакетов)

2. **Образовательные тексты** — заполнить LevelIntroInfo и checkpointDescriptions для каждого уровня (Level_0 уже частично заполнен тестовыми текстами)

3. **Создать сцены Level_1, Level_2, Level_3** — дублированием Level_0, добавить в Build Settings

4. **VHS-оверлей** — Canvas (Screen Space Overlay): scanlines + TextMeshPro с номером уровня

5. **Провода** — Blender, соединяют gameboy / клавиатуру / logs_Game

6. **Робот-подсказчик** — опционально

---

## Технические заметки

### Как работает OnMouseDown
`OnMouseDown` / `OnMouseEnter` / `OnMouseExit` — Unity вызывает автоматически при попадании луча от камеры. Работает в WebGL без Physics Raycaster.

**Требования:** Collider (не Trigger) на объекте, объект виден камере.

### Попапы (Canvas)
Оба попапа — дети Canvas (Screen Space Overlay):
- `LevelIntroPanel` — LevelIntroUI компонент
- `CheckpointPanel` — CheckpointPopupUI компонент, **должен быть inactive по умолчанию**

Структура CheckpointPanel:
```
CheckpointPanel (inactive по умолчанию)
└── Window
    ├── TitleText (TextMeshProUGUI)
    ├── BodyText (TextMeshProUGUI)
    └── CloseButton
```

### FindObjectOfType и неактивные объекты
`FindObjectOfType<T>(true)` — **true обязателен** для поиска среди неактивных объектов.
Используется в `TargetInfoTrigger.Awake()`.

### Дисплеи (commandsDisplay / logsDisplay)
**3D TextMeshPro** объекты (не Canvas). Тип поля в CommandStation — `TextMeshPro` (не `TextMeshProUGUI`).

### Размеры для Blender
- Поле внутри gameboy: **8×8 юнитов**
- Тайлы: 1×1 юнит основание, 0.5 высота
- 1 Unity unit = 1 Blender unit при импорте FBX

### Build Settings (порядок сцен)
```
0: Level_0      ← туториал
1: Level_1
2: Level_2
3: Level_3
4: Level_Test   ← тестовая, в конце
```

### URP настройки (файлы в Assets/Settings/)
- **PC_RPAsset.asset**: MSAA 4x, ShadowDistance 20, SoftShadows High, NormalBias 0.2
- **PC_Renderer.asset**: SSAO Intensity 1.5, Radius 0.6, High
- **SampleSceneProfile**: Tonemapping ACES, Bloom 0.25, Vignette 0.2, Contrast +25, Saturation +10

---

## Структура файлов

```
Assets/
├── Materials/
│   ├── Character.mat
│   ├── Fish_Mat.mat
│   ├── Pit_Mat.mat
│   ├── Table_Mat.mat
│   └── Wall_mat.mat
├── Prefabs/
│   ├── LevelButtons.prefab  ← кнопки навигации по уровням ⭐
│   ├── button_Game.fbx
│   ├── logs_Game.fbx
│   ├── tile_Game.fbx
│   ├── Character.prefab
│   ├── FloorTile.prefab
│   ├── Wall.prefab
│   └── Fish.prefab
├── Scripts/
│   ├── CommandStation.cs      ← центр управления ⭐
│   ├── PhysicalButton3D.cs    ← универсальная 3D кнопка ⭐
│   ├── CheckpointPopupUI.cs   ← попап при достижении цели ⭐
│   ├── TargetInfoTrigger.cs   ← клик на цель → clickBody ⭐
│   ├── LevelLoader.cs         ← переключение уровней ⭐
│   ├── LevelIntroUI.cs
│   ├── LevelIntroTrigger.cs
│   ├── GameController.cs
│   ├── GridManager.cs
│   ├── PlayerAgent.cs
│   └── TargetPoint.cs
├── Settings/
│   ├── PC_RPAsset.asset
│   ├── PC_Renderer.asset
│   └── SampleSceneProfile.asset
└── Scenes/
    ├── Level_0.unity     ← туториал (1 цель, нет препятствий)
    └── Level_Test.unity  ← тестовая сцена
```

## Иерархия сцены (актуальная)

```
Level_X
├── Main Camera            ← Perspective, X rotation ~60°
├── Directional Light
├── Global Volume          ← URP post-processing
├── GridManager            ← поле 8×8 (buildInEditMode=false)
├── GameController         ← логика игры, тексты уровня
├── Canvas                 ← Screen Space Overlay
│   ├── LevelIntroPanel    ← LevelIntroUI (авто-показ при старте)
│   └── CheckpointPanel    ← CheckpointPopupUI (inactive по умолчанию!)
│       └── Window
│           ├── TitleText
│           ├── BodyText
│           └── CloseButton
├── EventSystem
├── BackGround             ← Plane + Table_Mat
├── gameboy                ← 3D корпус (Blender FBX)
│   ├── start              ← PhysicalButton3D → TriggerRun
│   └── reboot             ← PhysicalButton3D → TriggerReset / TriggerFullRestart
├── Keyboard               ← 3D клавиатура
│   └── Keyboard
│       ├── Up/Down/Left/Right/While ← PhysicalButton3D → CommandStation.Add*
│       └── Display_Commands → Commands (3D TextMeshPro)
├── CommandStation         ← CommandStation.cs
├── logs_Game              ← 3D дисплей логов
├── button_Game            ← LevelIntroTrigger (памятка)
├── LevelButtons           ← Prefab: кнопки навигации по уровням
└── Tiles / Walls / Props  ← генерируется GridManager в Play mode
```

---

## Что сказать пользователю при старте новой сессии

**"Контекст загружен. Вся игровая логика и образовательная система полностью работают.**

**Что готово:** 3D клавиатура, кнопки, дисплеи, попапы (clickBody/reachBody), памятка уровня, переключение уровней через LevelButtons префаб, Level_0 и Level_Test сцены.

**Ближайшая задача — дизайн пазлов:**

Нужно придумать и настроить конкретные уровни — расставить цели, стены, ямы. Начинаем с Level_0 (1 цель, нет препятствий) и Level_1 (3 цели: БД → Сервер → Коммутатор).

В GameController выставляешь `targetCells` (координаты целей), в GridManager — `edgeWalls` (стены) и `pits` (ямы).

Хочешь начнём с дизайна пазлов или сначала создадим сцены Level_1/2/3?"**

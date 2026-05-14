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

**Суть игры:** игрок управляет персонажем на клеточном поле 8×8, вводя последовательность команд (Up, Down, Left, Right, While). Задача — дойти до трёх целей в правильном порядке, используя ограниченный набор команд.

**Образовательная концепция — 3 уровня:**
- Уровень 1 — серверная часть и дата-центр (база данных, сервер приложений, серверный коммутатор)
- Уровень 2 — сеть провайдера и маршрутизация
- Уровень 3 — последняя миля и доступ к пользователю

**Платформа:** WebGL (ПК/браузер)  
**Одна сцена:** `Assets/Scenes/Level_1.unity`  
**Рендер пайплайн:** URP (Universal Render Pipeline) — подтверждено, Global Volume уже есть в сцене  
**Камера:** Perspective (переключили с Orthographic), вид сверху-сбоку ~60° по X  

**В проекте нет:** серверной части, базы данных, авторизации, многосценовой навигации, ScriptableObject, JSON — данные уровня задаются через инспектор.

---

## Игровые механики (подробно)

### Команды
- **Up / Down / Left / Right** — шаг на одну клетку
- **While + направление** — двигаться до упора (стена или граница поля)
- Отображаются компактно: `→×5 ↑×2 ⟳↑×1`
- Ввод через **физические 3D кнопки** (не Canvas UI кнопки)

### Цели (чекпоинты)
- 3 цели, нужно пройти строго по порядку
- Правильная цель → клетка красится в красный, ввод очищается, новые команды доступны
- Неправильная цель или пустая клетка → блокировка, только Reset
- Яма → блокировка, только Reset
- Последняя (3-я) цель → завершение уровня

### Сброс
- **Reset** (короткое нажатие) — возврат на последний чекпоинт, восстановление лимитов
- **Full Restart** (удержать Reset 3 секунды) — возврат на самое начало

### Стены
- Задаются как рёбра между клетками (`edgeWalls` в инспекторе GridManager)
- `Vertical` = стена между (x,y) и (x+1,y)
- `Horizontal` = стена между (x,y) и (x,y+1)
- При встрече со стеной — персонаж пропускает ход в эту сторону, остальные команды продолжаются

### Ямы
- Координаты задаются в `GridManager.pits`
- Отображаются через `pitMaterial` (материал клетки)
- При падении — блокировка, только Reset

### Лимиты команд
- Задаются по чекпоинтам: `GameController.checkpointLimits`
- При исчерпании лимита — кнопка отключается (`SetInteractable(false)`)
- Reset восстанавливает лимиты текущего чекпоинта

### Памятка уровня
- Вызывается кликом по 3D-объекту `button_Game` (через `LevelIntroTrigger`)
- Показывает `Title` и `Body` из `GameController.LevelIntroInfo`

---

## Архитектура — все скрипты

### `GameController.cs` — главный контроллер
**Роль:** управляет состоянием игры, целями, чекпоинтами, лимитами команд.

**Ключевые поля (в инспекторе):**
- `agent` — ссылка на PlayerAgent
- `targetPrefabs` / `targetCells` — список целей и их координаты
- `checkpointLimits` — лимиты команд по чекпоинтам (массив CommandLimits)
- `LevelIntroInfo` — заголовок и текст памятки уровня

**Ключевые методы:**
- `TryConsume(Command cmd)` — попытка потратить лимит команды, возвращает bool
- `GetRemaining(Command cmd)` — сколько раз ещё можно использовать команду
- `BeginRun()` — вызывается перед запуском команд
- `ResetLevel()` — сброс к последнему чекпоинту
- `FullRestart()` — полный сброс к началу
- `HasLevelIntro()` / `GetLevelIntroTitle()` / `BuildLevelIntroBody()` — памятка уровня

**Enum RunOutcome:** `None`, `ReachedTarget`, `FellIntoPit`, `Mistake`, `AllCompleted`  
**Свойства:** `LastRunOutcome`, `LastReachedTargetIndex`

---

### `GridManager.cs` — поле из клеток
**Роль:** генерирует тайлы, стены, ямы. Предоставляет координатные методы.

**Ключевые поля (в инспекторе):**
- `Width = 8`, `Height = 8`, `CellSize = 1` — размер сетки
- `Origin = (0,0,0)` — начало координат поля
- `tilePrefab` — префаб тайла (сейчас FloorTile, планируется tile_Game.fbx)
- `wallPrefab` — префаб стены
- `edgeWalls` — массив стен (EdgeWall: cell + orientation Vertical/Horizontal)
- `pits` — массив координат ям
- `pitMaterial` — материал для клеток-ям
- `buildInEditMode` — если включено, поле перестраивается при изменениях в инспекторе

**Ключевые методы:**
- `IsBlockedByWall(Vector2Int from, Vector2Int to)` — есть ли стена между клетками
- `IsPit(Vector2Int cell)` — является ли клетка ямой
- `SetCellColor(Vector2Int cell, Color color)` — красит клетку (используется для чекпоинтов)
- `ResetTileColors()` — сбрасывает цвета клеток
- `CellToWorld(Vector2Int cell)` — координаты клетки в мировые координаты
- `Build()` — (пере)строить поле

**ВАЖНО — баг исправлен:** `OnValidate` теперь использует `EditorApplication.delayCall` вместо прямого вызова `Build()`, чтобы не было ошибки `DestroyImmediate not permitted`.

**Размеры поля:** 8×8 клеток × cellSize 1 = **8×8 юнитов** в мировом пространстве. Тайлы высотой 0.5 (Scale Y = 0.5).

---

### `CommandStation.cs` — НОВЫЙ контроллер ввода ⭐
**Роль:** центральный мозг для 3D-кнопок. Управляет очередью команд, блокировкой ввода, логами. НЕ зависит от UICommandBuilder.

**Файл:** `Assets/Scripts/CommandStation.cs`

**Поля (назначать в инспекторе):**
- `game` — ссылка на `GameController` ← **ОБЯЗАТЕЛЬНО НАЗНАЧИТЬ**
- `commandsDisplay` — `TextMeshPro` (3D, World Space) для отображения введённых команд на экране клавиатуры
- `logsDisplay` — `TextMeshPro` (3D, World Space) для дисплея логов (logs_Game)
- `commandButtons` — массив `PhysicalButton3D` всех кнопок команд (для блокировки)
- `runButton` — `PhysicalButton3D` кнопки start
- `resetButton` — `PhysicalButton3D` кнопки reboot

**Публичные методы (вызываются из PhysicalButton3D через UnityEvent):**
- `AddUp()`, `AddDown()`, `AddLeft()`, `AddRight()`, `AddWhile()` — добавить команду в очередь
- `TriggerRun()` — запустить выполнение команд
- `TriggerReset()` — сброс к чекпоинту
- `TriggerFullRestart()` — полный рестарт

**Как работает:** при `TriggerRun()` проверяет что очередь не пуста → вызывает `game.BeginRun()` → `game.agent.Run(queue)` → ждёт `OnRunFinished` → выводит результат в `logsDisplay`.

---

### `PhysicalButton3D.cs` — универсальная 3D кнопка ⭐
**Роль:** вешается на любой 3D-объект с коллайдером. Анимация нажатия + вызов действия.

**Файл:** `Assets/Scripts/PhysicalButton3D.cs`

**Поля:**
- `pressDepth = 0.08f` — глубина нажатия (насколько кнопка уходит вниз)
- `animSpeed = 20f` — скорость анимации
- `onClick` — UnityEvent: что вызвать при клике
- `onHold` — UnityEvent: что вызвать при удержании (необязательно)
- `holdTime = 3f` — через сколько секунд считается удержание

**Анимация:** при наведении кнопка чуть поднимается, при нажатии — уходит вниз, при отпускании — возвращается. Всё через Lerp по локальной позиции Y.

**Требования:** на объекте должен быть **Collider** (не Trigger). Без коллайдера `OnMouseDown` не сработает.

**Публичный метод:** `SetInteractable(bool)` — блокирует/разблокирует кнопку.

---

### `LevelIntroUI.cs` — попап памятки уровня ⭐
**Роль:** независимый скрипт для показа окна с описанием уровня. Вешается на Canvas (Screen Space Overlay).

**Поля (назначать в инспекторе):**
- `game` — ссылка на GameController
- `panel` — GameObject панели (показывать/скрывать)
- `titleText` / `bodyText` — TextMeshProUGUI
- `closeButton` — кнопка закрытия

**Публичные методы:**
- `Show()` — вызывается из `LevelIntroTrigger` при клике на `button_Game`
- `Hide()` — закрыть панель (также вешается на closeButton)

---

### `PlayerAgent.cs` — движение персонажа
**Роль:** выполняет команды, двигает персонажа по клеткам, проверяет стены и ямы.

**Ключевые методы:**
- `Run(List<Command> commands)` — запустить выполнение команд
- `MoveUntilBlocked(dir, callback)` — While-команда
- `MoveTo(Vector2Int cell)` — плавное перемещение с вращением

**Callbacks:**
- `OnRunFinished` — событие, когда команды выполнены
- `OnCellChanged` — событие при каждом шаге
- `IsBusy` — true пока выполняются команды

---

### `LevelIntroTrigger.cs` — памятка уровня
**Роль:** вешается на 3D-объект `button_Game` с коллайдером. При клике вызывает `LevelIntroUI.Show()`.

**Как работает:** `OnMouseDown` → `ui.Show()`.

---

### `TargetPoint.cs` — компонент цели
**Роль:** вешается на префаб цели. `SetCompleted(bool)` — деактивирует/реактивирует цель.

---

## Дизайн-концепция сцены (подробно)

### Общая идея
Игрок видит **рабочий стол в мастерской** — вид сверху-сбоку ~35-45°. Фон — синяя чертёжная бумага (blueprint). Всё физическое, 3D. Никаких Canvas UI кнопок для геймплея — только 3D объекты.

VHS-эффект: поверх всей сцены — полупрозрачные scanlines как от камеры наблюдения.

### Элементы сцены и их статус

#### ✅ `BackGround` (Plane + Table_Mat)
Синяя плоскость как поверхность стола. Материал `Table_Mat` уже назначен. Может иметь сетку-blueprint поверх, но пока просто синий.

#### ✅ `gameboy` (3D модель, сделана пользователем в Blender)
Корпус игрового устройства — главный элемент сцены. Внутри него визуально располагается поле (GridManager генерирует тайлы внутри).

**Размеры внутренней полости** должны совпадать с сеткой: **8×8 юнитов**.  
Толщина стенок корпуса: ~0.5-1 юнит.  
Глубина полости: ~2 юнита (тайлы 0.5 высотой + стены + персонаж).

Внизу корпуса две кнопки: `start` и `reboot`.

#### ✅ `start` (3D модель)
`PhysicalButton3D` → `onClick` → `CommandStation.TriggerRun` ✅

#### ✅ `reboot` (3D модель)
`PhysicalButton3D` → `onClick` → `CommandStation.TriggerReset`, `onHold` → `CommandStation.TriggerFullRestart` ✅

#### ✅ `logs_Game` (3D модель FBX, в сцене, дисплей настроен)
Дисплей для игровых логов ("Уровень пройден!", "Неверная клетка. Нажмите Reset." и т.д.).  
3D TextMeshPro (`logsDisplay`) назначен в `CommandStation`. Работает.

#### ✅ `button_Game` (3D объект с LevelIntroTrigger)
Кнопка-"книжка" — вызов памятки уровня. Уже работает.

#### ✅ Клавиатура (3D модель готова, в сцене)
3D устройство с кнопками Up/Down/Left/Right/While. На каждой кнопке — `PhysicalButton3D`, подключён к `CommandStation`.  
Над клавиатурой — экран с 3D TextMeshPro (`commandsDisplay`) — выводит введённые команды.  
Иерархия: `Keyboard` → `Keyboard` → `Up`, `Down`, `Left`, `Right`, `While`.

#### ❌ Провода
Статичные 3D модели, соединяющие gameboy с клавиатурой и logs_Game. Делает пользователь в Blender.

#### ❌ VHS-оверлей (в конце)
UI Canvas (Screen Space Overlay) поверх всей сцены:
- Текстура scanlines (полупрозрачная, горизонтальные полосы)
- Нижний правый угол: TextMeshPro с номером уровня, прогрессом, датой

#### ❌ Кнопки уровней (слева, вертикально)
Несколько 3D кнопок для навигации между уровнями. Делается в конце.

#### ❌ Робот-подсказчик (в самом конце, если получится)
Анимированный 3D объект, подлетающий к игроку с подсказкой. Сложно, fallback — UI всплывающее окно.

---

## Текущее состояние на момент остановки

### Что сделано
- [x] Камера — **Perspective**, вид сверху-сбоку
- [x] Blueprint фон (Plane + `Table_Mat`)
- [x] `PhysicalButton3D.cs` — работает (анимация нажатия, onClick/onHold)
- [x] `CommandStation.cs` — работает, полностью независим от UICommandBuilder
- [x] 3D клавиатура с кнопками Up/Down/Left/Right/While — **в сцене, все кнопки подключены**
- [x] `start` → `TriggerRun`, `reboot` → `TriggerReset` / `TriggerFullRestart`
- [x] Экран команд (3D TextMeshPro) на клавиатуре — **назначен в CommandStation.commandsDisplay**
- [x] Экран логов (3D TextMeshPro) на `logs_Game` — **назначен в CommandStation.logsDisplay**
- [x] `UICommandBuilder` — **удалён**, старый Canvas очищен (остался только LevelIntroPanel)
- [x] `LevelIntroUI.cs` написан, `LevelIntroTrigger` переключён на него
- [x] `button_Game` — памятка уровня работает через `LevelIntroUI`
- [x] `buildInEditMode` выключен — тайлы не накапливаются в edit mode
- [x] Баг `DestroyImmediate` в `GridManager.OnValidate` — исправлен
- [x] **Настройки графики URP улучшены** (файлы в `Assets/Settings/`):
  - MSAA: 1x → **4x** (сглаживание краёв)
  - Shadow Distance: 50 → **20** (чётче тени на маленькой сцене)
  - Shadow Normal Bias: 0.5 → **0.2** (тени не отрываются от объектов)
  - SSAO Intensity: 0.4 → **1.5**, Radius: 0.3 → **0.6**, BlurQuality: Low → **High** (глубина сцены)
  - Color Adjustments в Global Volume: Contrast **+25**, Saturation **+10**

### Что НЕ сделано (следующий фронт работ)

**Следующая сессия начинается с:**
> Заполнить образовательный контент — тексты для `LevelIntroInfo` в GameController (заголовок и описание уровня, что такое сервер, база данных, коммутатор). Это нужно для диплома.

1. **Образовательный контент** ← **НАЧАТЬ ОТСЮДА** — заполнить `LevelIntroInfo` в `GameController` (тексты уровней), настроить 3 цели и их чекпоинт-описания
2. **Провода** — статичные 3D модели в Blender, соединяют gameboy / клавиатуру / logs_Game
3. **VHS-оверлей** — UI Canvas (Screen Space Overlay): scanlines текстура + TextMeshPro с номером уровня внизу справа
4. **Кнопки уровней** — несколько 3D кнопок слева для навигации между уровнями
5. **Робот-подсказчик** — опционально, сложно

---

## Технические заметки

### Как работает OnMouseDown (важно!)
`OnMouseDown`, `OnMouseEnter`, `OnMouseExit`, `OnMouseUp` — Unity автоматически вызывает их на объектах с Collider при попадании луча от камеры. Работает без Physics Raycaster, работает в WebGL.

**Требования:**
- На объекте должен быть Collider (Box, Sphere, Capsule, Mesh — любой)
- Collider НЕ должен быть Trigger (`Is Trigger` = false)
- Камера должна видеть объект (не перекрыт другим Collider'ом)

### Дисплеи (commandsDisplay / logsDisplay)
Используются **3D TextMeshPro** объекты (не Canvas). Создаются через Hierarchy → 3D Object → Text - TextMeshPro, позиционируются прямо на поверхности 3D моделей. Тип поля в `CommandStation` — `TextMeshPro` (не `TextMeshProUGUI`).

### Размеры для Blender
- Поле внутри gameboy: **8×8 юнитов** (ширина и глубина)
- Тайлы: 1×1 юнит в основании, 0.5 юнит высота
- 1 Unity unit ≈ 1 Blender unit при стандартном импорте FBX

### URP и WebGL
- URP полностью совместим с WebGL
- Global Volume уже есть в сцене — можно добавлять post-processing эффекты
- World Space Canvas работает в WebGL без проблем
- `OnMouseDown` работает в WebGL

### Настройки графики URP (текущие, файлы в Assets/Settings/)
- **PC_RPAsset.asset** — главный URP ассет для PC:
  - `m_MSAA: 4` — 4x сглаживание
  - `m_ShadowDistance: 20` — дистанция теней
  - `m_MainLightShadowmapResolution: 2048` — разрешение теней
  - `m_SoftShadowsSupported: 1`, `m_SoftShadowQuality: 3` — мягкие тени High
  - `m_ShadowNormalBias: 0.2` — тени не отрываются от объектов
- **PC_Renderer.asset** — SSAO включён:
  - `Intensity: 1.5`, `Radius: 0.6`, `Samples: 2` (High), `BlurQuality: 2`
- **Global Volume** в сцене (SampleSceneProfile): Tonemapping ACES, Bloom 0.25, Vignette 0.2, Color Adjustments Contrast +25 Saturation +10

---

## Структура файлов

```
Assets/
├── Materials/
│   ├── Character.mat
│   ├── Fish_Mat.mat
│   ├── Ground_...mat
│   ├── Pit_Mat.mat
│   ├── Table_Mat.mat    ← материал стола/фона (синий)
│   └── Wall_mat.mat
├── Prefabs/
│   ├── button_Game.fbx  ← 3D модель кнопки-книжки (памятка уровня)
│   ├── logs_Game.fbx    ← 3D модель дисплея логов
│   ├── tile_Game.fbx    ← 3D модель тайла (можно назначить в GridManager)
│   ├── Character.prefab
│   ├── FloorTile.prefab ← текущий тайл (Cube, Scale Y=0.5)
│   ├── Wall.prefab
│   └── Fish.prefab      ← текущий префаб цели
├── Scripts/
│   ├── CommandStation.cs    ← центр управления 3D кнопками ⭐
│   ├── PhysicalButton3D.cs  ← универсальная 3D кнопка ⭐
│   ├── LevelIntroUI.cs      ← попап памятки уровня ⭐
│   ├── LevelIntroTrigger.cs ← триггер клика на button_Game
│   ├── GameController.cs
│   ├── GridManager.cs
│   ├── PlayerAgent.cs
│   └── TargetPoint.cs
├── Settings/
│   ├── PC_RPAsset.asset     ← URP настройки (MSAA, тени, SSAO)
│   ├── PC_Renderer.asset    ← SSAO renderer feature
│   └── SampleSceneProfile.asset ← Global Volume профиль
└── Scenes/
    └── Level_1.unity
```

## Иерархия сцены (текущая)

```
Level_1
├── Main Camera          ← Perspective, X rotation ~60°
├── Directional Light
├── Global Volume        ← URP post-processing (SampleSceneProfile)
├── GridManager          ← генерирует поле 8×8 (buildInEditMode=false)
├── GameController       ← логика игры
├── Canvas               ← только LevelIntroPanel (Screen Space Overlay)
│   └── LevelIntroPanel  ← попап памятки уровня (LevelIntroUI компонент)
├── EventSystem
├── Tiles / Walls / Props ← генерируется GridManager (пусто в edit mode)
├── button_Game          ← 3D кнопка памятки уровня (LevelIntroTrigger)
├── BackGround           ← Plane с Table_Mat (синий фон-blueprint)
├── gameboy              ← 3D корпус устройства (Blender FBX)
│   ├── start            ← PhysicalButton3D → CommandStation.TriggerRun
│   └── reboot           ← PhysicalButton3D → TriggerReset / TriggerFullRestart
├── Keyboard             ← 3D клавиатура (Blender FBX)
│   └── Keyboard
│       ├── Up / Down / Left / Right / While ← PhysicalButton3D → CommandStation.Add*
│       └── Display_Commands → Commands (3D TextMeshPro = commandsDisplay)
├── CommandStation       ← GameObject с CommandStation.cs
└── logs_Game            ← 3D дисплей логов (LogsText = 3D TextMeshPro = logsDisplay)
```

---

## Что сказать пользователю при старте новой сессии

Когда пользователь попросит прочитать CLAUDE.md и продолжить — скажи примерно следующее:

**"Контекст загружен. Игровая механика полностью работает: 3D клавиатура, кнопки, дисплеи, тени, SSAO — всё настроено.**

**Ближайшая задача — образовательный контент:**

Нужно заполнить тексты для памятки уровня. В Unity:
1. Выбери `GameController` в Hierarchy
2. В Inspector найди поле `Level Intro Info`
3. Заполни `Title` (например: "Уровень 1 — Серверная часть")
4. Заполни `Body` — описание уровня (что такое сервер, база данных, коммутатор)

Также нужно расставить 3 цели на поле и назначить им координаты в `GameController.targetCells`.

Хочешь начнём с текстов или сначала разберёмся с расстановкой целей?"**

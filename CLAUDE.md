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

### `UICommandBuilder.cs` — СТАРЫЙ контроллер (будет удалён)
**Роль:** старая UI система на Canvas. Пока работает параллельно, будет удалена когда CommandStation полностью возьмёт на себя всю логику.

**Добавлены публичные методы** (для временного использования):
`AddUp()`, `AddDown()`, `AddLeft()`, `AddRight()`, `AddWhile()`, `TriggerRun()`, `TriggerReset()`, `TriggerFullRestart()`

**Когда удалять:** после того как 3D клавиатура готова и CommandStation полностью работает.

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
**Роль:** вешается на 3D-объект `button_Game` с коллайдером. При клике показывает памятку уровня.

**Как работает:** `OnMouseDown` → raycast от камеры → вызов `UICommandBuilder.ShowLevelIntro()`.

**Примечание:** когда UICommandBuilder будет удалён, этот скрипт нужно переключить на новый показ памятки.

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

#### ✅ `start` (3D модель, сделана пользователем)
Кнопка запуска выполнения команд. Уже есть в сцене.  
Нужно: `PhysicalButton3D` → `onClick` → `CommandStation.TriggerRun`

#### ✅ `reboot` (3D модель, сделана пользователем)
Кнопка сброса. Уже есть в сцене.  
Нужно: `PhysicalButton3D` → `onClick` → `CommandStation.TriggerReset`, `onHold` → `CommandStation.TriggerFullRestart`

#### ✅ `logs_Game` (3D модель FBX, уже в сцене)
Дисплей для игровых логов ("Уровень пройден!", "Неверная клетка. Нажмите Reset." и т.д.).  
Находится внизу-справа от геймбоя, соединён проводами.  
Нужно: добавить World Space Canvas с TextMeshPro поверх экрана.

#### ✅ `button_Game` (3D объект с LevelIntroTrigger)
Кнопка-"книжка" — вызов памятки уровня. Уже работает.

#### ❌ Клавиатура (ещё не сделана)
3D устройство с ~8 кнопками команд. Находится сверху-справа от геймбоя, соединено проводами.  
На клавиатуре: кнопки Up/Down/Left/Right/While + возможно дополнительные.  
Над клавиатурой: экран для отображения введённых команд.  
**Пользователь делает 3D модель в Blender.**  
**Временное решение:** 5 примитивов (Cylinder) как заглушки.

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
- [x] Камера переключена на **Perspective**
- [x] Blueprint фон (Plane + `Table_Mat`)
- [x] `PhysicalButton3D.cs` написан и работает (анимация нажатия подтверждена)
- [x] `CommandStation.cs` написан (независимый от UICommandBuilder)
- [x] Публичные методы добавлены в `UICommandBuilder.cs` для совместимости
- [x] Баг `DestroyImmediate` в `GridManager.OnValidate` — **исправлен** (через `EditorApplication.delayCall`)
- [x] 3D модели `gameboy`, `reboot`, `start`, `logs_Game` — **созданы в Blender и добавлены в сцену**
- [x] `CLAUDE.md` создан для переноса контекста между машинами

### Что НЕ сделано (текущий фронт работ)

**Ближайшее — нужно сделать прямо сейчас:**

1. **Создать 5 временных кнопок команд** (пока 3D клавиатура не готова):
   - В Hierarchy → Create Empty → назови `Keyboard_Placeholder`
   - Внутри создай 5 × Cylinder: `btn_Up`, `btn_Down`, `btn_Left`, `btn_Right`, `btn_While`
   - На каждый: Add Component → `PhysicalButton3D`
   - Убедись что на каждом есть Collider (у Cylinder есть Capsule Collider — ок)

2. **Назначить onClick на каждой кнопке** (инспектор → PhysicalButton3D → onClick → +):
   - `btn_Up` → перетащи объект `CommandStation` → выбери `CommandStation.AddUp`
   - `btn_Down` → `CommandStation.AddDown`
   - `btn_Left` → `CommandStation.AddLeft`
   - `btn_Right` → `CommandStation.AddRight`
   - `btn_While` → `CommandStation.AddWhile`

3. **Назначить start и reboot** на CommandStation (инспектор → PhysicalButton3D):
   - `start` → `onClick` → `CommandStation.TriggerRun`
   - `reboot` → `onClick` → `CommandStation.TriggerReset`
   - `reboot` → `onHold` → `CommandStation.TriggerFullRestart`, `holdTime = 3`

4. **Назначить game в CommandStation** (инспектор → CommandStation → поле Game → перетащи GameController)

5. **Проверить** что всё работает: нажать несколько btn_Up/btn_Right → нажать start → персонаж должен пойти

**После того как это заработает:**

6. Сделать реальную 3D клавиатуру в Blender (корпус + 8 кнопок)
7. Импортировать, поставить в сцену справа-сверху от gameboy
8. Перенести `PhysicalButton3D` со старых Cylinder на новые 3D кнопки
9. Удалить `Keyboard_Placeholder`
10. Добавить World Space Canvas на экран клавиатуры (TextMeshPro для команд)
11. Добавить World Space Canvas на `logs_Game` (TextMeshPro для логов)
12. Назначить `commandsDisplay` и `logsDisplay` в инспекторе CommandStation
13. **Удалить** `UICommandBuilder` компонент с Canvas и все старые UI-кнопки
14. Сделать провода (3D модели в Blender)
15. VHS-оверлей (UI Canvas Overlay + scanlines текстура + TextMeshPro инфо)
16. Кнопки уровней (слева, вертикально)
17. Образовательный контент (тексты уровней, сообщения чекпоинтов)

---

## Технические заметки

### Как работает OnMouseDown (важно!)
`OnMouseDown`, `OnMouseEnter`, `OnMouseExit`, `OnMouseUp` — Unity автоматически вызывает их на объектах с Collider при попадании луча от камеры. Работает без Physics Raycaster, работает в WebGL.

**Требования:**
- На объекте должен быть Collider (Box, Sphere, Capsule, Mesh — любой)
- Collider НЕ должен быть Trigger (`Is Trigger` = false)
- Камера должна видеть объект (не перекрыт другим Collider'ом)

### Как добавить World Space Canvas (для logs_Game и экрана клавиатуры)
1. В Hierarchy выбери объект (например `logs_Game`) → правая кнопка → UI → Canvas
2. В инспекторе Canvas → `Render Mode` → **World Space**
3. Подстрой размер и позицию Canvas чтобы совпадал с экраном 3D модели
4. Внутри Canvas: правая кнопка → UI → Text - TextMeshPro
5. На этот TextMeshPro-объект ссылаться из CommandStation как `logsDisplay` (но тип будет `TextMeshProUGUI`, не `TextMeshPro`)

**Примечание:** `TextMeshPro` (3D) и `TextMeshProUGUI` (Canvas) — разные компоненты! World Space Canvas использует `TextMeshProUGUI`. Нужно поменять тип поля в `CommandStation.cs` с `TextMeshPro` на `TextMeshProUGUI`.

### Размеры для Blender
- Поле внутри gameboy: **8×8 юнитов** (ширина и глубина)
- Тайлы: 1×1 юнит в основании, 0.5 юнит высота
- 1 Unity unit ≈ 1 Blender unit при стандартном импорте FBX

### URP и WebGL
- URP полностью совместим с WebGL
- Global Volume уже есть в сцене — можно добавлять post-processing эффекты
- World Space Canvas работает в WebGL без проблем
- `OnMouseDown` работает в WebGL

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
│   ├── CommandStation.cs    ← НОВЫЙ ⭐
│   ├── PhysicalButton3D.cs  ← НОВЫЙ ⭐
│   ├── GameController.cs
│   ├── GridManager.cs
│   ├── UICommandBuilder.cs  ← СТАРЫЙ, будет удалён
│   ├── PlayerAgent.cs
│   ├── LevelIntroTrigger.cs
│   └── TargetPoint.cs
└── Scenes/
    └── Level_1.unity
```

## Иерархия сцены (текущая)

```
Level_1
├── Main Camera          ← Perspective, X rotation ~60°
├── Directional Light
├── Global Volume        ← URP post-processing
├── GridManager          ← генерирует поле 8×8
├── GameController       ← логика игры
├── Canvas               ← СТАРЫЙ UI (будет удалён)
│   ├── CommandInput
│   ├── FeedBack_Image
│   ├── RunButton        ← заменяется на 3D кнопку start
│   ├── ResetButton      ← заменяется на 3D кнопку reboot
│   ├── Up/Down/Left/Right/While  ← заменяются на 3D кнопки клавиатуры
│   └── LevelIntroPanel
├── EventSystem
├── logs_Game            ← 3D дисплей логов (нужен World Space Canvas)
├── Tiles                ← генерируется GridManager
├── Walls                ← генерируется GridManager
├── Props                ← генерируется GridManager
├── button_Game          ← 3D кнопка памятки уровня (уже работает)
├── BackGround           ← Plane с Table_Mat
├── gameboy              ← 3D корпус устройства
├── reboot               ← 3D кнопка Reset (нужен PhysicalButton3D)
└── start                ← 3D кнопка Run (нужен PhysicalButton3D)
```

---

## Что сказать пользователю при старте новой сессии

Когда пользователь попросит прочитать CLAUDE.md и продолжить — скажи примерно следующее:

**"Контекст загружен. Мы разрабатываем 3D механический интерфейс для игры. На чём остановились:**

**Ближайшая задача — заставить кнопки работать:**
1. Создай 5 Cylinder в Hierarchy (`btn_Up`, `btn_Down`, `btn_Left`, `btn_Right`, `btn_While`)
2. На каждый — Add Component → `PhysicalButton3D` + назначь `onClick` → `CommandStation.AddUp/Down/Left/Right/AddWhile`
3. На `start` → `PhysicalButton3D.onClick` → `CommandStation.TriggerRun`
4. На `reboot` → `PhysicalButton3D.onClick` → `CommandStation.TriggerReset`, `onHold` → `TriggerFullRestart`
5. В инспекторе `CommandStation` → поле `Game` → перетащи `GameController`

После этого нажми несколько кнопок команд → нажми `start` → персонаж должен пойти. Проверяем?"**

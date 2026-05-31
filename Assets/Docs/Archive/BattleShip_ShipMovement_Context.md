---
project: BattleShip
engine: Unity 6
language: ru
created_at: 2026-04-26
context_purpose: "Restore design context for ship movement, propulsion, rudder, maneuvering, heel, and visual controllers in a future ChatGPT session."
current_architecture_stage: "Kinematic ship controller with engine telegraph inertia, rudder target/actual separation, turning radius model, rudder drag, visual heel, wave motion separation, visual propellers and rudders."
important_note_for_next_session: "Use this file as the source of truth for the current design direction. The project favors gameplay-tunable kinematic systems over full Rigidbody naval physics."
---

# BattleShip — контекст по системе движения корабля

Этот файл фиксирует текущие решения по управлению кораблём в Unity 6. Его можно приложить или вставить в следующей сессии, чтобы быстро восстановить контекст.

## 1. Общая цель

Игра про корабли в духе World of Warships. Сейчас прорабатывается система движения большого корабля Yamato-like:

- машинный телеграф управляет режимом двигателя;
- корпус движется по инерции;
- руль имеет командное и фактическое положение;
- поворот идёт через радиус циркуляции, а не через простой `turnSpeed`;
- полный руль влияет на скорость через дополнительное сопротивление;
- корпус визуально кренится при манёвре;
- перья руля и винты имеют отдельные visual controllers;
- качка от волн и крен от манёвра должны работать на разных `Transform`-уровнях.

Архитектурный подход: **кинематическая игровая модель**, а не полноценная физика через массу, Rigidbody, тягу, воду и drag coefficients.

---

# 2. Работа двигателя — какие варианты обсуждались

## Вариант 1 — простой `targetSpeed`

Машинный телеграф напрямую задаёт целевую скорость корпуса:

```text
Full Ahead  → targetSpeed = +27 knots
Stop        → targetSpeed = 0
Full Astern → targetSpeed = -8 knots
```

`ShipMovementController` просто двигает `currentSpeed` к `targetSpeed` через acceleration/deceleration.

### Почему не выбрали

`Stop` превращается в тормоз, а для корабля это неверно. Если двигатели остановлены, корпус всё равно долго идёт по инерции.

---

## Вариант 2 — режим двигателя + инерция корпуса

Это выбранный вариант.

Машинный телеграф задаёт не мгновенную скорость корпуса, а **режим работы двигателя**:

```text
Flank Ahead / Full Ahead / Half Ahead / Slow Ahead → тяга вперёд
Stop                                             → тяги нет
Slow Astern / Full Astern                       → тяга назад
```

Корпус имеет свою фактическую скорость, которая меняется через:

- разгон вперёд;
- разгон назад;
- естественный выбег;
- активное торможение обратной тягой.

### Универсальная логика

Не используется таблица всех переходов режимов. Каждый кадр смотрим только на:

```text
currentSpeedKnots
engineOrderSpeedKnots
```

Правила:

```text
1. engineOrderSpeed == 0
   → выбег к нулю через Natural Coasting Deceleration.

2. currentSpeed == 0
   → разгон в сторону выбранного engine order.

3. currentSpeed и engineOrderSpeed одного знака
   → если currentSpeed ниже orderSpeed: разгон;
   → если currentSpeed выше orderSpeed: мягкий выбег / drag.

4. currentSpeed и engineOrderSpeed разных знаков
   → активное торможение обратной тягой;
   → после перехода через 0 — разгон в сторону нового режима.
```

### Примеры

```text
Full Ahead → Stop
    Двигатель больше не даёт тягу, корабль долго идёт по инерции.

Flank Ahead → Full Ahead
    Оба режима вперёд. Активного торможения нет.
    Корабль мягко сбрасывает лишнюю скорость через Natural Coasting Deceleration.

Full Ahead → Full Astern
    Направление тяги противоположно текущей скорости.
    Используется Opposite Thrust Deceleration.
    Скорость идёт: +speed → 0 → -speed.

Slow Ahead с нуля
    Обычный разгон до скорости режима Slow Ahead.
```

### Выбранные параметры двигателя в `ShipConfig`

```text
Forward Acceleration Knots Per Second
Reverse Acceleration Knots Per Second
Natural Coasting Deceleration Knots Per Second
Opposite Thrust Deceleration Knots Per Second
```

Пример стартовых значений для Yamato-like:

```text
Forward Acceleration Knots Per Second = 1.0
Reverse Acceleration Knots Per Second = 0.6
Natural Coasting Deceleration Knots Per Second = 0.05
Opposite Thrust Deceleration Knots Per Second = 0.6
```

---

## Вариант 3 — физическая модель через массу, тягу и сопротивление

Обсуждалась формула:

```text
acceleration = (engineThrust - hullDrag - rudderDrag) / shipMass
```

Для этого потребовались бы параметры:

```text
displacement / mass
forward thrust
reverse thrust
hull drag coefficient
rudder drag coefficient
propeller efficiency
water density / scale multipliers
```

### Почему пока не выбрали

Для игры это слишком сложно настраивать. Масса сама по себе не заменяет параметры разгона и торможения. Чтобы получить нужное поведение, всё равно пришлось бы подбирать тягу, сопротивление и коэффициенты.

Решение: пока оставить gameplay-параметры ускорения/выбега/торможения. Массу или водоизмещение можно позже добавить как информационную характеристику корабля или для столкновений/тарана/повреждений.

---

# 3. Работа руля — какие варианты обсуждались

## Вариант 1 — простой поворот по `turnSpeed`

```text
rudder value → turnSpeed → transform.Rotate
```

### Почему не выбрали

Слишком аркадно. Корабль ощущается как машина на воде, а не как большое судно.

---

## Вариант 2 — радиус циркуляции

Это выбранный вариант.

Руль и скорость дают целевой радиус циркуляции:

```text
speed + rudder
→ rudder effectiveness
→ speed effectiveness
→ turn effect
→ turning radius
→ target yaw rate
→ current yaw rate
→ yaw rotation
```

Поворот не применяется мгновенно. Есть `currentYawRate`, который плавно стремится к `targetYawRate`.

### Основные параметры манёвренности в `ShipConfig`

```text
Ship Length
Minimum Turning Radius In Ship Lengths
Maximum Turning Radius In Ship Lengths
Rudder To Turn Effectiveness
Speed To Rudder Effectiveness
Turn Acceleration
Turn Rate Multiplier
```

Пример стартовых значений для Yamato-like:

```text
Ship Length = 263
Minimum Turning Radius In Ship Lengths = 4.5
Maximum Turning Radius In Ship Lengths = 20
Turn Acceleration = 0.65–0.75
Turn Rate Multiplier = 1.5
```

`ShipLength` оставлен специально: радиус циркуляции удобнее задавать в длинах корпуса, а не в голых Unity units.

---

## Вариант 3 — Target Rudder и Actual Rudder

Это выбранный вариант.

Руль разделён на два состояния:

```text
Target Rudder Signed Value
    команда игрока / команда рулевого телеграфа

Actual Rudder Signed Value
    фактическое положение пера руля
```

Логика:

```text
игрок нажал клавишу руля
→ Target меняется сразу
→ UI-ползунок идёт к Target со своей visual speed
→ Actual Rudder догоняет Target через Rudder Shift Time
→ ShipMovementController использует Actual Rudder для поворота
```

### Важный выбор

Для физики и движения используется **ActualRudderSignedValue**, не Target.

Для UI-ползунка руля используется **TargetRudderSignedValue**, чтобы он работал как машинный телеграф: игрок сразу видит заданную команду, а реальный руль догоняет её позже.

Параметр:

```text
Rudder Shift Time From Center To Full
```

Пример:

```text
Rudder Shift Time From Center To Full = 6 seconds
```

---

## Вариант 4 — визуальные перья руля

Добавлен отдельный visual controller:

```text
ShipRudderVisualController
```

Он читает:

```text
ShipStatuses.ActualRudderSignedValue
```

и поворачивает один или несколько rudder pivot-объектов. Для Yamato поддерживаются два руля.

Максимальный угол пера руля вынесен в общий `ShipConfig`:

```text
Max Rudder Angle Degrees
```

Пример:

```text
Max Rudder Angle Degrees = 35
```

Важно: в массив `Rudders` нужно назначать не сам mesh, а pivot-объект, расположенный на оси вращения пера руля.

Пример иерархии:

```text
ShipVisualRoot
 └── Rudders
      ├── RudderPivot_L
      │    └── RudderMesh_L
      └── RudderPivot_R
           └── RudderMesh_R
```

`angleMultiplier` в элементах массива должен быть `1`, не `0`. Если руль крутится не в ту сторону, использовать `Invert Direction`.

---

## Вариант 5 — влияние руля на скорость

Это также выбрано и добавлено.

Полный руль должен не только поворачивать корабль, но и добавлять сопротивление:

```text
Actual Rudder
→ rudder drag
→ дополнительное сопротивление
→ скорость немного падает в манёвре
```

Это работает даже если режим двигателя не меняется:

```text
Full Ahead + руль прямо
    максимальная скорость режима

Full Ahead + полный руль
    скорость постепенно проседает из-за сопротивления руля
```

Параметры в `ShipConfig`:

```text
Max Rudder Drag Deceleration Knots Per Second
Max Rudder Speed Loss Fraction
Rudder To Drag Effectiveness
```

Пример стартовых значений:

```text
Max Rudder Drag Deceleration Knots Per Second = 0.10
Max Rudder Speed Loss Fraction = 0.10
```

---

# 4. Визуальный крен корпуса

Добавлен отдельный визуальный контроллер:

```text
ShipHeelVisualController
```

Он должен быть визуальным слоем, не частью физики движения.

Крен считается от реальной интенсивности манёвра, то есть от:

```text
CurrentYawRateDegreesPerSecond
CurrentSpeedNormalized
```

а не от команды руля.

Это важно: корпус должен крениться тогда, когда корабль реально входит в циркуляцию, а не сразу после нажатия клавиши.

### Параметры в `ShipConfig`

```text
Max Maneuver Heel Angle
Yaw Rate For Full Heel
Heel Response Speed
Heel Recovery Speed
```

Пример для Yamato-like:

```text
Max Maneuver Heel Angle = 3.5–4
Yaw Rate For Full Heel = 0.8
Heel Response Speed = 1.5–2
Heel Recovery Speed = 1.2–1.5
```

Если крен в неправильную сторону, использовать:

```text
Invert Heel Direction = true
```

---

# 5. Качка от волн и крен от манёвра

`ShipWaveMotionController` уже существует и наклоняет `visualRoot` через `localRotation`.

`ShipHeelVisualController` тоже меняет `localRotation` своего target.

Поэтому нельзя назначать им один и тот же Transform.

Правильная иерархия:

```text
ShipRoot
 ├── Colliders
 ├── Cameras / Anchors
 └── ManeuverHeelPivot          ← крен от манёвра
      └── WaveMotionPivot       ← качка от волн
           └── ShipModel
```

Назначения:

```text
ShipHeelVisualController.HeelVisualRoot = ManeuverHeelPivot
ShipWaveMotionController.VisualRoot = WaveMotionPivot
```

`ManeuverHeelPivot` — обычный пустой GameObject-контейнер с:

```text
Position = 0,0,0
Rotation = 0,0,0
Scale = 1,1,1
```

---

# 6. Винты

Изначально `ShipPropellerVisualController` крутил винты от фактической скорости корпуса:

```text
movementController.CurrentSpeedKnots
```

После ввода инерции это стало неидеально:

```text
Full Ahead → Stop
    корпус ещё идёт быстро,
    но двигатели уже остановлены,
    значит винты не должны продолжать быстро крутиться от скорости корпуса.
```

Решение: визуальные винты должны уметь крутиться от:

```text
Engine Telegraph Order / engine output / propeller rpm
```

а не только от скорости корпуса.

В новом `ShipPropellerVisualController` есть режим:

```text
Rotation Source = Engine Telegraph Order
```

Если этот режим включён, то:

```text
Flank Ahead → Full Ahead
    винты быстро переходят на обороты Full Ahead,
    а корпус ещё некоторое время идёт быстрее по инерции.

Full Ahead → Stop
    винты замедляются/останавливаются,
    корпус ещё идёт по инерции.

Full Ahead → Full Astern
    винты начинают крутиться назад,
    корпус ещё некоторое время идёт вперёд и тормозит.
```

---

# 7. Какие компоненты сейчас считаются актуальными

## Оставить

```text
ShipConfig
ShipStatuses
ShipMovementController
ShipManeuveringModel
ShipPropulsionModel
RudderInputController
RudderSliderView
EngineTelegraphView
ShipRudderVisualController
ShipPropellerVisualController
ShipHeelVisualController
ShipWaveMotionController
```

## Не использовать одновременно

```text
RudderInputController
RudderInputSystem
```

Выбрано оставить:

```text
RudderInputController
```

`RudderInputSystem` лучше убрать/отключить, чтобы не было двойного изменения руля от одного нажатия.

---

# 8. Общий поток данных

## Двигатель

```text
EngineTelegraphView / input
→ ShipStatuses engine order
→ ShipPropulsionModel
→ currentSpeedKnots
→ ShipMovementController moves ShipRoot
→ ShipPropellerVisualController rotates propellers from engine order
```

## Руль

```text
RudderInputController
→ ShipStatuses.TargetRudderSignedValue
→ RudderSliderView displays target command
→ ShipStatuses.ActualRudderSignedValue moves toward target
→ ShipManeuveringModel calculates target yaw rate
→ ShipMovementController applies current yaw rate
→ ShipRudderVisualController rotates rudder blades
→ ShipPropulsionModel receives rudder drag influence
```

## Крен

```text
ShipMovementController.CurrentYawRateDegreesPerSecond
+ ShipMovementController.CurrentSpeedNormalized
→ ShipHeelVisualController
→ ManeuverHeelPivot local roll
```

## Волны

```text
ShipWaveMotionController
→ WaveMotionPivot local pitch/roll
```

---

# 9. Текущая рекомендуемая иерархия объекта корабля

```text
ShipRoot
 ├── ShipStatuses
 ├── ShipMovementController
 ├── RudderInputController
 ├── ShipHeelVisualController
 ├── ShipRudderVisualController
 ├── ShipPropellerVisualController
 ├── Colliders
 ├── Cameras / Anchors
 └── ManeuverHeelPivot
      └── WaveMotionPivot
           └── ShipVisualRoot / ShipModel
                ├── Propellers
                │    ├── Propeller_L
                │    └── Propeller_R
                └── Rudders
                     ├── RudderPivot_L
                     │    └── RudderMesh_L
                     └── RudderPivot_R
                          └── RudderMesh_R
```

---

# 10. Важные технические решения

## Не наклонять `ShipRoot`

`ShipRoot` должен отвечать только за:

```text
position
yaw rotation
movement direction
collisions / anchors / cameras
```

Крен и качка должны быть на визуальных дочерних объектах.

## Не использовать один Transform для wave и heel

`ShipWaveMotionController` и `ShipHeelVisualController` оба перезаписывают `localRotation`. Поэтому им нужны разные Transform-слои.

## UI руля показывает Target, физика использует Actual

```text
RudderSliderView → TargetRudderSignedValue
ShipMovementController → ActualRudderSignedValue
ShipRudderVisualController → ActualRudderSignedValue
```

## Stop не означает мгновенную остановку

`Stop` означает:

```text
engine thrust = 0
```

а не:

```text
target speed = 0 with active braking
```

## Полный руль влияет на скорость

Потеря скорости от руля — это отдельный drag-слой, а не изменение engine order.

---

# 11. Ориентировочные стартовые параметры Yamato-like

```text
Ship Length = 263

Rudder:
Max Rudder Angle Degrees = 35
Rudder Shift Time From Center To Full = 6

Maneuvering:
Minimum Turning Radius In Ship Lengths = 4.5
Maximum Turning Radius In Ship Lengths = 20
Turn Acceleration = 0.65–0.75
Turn Rate Multiplier = 1.5

Propulsion:
Forward Acceleration Knots Per Second = 1.0
Reverse Acceleration Knots Per Second = 0.6
Natural Coasting Deceleration Knots Per Second = 0.05
Opposite Thrust Deceleration Knots Per Second = 0.6

Rudder Drag:
Max Rudder Drag Deceleration Knots Per Second = 0.10
Max Rudder Speed Loss Fraction = 0.10

Maneuver Heel:
Max Maneuver Heel Angle = 3.5–4
Yaw Rate For Full Heel = 0.8
Heel Response Speed = 1.5–2
Heel Recovery Speed = 1.2–1.5

Rudder UI:
RudderSliderView Smooth Movement = true
RudderSliderView Smooth Speed = 4–6
```

---

# 12. Что делать в следующей сессии

Если нужно продолжить работу, начать с этого контекста:

```text
Мы делаем Unity 6 игру BattleShip про корабли в стиле World of Warships.
У нас уже выбрана кинематическая модель движения корабля:
- телеграф задаёт режим двигателя, а не мгновенную скорость;
- скорость корпуса меняется через ShipPropulsionModel: разгон, выбег, обратная тяга;
- руль разделён на Target и Actual;
- UI руля показывает Target, физика использует Actual;
- поворот считается через радиус циркуляции и yaw rate;
- полный руль добавляет rudder drag и может снижать скорость;
- крен от манёвра реализован визуально через отдельный ManeuverHeelPivot;
- качка от волн работает на отдельном WaveMotionPivot;
- визуальные рули читают ActualRudderSignedValue;
- винты желательно крутить от engine order / propeller rpm, а не от скорости корпуса.

Не предлагать сразу Rigidbody/full physics, если пользователь явно не попросит.
Продолжать в стиле gameplay-tunable kinematic architecture.
```

---

# 13. Возможные следующие задачи

```text
1. Тонкая настройка параметров разгона, выбега и торможения.
2. Настройка кривых Rudder To Turn Effectiveness и Speed To Rudder Effectiveness.
3. Настройка Rudder To Drag Effectiveness.
4. Этап 2 манёвренности: pivot point / вынос кормы при развороте.
5. Улучшение ShipPropellerVisualController через отдельную модель propeller rpm.
6. Добавление wake/foam эффекта для ощущения скорости.
7. Добавление damage modifiers: повреждение руля, двигателя, винтов.
8. Добавление displacementTons как информационной характеристики для будущих систем.
```

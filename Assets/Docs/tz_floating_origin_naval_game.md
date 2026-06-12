# Техническое задание
## Система больших координат, Floating Origin и секторного мира для морской игры

## 1. Назначение системы

Разработать собственную систему больших координат и Floating Origin для Unity 6, позволяющую создавать и поддерживать морскую карту размером примерно **100×100 км** без заметного дрожания объектов, камеры, воды, снарядов и кораблей.

Система должна позволять:

```text
- хранить реальные координаты объектов в double
- отображать ближайший мир в локальных Unity float-координатах
- периодически сдвигать активный мир к Unity origin
- грузить и выгружать сектора карты
- отделять игровую симуляцию от визуального представления
- поддерживать корабли, острова, снаряды, эффекты, воду и радар
```

---

## 2. Исходные условия проекта

```text
Движок: Unity 6
Жанр: морские сражения
Мир: открытая морская карта
Размер карты: около 100×100 км
Мультиплеер: нет
Основные объекты: корабли, острова, порты, снаряды, эффекты, маркеры
Сложный terrain: нет
Вода: бесконечная / камероцентричная ocean system
Основная проблема: точность координат и производительность на большой карте
```

---

## 3. Главный принцип архитектуры

Unity `Transform.position` **не считается источником истины**.

Настоящие координаты объектов должны храниться отдельно:

```text
WorldPosition = double X, double Z
```

А Unity Transform используется только для локального отображения рядом с текущим origin:

```text
UnityPosition = WorldPosition - WorldOrigin
```

Пример:

```text
Реальная позиция корабля:
X = 42 350.25 м
Z = -18 920.75 м

Текущий origin:
X = 40 000.00 м
Z = -20 000.00 м

Локальная Unity-позиция:
X = 2 350.25
Z = 1 079.25
```

---

## 4. Область ответственности системы

Система должна отвечать за:

```text
1. Глобальные координаты объектов
2. Преобразование double world position → Vector3 Unity position
3. Floating Origin shift
4. Регистрацию объектов, которые нужно сдвигать
5. Загрузку/выгрузку секторов
6. Подключение визуальных объектов к симуляционным данным
7. Поддержку карты, радара и маркеров
8. Безопасную работу с Rigidbody, VFX, TrailRenderer, LineRenderer
```

Система **не должна** отвечать за:

```text
- AI кораблей
- детальную баллистику
- damage model
- управление кораблём
- генерацию волн
- сетевой код
- редактор карт
```

Но она должна предоставить API, чтобы эти системы могли работать в глобальных координатах.

---

## 5. Основные модули

### 5.1. WorldOriginManager

Главный модуль Floating Origin.

#### Назначение

Хранит текущий глобальный origin мира и выполняет сдвиг активных Unity-объектов, когда игрок слишком далеко ушёл от локального нуля.

#### Данные

```csharp
public sealed class WorldOriginManager : MonoBehaviour
{
    public static WorldOriginManager Instance { get; }

    public double OriginX { get; private set; }
    public double OriginZ { get; private set; }

    public float RebaseThreshold = 4096f;
}
```

#### Обязанности

```text
- хранить текущий WorldOrigin
- переводить world position в local Unity position
- переводить local Unity position в world position
- проверять расстояние игрока от локального origin
- запускать процедуру rebase
- уведомлять подписчиков о сдвиге origin
```

#### Обязательные методы

```csharp
public Vector3 WorldToUnity(WorldPosition worldPosition);

public WorldPosition UnityToWorld(Vector3 unityPosition);

public void Register(IFloatingOriginObject obj);

public void Unregister(IFloatingOriginObject obj);

public void ForceRebase(Vector3 localShift);
```

---

### 5.2. WorldPosition

Структура для хранения реальной позиции в мире.

```csharp
[System.Serializable]
public struct WorldPosition
{
    public double x;
    public double z;

    public WorldPosition(double x, double z)
    {
        this.x = x;
        this.z = z;
    }
}
```

Дополнительно можно добавить Y, но для морской игры лучше разделять:

```text
X/Z = глобальные координаты мира
Y = локальная высота над водой / высота объекта
```

Если позже понадобятся самолёты, ракеты или подводные объекты, можно расширить до:

```csharp
public struct WorldPosition3D
{
    public double x;
    public double y;
    public double z;
}
```

---

### 5.3. IFloatingOriginObject

Интерфейс для объектов, которые должны реагировать на сдвиг origin.

```csharp
public interface IFloatingOriginObject
{
    void OnOriginShift(Vector3 shift);
}
```

Примеры объектов:

```text
- корабли
- острова
- снаряды
- частицы
- следы
- буи
- визуальные маркеры
- порты
- облака низкого уровня, если они world-space
```

---

### 5.4. FloatingOriginTransform

Компонент для обычных объектов, которые нужно просто сдвигать вместе с миром.

```csharp
public sealed class FloatingOriginTransform : MonoBehaviour, IFloatingOriginObject
{
    public void OnOriginShift(Vector3 shift)
    {
        transform.position -= shift;
    }
}
```

Применяется к:

```text
- островам
- статичным объектам
- портам
- маякам
- обломкам
- декоративным объектам
```

---

### 5.5. FloatingOriginRigidbody

Компонент для объектов с Rigidbody.

#### Назначение

При сдвиге origin корректно перемещает Rigidbody, не ломая скорость, вращение и физическое состояние.

```csharp
public sealed class FloatingOriginRigidbody : MonoBehaviour, IFloatingOriginObject
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnOriginShift(Vector3 shift)
    {
        if (rb != null)
        {
            rb.position -= shift;
            rb.Sleep();
            rb.WakeUp();
        }
        else
        {
            transform.position -= shift;
        }
    }
}
```

#### Важно

Нельзя менять глобальные данные корабля при origin shift.

```text
ShipSim.worldPosition не меняется.
ShipView.transform.position меняется.
```

---

## 6. Логика Floating Origin

### 6.1. Условие сдвига

Сдвиг выполняется, если локальная позиция игрока превысила порог.

Рекомендуемый стартовый порог:

```text
4096 метров
```

Почему 4096:

```text
- достаточно далеко от origin
- удобно для ocean systems
- степень двойки
- сдвиги происходят не слишком часто
```

Проверка:

```csharp
Vector3 playerLocalPos = player.transform.position;

float sqrDistance = playerLocalPos.x * playerLocalPos.x +
                    playerLocalPos.z * playerLocalPos.z;

if (sqrDistance > RebaseThreshold * RebaseThreshold)
{
    Rebase(playerLocalPos);
}
```

---

### 6.2. Сдвиг только по XZ

Для морской игры origin shift должен происходить только по горизонтали:

```csharp
Vector3 shift = new Vector3(
    player.transform.position.x,
    0f,
    player.transform.position.z
);
```

Y лучше не сдвигать, чтобы не ломать:

```text
- высоту воды
- buoyancy
- эффекты всплесков
- камеру
- физику вертикального движения
```

---

### 6.3. Процедура Rebase

Алгоритм:

```text
1. Получить shift из локальной позиции игрока
2. Добавить shift к глобальному WorldOrigin
3. Сдвинуть все зарегистрированные Unity-объекты на -shift
4. Уведомить системы воды, секторов, камеры, VFX, радара
5. Проверить, что игрок снова около локального нуля
```

Псевдокод:

```csharp
private void Rebase(Vector3 shift)
{
    shift.y = 0f;

    OriginX += shift.x;
    OriginZ += shift.z;

    foreach (var obj in registeredObjects)
    {
        obj.OnOriginShift(shift);
    }

    OriginShifted?.Invoke(shift, new WorldPosition(OriginX, OriginZ));
}
```

---

## 7. Секторная система мира

### 7.1. Размер сектора

Для карты 100×100 км рекомендуется начать с:

```text
Сектор: 5×5 км
Карта: 20×20 секторов
Всего: 400 секторов
```

Альтернатива:

```text
Сектор: 2×2 км
Карта: 50×50 секторов
Всего: 2500 секторов
```

Для морской игры стартовый вариант:

```text
5×5 км
```

потому что объектов мало, а большая часть карты — вода.

---

### 7.2. SectorId

```csharp
public struct SectorId
{
    public int x;
    public int z;
}
```

Получение сектора из глобальной позиции:

```csharp
public SectorId GetSector(WorldPosition pos)
{
    int sx = Mathf.FloorToInt((float)(pos.x / SectorSize));
    int sz = Mathf.FloorToInt((float)(pos.z / SectorSize));

    return new SectorId { x = sx, z = sz };
}
```

Для отрицательных координат нужно обязательно протестировать `FloorToInt`, чтобы сектора корректно работали в зоне `-50 000 ... +50 000`.

---

### 7.3. SectorManager

#### Назначение

Загружает и выгружает сектора вокруг игрока.

```csharp
public sealed class SectorManager : MonoBehaviour
{
    public float SectorSize = 5000f;
    public int LoadRadius = 1;
    public int PreloadRadius = 2;
}
```

#### Правило загрузки

Для сектора 5×5 км:

```text
LoadRadius = 1
```

означает:

```text
загружены 3×3 сектора вокруг игрока
```

Это покрывает область:

```text
15×15 км
```

Для морской игры этого обычно достаточно.

---

### 7.4. Содержимое сектора

Каждый сектор может содержать:

```text
- острова
- порты
- маяки
- рифы
- навигационные объекты
- spawn points
- mission triggers
- декоративные объекты
```

Сектора можно хранить как:

```text
Вариант A: additive scenes
Вариант B: Addressables prefabs
Вариант C: ScriptableObject с данными + runtime spawn
```

Рекомендуемый вариант для начала:

```text
ScriptableObject SectorData + Addressables prefab для крупных объектов
```

---

## 8. Разделение симуляции и визуала

### 8.1. ShipSim

Симуляционная модель корабля.

Она существует всегда, даже если корабль далеко и не имеет GameObject.

```csharp
public sealed class ShipSim
{
    public string id;

    public WorldPosition worldPosition;

    public double headingDegrees;
    public double speedMetersPerSecond;

    public float health;

    public bool isPlayer;
    public bool isVisible;
}
```

#### ShipSim отвечает за:

```text
- глобальную позицию
- курс
- скорость
- состояние
- AI-движение
- дальнюю симуляцию
- данные для сохранения
- данные для радара/карты
```

---

### 8.2. ShipView

Unity-представление корабля.

```csharp
public sealed class ShipView : MonoBehaviour, IFloatingOriginObject
{
    public ShipSim Sim { get; private set; }

    public Rigidbody Rigidbody { get; private set; }

    public void Bind(ShipSim sim)
    {
        Sim = sim;
        transform.position = WorldOriginManager.Instance.WorldToUnity(sim.worldPosition);
    }

    public void OnOriginShift(Vector3 shift)
    {
        Rigidbody.position -= shift;
    }
}
```

#### ShipView отвечает за:

```text
- mesh
- Rigidbody
- buoyancy
- wake
- particles
- audio
- animations
- LOD
- визуальные повреждения
```

---

### 8.3. Правило жизни объекта

```text
ShipSim живёт всегда.
ShipView создаётся только когда корабль достаточно близко или видим.
```

Пример дистанций:

```text
0–3 км:
  полный ShipView + Rigidbody + эффекты

3–10 км:
  упрощённый ShipView без тяжёлых эффектов

10–30 км:
  возможно только LOD/impostor

30+ км:
  только ShipSim, без GameObject
```

---

## 9. Снаряды и баллистика

### 9.1. Разделение

Снаряды должны быть разделены на:

```text
ProjectileSim
ProjectileView
```

---

### 9.2. ProjectileSim

Хранит глобальную баллистику.

```csharp
public sealed class ProjectileSim
{
    public WorldPosition startPosition;
    public WorldPosition currentPosition;
    public WorldPosition targetPosition;

    public double launchTime;
    public double flightTime;

    public float caliber;
    public float damage;
}
```

---

### 9.3. ProjectileView

Создаётся только если снаряд находится рядом с игроком/камерой.

```text
- visual shell
- trail
- sound
- impact particles
```

---

### 9.4. Правило

Нельзя делать все снаряды на 100×100 км как Rigidbody.

Допускается:

```text
ближние снаряды: GameObject / Rigidbody / collision
дальние снаряды: математический расчёт
```

---

## 10. Интеграция с водой

### 10.1. OceanManager

Система воды должна быть независима от глобального размера карты.

Океан должен быть:

```text
- бесконечным визуально
- камероцентричным
- совместимым с origin shift
- способным отдавать высоту волны в локальной точке
```

---

### 10.2. Интерфейс воды

```csharp
public interface IOceanSurface
{
    float GetWaterHeight(Vector3 unityPosition);
    Vector3 GetWaterNormal(Vector3 unityPosition);
}
```

Buoyancy кораблей должна работать в локальных Unity-координатах.

---

### 10.3. При origin shift

Ocean system должна:

```text
- не дёргать визуально волны
- корректно продолжать foam/wake
- не сбрасывать высоту воды
- не ломать buoyancy
```

Если используется Crest или другая готовая ocean system, нужно проверить наличие поддержки floating origin.

---

## 11. Интеграция с камерой

Камера должна всегда находиться рядом с локальным origin.

После rebase:

```text
позиция игрока должна быть около 0,0,0
камера должна продолжить следить за игроком
Cinemachine target не должен отрываться
постобработка не должна мигать
```

Если используется Cinemachine, в список floating origin objects нужно добавить:

```text
- player root
- camera target
- camera rigs
- virtual camera targets, если они world-space
```

---

## 12. Интеграция с эффектами

Floating Origin должен корректно обработать:

```text
- ParticleSystem
- TrailRenderer
- LineRenderer
- Decals
- wake emitters
- splash effects
- explosion effects
- projectile trails
```

### 12.1. ParticleSystem

Нужно создать отдельный компонент:

```csharp
public sealed class FloatingOriginParticles : MonoBehaviour, IFloatingOriginObject
{
    public void OnOriginShift(Vector3 shift)
    {
        transform.position -= shift;

        // Дополнительно:
        // при необходимости сдвигать частицы через ParticleSystem.GetParticles / SetParticles
    }
}
```

Для world-space particles простого сдвига transform может быть недостаточно. Нужно протестировать.

---

### 12.2. TrailRenderer / LineRenderer

Для TrailRenderer может потребоваться сброс или ручной сдвиг точек.

Требование:

```text
После origin shift trail не должен растягиваться на километры.
```

Возможные решения:

```text
Вариант A: очистить trail при rebase
Вариант B: вручную сдвинуть позиции trail
Вариант C: не использовать долгие trails для объектов, переживающих rebase
```

Для морской игры допустимо очищать дальние trails при rebase, если это не заметно.

---

## 13. Радар и карта

Радар и карта должны работать только с глобальными координатами.

```text
RadarPosition = ShipSim.worldPosition - PlayerShipSim.worldPosition
```

Не использовать `Transform.position` для карты мира.

---

### 13.1. Требования к радару

```text
- отображать корабли на расстоянии 0–50 км
- не зависеть от origin shift
- не мигать при rebase
- использовать WorldPosition
```

---

### 13.2. Требования к глобальной карте

```text
- карта показывает координаты в пределах 100×100 км
- маркеры островов берутся из SectorData
- корабли берутся из ShipSim
- игрок берётся из PlayerShipSim
```

---

## 14. Сохранения

Сохранять нужно только глобальные данные.

```text
Сохранять:
- WorldPosition кораблей
- курс
- скорость
- HP
- состояние миссий
- состояние секторов
- позицию игрока в double

Не сохранять:
- локальные Unity Transform.position как источник истины
- текущий WorldOrigin как обязательную часть игрового состояния
```

При загрузке:

```text
1. Загрузить PlayerShipSim.worldPosition
2. Установить WorldOrigin рядом с игроком
3. Создать PlayerShipView около локального origin
4. Загрузить сектора вокруг игрока
5. Создать видимые ShipView
```

---

## 15. Требования к точности

### 15.1. Глобальная точность

WorldPosition должен храниться в `double`.

```text
Минимальная точность координат:
до сантиметров на карте 100×100 км
```

---

### 15.2. Локальная точность Unity

Активные физические объекты должны находиться в разумной дистанции от локального origin.

Рекомендуемо:

```text
0–5 км от origin — хорошо
5–10 км — допустимо для визуала
10+ км — нежелательно для активной физики
```

---

## 16. Требования к производительности

### 16.1. Активная зона

Одновременно активная физическая зона:

```text
радиус 3–5 км вокруг игрока
```

---

### 16.2. Количество объектов

Целевые значения для прототипа:

```text
активные корабли с Rigidbody: 10–30
дальние ShipSim без GameObject: 100–500
активные снаряды-визуалы: до 100
дальние ProjectileSim: до 1000
загруженные сектора: 3×3 или 5×5
```

---

## 17. Критерии приёмки

Система считается рабочей, если выполняются следующие условия.

### 17.1. Тест движения на 100 км

Сценарий:

```text
Игрок плывёт от одного края карты до другого.
Дистанция: минимум 100 км.
```

Ожидаемый результат:

```text
- камера не дрожит
- корабль не дёргается
- вода не ломается
- координаты игрока корректны
- origin shift происходит незаметно
- карта и радар не сбрасываются
```

---

### 17.2. Тест origin shift

Сценарий:

```text
Игрок пересекает порог 4096 м.
```

Ожидаемый результат:

```text
- игрок возвращается близко к локальному origin
- глобальная позиция игрока не меняется
- острова остаются визуально на своих местах
- снаряды не телепортируются неправильно
- эффекты не растягиваются
- камера не мигает
```

---

### 17.3. Тест островов

Сценарий:

```text
Игрок приближается к острову издалека.
```

Ожидаемый результат:

```text
- сектор загружается заранее
- остров появляется без рывков
- позиция острова совпадает с глобальной картой
- после origin shift остров остаётся на правильном месте
```

---

### 17.4. Тест дальнего корабля

Сценарий:

```text
Вражеский корабль находится в 25 км от игрока.
```

Ожидаемый результат:

```text
- ShipSim существует
- корабль отображается на карте/радаре
- полный ShipView не создаётся
- AI/курс/скорость продолжают считаться
- при приближении создаётся ShipView в правильной позиции
```

---

### 17.5. Тест дальнего выстрела

Сценарий:

```text
Корабль стреляет по цели на 15 км.
```

Ожидаемый результат:

```text
- баллистика считается в глобальных координатах
- визуальный снаряд создаётся только если он близко к игроку/камере
- попадание происходит в правильной глобальной точке
- impact effect появляется в правильном месте
```

---

## 18. Минимальный MVP

Для первой версии системы нужно реализовать:

```text
1. WorldPosition
2. WorldOriginManager
3. IFloatingOriginObject
4. FloatingOriginTransform
5. FloatingOriginRigidbody
6. PlayerShipSim
7. PlayerShipView
8. Конвертацию World ↔ Unity
9. Rebase при 4096 м
10. Один тестовый остров
11. Один тестовый AI-корабль
12. Один простой снаряд
13. Простую карту/радар на WorldPosition
```

---

## 19. Необходимые тестовые сцены

### Scene 1: FloatingOrigin_Test

```text
- вода
- игрок
- несколько буёв каждые 1 км
- origin shift каждые 4096 м
- debug UI с OriginX/OriginZ/PlayerWorldPosition
```

---

### Scene 2: SectorStreaming_Test

```text
- карта 100×100 км
- сектора 5×5 км
- несколько островов
- подгрузка/выгрузка вокруг игрока
```

---

### Scene 3: NavalCombat_Test

```text
- игрок
- 5 AI-кораблей
- дальность 1–20 км
- стрельба
- снаряды
- радар
- origin shift во время боя
```

---

## 20. Debug UI

Нужно сделать простой debug overlay:

```text
Local Position: x, y, z
World Position: x, z
Current Origin: x, z
Current Sector: x, z
Loaded Sectors: count
Active ShipViews: count
ShipSims: count
Active Projectiles: count
Origin Shifts: count
Distance From Origin: meters
```

Это сильно поможет ловить ошибки.

---

## 21. Основные риски

### Риск 1: эффекты ломаются при сдвиге origin

Особенно:

```text
- trails
- particles
- wake
- foam
- decals
```

Решение:

```text
делать отдельные адаптеры для каждого типа эффекта
или очищать временные эффекты при rebase
```

---

### Риск 2: вода не поддерживает floating origin

Решение:

```text
проверить ocean system в отдельной тестовой сцене до интеграции
```

---

### Риск 3: кто-то из программистов начнёт использовать Transform как настоящую позицию

Решение:

```text
ввести правило:
Transform.position нельзя использовать для глобальной логики
только WorldPosition / ShipSim / SectorData
```

---

### Риск 4: сохранения сохраняют локальные координаты

Решение:

```text
в save-файлах хранить только WorldPosition
```

---

## 22. Правила для команды разработки

Обязательные правила:

```text
1. Любая долгоживущая сущность имеет WorldPosition.
2. Transform.position — только визуальное представление.
3. AI работает в глобальных координатах.
4. Радар работает в глобальных координатах.
5. Снаряды дальнего боя считаются в глобальных координатах.
6. Physics используется только рядом с игроком.
7. Origin shift не должен менять игровые координаты.
8. Все объекты с Transform, которые живут в мире, должны быть зарегистрированы в FloatingOriginManager.
```

---

## 23. Пример итоговой структуры папок

```text
Assets/
  Game/
    World/
      Coordinates/
        WorldPosition.cs
        WorldPosition3D.cs
        WorldMath.cs

      Origin/
        WorldOriginManager.cs
        IFloatingOriginObject.cs
        FloatingOriginTransform.cs
        FloatingOriginRigidbody.cs
        FloatingOriginParticles.cs
        FloatingOriginTrail.cs

      Sectors/
        SectorId.cs
        SectorData.cs
        SectorManager.cs
        SectorLoader.cs

    Ships/
      Simulation/
        ShipSim.cs
        ShipSimulationSystem.cs

      View/
        ShipView.cs
        ShipViewSpawner.cs
        ShipBuoyancy.cs

    Projectiles/
      ProjectileSim.cs
      ProjectileView.cs
      BallisticsSystem.cs

    Ocean/
      IOceanSurface.cs
      OceanManager.cs
      CrestOceanAdapter.cs
      HdrpWaterAdapter.cs

    UI/
      Radar/
        RadarController.cs

      Map/
        WorldMapController.cs

      Debug/
        WorldDebugOverlay.cs
```

---

## 24. Приоритеты разработки

### Этап 1 — координаты и origin

```text
- WorldPosition
- WorldOriginManager
- WorldToUnity / UnityToWorld
- простой rebase
- debug UI
```

### Этап 2 — корабль игрока

```text
- PlayerShipSim
- PlayerShipView
- синхронизация sim ↔ view
- Rigidbody после origin shift
```

### Этап 3 — вода

```text
- подключение ocean system
- buoyancy
- проверка origin shift на воде
```

### Этап 4 — сектора

```text
- SectorId
- SectorData
- загрузка 3×3 секторов
- тестовые острова
```

### Этап 5 — корабли AI

```text
- ShipSim без GameObject
- ShipView spawning/despawning
- LOD-логика
```

### Этап 6 — снаряды

```text
- ProjectileSim
- ProjectileView
- дальняя баллистика
- impact effects
```

### Этап 7 — полировка

```text
- particles
- trails
- wake
- radar
- save/load
- стресс-тест 100 км
```

---

## 25. Итоговая формулировка задачи для разработчика

Разработать систему больших координат и Floating Origin для single-player морской игры в Unity 6. Система должна позволять симулировать карту 100×100 км, хранить реальные координаты объектов в double, отображать активную область мира около Unity origin, выполнять незаметный сдвиг локального мира при удалении игрока от origin, загружать/выгружать сектора карты и поддерживать разделение между симуляционными объектами и их Unity-представлением.

Главный результат: игрок должен иметь возможность пройти/проплыть через всю карту 100 км без заметного дрожания, ошибок координат, поломки воды, снарядов, островов, камеры и радара.

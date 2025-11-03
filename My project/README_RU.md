# Philosophical Life Cycle Game — Руководство по настройке (Unity 2022.3.49f1, URP)

Ниже — подробные инструкции по настройке диалоговых триггеров, переходов между сценами и музыки, опираясь на УЖЕ СУЩЕСТВУЮЩИЙ проект и скрипты.

Версия Unity: 2022.3.49f1
Рендер-пайплайн: Universal Render Pipeline 14.0.11

## 1. Структура проекта (важное)
- Сцены и ресурсы: `My project/Assets/...`
- Скрипты управления системой: `Assets/Scripts/System/`
  - `GameManager.cs` — прогресс и последовательность сцен (6 этапов)
  - `SceneLoader.cs` — асинхронная загрузка сцен с событиями (прогресс, старт, завершение)
  - `DialogueTypes.cs` — структуры данных для JSON-диалогов
  - `DialogueManager.cs` — парсинг JSON, тайпрайтер, события для UI
  - `AudioManager.cs` — музыка/эффекты/звук печати
  - `TriggerZone.cs` — напольный триггер: старт диалога и/или переход к следующей сцене
  - `DialogueUIBinder.cs` — привязка UI к событиям диалога без ручной настройки Event в инспекторе
- Ресурсы диалогов: `Assets/Resources/Dialogues/`
  - `scene1_intro.json` — пример JSON по вашей спецификации
- Существующие игровые скрипты (НЕ трогаем):
  - Движение: `Assets/Scripts/Movement/` (`MouseLook.cs`, `PlayerMovement.cs`)
  - Взаимодействие: `Assets/Scripts/Interact/` (`Item.cs`, `Inventory.cs`, `PickupItem.cs`, ...)
  - NPC: `Assets/Scripts/NPC/`
  - Оружие: `Assets/Scripts/Weapon/`

## 2. Подготовка 6 сцен ⚠️ КРИТИЧЕСКИ ВАЖНО

**ШАГ 1: Создайте или переименуйте 6 сцен**
Создайте 6 сцен (или переименуйте существующие) СТРОГО в таком порядке:
1) `Birth`
2) `Infancy`
3) `Childhood`
4) `Adolescence`
5) `Adulthood`
6) `OldAge`

**ШАГ 2: Добавьте сцены в Build Settings (ОБЯЗАТЕЛЬНО!)**

⚠️ **БЕЗ ЭТОГО ШАГА ИГРА НЕ БУДЕТ РАБОТАТЬ!**

1. Откройте меню: `File → Build Settings...` (или `Ctrl+Shift+B`)
2. В открывшемся окне найдите кнопку `Add Open Scenes` (внизу слева)
3. Поочерёдно откройте каждую сцену и нажмите `Add Open Scenes`:
   - Откройте сцену `Birth` → `Add Open Scenes`
   - Откройте сцену `Infancy` → `Add Open Scenes`
   - Откройте сцену `Childhood` → `Add Open Scenes`
   - Откройте сцену `Adolescence` → `Add Open Scenes`
   - Откройте сцену `Adulthood` → `Add Open Scenes`
   - Откройте сцену `OldAge` → `Add Open Scenes`
4. Убедитесь, что в списке `Scenes In Build` все 6 сцен присутствуют и в правильном порядке (можно перетащить для изменения порядка)
5. Закройте окно Build Settings

**Альтернативный способ:**
- Перетащите сцены из Project окна прямо в список `Scenes In Build` в Build Settings

**Если имена сцен другие:**
- Отредактируйте список в инспекторе `GameManager` → `stageSceneNames` (на объекте `Systems` в сцене `Birth`)
- Имена в списке должны **точно совпадать** с именами сцен в Project окне

## 3. Бутстрап системы (делается ТОЛЬКО в первой сцене — `Birth`)
1) Создайте пустой объект `Systems` в сцене `Birth`.
2) Добавьте на него компоненты: `GameManager`, `SceneLoader`, `AudioManager`, `DialogueManager`.
   - `GameManager` и `AudioManager` переживают загрузку сцен (DontDestroyOnLoad). В остальных сценах их НЕ добавляйте.
3) `SceneLoader`: по желанию задайте `Minimum Load Screen Time` (например 0.5 сек).
4) `AudioManager`:
   - Либо назначьте 3 `AudioSource` (SFX, Music, Typing), либо оставьте пустыми — источники создадутся автоматически.
   - В список `Typing Clips` добавьте аудиоклип с именем `type_slow` (совпадает с JSON).

## 4. Игрок (используем существующие скрипты)
1) Создайте объект `Player` (или используйте ваш `Player.prefab`).
2) Добавьте:
   - На камеру: `MouseLook` (если так принято в вашем проекте).
   - На корень игрока: `PlayerMovement`.
3) Установите тег объекта: `Player` (важно для `TriggerZone`).
4) Сохраните как `Player.prefab` и используйте во всех сценах.

Советы по физике (если проваливается через пол):
- У пола должен быть коллайдер (например `BoxCollider`), `Is Trigger` — выключен.
- На игроке используйте либо `CharacterController`, либо `Rigidbody + CapsuleCollider` (не вместе).
- При `Rigidbody` включите `Collision Detection: Continuous Dynamic`, убедитесь, что движение в `FixedUpdate`.

## 5. UI диалога (ПОДРОБНАЯ настройка через `DialogueUIBinder`)

### 5.1. Создание Canvas и структуры UI (пошагово)

**ШАГ 1: Создайте Canvas**
1. В иерархии сцены: правый клик → `UI` → `Canvas`
2. В инспекторе Canvas:
   - `Render Mode`: `Screen Space - Overlay`
   - `Canvas Scaler`: `Scale With Screen Size` (опционально, для адаптивности)
   - `Reference Resolution`: `1920 x 1080` (или ваш стандарт)

**ШАГ 2: Создайте панель для диалога**
1. Правый клик на `Canvas` → `UI` → `Panel`
2. Переименуйте в `DialoguePanel`
3. В инспекторе Panel:
   - `Rect Transform`: установите нужные размеры (например, нижняя часть экрана)
   - `Image (Script)`: цвет фона — полупрозрачный тёмный (например, чёрный с Alpha 200)
   - **ВАЖНО**: В инспекторе `DialoguePanel` снимите галочку `Active` (чтобы панель была скрыта по умолчанию)

**ШАГ 3: Создайте текст для имени говорящего**
1. Правый клик на `DialoguePanel` → `UI` → `Text - TextMeshPro` (если появится окно "TMP Importer" — нажмите "Import TMP Essentials")
2. Переименуйте в `SpeakerText`
3. В инспекторе:
   - `Rect Transform`: разместите в верхней части панели
   - `TextMeshPro - Text (UI)`: 
     - `Text`: `The Voice` (временный текст, будет заменён автоматически)
     - `Font Size`: 24–32 (по желанию)
     - `Alignment`: по центру или слева
     - `Color`: белый или светлый

**ШАГ 4: Создайте текст для основного текста диалога**
1. Правый клик на `DialoguePanel` → `UI` → `Text - TextMeshPro`
2. Переименуйте в `BodyText`
3. В инспекторе:
   - `Rect Transform`: разместите ниже `SpeakerText`, с отступами
   - `TextMeshPro - Text (UI)`:
     - `Text`: `...` (временный текст)
     - `Font Size`: 18–24 (обычно чуть меньше, чем у SpeakerText)
     - `Alignment`: по центру или слева
     - `Color`: белый
     - `Wrapping`: включите, чтобы длинный текст переносился

**ШАГ 5: Добавьте компонент `DialogueUIBinder`**
1. Выберите объект `DialoguePanel` (или создайте отдельный пустой объект-контейнер)
2. В инспекторе: `Add Component` → найдите `Dialogue UIBinder` (Scripts → System)
3. В полях компонента `DialogueUIBinder`:
   - `Speaker Text`: перетащите сюда `SpeakerText` из иерархии
   - `Body Text`: перетащите сюда `BodyText` из иерархии
   - `Panel Root`: перетащите сюда `DialoguePanel` (или сам объект, на котором висит компонент)

### 5.2. Проверка настройки

**Как это работает:**
- При входе игрока в триггер с диалогом:
  1. `DialogueManager` вызывает событие `OnDialogueStarted`
  2. `DialogueUIBinder` получает событие и включает `Panel Root` (делает панель видимой)
  3. `DialogueManager` вызывает `OnSpeakerChanged` с именем говорящего
  4. `DialogueUIBinder` обновляет `SpeakerText` текстом имени
  5. `DialogueManager` начинает тайпрайтер: вызывает `OnTextUpdated` по буквам
  6. `DialogueUIBinder` обновляет `BodyText` накапливающимся текстом
  7. После окончания: `OnDialogueFinished` → панель скрывается

**Если текст не появляется — проверьте:**
1. ✅ `DialoguePanel` скрыт по умолчанию (`Active` = false в инспекторе)
2. ✅ В `DialogueUIBinder` все три поля заполнены (Speaker Text, Body Text, Panel Root)
3. ✅ `DialogueManager` существует в сцене (на объекте `Systems`)
4. ✅ В `TriggerZone` указаны правильные `Dialogue Resource Path` и `Dialogue Id`
5. ✅ JSON-файл существует по пути `Assets/Resources/Dialogues/...` и содержит правильный `dialogueID`

**Альтернатива (через события вручную):**
Если не хотите использовать `DialogueUIBinder`, можно вручную привязать события `DialogueManager`:
- `OnSpeakerChanged` → метод, который вызывает `TMP_Text.SetText(string)`
- `OnTextUpdated` → метод, который вызывает `TMP_Text.SetText(string)`
- `OnDialogueStarted` → `GameObject.SetActive(true)` на панели
- `OnDialogueFinished` → `GameObject.SetActive(false)` на панели

## 6. Диалоговые JSON-файлы
Пример уже добавлен: `Assets/Resources/Dialogues/scene1_intro.json`
Формат:
```json
{
  "dialogues": [
    {
      "dialogueID": "scene1_intro",
      "speaker": "The Voice",
      "text": "You awaken in a pure white space. No memory, no knowledge...",
      "typingSpeed": 0.05,
      "audioClip": "type_slow"
    }
  ]
}
```

Вы можете добавлять новые JSON в `Assets/Resources/Dialogues/` и вызывать их по `resourcePath` без расширения.

## 7. Напольный триггер с подсветкой (диалог + переход)
1) Создайте эффект подсветки (Particle System) — круг на полу c мягким пульсом.
   - Сохраните как префаб (например, `TriggerGlow.prefab`).
2) В сцене создайте объект `IntroTrigger` с `BoxCollider` (`Is Trigger` = true).
3) Добавьте `TriggerZone` и заполните поля:
   - `Dialogue Resource Path`: `Dialogues/scene1_intro`
   - `Dialogue Id`: `scene1_intro`
   - `Advance Scene On Dialogue Finish`: включить (после диалога автоматический переход к следующей сцене)
   - `Glow`: укажите ваш `ParticleSystem` подсветки (чтобы исчез после срабатывания)
4) Разместите триггер перед точкой появления игрока.

Для триггеров БЕЗ диалога (только переход): оставьте `Dialogue Resource Path` и `Dialogue Id` пустыми, но включите `Advance Scene On Dialogue Finish` — переход выполнится сразу при входе игрока в триггер.

## 8. Переходы между сценами и экран загрузки
- `GameManager` хранит текущий индекс сцены в `PlayerPrefs` и загружает следующую сцену по порядку.
- Для каждого завершения сцены разместите `TriggerZone` с включенным `Advance Scene On Dialogue Finish`.
- Если нужен экран загрузки:
  1) Создайте `Canvas` с панелью, прогресс-баром (`Slider`) и текстом «Loading…».
  2) Привяжите его к событиям `SceneLoader` (на объекте `Systems`):
     - `OnLoadStarted` → включить `LoadingCanvas`
     - `OnLoadProgress(float)` → обновлять `Slider.value`
     - `OnLoadCompleted` → выключить `LoadingCanvas`

## 9. Музыка и звук

### 9.1. Звук тайпрайтера
- В `AudioManager` (на объекте `Systems`) добавьте клип в список `Typing Clips`
- **ВАЖНО**: имя аудиоклипа должно **точно совпадать** со значением `audioClip` в JSON (например, `type_slow`)
- Если имя не совпадает, звук печати не будет воспроизводиться

### 9.2. Фоновая музыка сцены — КОНТРОЛЬ ВРЕМЕНИ ВОСПРОИЗВЕДЕНИЯ

**Проблема**: музыка начинает играть сразу при загрузке сцены, но нужна задержка или пауза во время диалога.

**РЕШЕНИЕ А: Музыка играет ПОСЛЕ диалога (запуск по событию)**

Создайте скрипт, который запускает музыку после окончания диалога:

```csharp
using Systems;
using UnityEngine;

public class MusicAfterDialogue : MonoBehaviour
{
	[SerializeField] private AudioClip musicClip;
	
	private void OnEnable()
	{
		if (DialogueManager.Instance != null)
		{
			DialogueManager.Instance.OnDialogueFinished.AddListener(StartMusic);
		}
	}
	
	private void OnDisable()
	{
		if (DialogueManager.Instance != null)
		{
			DialogueManager.Instance.OnDialogueFinished.RemoveListener(StartMusic);
		}
	}
	
	private void StartMusic()
	{
		if (AudioManager.Instance != null && musicClip != null)
		{
			AudioManager.Instance.PlayMusic(musicClip);
		}
		// Отключаем этот скрипт после первого срабатывания (если музыка нужна только один раз)
		enabled = false;
	}
}
```

**РЕШЕНИЕ Б: Музыка приглушается/останавливается во время диалога**

Создайте скрипт для управления музыкой во время диалога:

```csharp
using Systems;
using UnityEngine;

public class MusicDuringDialogue : MonoBehaviour
{
	[SerializeField] private AudioClip musicClip;
	[SerializeField] private bool pauseMusicDuringDialogue = true;
	
	private AudioSource musicSource;
	private float originalVolume;
	
	private void Start()
	{
		if (AudioManager.Instance != null && musicClip != null)
		{
			AudioManager.Instance.PlayMusic(musicClip);
			// Получаем источник музыки (нужно расширить AudioManager или использовать рефлексию)
		}
		
		if (DialogueManager.Instance != null)
		{
			DialogueManager.Instance.OnDialogueStarted.AddListener(PauseMusic);
			DialogueManager.Instance.OnDialogueFinished.AddListener(ResumeMusic);
		}
	}
	
	private void OnDestroy()
	{
		if (DialogueManager.Instance != null)
		{
			DialogueManager.Instance.OnDialogueStarted.RemoveListener(PauseMusic);
			DialogueManager.Instance.OnDialogueFinished.RemoveListener(ResumeMusic);
		}
	}
	
	private void PauseMusic()
	{
		if (pauseMusicDuringDialogue && AudioManager.Instance != null)
		{
			AudioManager.Instance.PauseMusic();
		}
	}
	
	private void ResumeMusic()
	{
		if (pauseMusicDuringDialogue && AudioManager.Instance != null)
		{
			AudioManager.Instance.ResumeMusic();
		}
	}
}
```

**РЕШЕНИЕ В: Музыка с задержкой (простой способ)**

Музыка начинает играть через N секунд после загрузки сцены:

```csharp
using Systems;
using UnityEngine;
using System.Collections;

public class DelayedMusic : MonoBehaviour
{
	[SerializeField] private AudioClip musicClip;
	[SerializeField] private float delaySeconds = 5f; // Задержка в секундах
	
	private void Start()
	{
		StartCoroutine(PlayMusicDelayed());
	}
	
	private IEnumerator PlayMusicDelayed()
	{
		yield return new WaitForSeconds(delaySeconds);
		
		if (AudioManager.Instance != null && musicClip != null)
		{
			AudioManager.Instance.PlayMusic(musicClip);
		}
	}
}
```

**РЕШЕНИЕ Г: Музыка сразу при загрузке (если диалога нет)**

Если в сцене нет диалога или музыка должна играть сразу:

```csharp
using Systems;
using UnityEngine;

public class AmbientMusic : MonoBehaviour
{
	[SerializeField] private AudioClip clip;
	
	private void OnEnable()
	{
		if (AudioManager.Instance != null && clip != null)
		{
			AudioManager.Instance.PlayMusic(clip);
		}
	}
}
```

**Рекомендация для первой сцены с диалогом:**
- Используйте **РЕШЕНИЕ А** (музыка после диалога), чтобы музыка не мешала восприятию текста
- Для последующих сцен: используйте **РЕШЕНИЕ Б** или **РЕШЕНИЕ Г** в зависимости от вашей концепции

### 9.3. SFX (звуковые эффекты)
- Для разовых эффектов используйте `AudioManager.Instance.PlaySfx(clip, volume)`.
- `volume` должен быть от 0.0 до 1.0 (по умолчанию 1.0).

## 10. Проверка «от начала до конца»
1) Очистите прогресс (опционально): можно временно вызвать `PlayerPrefs.DeleteAll()` из любого вашего тестового метода.
2) Откройте сцену `Birth`, нажмите Play.
3) Войдите в `IntroTrigger` — отобразится панель диалога, пойдут «печатающиеся» буквы и звук печати.
4) По окончании диалога автоматически загрузится `Infancy`.
5) В каждом последующем зале разместите завершающий `TriggerZone` (с диалогом или без) для перехода к следующей сцене.
6) Музыка: проверьте, что в каждой сцене вызывается `PlayMusic` с нужным клипом.

## 11. Частые проблемы и решения
- Игрок проваливается через пол:
  - Убедитесь, что у пола есть `Collider` и `Is Trigger` выключен.
  - Игрок: используйте либо `CharacterController`, либо `Rigidbody + CapsuleCollider`.
  - Проверьте матрицу коллизий (`Edit → Project Settings → Physics`).
  - Проверьте позицию спавна (не внутри пола) и масштаб (1,1,1).
- **Не видно диалога (текст не появляется):**
  - ✅ `DialoguePanel` должен быть скрыт по умолчанию (`Active` = false в инспекторе)
  - ✅ В компоненте `DialogueUIBinder` все три поля заполнены:
    - `Speaker Text` → ссылка на `SpeakerText` (TMP_Text)
    - `Body Text` → ссылка на `BodyText` (TMP_Text)
    - `Panel Root` → ссылка на `DialoguePanel` (GameObject)
  - ✅ `DialogueManager` существует в сцене (на объекте `Systems` в первой сцене)
  - ✅ `DialogueUIBinder` добавлен на объект, который активен в сцене (например, на `DialoguePanel`)
  - ✅ В `TriggerZone` указаны:
    - `Dialogue Resource Path`: `Dialogues/scene1_intro` (без расширения .json)
    - `Dialogue Id`: `scene1_intro` (должен совпадать с `dialogueID` в JSON)
  - ✅ JSON-файл существует: `Assets/Resources/Dialogues/scene1_intro.json`
  - ✅ В JSON правильный формат: `{"dialogues": [{"dialogueID": "scene1_intro", ...}]}`
  - ✅ Игрок имеет тег `Player` (для срабатывания триггера)
  - ✅ В консоли Unity нет ошибок (проверьте `Window → General → Console`)
- Нет звука печати:
  - В `AudioManager` добавьте клип в `Typing Clips`. Имя клипа должно совпадать с `audioClip` в JSON.
- **Переходы не работают / NullReferenceException в SceneLoader:**
  - ⚠️ **САМАЯ ЧАСТАЯ ПРИЧИНА**: Сцены не добавлены в Build Settings!
    - Откройте `File → Build Settings...`
    - Проверьте, что все 6 сцен присутствуют в списке `Scenes In Build`
    - Если какой-то сцены нет — добавьте её через `Add Open Scenes` или перетащите из Project
    - В консоли Unity будет ошибка: `Scene 'OldAge' couldn't be loaded because it has not been added to the build settings`
  - Убедитесь, что объект игрока имеет тег `Player`.
  - В `TriggerZone` включён `Advance Scene On Dialogue Finish` (или заполнены поля диалога для автоперехода после окончания).
  - Имена сцен в `GameManager.stageSceneNames` должны **точно совпадать** с именами файлов сцен (без расширения .unity)
  - Проверьте консоль Unity на наличие ошибок (`Window → General → Console`)

## 12. Где редактировать что
- Список сцен и порядок: инспектор компонента `GameManager` на объекте `Systems` (`stageSceneNames`).
- JSON-диалоги: `Assets/Resources/Dialogues/*.json`.
- Параметры тайпрайтера (скорость и звук): в JSON (`typingSpeed`, `audioClip`).
- Громкость/источники: на `AudioManager` (или на `AudioSource` внутри него).
- Минимальное время экрана загрузки: `SceneLoader.minimumLoadScreenTime`.

## 13. Рекомендуемые префабы
- `Player.prefab` — игрок с вашими `Movement`/`MouseLook`.
- `DialogueUI.prefab` — канвас с `SpeakerText`, `BodyText`, `DialogueUIBinder`.
- `TriggerGlow.prefab` — круговая подсветка триггера.
- `EndOfSceneTrigger.prefab` — триггер без диалога (только переход).
- `DialogueTrigger.prefab` — триггер с полями диалога (для вступлений и заключений).

---

Если нужна автоматическая генерация 6 сцен-шаблонов с базовыми триггерами и подключенной UI — дайте знать, я подготовлю их, не изменяя уже рабочую логику и не затрагивая ваши существующие скрипты движения/оружия/взаимодействия.

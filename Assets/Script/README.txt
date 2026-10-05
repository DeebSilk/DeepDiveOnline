=== СТРУКТУРА ПАПОК Script/ ===

Camera/                 - Скрипты камеры
  GameState.cs          - Управление состояниями игры (Gameplay, Paused)
  GenshinCamera.cs      - Управление камерой от третьего лица (как в Genshin)

Interaction/            - Всё, связанное с взаимодействием с объектами
  NPCInteractable.cs    - Компонент для NPC, с которым можно взаимодействовать
  PlayerInteraction.cs  - Raycast от игрока для взаимодействия с объектами

NPC/                    - Все скрипты, связанные с NPC (неигровые персонажи)
  Dialogue/             - Диалоговая система
    Dialogue.asset      - Данные диалога (ScriptableObject asset)
    DialogueData.cs     - ScriptableObject с данными для диалогов
    DialogueManager.cs  - Менеджер для управления диалогами
    DialogueUI.cs       - UI компонент для отображения диалогов

  Enemy/                - Скрипты врагов
    EnemyController.cs  - Поведение врага (патруль, преследование, атака)
    EnemyDefinition.cs  - ScriptableObject с определением типа врага
    EnemyDefinition.asset       - Экземпляр определения врага
    EnemyDefinition 1.asset     - Второй экземпляр определения врага
    EnemyHealthBar.cs           - Полоска здоровья над врагом
    EnemySpawner.cs             - Спавнер, создающий врагов в точке

  Friendly/             - Скрипты дружественных NPC
    NPCController.cs    - Поведение дружественного NPC

Player/                 - Скрипты игрока (главного персонажа)
  CombatInput.cs        - Обработка ввода для боевых команд (Input System)
  PlayerCombat.cs       - Боевая система игрока (атаки, комбо, экипировка через ItemData)
  PlayerHealth.cs       - Здоровье игрока
  PlayerMovement.cs     - Передвижение игрока (бег, прыжки, рывок)
  SwordPickup.cs        - Подбор оружия в мире (ItemData → InventoryManager → PlayerCombat)

Systems/                - Глобальные системы игры
  AudioManager.cs       - Управление воспроизведением звуков и музыки
  Damageable.cs         - Компонент "можно получить урон" (для врагов и объектов)
  GameSaveManager.cs    - Сохранение и загрузка игры
  GameSettings.cs       - Настройки игры (графика, звук, управление)

UI/                     - Все скрипты интерфейса
  HUDCanvasController.cs - Контроллер HUD (здоровье, инвентарь, опыт)
  InventorySlotUI.cs    - UI слот в инвентаре
  InventoryUI.cs        - Окно инвентаря
  MainMenu.cs           - Главное меню
  PauseMenu.cs          - Меню паузы
  PlayerHUD.cs          - HUD игрока (полоски, иконки)
  UIButtonAnimator.cs   - Анимация кнопок интерфейса

World/                  - Скрипты мира и предметов
  InventoryManager.cs   - Логика инвентаря и экипировки (ItemData)
  ItemData.cs           - Данные предмета и оружия (ScriptableObject, единый источник правды)
  PickupItem.cs         - Подбираемый предмет на сцене
  SurfaceDetector.cs    - Определение типа поверхности под ногами
  TerrainTreeFixer.cs   - Скрипт для фикса деревьев на террейне

=== ИНСТРУКЦИЯ ===

1. Все скрипты рассортированы по папкам по их назначению
2. Если вы создаёте новый скрипт - размещайте его в соответствующей папке
3. Для ScriptableObject ассетов создавайте их в той же папке, что и скрипт
4. После перемещения файлов в проводнике Windows, откройте Unity и дайте ему 
   переимпортировать ассеты (это может занять несколько секунд)
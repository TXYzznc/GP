using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 摄像机视角模式
/// </summary>
public enum CameraViewMode
{
    ThirdPerson, // 第三人称视角
    TopDown, // 俯视角
    Combat, // ⭐ 战斗视角（新增）
}

public class PlayerInputManager : SingletonBase<PlayerInputManager>
{
    [Header("输入开关")]
    [SerializeField]
    private bool enableInput = true;

    [Header("鼠标灵敏度")]
    [SerializeField]
    private float mouseSensitivityX = 3.0f;

    [SerializeField]
    private float mouseSensitivityY = 1.0f;

    // 输入数据
    public Vector2 Move { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public float ScrollDelta { get; private set; } // 滚轮增量（用于调整摄像机距离）
    public bool SprintDown { get; private set; } // Shift 按下事件
    public bool IsMouseLocked { get; private set; }

    // 视角模式
    public CameraViewMode ViewMode { get; private set; } = CameraViewMode.ThirdPerson;
    public bool ViewModeSwitchTriggered { get; private set; } // 视角切换触发

    // 背包开关
    public bool InventoryToggleTriggered { get; private set; }

    // 仓库开关
    public bool WarehouseToggleTriggered { get; private set; }

    // 背包翻页（A=上一页，D=下一页，仅背包打开时使用）
    public bool InventoryPagePrevTriggered { get; private set; }
    public bool InventoryPageNextTriggered { get; private set; }

    // 快捷栏数字键 1-5（index 0 = Alpha1 ... index 4 = Alpha5）
    private readonly bool[] m_HotbarKeyDown = new bool[5];
    public bool GetHotbarKeyDown(int slot) => slot >= 1 && slot <= 5 && m_HotbarKeyDown[slot - 1];

    // 鼠标灵敏度属性
    public float MouseSensitivityX
    {
        get => mouseSensitivityX;
        set => mouseSensitivityX = Mathf.Max(0.1f, value);
    }

    public float MouseSensitivityY
    {
        get => mouseSensitivityY;
        set => mouseSensitivityY = Mathf.Max(0.1f, value);
    }

    // 鼠标按钮输入
    public bool LeftMouseButtonDown { get; private set; }
    public bool RightMouseButtonDown { get; private set; }

    // 空格键输入（用于交互/触发事件）
    public bool SpaceKeyDown { get; private set; }

    // ⭐ [测试功能] 游戏暂停触发（按空格键）
    public bool GamePauseTestTriggered { get; private set; }

    private bool[] skillDown = new bool[10];
    private bool[] summonerSkillDown = new bool[4];         // 槽位 1-3：Q/E/R 键盘
    private readonly bool[] m_SummonerSkillButtonPending = new bool[4]; // 槽位 1-3：UI 按钮触发
    private IPlayerInputSource source;

    private void Awake()
    {
        base.Awake();

        // 根据设备平台选择输入源
        source = new KeyboardInputSource();
    }

    private void Start()
    {
        // 默认自动锁定鼠标，便于操控
    }

    private void Update()
    {
        // ⭐ [测试功能] 按空格暂停游戏播放
        GamePauseTestTriggered = Input.GetKeyDown(KeyCode.Space);
        if (GamePauseTestTriggered)
        {
            Time.timeScale = Time.timeScale == 0f ? 1f : 0f;  // 切换暂停状态
        }

        // Tab 键：背包开关
        InventoryToggleTriggered = Input.GetKeyDown(KeyCode.Tab);

        // Alt 键：切换鼠标锁定
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            ToggleCursorLock();
        }

        // G 键：仓库开关
        WarehouseToggleTriggered = Input.GetKeyDown(KeyCode.G);

        // 背包翻页（A/D，仅背包打开时生效，由 InventoryUI 自行判断）
        InventoryPagePrevTriggered = Input.GetKeyDown(KeyCode.A);
        InventoryPageNextTriggered = Input.GetKeyDown(KeyCode.D);

        // 按下 I 键切换视角模式
        ViewModeSwitchTriggered = Input.GetKeyDown(KeyCode.I);
        if (ViewModeSwitchTriggered)
        {
            ToggleViewMode();
        }

        if (!enableInput)
        {
            Move = Vector2.zero;
            MouseDelta = Vector2.zero;
            ScrollDelta = 0f;
            SprintDown = false;
            for (int i = 0; i < skillDown.Length; i++)
                skillDown[i] = false;
            for (int i = 0; i < summonerSkillDown.Length; i++)
                summonerSkillDown[i] = false;
            return;
        }

        // 获取移动输入
        Move = source.GetMove();

        // 获取鼠标移动（只在鼠标锁定时，且处于第三人称模式下）
        if (IsMouseLocked && ViewMode == CameraViewMode.ThirdPerson)
        {
            // MouseDelta 直接表示角度变化，不是位置增量
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivityY;
            MouseDelta = new Vector2(mouseX, mouseY);
        }
        else
        {
            MouseDelta = Vector2.zero;
        }

        // 获取滚轮增量（用于调整摄像机/高度等）
        ScrollDelta = Input.mouseScrollDelta.y;

        // 获取冲刺输入
        SprintDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        // 鼠标按钮输入
        LeftMouseButtonDown = Input.GetMouseButtonDown(0); // 0 = 左键
        RightMouseButtonDown = Input.GetMouseButtonDown(1); // 1 = 右键

        // 空格键输入（⭐ 已被测试暂停功能占用，见上方）
        // SpaceKeyDown 已在 GamePauseTestTriggered 中处理

        // 技能输入
        for (int slot = 1; slot <= 3; slot++)
            skillDown[slot] = source.GetSkillDown(slot);

        // 召唤师技能输入（Q/E/R → 槽位 1/2/3）
        summonerSkillDown[1] = Input.GetKeyDown(KeyCode.Q);
        summonerSkillDown[2] = Input.GetKeyDown(KeyCode.E);
        summonerSkillDown[3] = Input.GetKeyDown(KeyCode.R);

        // 快捷栏 1-5
        m_HotbarKeyDown[0] = Input.GetKeyDown(KeyCode.Alpha1);
        m_HotbarKeyDown[1] = Input.GetKeyDown(KeyCode.Alpha2);
        m_HotbarKeyDown[2] = Input.GetKeyDown(KeyCode.Alpha3);
        m_HotbarKeyDown[3] = Input.GetKeyDown(KeyCode.Alpha4);
        m_HotbarKeyDown[4] = Input.GetKeyDown(KeyCode.Alpha5);
    }

    /// <summary>
    /// 设置鼠标锁定状态
    /// </summary>
    public void SetCursorLock(bool locked)
    {
        // 俯视角模式下禁止锁定鼠标
        if (locked && ViewMode == CameraViewMode.TopDown)
        {
            return;
        }

        IsMouseLocked = locked;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// 切换鼠标锁定状态
    /// </summary>
    public void ToggleCursorLock()
    {
        // 俯视角模式下不允许切换鼠标锁定
        if (ViewMode == CameraViewMode.TopDown)
        {
            return;
        }

        SetCursorLock(!IsMouseLocked);
    }

    /// <summary>
    /// 切换视角模式
    /// </summary>
    private void ToggleViewMode()
    {
        ViewMode =
            ViewMode == CameraViewMode.ThirdPerson
                ? CameraViewMode.TopDown
                : CameraViewMode.ThirdPerson;

        // 俯视角模式下不锁定鼠标，第三人称模式下锁定鼠标
        if (ViewMode == CameraViewMode.TopDown)
        {
            SetCursorLock(false);
        }
        else
        {
            SetCursorLock(true);
        }
    }

    /// <summary>
    /// 设置视角模式
    /// </summary>
    public void SetViewMode(CameraViewMode mode)
    {
        if (ViewMode != mode)
        {
            ViewMode = mode;

            // 俯视角模式下不锁定鼠标，第三人称模式下锁定鼠标
            if (ViewMode == CameraViewMode.TopDown)
            {
                SetCursorLock(false);
            }
            else
            {
                SetCursorLock(true);
            }
        }
    }

    /// <summary>
    /// 归一化输入向量，确保对角线移动不会超速
    /// </summary>
    public static Vector2 NormalizeInput(Vector2 input)
    {
        if (input.sqrMagnitude > 1f)
        {
            return input.normalized;
        }
        return input;
    }

    public bool SkillDown(int slot) => skillDown[slot];

    /// <summary>召唤师技能按键（slot 1=Q, 2=E, 3=R），键盘或 UI 按钮均可触发，按钮触发消耗后清除</summary>
    public bool SummonerSkillDown(int slot)
    {
        if (slot < 1 || slot > 3) return false;
        bool result = summonerSkillDown[slot] || m_SummonerSkillButtonPending[slot];
        m_SummonerSkillButtonPending[slot] = false;
        return result;
    }

    /// <summary>由 UI 按钮调用，在下一次 SummonerSkillDown 查询时触发一次技能释放</summary>
    public void TriggerSummonerSkill(int slot)
    {
        if (slot >= 1 && slot <= 3)
            m_SummonerSkillButtonPending[slot] = true;
    }

    public void SetEnable(bool v) => enableInput = v;
}

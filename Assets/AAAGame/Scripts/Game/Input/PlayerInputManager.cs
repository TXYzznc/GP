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

    // 添加：检测右键按下
    public bool RightMouseButtonDown { get; private set; }
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
        // 按下 Tab 键切换鼠标锁定
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleCursorLock();
        }

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

        // 每帧检测获取右键按下
        RightMouseButtonDown = Input.GetMouseButtonDown(1); // 1 = 右键

        // 技能输入
        for (int slot = 1; slot <= 3; slot++)
            skillDown[slot] = source.GetSkillDown(slot);

        // 召唤师技能输入（Q/E/R → 槽位 1/2/3）
        summonerSkillDown[1] = Input.GetKeyDown(KeyCode.Q);
        summonerSkillDown[2] = Input.GetKeyDown(KeyCode.E);
        summonerSkillDown[3] = Input.GetKeyDown(KeyCode.R);
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

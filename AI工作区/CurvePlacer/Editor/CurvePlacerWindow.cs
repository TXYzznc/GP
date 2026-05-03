using UnityEditor;
using UnityEngine;

namespace Tool_Plugins.CurvePlacer
{
    public class CurvePlacerWindow : EditorWindow
    {
        // ── 工具状态 ──────────────────────────────────────────────
        private enum ToolMode
        {
            None,
            Draw,
            Edit,
        }

        private ToolMode _mode = ToolMode.None;

        // ── 数据引用 ──────────────────────────────────────────────
        private CurvePlacerData _data;

        // ── 编辑状态 ──────────────────────────────────────────────
        private int _selectedPointIndex = -1;
        private bool _isDragging = false;

        // ── 样式缓存 ──────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesInit = false;

        // ── 轴名称 ────────────────────────────────────────────────
        private static readonly string[] AXIS_NAMES = { "X", "Y", "Z" };

        // ── 常量 ──────────────────────────────────────────────────
        private const float NODE_HANDLE_SIZE = 0.15f;
        private const float NODE_PICK_DISTANCE = 20f;
        private static readonly Color COLOR_CURVE = new Color(0.2f, 0.9f, 0.4f);
        private static readonly Color COLOR_NODE = new Color(1f, 0.8f, 0.1f);
        private static readonly Color COLOR_SELECTED = new Color(1f, 0.3f, 0.3f);

        // ─────────────────────────────────────────────────────────
        [MenuItem("Tools/Curve Placer")]
        public static void Open()
        {
            var win = GetWindow<CurvePlacerWindow>("Curve Placer");
            win.minSize = new Vector2(300, 520);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            _data = FindObjectOfType<CurvePlacerData>();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SetMode(ToolMode.None);
        }

        // ─────────────────────────────────────────────────────────
        // Inspector UI
        // ─────────────────────────────────────────────────────────
        private void OnGUI()
        {
            InitStyles();
            DrawHeader();
            DrawDataSection();
            EditorGUILayout.Space(4);
            DrawModeButtons();
            EditorGUILayout.Space(4);
            DrawCurveSettings();
            EditorGUILayout.Space(4);
            DrawPlacementSettings();
            EditorGUILayout.Space(4);
            DrawActions();
            EditorGUILayout.Space(4);
            DrawPointList();
        }

        private void InitStyles()
        {
            if (_stylesInit)
                return;
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
            };
            _sectionStyle = new GUIStyle(EditorStyles.helpBox);
            _stylesInit = true;
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("✦ Curve Object Placer", _headerStyle);
            EditorGUILayout.Space(4);
        }

        private void DrawDataSection()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("数据对象", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _data = (CurvePlacerData)
                EditorGUILayout.ObjectField(
                    "CurvePlacerData",
                    _data,
                    typeof(CurvePlacerData),
                    true
                );
            if (EditorGUI.EndChangeCheck())
                Repaint();

            if (_data == null)
            {
                EditorGUILayout.HelpBox(
                    "场景中没有 CurvePlacerData，点击下方按钮创建。",
                    MessageType.Info
                );
                if (GUILayout.Button("在场景中创建数据对象"))
                    CreateDataObject();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawModeButtons()
        {
            if (_data == null)
                return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("编辑模式", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            DrawModeButton("✏ 绘制节点", ToolMode.Draw);
            DrawModeButton("⊙ 编辑节点", ToolMode.Edit);
            if (GUILayout.Button("退出", GUILayout.Height(28)))
                SetMode(ToolMode.None);
            EditorGUILayout.EndHorizontal();

            string hint = _mode switch
            {
                ToolMode.Draw => "左键：添加节点  |  Backspace：删除最后节点  |  Esc：退出",
                ToolMode.Edit => "左键拖拽：移动节点  |  右键：删除节点  |  Esc：退出",
                _ => "选择模式开始编辑",
            };
            EditorGUILayout.HelpBox(hint, MessageType.None);
            EditorGUILayout.EndVertical();
        }

        private void DrawModeButton(string label, ToolMode target)
        {
            bool active = _mode == target;
            GUI.backgroundColor = active ? new Color(0.4f, 0.9f, 0.5f) : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(28)))
                SetMode(active ? ToolMode.None : target);
            GUI.backgroundColor = Color.white;
        }

        private void DrawCurveSettings()
        {
            if (_data == null)
                return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("曲线设置", EditorStyles.boldLabel);

            Undo.RecordObject(_data, "Curve Settings");
            _data.IsClosed = EditorGUILayout.Toggle("闭合曲线", _data.IsClosed);
            _data.segmentStep = EditorGUILayout.Slider(
                "采样精度（步长）",
                _data.segmentStep,
                0.01f,
                0.5f
            );

            EditorGUILayout.Space(4);
            _data.lockAxis = EditorGUILayout.Toggle("锁定轴（强制所有节点一致）", _data.lockAxis);
            if (_data.lockAxis)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("锁定轴", GUILayout.Width(60));
                string[] axisNames = { "X", "Y", "Z" };
                _data.lockAxisIndex = GUILayout.Toolbar(_data.lockAxisIndex, axisNames);
                EditorGUILayout.EndHorizontal();

                _data.lockAxisValue = EditorGUILayout.FloatField("锁定值", _data.lockAxisValue);

                if (GUILayout.Button("将所有节点对齐到当前锁定值"))
                {
                    Undo.RecordObject(_data, "Align All Points");
                    for (int i = 0; i < _data.controlPoints.Count; i++)
                        _data.controlPoints[i] = ApplyAxisLock(_data.controlPoints[i]);
                    EditorUtility.SetDirty(_data);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPlacementSettings()
        {
            if (_data == null)
                return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("对象放置设置", EditorStyles.boldLabel);

            Undo.RecordObject(_data, "Placement Settings");
            _data.prefabToPlace = (GameObject)
                EditorGUILayout.ObjectField(
                    "预制体",
                    _data.prefabToPlace,
                    typeof(GameObject),
                    false
                );
            _data.objectSpacing = EditorGUILayout.FloatField("对象间隔", _data.objectSpacing);
            _data.alignToTangent = EditorGUILayout.Toggle("朝向切线方向", _data.alignToTangent);
            if (_data.alignToTangent)
                _data.rotationOffset = EditorGUILayout.Vector3Field(
                    "旋转偏移",
                    _data.rotationOffset
                );

            EditorGUILayout.EndVertical();
        }

        private void DrawActions()
        {
            if (_data == null)
                return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            // 生成 / 清除当前批次
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("生成对象", GUILayout.Height(30)))
                PlaceObjects();
            if (GUILayout.Button("清除生成对象", GUILayout.Height(30)))
                ClearPlacedObjects();
            EditorGUILayout.EndHorizontal();

            // 保存当前批次
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("✦ 保存当前批次", GUILayout.Height(30)))
                SaveCurrentBatch();
            GUI.backgroundColor = Color.white;

            // 已保存批次列表
            int batchCount = _data.transform.childCount;
            int savedCount = 0;
            for (int i = 0; i < batchCount; i++)
            {
                if (_data.transform.GetChild(i).name.StartsWith("Batch_"))
                    savedCount++;
            }

            if (savedCount > 0)
            {
                EditorGUILayout.LabelField($"已保存批次：{savedCount} 个", EditorStyles.miniLabel);
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("清除所有已保存批次", GUILayout.Height(24)))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "确认",
                            "删除所有已保存的批次对象？此操作不可撤销。",
                            "确认",
                            "取消"
                        )
                    )
                        ClearAllBatches();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("清除所有节点", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("确认", "清除所有曲线节点？", "确认", "取消"))
                {
                    Undo.RecordObject(_data, "Clear Points");
                    _data.controlPoints.Clear();
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPointList()
        {
            if (_data == null || _data.controlPoints.Count == 0)
                return;
            EditorGUILayout.BeginVertical(_sectionStyle);
            EditorGUILayout.LabelField(
                $"节点列表（共 {_data.controlPoints.Count} 个）",
                EditorStyles.boldLabel
            );

            for (int i = 0; i < _data.controlPoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = EditorGUILayout.Vector3Field($"  [{i}]", _data.controlPoints[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_data, "Move Point");
                    _data.controlPoints[i] = newPos;
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    Undo.RecordObject(_data, "Remove Point");
                    _data.controlPoints.RemoveAt(i);
                    SceneView.RepaintAll();
                    break;
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        // ─────────────────────────────────────────────────────────
        // Scene GUI
        // ─────────────────────────────────────────────────────────
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_data == null || _mode == ToolMode.None)
                return;

            DrawCurveInScene();
            DrawNodeHandles(sceneView);
            HandleInput(sceneView);

            // 阻止场景默认选择行为
            if (_mode != ToolMode.None)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }

        private void DrawCurveInScene()
        {
            var pts = _data.controlPoints;
            if (pts.Count < 2)
                return;

            var samples = CatmullRomSpline.Sample(pts, _data.segmentStep, _data.IsClosed);
            if (samples.Count < 2)
                return;

            Handles.color = COLOR_CURVE;
            for (int i = 1; i < samples.Count; i++)
                Handles.DrawLine(samples[i - 1].pos, samples[i].pos, 2f);

            if (_data.IsClosed && samples.Count > 1)
                Handles.DrawLine(samples[samples.Count - 1].pos, samples[0].pos, 2f);
        }

        private void DrawNodeHandles(SceneView sceneView)
        {
            var pts = _data.controlPoints;
            for (int i = 0; i < pts.Count; i++)
            {
                float size = HandleUtility.GetHandleSize(pts[i]) * NODE_HANDLE_SIZE;
                Handles.color = (i == _selectedPointIndex) ? COLOR_SELECTED : COLOR_NODE;
                Handles.SphereHandleCap(0, pts[i], Quaternion.identity, size, EventType.Repaint);

                // 节点序号标签
                Handles.Label(pts[i] + Vector3.up * size * 1.5f, $"{i}", EditorStyles.boldLabel);
            }
        }

        private void HandleInput(SceneView sceneView)
        {
            Event e = Event.current;

            // Esc 退出
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                SetMode(ToolMode.None);
                e.Use();
                return;
            }

            if (_mode == ToolMode.Draw)
                HandleDrawMode(e);
            else if (_mode == ToolMode.Edit)
                HandleEditMode(e);
        }

        private void HandleDrawMode(Event e)
        {
            // 左键点击添加节点
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Vector3 worldPos = GetWorldPosition(e.mousePosition);
                Undo.RecordObject(_data, "Add Control Point");
                _data.controlPoints.Add(ApplyAxisLock(worldPos));
                EditorUtility.SetDirty(_data);
                SceneView.RepaintAll();
                Repaint();
                e.Use();
            }

            // Backspace 删除最后一个节点
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Backspace)
            {
                if (_data.controlPoints.Count > 0)
                {
                    Undo.RecordObject(_data, "Remove Last Point");
                    _data.controlPoints.RemoveAt(_data.controlPoints.Count - 1);
                    EditorUtility.SetDirty(_data);
                    SceneView.RepaintAll();
                    Repaint();
                    e.Use();
                }
            }
        }

        private void HandleEditMode(Event e)
        {
            var pts = _data.controlPoints;

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                // 找最近节点
                int nearest = GetNearestPointIndex(e.mousePosition);
                if (nearest >= 0)
                {
                    _selectedPointIndex = nearest;
                    _isDragging = true;
                    e.Use();
                }
                else
                {
                    _selectedPointIndex = -1;
                }
                Repaint();
            }

            if (
                e.type == EventType.MouseDrag
                && e.button == 0
                && _isDragging
                && _selectedPointIndex >= 0
            )
            {
                Vector3 worldPos = GetWorldPosition(e.mousePosition);
                Undo.RecordObject(_data, "Move Control Point");
                pts[_selectedPointIndex] = ApplyAxisLock(worldPos);
                EditorUtility.SetDirty(_data);
                SceneView.RepaintAll();
                Repaint();
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDragging = false;
            }

            // 右键删除节点
            if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
            {
                int nearest = GetNearestPointIndex(e.mousePosition);
                if (nearest >= 0)
                {
                    Undo.RecordObject(_data, "Remove Control Point");
                    pts.RemoveAt(nearest);
                    _selectedPointIndex = -1;
                    EditorUtility.SetDirty(_data);
                    SceneView.RepaintAll();
                    Repaint();
                    e.Use();
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // 工具方法
        // ─────────────────────────────────────────────────────────
        private void SetMode(ToolMode mode)
        {
            _mode = mode;
            _selectedPointIndex = -1;
            _isDragging = false;
            SceneView.RepaintAll();
            Repaint();
        }

        private void CreateDataObject()
        {
            var go = new GameObject("CurvePlacerData");
            _data = go.AddComponent<CurvePlacerData>();
            Undo.RegisterCreatedObjectUndo(go, "Create CurvePlacerData");
            Selection.activeGameObject = go;
            Repaint();
        }

        /// <summary>
        /// 将屏幕坐标射线投影到 Y=0 平面（或地形）
        /// </summary>
        private Vector3 GetWorldPosition(Vector2 mousePos)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

            // 优先射线检测场景物体
            if (Physics.Raycast(ray, out RaycastHit hit))
                return hit.point;

            // 回退到 Y=0 平面
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0f)
                return ray.origin + ray.direction * t;

            return ray.origin + ray.direction * 10f;
        }

        /// <summary>
        /// 如果启用了轴锁定，将指定轴的值替换为锁定值
        /// </summary>
        private Vector3 ApplyAxisLock(Vector3 pos)
        {
            if (!_data.lockAxis)
                return pos;
            switch (_data.lockAxisIndex)
            {
                case 0:
                    pos.x = _data.lockAxisValue;
                    break;
                case 1:
                    pos.y = _data.lockAxisValue;
                    break;
                case 2:
                    pos.z = _data.lockAxisValue;
                    break;
            }
            return pos;
        }

        private int GetNearestPointIndex(Vector2 mousePos)
        {
            float minDist = NODE_PICK_DISTANCE;
            int index = -1;
            for (int i = 0; i < _data.controlPoints.Count; i++)
            {
                Vector2 screenPos = HandleUtility.WorldToGUIPoint(_data.controlPoints[i]);
                float dist = Vector2.Distance(screenPos, mousePos);
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            return index;
        }

        private void PlaceObjects()
        {
            if (_data == null || _data.prefabToPlace == null)
            {
                EditorUtility.DisplayDialog("提示", "请先设置要放置的预制体。", "OK");
                return;
            }
            if (_data.controlPoints.Count < 2)
            {
                EditorUtility.DisplayDialog("提示", "至少需要 2 个节点才能生成对象。", "OK");
                return;
            }

            ClearPlacedObjects();

            var samples = CatmullRomSpline.Sample(
                _data.controlPoints,
                _data.segmentStep,
                _data.IsClosed
            );
            var points = CatmullRomSpline.GetEvenlySpacedPoints(samples, _data.objectSpacing);

            // 确保父对象存在
            Transform parent = _data.transform.Find("PlacedObjects");
            if (parent == null)
            {
                var parentGo = new GameObject("PlacedObjects");
                parentGo.transform.SetParent(_data.transform);
                parent = parentGo.transform;
                Undo.RegisterCreatedObjectUndo(parentGo, "Create PlacedObjects Parent");
            }

            foreach (var (pos, tangent) in points)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(_data.prefabToPlace, parent);
                go.transform.position = pos;

                if (_data.alignToTangent && tangent.sqrMagnitude > 0.001f)
                {
                    Quaternion rot = Quaternion.LookRotation(tangent.normalized, Vector3.up);
                    go.transform.rotation = rot * Quaternion.Euler(_data.rotationOffset);
                }
                else
                {
                    go.transform.rotation = Quaternion.Euler(_data.rotationOffset);
                }

                Undo.RegisterCreatedObjectUndo(go, "Place Object");
                _data.placedObjects.Add(go);
            }

            EditorUtility.SetDirty(_data);
            Debug.Log($"[CurvePlacer] 已生成 {points.Count} 个对象。");
        }

        private void SaveCurrentBatch()
        {
            if (_data == null || _data.placedObjects.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有可保存的对象，请先生成对象。", "OK");
                return;
            }

            // 找一个不重复的批次名
            int index = 1;
            while (_data.transform.Find($"Batch_{index}") != null)
                index++;

            Transform parent = _data.transform.Find("PlacedObjects");
            if (parent != null)
            {
                Undo.RecordObject(parent.gameObject, "Save Batch");
                parent.name = $"Batch_{index}";
            }

            // 从 placedObjects 移除，让 ClearPlacedObjects 不再管这些对象
            _data.placedObjects.Clear();
            EditorUtility.SetDirty(_data);

            Debug.Log($"[CurvePlacer] 批次已保存为 Batch_{index}");
            Repaint();
        }

        private void ClearAllBatches()
        {
            if (_data == null)
                return;
            var toDelete = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < _data.transform.childCount; i++)
            {
                var child = _data.transform.GetChild(i);
                if (child.name.StartsWith("Batch_"))
                    toDelete.Add(child.gameObject);
            }
            foreach (var go in toDelete)
                Undo.DestroyObjectImmediate(go);

            EditorUtility.SetDirty(_data);
            Repaint();
        }

        private void ClearPlacedObjects()
        {
            if (_data == null)
                return;
            foreach (var go in _data.placedObjects)
            {
                if (go != null)
                    Undo.DestroyObjectImmediate(go);
            }
            _data.placedObjects.Clear();

            // 同时清理父容器
            Transform parent = _data.transform.Find("PlacedObjects");
            if (parent != null)
                Undo.DestroyObjectImmediate(parent.gameObject);

            EditorUtility.SetDirty(_data);
        }
    }
}

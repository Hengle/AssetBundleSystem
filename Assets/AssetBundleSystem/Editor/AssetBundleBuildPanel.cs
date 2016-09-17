using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AssetBundleSystem
{
    public class AssetBundleBuildPanel : EditorWindow
    {
        const string configPath = "Assets/AssetBundleSystem/config.asset";

        private AssetBundleBuildConfig _config;
        private static BuildTarget _buildTarget = BuildTarget.StandaloneWindows;

        private bool _mouseWasDown;
        private bool _hasFocus;
        private float _maxWidth;

        private bool _beginBuild = false;

        // hierarchy
        private bool _hasPendingSelection;
        private AssetBundleConfigNode _pendingSelection;
        private AssetBundleConfigNode _selectedNode;
        private Rect _hierarchyRect;
        private Vector2 _hierarchyScrollPos;
        private GUIContent _content;
        private ExpandedCache _expandedCache;

        // split
        private float _seperatorPos = 200f;
        private float _resizeInitPos;
        private Vector2 _inspectorScrollPos;
        private bool _bResize;

        // inspector
        private int _inspectorType = 0;

        [MenuItem("AssetBundleSystem/Builder Panel")]
        static void Open()
        {
            AssetBundleBuildPanel panel = GetWindow<AssetBundleBuildPanel>("AssetBundle", true);
        }

        static void BuildAssetBundles()
        {
            AssetBundleBuildConfig config = LoadAssetAtPath<AssetBundleBuildConfig>(configPath);
            if (config == null)
                return;

#if UNITY_5
            AssetBundleBuilder builder = new AssetBundleBuilder5x(new AssetBundlePathResolver());
#else
            AssetBundleBuilder builder = new AssetBundleBuilder4x(new AssetBundlePathResolver());
#endif
            if (config.bundleInfoFileFormt == AssetBundleBuildConfig.Format.Binary)
                builder.SetDataWriter(new AssetBundleDataWriterBinary());
            else
                builder.SetDataWriter(new AssetBundleDataWriterText());
            
            builder.Begin();
            builder.AddTargets(config);
            builder.Export(_buildTarget);
            builder.End();
        }

        static T LoadAssetAtPath<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                _config = LoadAssetAtPath<AssetBundleBuildConfig>(configPath);
                if (_config == null)
                    _config = new AssetBundleBuildConfig();
            }

            if (_expandedCache == null)
                _expandedCache = new ExpandedCache();

            Draw();
        }

        private void Update()
        {
            if (_hasPendingSelection)
            {
                _selectedNode = _pendingSelection;
                _pendingSelection = null;
                _hasPendingSelection = false;
            }

            if (_beginBuild)
            {
                Build();
                _beginBuild = false;
            }
        }

        private class ExpandedCache
        {
            private Dictionary<int, bool> _expandedState = new Dictionary<int, bool>();

            public void Clear()
            {
                _expandedState.Clear();
            }

            public bool IsExpanded(int ID)
            {
                return _expandedState.ContainsKey(ID) ? _expandedState[ID] : false;
            }

            public void SetExpanded(int ID, bool expanded)
            {
                _expandedState[ID] = expanded;
            }
        }

        class Styles
        {
            public static GUIStyle box;
            public static GUIStyle toolbar;
            public static GUIStyle toolbarButton;
            public static GUIStyle tootip;
            public static GUIStyle lineStyle;
            public static GUIStyle lineDisabledStyle;
            public static GUIStyle foldoutStyle;
            public static GUIStyle splitStyle;
        }

        private void InitStyles()
        {
            if (Styles.box == null)
            {
                Styles.box = new GUIStyle(GUI.skin.box);
                Styles.box.margin = new RectOffset();
                Styles.box.padding = new RectOffset();
            }
            if (Styles.toolbar == null)
            {
                Styles.toolbar = new GUIStyle(EditorStyles.toolbar);
                Styles.toolbar.margin = new RectOffset();
                Styles.toolbar.padding = new RectOffset();
            }
            if (Styles.toolbarButton == null)
            {
                Styles.toolbarButton = EditorStyles.toolbarButton;
            }
            if (Styles.tootip == null)
            {
                Styles.tootip = GUI.skin.GetStyle("AssetLabel");
            }
            if (Styles.lineStyle == null)
            {
                Styles.lineStyle = new GUIStyle("PR Label");
                Styles.lineStyle.margin = new RectOffset();
                Styles.lineStyle.padding = new RectOffset(1, 0, 2, 0);
                Styles.lineStyle.normal.textColor = Styles.lineStyle.focused.textColor;
            }
            if (Styles.lineDisabledStyle == null)
            {
                Styles.lineDisabledStyle = new GUIStyle("Disabled Label");
                Styles.lineDisabledStyle.margin = new RectOffset();
                Styles.lineDisabledStyle.padding = new RectOffset(1, 0, 2, 0);
                Styles.lineDisabledStyle.normal.textColor = Color.gray;
            }
            if (_content == null)
            {
                _content = new GUIContent();
            }
            if (Styles.foldoutStyle == null)
            {
                Styles.foldoutStyle = new GUIStyle(EditorStyles.foldout);
                Styles.foldoutStyle.overflow.top = -2;
                Styles.foldoutStyle.active.textColor = EditorStyles.foldout.normal.textColor;
                Styles.foldoutStyle.onActive.textColor = EditorStyles.foldout.normal.textColor;
                Styles.foldoutStyle.onFocused.textColor = EditorStyles.foldout.normal.textColor;
                Styles.foldoutStyle.onFocused.background = EditorStyles.foldout.onNormal.background;
                Styles.foldoutStyle.focused.textColor = EditorStyles.foldout.normal.textColor;
                Styles.foldoutStyle.focused.background = EditorStyles.foldout.normal.background;
            }
            if (Styles.splitStyle == null)
            {
                Styles.splitStyle = new GUIStyle(GUIStyle.none);
                Styles.splitStyle.normal.background = EditorGUIUtility.whiteTexture;
            }
        }

        private void SetSelected(AssetBundleConfigNode node)
        {
            _pendingSelection = node;
            _hasPendingSelection = true;
        }
        
        private void DrawNode(AssetBundleConfigNode node)
        {
            bool isSelected = (node == _selectedNode);
            //bool bFoldOut = _expandedCache.IsExpanded(id);
            //bool bExpanded = bFoldOut;
            _content.text = node.name;
            Vector2 vector = Styles.lineStyle.CalcSize(_content);
            vector.x += 13f * node.level;
            Rect position = GUILayoutUtility.GetRect(vector.x, 18f, GUILayout.ExpandWidth(true));
            Event curEvent = Event.current;
            if (curEvent.type == EventType.Repaint && isSelected)
                Styles.lineStyle.Draw(position, false, false, isSelected, true);
            /*if (node.children.Count > 0)
            {
                Rect foldRect = position;
                foldRect.x += indent;
                foldRect.width = 13f;
                bExpanded = EditorGUI.Foldout(foldRect, bFoldOut, GUIContent.none, Styles.foldoutStyle);
                if (bExpanded != bFoldOut)
                {
                    _expandedCache.SetExpanded(id, bExpanded);
                    _hasFocus = true;
                }
            }*/
            if (Event.current.type == EventType.Repaint)
            {
                Rect nameRect = position;
                nameRect.x += 13f * node.level + 13f;
                nameRect.width -= 20f;
                if (node.enabled)
                    Styles.lineStyle.Draw(nameRect, _content, false, false, isSelected, true);
                else
                    Styles.lineDisabledStyle.Draw(nameRect, _content, false, false, isSelected, true);
            }

            /*if (bExpanded)
            {
                DrawNodes(node.children);
            }*/

            switch (curEvent.type)
            {
                case EventType.MouseUp:
                    if (curEvent.button == 0 && _mouseWasDown && position.Contains(curEvent.mousePosition) /*&& bExpanded == bFoldOut*/)
                        SetSelected(node);
                    break;
                case EventType.Repaint:
                    _maxWidth = Mathf.Max(_maxWidth, position.width);
                    break;
            }
        }

        private void DrawNodes(List<AssetBundleConfigNode> nodes)
        {
            nodes.ForEach(o => DrawNode(o));
            nodes.RemoveAll(o => o.needDelete);
        }

        private void DrawHierarchy()
        {
            _hierarchyScrollPos = EditorGUILayout.BeginScrollView(_hierarchyScrollPos, GUILayout.Width(_seperatorPos));
            {
                if (Event.current.type == EventType.Repaint)
                    _maxWidth = 0;

                DrawNodes(_config.allDependNodes);
            }
            EditorGUILayout.EndScrollView();

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    _mouseWasDown = _hierarchyRect.Contains(Event.current.mousePosition);
                    break;
                case EventType.MouseUp:
                    if (!_mouseWasDown)
                    {
                        _hasFocus = false;
                        break;
                    }
                    _mouseWasDown = false;
                    _hasFocus = true;
                    if (_pendingSelection == null)
                        SetSelected(null);
                    break;
                case EventType.Repaint:
                    _hierarchyRect = GUILayoutUtility.GetLastRect();
                    if (_maxWidth > _seperatorPos)
                    {
                        GUIStyle style = GUI.skin.GetStyle("horizontalscrollbar");
                        _hierarchyRect.height -= style.fixedHeight;
                    }
                    break;
                default:
                    break;
            }
        }

        private void DrawSplit()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(4f));
            GUILayout.Label("", Styles.splitStyle, GUILayout.MaxHeight(position.height));
            EditorGUILayout.EndVertical();

            Rect lastRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.ResizeHorizontal);
            
            Event curEvent = Event.current;
            if (curEvent.type == EventType.MouseDown && lastRect.Contains(curEvent.mousePosition))
            {
                _resizeInitPos = curEvent.mousePosition.x;
                _bResize = true;
                curEvent.Use();
            }
            else if (_bResize && (curEvent.type == EventType.MouseUp || curEvent.rawType == EventType.MouseUp))
            {
                _bResize = false;
                curEvent.Use();
            }

            if (_bResize)
            {
                float num = curEvent.mousePosition.x - _resizeInitPos;
                float width = position.width;
                _seperatorPos = Mathf.Clamp(_seperatorPos + num, 200, width);
                _resizeInitPos = Mathf.Clamp(curEvent.mousePosition.x, 200f, width);
                Repaint();
            }
        }

        private void DrawInspector(AssetBundleConfigNode node)
        {
            EditorGUILayout.BeginVertical();
            {
                string[] inspectorTypes = { "Properties", "PersistentAssets" };
                _inspectorType = GUILayout.Toolbar(_inspectorType, inspectorTypes, EditorStyles.toolbarButton);

                if (_inspectorType == 0)
                {
                    if (node != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        {
                            node.enabled = GUILayout.Toggle(node.enabled, "", GUILayout.ExpandWidth(false));

                            GUILayout.Label("Name:", GUILayout.ExpandWidth(false));
                            node.name = EditorGUILayout.TextField(node.name, GUILayout.ExpandWidth(false));

                            GUILayout.Label("Dir:", GUILayout.ExpandWidth(false));
                            GUI.enabled = false;
                            node.dir = EditorGUILayout.TextField(node.dir, GUILayout.ExpandWidth(true));
                            GUI.enabled = true;

                            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                            {
                                string selectedPath = EditorUtility.OpenFolderPanel("Path", Application.dataPath, "");
                                if (!string.IsNullOrEmpty(selectedPath))
                                    node.dir = AssetBundleUtils.ConvertToAssetPath(selectedPath);
                            }

                            GUILayout.Label("Independent:", GUILayout.ExpandWidth(false));
                            node.isIndependent = GUILayout.Toggle(node.isIndependent, "", GUILayout.ExpandWidth(false));

                            GUILayout.Label("Export:", GUILayout.ExpandWidth(false));
                            node.exportType = (ExportType)EditorGUILayout.EnumPopup(node.exportType, GUILayout.Width(80f));
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("AddFilter", EditorStyles.miniButtonLeft))
                            {
                                node.filters.Add(new AssetBundleFilter(true));
                            }
                            if (GUILayout.Button("AddFile", EditorStyles.miniButtonRight))
                            {
                                node.filters.Add(new AssetBundleFilter(false));
                            }
                        }
                        EditorGUILayout.EndHorizontal();

                        _inspectorScrollPos = EditorGUILayout.BeginScrollView(_inspectorScrollPos);
                        {
                            for (int ii = 0; ii < node.filters.Count; ++ii)
                            {
                                AssetBundleFilter filter = node.filters[ii];
                                EditorGUILayout.BeginHorizontal();
                                {
                                    GUI.enabled = true;
                                    filter.valid = GUILayout.Toggle(filter.valid, "", GUILayout.ExpandWidth(false));
                                    GUI.enabled = filter.valid;

                                    GUI.enabled = false;
                                    filter.path = GUILayout.TextField(filter.path, GUILayout.ExpandWidth(true));
                                    GUI.enabled = true;

                                    GUI.enabled = filter.valid;

                                    if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
                                    {
                                        if (string.IsNullOrEmpty(node.dir))
                                        {
                                            ShowNotification(new GUIContent("需要先指定节点目录"));
                                        }
                                        else
                                        {
                                            string selectedPath = filter.isFolder ? EditorUtility.OpenFolderPanel("Path", node.dir, "") : EditorUtility.OpenFilePanel("Path", node.dir, "");
                                            if (!string.IsNullOrEmpty(selectedPath))
                                            {
                                                selectedPath = AssetBundleUtils.ConvertToAssetPath(selectedPath);
                                                if (selectedPath.StartsWith(node.dir))
                                                    filter.path = selectedPath.Substring((node.dir + "/").Length).Replace("\\", "/");
                                                else
                                                    ShowNotification(new GUIContent("不能在" + node.dir + "目录之外!"));
                                            }
                                        }
                                    }

                                    if (filter.isFolder)
                                    {
                                        filter.filter = EditorGUILayout.TextField(filter.filter, GUILayout.Width(100));
                                        filter.searchOption = (System.IO.SearchOption)EditorGUILayout.EnumPopup(filter.searchOption, GUILayout.Width(80));
                                    }

                                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                    {
                                        node.filters.RemoveAt(ii);
                                        --ii;
                                    }
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUILayout.EndScrollView();
                    }
                }
                else if (_inspectorType == 1)
                {
                    if (GUILayout.Button("AddFile", EditorStyles.miniButton))
                    {
                        _config.persistentAssets.Add(string.Empty);
                    }

                    EditorGUILayout.BeginScrollView(Vector2.zero);
                    {
                        for (int ii = 0; ii < _config.persistentAssets.Count; ++ii)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUI.enabled = false;
                                GUILayout.TextField(_config.persistentAssets[ii], GUILayout.ExpandWidth(true));
                                GUI.enabled = true;

                                if (GUILayout.Button("...", GUILayout.ExpandWidth(false)))
                                {
                                    string selectedPath = EditorUtility.OpenFilePanel("Path", _config.persistentAssets[ii], "");
                                    if (!string.IsNullOrEmpty(selectedPath))
                                    {
                                        if (selectedPath.StartsWith(Application.dataPath))
                                            _config.persistentAssets[ii] = AssetBundleUtils.ConvertToAssetPath(selectedPath);
                                        else
                                            ShowNotification(new GUIContent("不能在Assets目录之外！"));
                                    }
                                }

                                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                                {
                                    _config.persistentAssets.RemoveAt(ii);
                                    --ii;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void Draw()
        {
            InitStyles();

            // 工具栏
            EditorGUILayout.BeginHorizontal(Styles.toolbar);
            {
                if (GUILayout.Button("Add", EditorStyles.toolbarButton))
                {
                    if (_selectedNode == null)
                        SetSelected(_config.AddNode(_config.allDependNodes.Count > 0 ? _config.allDependNodes[_config.allDependNodes.Count - 1].level : 0));
                    else
                        SetSelected(_pendingSelection = _config.InsertNode(_config.allDependNodes.IndexOf(_selectedNode) + 1, _selectedNode.level));
                }

                if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
                {
                    if (_selectedNode != null)
                    {
                        _config.RemoveNode(_selectedNode);
                        _selectedNode = null;
                    }
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    Save();
                }

                // 配置文件格式
                GUILayout.Label("Format:");
                _config.bundleInfoFileFormt = (AssetBundleBuildConfig.Format)EditorGUILayout.EnumPopup(_config.bundleInfoFileFormt, EditorStyles.toolbarPopup, GUILayout.Width(50));

                // 配置文件路径
                GUILayout.Label("Path:");
                GUI.enabled = false;
                _config.bundlePath = GUILayout.TextField(_config.bundlePath, GUILayout.MinWidth(120f), GUILayout.ExpandWidth(true));
                GUI.enabled = true;
                if (GUILayout.Button("...", EditorStyles.toolbarButton))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("Path", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                        _config.bundlePath = selectedPath.Replace("\\", "/");
                }

                // 节点层级控制
                GUILayout.Space(20f);
                if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    if (_selectedNode != null)
                    {
                        int idx = _config.allDependNodes.IndexOf(_selectedNode);
                        if (idx > 0 && _config.allDependNodes[idx - 1].level >= _selectedNode.level)
                            ++_selectedNode.level;
                    }
                }
                if (GUILayout.Button("-", EditorStyles.miniButtonMid, GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    if (_selectedNode != null && _selectedNode.level > 0)
                        --_selectedNode.level;
                }
                if (GUILayout.Button("x", EditorStyles.miniButtonRight, GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    if (_selectedNode != null)
                    {
                        _selectedNode.needDelete = true;
                        _selectedNode = null;
                    }
                }
                if (GUILayout.Button("U", EditorStyles.miniButtonLeft, GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    if (_selectedNode != null)
                        _config.PopNode(_selectedNode, true);
                }
                if (GUILayout.Button("D", EditorStyles.miniButtonRight, GUILayout.Width(20f), GUILayout.Height(16f)))
                {
                    if (_selectedNode != null)
                        _config.PopNode(_selectedNode, false);
                }

                GUILayout.FlexibleSpace();

                // 目标平台
                GUILayout.Label("Target:");
                _buildTarget = (BuildTarget)EditorGUILayout.EnumPopup(_buildTarget, EditorStyles.toolbarPopup, GUILayout.Width(150));

                // 编版本
                if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                {
                    _beginBuild = true;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                // 依赖层级结构
                DrawHierarchy();

                // 分割线
                DrawSplit();

                // 选中节点信息
                DrawInspector(_selectedNode);
            }
            EditorGUILayout.EndHorizontal();

            if (GUI.changed)
                EditorUtility.SetDirty(_config);
        }

        private void Save()
        {
            if (LoadAssetAtPath<AssetBundleBuildConfig>(configPath) == null)
                AssetDatabase.CreateAsset(_config, configPath);
            else
                EditorUtility.SetDirty(_config);
        }

        private void Build()
        {
            Save();
            BuildAssetBundles();
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniSimple.Editor.Prefab
{
    public class UIPrefabBinder : EditorWindow
    {
        private readonly Dictionary<string, string> _componentMapping = new()
        {
            { "Btn", "Button" }, // 命名以 Btn 结尾 -> Button 组件
            { "Txt", "Text" }, // 命名以 Txt 结尾 -> Text 组件
            { "Img", "Image" }, // 命名以 Img 结尾 -> Image 组件
            { "Raw", "RawImage" }, // 命名以 Raw 结尾 -> RawImage 组件
            { "Tog", "Toggle" }, // 命名以 Tog 结尾 -> Toggle 组件
            { "Sli", "Slider" }, // 命名以 Sli 结尾 -> Slider 组件
            { "Input", "InputField" }, // 命名以 Input 结尾 -> InputField 组件
            { "Scroll", "ScrollRect" }, // 命名以 Scroll 结尾 -> ScrollRect 组件
            { "Trans", "Transform" }, // 命名以 Trans 结尾 -> Transform
            { "Go", "GameObject" } // 命名以 Go 结尾 -> GameObject
        };

        private GameObject _targetObj;
        private string _savePath = "Assets/Scripts/UI/";

        [MenuItem("Tools/UI Prefab Binder Tool")]
        private static void ShowWindow()
        {
            GetWindow<UIPrefabBinder>("UI Prefab Binder");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI 绑定代码生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _targetObj = (GameObject)EditorGUILayout.ObjectField("目标预制体", _targetObj, typeof(GameObject), true);
            _savePath = EditorGUILayout.TextField("保存路径", _savePath);
            EditorGUILayout.Space();

            var sbText = new StringBuilder("组件对应的结尾命名\n");
            foreach (var kvp in _componentMapping)
            {
                sbText.AppendLine($"{kvp.Key} : {kvp.Value}");
            }

            EditorGUILayout.LabelField(sbText.ToString(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("生成 C# 脚本", GUILayout.Height(30)))
            {
                if (_targetObj is null)
                {
                    ShowNotification(new GUIContent("请先拖入目标对象！"));
                    return;
                }

                GenerateCode();
            }
        }

        private void GenerateCode()
        {
            // 1. 准备数据
            var className = _targetObj.name.Replace(" ", ""); // 类名去除空格
            var sbFields = new StringBuilder();
            var sbBind = new StringBuilder();

            // 获取所有的子节点
            var allChildren = _targetObj.GetComponentsInChildren<Transform>(true);

            foreach (var child in allChildren)
            {
                // 跳过根节点自己
                if (child == _targetObj.transform) continue;

                var childName = child.name;
                var typeName = GetTypeBySuffix(childName);

                // 如果匹配到了规则
                if (!string.IsNullOrEmpty(typeName))
                {
                    // 生成字段定义： public Button startBtn;
                    sbFields.AppendLine($"\tpublic {typeName} {childName};");

                    // 生成查找路径
                    var path = GetPath(_targetObj.transform, child);

                    // 生成绑定代码逻辑 (如果是 GameObject 不需要 GetComponent)
                    if (typeName == "GameObject")
                    {
                        sbBind.AppendLine($"\t\t\t\t{childName} = transform.Find(\"{path}\").gameObject;");
                    }
                    else if (typeName == "Transform")
                    {
                        sbBind.AppendLine($"\t\t\t\t{childName} = transform.Find(\"{path}\");");
                    }
                    else
                    {
                        sbBind.AppendLine($"\t\t\t\t{childName} = transform.Find(\"{path}\").GetComponent<{typeName}>();");
                    }
                }
            }

            // 2. 组装完整的类内容
            var sbClass = new StringBuilder();
            sbClass.AppendLine("using UnityEngine;");
            sbClass.AppendLine("using UnityEngine.UI;");
            sbClass.AppendLine("");
            sbClass.AppendLine($"public class {className} : MonoBehaviour");
            sbClass.AppendLine("{");

            // 字段区域
            sbClass.Append(sbFields.ToString());
            sbClass.AppendLine("");

            // 自动绑定方法 (ContextMenu 允许在 Inspector 右键点击执行)
            sbClass.AppendLine("\t\t// [ContextMenu(\"Auto Bind\")]");
            sbClass.AppendLine("\t\tpublic void AutoBind()");
            sbClass.AppendLine("\t\t{");
            sbClass.Append(sbBind.ToString());
            sbClass.AppendLine("\t\t}");

            // Awake 时可选调用
            sbClass.AppendLine("");
            sbClass.AppendLine("\t\tprivate void Awake()");
            sbClass.AppendLine("\t\t{");

            // 如果不想手动绑定，可以取消注释下面这行，运行时自动查找
            sbClass.AppendLine("\t\t\t\tAutoBind();");
            sbClass.AppendLine("\t\t}");
            sbClass.AppendLine("}");

            // 3. 写入文件
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }

            var fullPath = Path.Combine(_savePath, className + ".cs");
            File.WriteAllText(fullPath, sbClass.ToString(), Encoding.UTF8);

            // 4. 刷新资源
            AssetDatabase.Refresh();
        }

        // 根据后缀判断类型
        private string GetTypeBySuffix(string suffixName)
        {
            foreach (var kvp in _componentMapping)
            {
                if (suffixName.EndsWith(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        // 获取相对路径
        private string GetPath(Transform root, Transform target)
        {
            string path = target.name;
            while (target.parent != null && target.parent != root)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }

            return path;
        }
    }
}
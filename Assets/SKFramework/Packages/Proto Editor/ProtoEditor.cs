#if UNITY_EDITOR
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using LitJson;
using UnityEngine;
using UnityEditor;

namespace SK.Framework.Sockets
{
    /// <summary>
    /// Protobuf通信协议类编辑器
    /// </summary>
    [Package("Proto Editor", "1.0.0")]
    public class ProtoEditor : EditorWindow
    {
        [MenuItem("Multiplayer/Proto Editor")]
        public static void Open()
        {
            GetWindow<ProtoEditor>("Proto Editor").Show();
        }

        /// <summary>
        /// 修饰符类型
        /// </summary>
        public enum ModifierType
        {
            /// <summary>
            /// 必需字段
            /// </summary>
            Required,
            /// <summary>
            /// 可选字段
            /// </summary>
            Optional,
            /// <summary>
            /// 可重复字段
            /// </summary>
            Repeated
        }

        /// <summary>
        /// 字段类型
        /// </summary>
        public enum FieldsType
        {
            Double,
            Float,
            Int,
            Long,
            Bool,
            String,
            Custom,
        }

        /// <summary>
        /// 类
        /// </summary>
        public class Message
        {
            /// <summary>
            /// 类名
            /// </summary>
            public string name = "New Message";
            /// <summary>
            /// 所有字段
            /// </summary>
            public List<Fields> fieldsList = new List<Fields>(0);

            public bool IsValid()
            {
                bool flag = !string.IsNullOrEmpty(name);
                for (int i = 0; i < fieldsList.Count; i++)
                {
                    flag &= fieldsList[i].IsValid();
                    if (!flag) return false;
                    for (int j = 0; j < fieldsList.Count; j++)
                    {
                        if (i != j)
                        {
                            flag &= fieldsList[i].flag != fieldsList[j].flag;
                        }
                        if (!flag) return false;
                    }
                }
                return flag;
            }
        }
        /// <summary>
        /// 字段
        /// </summary>
        public class Fields
        {
            public ModifierType modifier;
            public FieldsType type;
            public string typeName;
            public string name;
            public int flag;

            public Fields() { }

            public Fields(int flag)
            {
                modifier = ModifierType.Required;
                type = FieldsType.String;
                name = "FieldsName"; 
                typeName = "FieldsType";
                this.flag = flag;
            }

            public bool IsValid()
            {
                return type != FieldsType.Custom || (type == FieldsType.Custom && !string.IsNullOrEmpty(typeName));
            }
        }

        //.proto文件名称
        private string fileName;
        //存储所有类
        private List<Message> messages = new List<Message>();
        //滚动值
        private Vector2 scroll;
        //字段存储折叠状态
        private readonly Dictionary<Message, bool> foldoutDic = new Dictionary<Message, bool>();
        //json文件存放路径
        private const string workspacePath = "/SKFramework/Packages/Proto Editor/Workspace";

        private void OnGUI()
        {
            OnEditGUI();
            OnBottomMenuGUI();
        }

        //编辑
        private void OnEditGUI()
        {
            //编辑.proto文件名称
            fileName = EditorGUILayout.TextField(".proto File Name", fileName);

            EditorGUILayout.Space();

            //滚动视图
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < messages.Count; i++)
            {
                var message = messages[i];

                GUILayout.BeginHorizontal();
                foldoutDic[message] = EditorGUILayout.Foldout(foldoutDic[message], message.name, true);
                //插入新类
                if (GUILayout.Button("+", GUILayout.Width(20f)))
                {
                    Message insertMessage = new Message();
                    messages.Insert(i + 1, insertMessage);
                    foldoutDic.Add(insertMessage, true);
                    Repaint();
                    return;
                }
                //删除该类
                if (GUILayout.Button("-", GUILayout.Width(20f)))
                {
                    messages.Remove(message);
                    foldoutDic.Remove(message);
                    Repaint();
                    return;
                }
                GUILayout.EndHorizontal();

                //如果折叠栏为打开状态 绘制具体字段内容
                if (foldoutDic[message])
                {
                    //编辑类名
                    message.name = EditorGUILayout.TextField("Name", message.name);
                    //字段数量为0 提供按钮创建
                    if (message.fieldsList.Count == 0)
                    {
                        if (GUILayout.Button("New Field"))
                        {
                            message.fieldsList.Add(new Fields(1));
                        }
                    }
                    else
                    {
                        for (int j = 0; j < message.fieldsList.Count; j++)
                        {
                            var item = message.fieldsList[j];
                            GUILayout.BeginHorizontal();
                            //修饰符类型
                            item.modifier = (ModifierType)EditorGUILayout.EnumPopup(item.modifier);
                            //字段类型
                            item.type = (FieldsType)EditorGUILayout.EnumPopup(item.type);
                            if (item.type == FieldsType.Custom)
                            {
                                item.typeName = GUILayout.TextField(item.typeName);
                            }
                            //编辑字段名
                            item.name = EditorGUILayout.TextField(item.name);
                            GUILayout.Label("=", GUILayout.Width(15f));
                            //分配标识号
                            item.flag = EditorGUILayout.IntField(item.flag, GUILayout.Width(50f));
                            //插入新字段
                            if (GUILayout.Button("+", GUILayout.Width(20f)))
                            {
                                message.fieldsList.Insert(j + 1, new Fields(message.fieldsList.Count + 1));
                                Repaint();
                                return;
                            }
                            //删除该字段
                            if (GUILayout.Button("-", GUILayout.Width(20f)))
                            {
                                message.fieldsList.Remove(item);
                                Repaint();
                                return;
                            }
                            GUILayout.EndHorizontal();
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
        }

        //底部菜单
        private void OnBottomMenuGUI()
        {
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            //创建新的类
            if (GUILayout.Button("New Message"))
            {
                Message message = new Message();
                messages.Add(message);
                foldoutDic.Add(message, true);
            }
            //清空所有类
            if (GUILayout.Button("Clear Messages"))
            {
                //确认弹窗
                if (EditorUtility.DisplayDialog("Confirm", "是否确认清空所有类型？", "确认", "取消"))
                {
                    //清空
                    messages.Clear();
                    foldoutDic.Clear();
                    //重新绘制
                    Repaint();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            //导出Json
            if (GUILayout.Button("Export Json File"))
            {
                if (!ContentIsValid())
                {
                    EditorUtility.DisplayDialog("Error", "请按以下内容逐项检查：\r\n1.proto File Name是否为空\r\n2.message类名是否为空\r\n" +
                        "3.字段类型为自定义时 是否填写了类型名称\r\n4.标识号是否唯一", "ok");
                }
                else
                {
                    //文件夹路径
                    string dirPath = Application.dataPath + workspacePath;
                    //文件夹不存在则创建
                    if (!Directory.Exists(dirPath))
                        Directory.CreateDirectory(dirPath);
                    //json文件路径
                    string filePath = dirPath + "/" + fileName + ".json";
                    if (EditorUtility.DisplayDialog("Confirm", "是否保存当前编辑内容到" + filePath, "确认", "取消"))
                    {
                        //序列化
                        string json = JsonMapper.ToJson(messages);
                        //写入
                        File.WriteAllText(filePath, json);
                        //刷新
                        AssetDatabase.Refresh();
                    }
                }
            }
            //导入Json
            if (GUILayout.Button("Import Json File"))
            {
                //选择json文件路径
                string filePath = EditorUtility.OpenFilePanel("Import Json File", Application.dataPath + workspacePath, "json");
                //判断路径有效性
                if (File.Exists(filePath))
                {
                    //读取json内容
                    string json = File.ReadAllText(filePath);
                    //清空
                    messages.Clear();
                    foldoutDic.Clear();
                    //反序列化
                    messages = JsonMapper.ToObject<List<Message>>(json);
                    //填充字典
                    for (int i = 0; i < messages.Count; i++)
                    {
                        foldoutDic.Add(messages[i], true);
                    }
                    //文件名称
                    FileInfo fileInfo = new FileInfo(filePath);
                    fileName = fileInfo.Name.Replace(".json", "");
                    //重新绘制
                    Repaint();
                    return;
                }
            }
            GUILayout.EndHorizontal();

            //生成proto文件
            if (GUILayout.Button("Generate Proto File"))
            {
                if (!ContentIsValid())
                {
                    EditorUtility.DisplayDialog("Error", "请按以下内容逐项检查：\r\n1.proto File Name是否为空\r\n2.message类名是否为空\r\n" +
                        "3.字段类型为自定义时 是否填写了类型名称\r\n4.标识号是否唯一", "ok");
                }
                else
                {
                    string protoFilePath = EditorUtility.SaveFilePanel("Generate Proto File", Application.dataPath, fileName, "proto");
                    if (!string.IsNullOrEmpty(protoFilePath))
                    {
                        StringBuilder protoContent = new StringBuilder();
                        for (int i = 0; i < messages.Count; i++)
                        {
                            var message = messages[i];
                            StringBuilder sb = new StringBuilder();
                            sb.Append("message " + message.name + "\r\n" + "{\r\n");
                            for (int n = 0; n < message.fieldsList.Count; n++)
                            {
                                var field = message.fieldsList[n];
                                //缩进
                                sb.Append("    ");
                                //修饰符
                                sb.Append(field.modifier.ToString().ToLower());
                                //空格
                                sb.Append(" ");
                                //如果是自定义类型 拼接typeName 
                                switch (field.type)
                                {
                                    case FieldsType.Int: sb.Append("int32"); break;
                                    case FieldsType.Long: sb.Append("int64"); break;
                                    case FieldsType.Custom: sb.Append(field.typeName); break;
                                    default: sb.Append(field.type.ToString().ToLower()); break;
                                }
                                //空格
                                sb.Append(" ");
                                //字段名
                                sb.Append(field.name);
                                //等号
                                sb.Append(" = ");
                                //标识号
                                sb.Append(field.flag);
                                //分号及换行符
                                sb.Append(";\r\n");
                            }
                            sb.Append("}\r\n");
                            protoContent.Append(sb.ToString());
                        }
                        //写入文件
                        File.WriteAllText(protoFilePath, protoContent.ToString());
                        //刷新(假设路径在工程内 可以避免手动刷新才看到)
                        AssetDatabase.Refresh();
                        //打开该文件夹
                        FileInfo fileInfo = new FileInfo(protoFilePath);
                        Process.Start(fileInfo.Directory.FullName);
                    }
                }
            }

            //创建.bat文件
            if (GUILayout.Button("Create .bat"))
            {
                //选择路径（protogen.exe所在的文件夹路径）
                string rootPath = EditorUtility.OpenFolderPanel("Create .bat file（protogen.exe所在的文件夹）", Application.dataPath, string.Empty);
                //取消
                if (string.IsNullOrEmpty(rootPath)) return;
                //protogen.exe文件路径
                string protogenPath = rootPath + "/protogen.exe";
                //不是protogen.exe所在的文件夹路径
                if (!File.Exists(protogenPath))
                {
                    EditorUtility.DisplayDialog("Error", "请选择protogen.exe所在的文件夹路径", "ok");
                }
                else
                {
                    string protoPath = rootPath + "/proto";
                    DirectoryInfo di = new DirectoryInfo(protoPath);
                    //获取所有.proto文件信息
                    FileInfo[] protos = di.GetFiles("*.proto");
                    //使用StringBuilder拼接字符串
                    StringBuilder sb = new StringBuilder();
                    //遍历
                    for (int i = 0; i < protos.Length; i++)
                    {
                        string proto = protos[i].Name;
                        //拼接编译指令
                        sb.Append(rootPath + @"/protogen.exe -i:proto\" + proto + @" -o:cs\" + proto.Replace(".proto", ".cs") + "\r\n");
                    }
                    sb.Append("pause");

                    //生成".bat文件"
                    string batPath = $"{rootPath}/run.bat";
                    File.WriteAllText(batPath, sb.ToString());
                    //打开该文件夹
                    Process.Start(rootPath);
                }
            }
        }

        //编辑的内容是否有效
        private bool ContentIsValid()
        {
            bool flag = !string.IsNullOrEmpty(fileName);
            flag &= messages.Count > 0;
            for (int i = 0; i < messages.Count; i++)
            {
                flag &= messages[i].IsValid();
                if (!flag) break;
            }
            return flag;
        }
    }
}
#endif
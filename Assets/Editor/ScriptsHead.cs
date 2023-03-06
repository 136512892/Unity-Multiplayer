using System;
using System.IO;
using System.Text;

public class ScriptHead : UnityEditor.AssetModificationProcessor
{
    private static void OnWillCreateAsset(string path)
    {
        path = path.Replace(".meta", "");
        if (path.EndsWith(".cs"))
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("/****************************************************");
            string[] strs = path.Split('/');
            string scriptName = strs[strs.Length - 1];
            sb.AppendLine(string.Format("*	文件：{0}", scriptName));
            sb.AppendLine(string.Format("*	作者：{0}", "张寿昆(CoderZ)"));//Environment.UserName
            sb.AppendLine("*	邮箱：136512892@qq.com");
            sb.AppendLine(string.Format("*	日期：{0}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
            sb.AppendLine("*	功能：Func");
            sb.AppendLine("*****************************************************/");
            sb.AppendLine("\n");
            string str = File.ReadAllText(path);
            sb.AppendLine(str);

            File.WriteAllText(path, sb.ToString());
        }
    }
}
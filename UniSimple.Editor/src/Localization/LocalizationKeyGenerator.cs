using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UniSimple.Editor.Localization
{
    public static class LocalizationKeyGenerator
    {
        [MenuItem("Tools/Localization/Generate Keys from CSV")]
        public static void GenerateKeysFromCsv()
        {
            // 选择 CSV 文件
            var csvPath = EditorUtility.OpenFilePanel("Select CSV File", Application.dataPath, "csv");
            if (string.IsNullOrEmpty(csvPath))
            {
                Debug.Log("Select operation cancelled");
                return;
            }

            // 选择输出路径
            var outputPath = EditorUtility.SaveFilePanel("Save Generated Class", Application.dataPath, "LocalizationKeys", "cs");
            if (string.IsNullOrEmpty(outputPath))
            {
                Debug.Log("Save operation cancelled");
                return;
            }

            try
            {
                Debug.Log($"Ready to generated: {outputPath}");
                GenerateClass(csvPath, outputPath);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"Generation failed: {e.Message}");
            }
        }

        private static void GenerateClass(string csvPath, string outputPath)
        {
            // 读取 CSV 文件
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            if (lines.Length == 0)
            {
                throw new Exception("CSV file is empty");
            }

            // 生成类
            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// This file is auto-generated.");
            sb.AppendLine("// Do not modify manually.");
            sb.AppendLine($"// Generated from: {Path.GetFileName(csvPath)}");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("public static class LocalizationKeys");
            sb.AppendLine("{");

            // 假设第一列是 key，跳过标题行
            var skipFirstLine = true;
            foreach (var line in lines)
            {
                if (skipFirstLine)
                {
                    skipFirstLine = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 获取第一列作为 key
                var key = GetFirstColumn(line);
                if (string.IsNullOrEmpty(key))
                    continue;

                // 转换为常量名
                var constName = ConvertToConstName(key);

                // 生成常量
                sb.AppendLine($"    public const string {constName} = \"{key}\";");
            }

            sb.AppendLine("}");

            // 写入文件
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 从 CSV 行获取第一列
        /// </summary>
        private static string GetFirstColumn(string csvLine)
        {
            // 简单的 CSV 解析，处理引号
            if (csvLine.StartsWith("\""))
            {
                // 处理带引号的情况
                var endIndex = csvLine.IndexOf("\"", 1, StringComparison.Ordinal);
                if (endIndex > 0)
                {
                    return csvLine.Substring(1, endIndex - 1);
                }
            }

            // 不带引号的情况
            var commaIndex = csvLine.IndexOf(',');
            if (commaIndex > 0)
            {
                return csvLine.Substring(0, commaIndex).Trim();
            }

            return csvLine.Trim();
        }

        /// <summary>
        /// 将 key 转换为大写下划线格式的常量名
        /// </summary>
        private static string ConvertToConstName(string key)
        {
            // 移除非法字符，只保留字母、数字和下划线
            var cleaned = Regex.Replace(key, "[^a-zA-Z0-9_]", "_");

            // 转换驼峰为下划线
            var snakeCase = Regex.Replace(cleaned, "([a-z])([A-Z])", "$1_$2");

            // 转大写
            var upperCase = snakeCase.ToUpper();

            // 确保不以数字开头
            if (char.IsDigit(upperCase[0]))
            {
                upperCase = "_" + upperCase;
            }

            // 移除连续的下划线
            upperCase = Regex.Replace(upperCase, "_+", "_");

            // 移除首尾下划线
            upperCase = upperCase.Trim('_');

            return string.IsNullOrEmpty(upperCase) ? "INVALID_KEY" : upperCase;
        }
    }
}
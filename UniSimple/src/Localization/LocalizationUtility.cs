using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UniSimple.Localization
{
    public static class LocalizationUtility
    {
        private static readonly StringBuilder SharedBuilder = new(512);

        internal static void ParseCsv(string content, Dictionary<string, string> dict)
        {
            if (string.IsNullOrEmpty(content)) return;

            // 假设平均一行 50 字符。EnsureCapacity 只有在当前容量不足时才会分配内存
            // 注意：EnsureCapacity 是 .NET Standard 2.1 API
            int estimatedRows = content.Length / 50;
            dict.EnsureCapacity(estimatedRows);

            using var reader = new StringReader(content);
            string line;

            // 复用 StringBuilder 清空

            while ((line = reader.ReadLine()) != null)
            {
                // 将行转换为 Span，后续所有操作都在栈上进行，不分配堆内存
                var span = line.AsSpan();

                // 跳过空行和注释
                if (span.IsEmpty || span.IsWhiteSpace()) continue;

                // 检查注释 // 或 #
                var trimmedStart = span.TrimStart();
                if (trimmedStart.StartsWith("//") || trimmedStart.StartsWith("#")) continue;

                // 查找分隔符
                var separatorIndex = span.IndexOf('=');
                if (separatorIndex <= 0) continue;

                // Slice 切片不产生新字符串，Trim 也是在 Span 上操作
                var keySpan = span.Slice(0, separatorIndex).Trim();
                var valueSpan = span.Slice(separatorIndex + 1).Trim();

                // 处理外层引号 "..."
                if (valueSpan.Length >= 2 && valueSpan[0] == '"' && valueSpan[^1] == '"')
                {
                    valueSpan = valueSpan.Slice(1, valueSpan.Length - 2);
                }

                // --- 最终生成字符串 ---

                var finalKey = keySpan.ToString(); // 这里必须分配一次内存生成 Key
                string finalValue;

                // 检查是否包含特殊字符（需转义），如果不包含，直接 ToString，最快路径
                // 检查 " 或 \
                if (valueSpan.IndexOf('"') < 0 && valueSpan.IndexOf('\\') < 0)
                {
                    finalValue = valueSpan.ToString();
                }
                else
                {
                    // 慢路径：包含转义字符，使用 StringBuilder 拼接
                    finalValue = ProcessEscapes(valueSpan);
                }

                dict[finalKey] = finalValue;
            }
        }

        private static string ProcessEscapes(ReadOnlySpan<char> input)
        {
            SharedBuilder.Clear();

            // 预估容量，避免 Builder 扩容
            SharedBuilder.EnsureCapacity(input.Length);

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];

                // 处理 "" -> "
                if (c == '"' && i + 1 < input.Length && input[i + 1] == '"')
                {
                    SharedBuilder.Append('"');
                    i++; // 跳过下一个字符
                    continue;
                }

                // 处理 \n -> 换行符
                if (c == '\\' && i + 1 < input.Length && input[i + 1] == 'n')
                {
                    SharedBuilder.Append('\n');
                    i++; // 跳过下一个字符
                    continue;
                }

                SharedBuilder.Append(c);
            }

            return SharedBuilder.ToString();
        }
    }
}
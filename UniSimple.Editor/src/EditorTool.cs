using UnityEditor;
using UnityEngine;

namespace UniSimple.Editor;

public static class EditorTool
{
    private static string Suffix => $"{Application.companyName}.{Application.productName}";

    public static bool HasKey(string key)
    {
        return EditorPrefs.HasKey($"{Suffix}.{key}");
    }

    public static string GetString(string key)
    {
        return EditorPrefs.GetString($"{Suffix}.{key}");
    }

    public static void SetString(string key, string value)
    {
        EditorPrefs.SetString($"{Suffix}.{key}", value);
    }
}
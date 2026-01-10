using System;

namespace UniSimple.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UIWindowSettingAttribute : Attribute
    {
        public readonly UILayer Layer;
        public readonly string Name;
        public readonly string AssetPath;

        public UIWindowSettingAttribute(UILayer layer, string name, string assetPath)
        {
            Layer = layer;
            Name = name;
            AssetPath = assetPath;
        }
    }
}
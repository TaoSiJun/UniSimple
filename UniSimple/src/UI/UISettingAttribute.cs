using System;

namespace UniSimple.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UISettingAttribute : Attribute
    {
        public readonly UILayer Layer;
        public readonly string Name;
        public readonly string Address;

        public UISettingAttribute(UILayer layer, string name, string address)
        {
            Layer = layer;
            Name = name;
            Address = address;
        }
    }
}
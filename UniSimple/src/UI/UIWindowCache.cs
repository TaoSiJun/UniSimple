using System;
using System.Collections.Generic;

namespace UniSimple.UI
{
    internal sealed class UIWindowCache
    {
        public Dictionary<Type, UIWindow> Opened { get; } = new(100);
        public Dictionary<Type, UIWindow> Cached { get; } = new(100);
    }
}
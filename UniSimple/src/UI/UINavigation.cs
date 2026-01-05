using System.Collections.Generic;

namespace UniSimple.UI
{
    internal sealed class UINavigation
    {
        public List<UIWindow> Stack { get; } = new(100);

        /// <summary>
        /// 入栈
        /// </summary>
        public void Push(UIWindow window)
        {
            if (!window.IsStack) return;

            if (Stack.Count > 0)
            {
                var top = Stack[^1];
                top.OnPause();
            }

            if (!Stack.Contains(window))
            {
                Stack.Add(window);
                window.OnResume();
            }
        }

        /// <summary>
        /// 出栈
        /// </summary>
        public void Pop(UIWindow window)
        {
            if (!window.IsStack) return;

            if (Stack.Remove(window))
            {
                window.OnPause();
            }

            if (Stack.Count > 0)
            {
                var top = Stack[^1];
                top.OnResume();
            }
        }
    }
}
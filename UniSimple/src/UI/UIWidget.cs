using UnityEngine;

namespace UniSimple.UI
{
    public abstract class UIWidget
    {
        public GameObject GameObject { get; private set; }
        public Transform Transform => GameObject?.transform;
        public RectTransform RectTransform { get; private set; }

        public bool Visible
        {
            get => GameObject.activeSelf;
            set => GameObject.SetActive(value);
        }
    }
}
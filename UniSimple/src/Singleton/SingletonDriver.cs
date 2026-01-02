using UnityEngine;

namespace UniSimple.Singleton
{
    public class SingletonDriver : MonoBehaviour
    {
        private void Update()
        {
            SingletonManager.Update(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            SingletonManager.DestroyAll();
        }

        private void OnApplicationQuit()
        {
            SingletonManager.DestroyAll();
        }
    }
}
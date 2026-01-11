using UnityEngine;

namespace UniSimple.Singleton
{
    public class SingletonDriver : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("SingletonDriver is running.");
        }

        private void Update()
        {
            SingletonManager.Update(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            SingletonManager.DestroyAll();
        }
    }
}
using UnityEngine;

namespace toshi.VLiveKit.VLiveCameraUnit
{
    public sealed class VLiveCameraSandboxProbe : MonoBehaviour
    {
        [SerializeField]
        private string note = "Sandbox test script";

        public string Note => note;
    }
}

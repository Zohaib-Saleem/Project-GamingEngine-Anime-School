using UnityEngine;

namespace KikiNgao.SimpleBikeControl
{
    public class InputManager : MonoBehaviour
    {
        public float horizontal, vertical;
        public KeyCode enterExitKey = KeyCode.F;
        public KeyCode speedUpKey = KeyCode.LeftShift;
        [HideInInspector]
        public bool enterExitVehicle;
        [HideInInspector]
        public bool speedUp;

        private void Update()
        {
            horizontal = ControlFreak2.CF2Input.GetAxis("Horizontal");
            vertical = ControlFreak2.CF2Input.GetAxis("Vertical");

            enterExitVehicle = ControlFreak2.CF2Input.GetKeyDown(enterExitKey);

            // read the held state every frame so a missed key-up can't leave speed up stuck on
            speedUp = ControlFreak2.CF2Input.GetKey(speedUpKey);
        }

    }
}

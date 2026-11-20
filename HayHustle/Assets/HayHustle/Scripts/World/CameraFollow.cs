using UnityEngine;
using UnityEngine.InputSystem;

namespace HayHustle
{
    /// <summary>Third-person camera. Right-drag to rotate, scroll to zoom. Orbits the farm on the menus.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 14f, Yaw = 180f, Pitch = 40f;
        float menuYaw = 200f;
        bool showcase;
        Vector3 showPos, showLook;

        /// <summary>Fixed camera shot (character select).</summary>
        public void Showcase(Vector3 pos, Vector3 lookAt)
        {
            showcase = true;
            showPos = pos;
            showLook = lookAt;
        }

        public void ClearShowcase() => showcase = false;

        void LateUpdate()
        {
            if (Target == null && showcase)
            {
                float k = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, showPos, k);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(showLook - showPos), k);
                return;
            }
            if (Target == null)
            {
                menuYaw += 4f * Time.unscaledDeltaTime;
                var r = Quaternion.Euler(28f, menuYaw, 0f);
                transform.position = GameManager.PastureCenter + Vector3.up * 2f - r * Vector3.forward * 55f;
                transform.rotation = r;
                return;
            }

            var mouse = Mouse.current;
            bool menuOpen = HUD.I != null && HUD.I.BlocksGameplay;
            if (mouse != null && !menuOpen)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    Yaw += d.x * 0.2f;
                    Pitch = Mathf.Clamp(Pitch - d.y * 0.2f, 15f, 75f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) Distance = Mathf.Clamp(Distance - Mathf.Sign(scroll) * 1.5f, 3f, 28f);
            }

            var rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 focus = Target.position + Vector3.up * 1.2f;
            Vector3 desired = focus - rot * Vector3.forward * Distance;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-10f * Time.deltaTime));
            transform.rotation = rot;
        }
    }
}

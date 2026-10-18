using UnityEngine;

namespace BattleSummoner.UI
{
    /// <summary>
    /// ShowcaseTurntableController provides smooth cinematic turntable orbit,
    /// mouse drag rotation, zoom, and preset anatomical focus framing for character inspection.
    /// </summary>
    public class ShowcaseTurntableController : MonoBehaviour
    {
        [Header("Target & Focus")]
        public Transform target;
        public Vector3 focalPoint = new Vector3(0f, 0.95f, 0f);

        [Header("Orbit Parameters")]
        public float distance = 2.85f;
        public float minDistance = 0.9f;
        public float maxDistance = 5.0f;
        public float xSpeed = 120.0f;
        public float ySpeed = 80.0f;
        public float yMinLimit = -15f;
        public float yMaxLimit = 60f;
        public float zoomSpeed = 2.0f;
        public float damping = 5.0f;

        [Header("Turntable Auto-Rotation")]
        public bool autoRotate = false;
        public float autoRotateSpeed = 12.0f;

        private float currentX = 0.0f;
        private float currentY = 5.0f;
        private float targetX = 0.0f;
        private float targetY = 5.0f;
        private float targetDistance;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            targetX = currentX = angles.y;
            targetY = currentY = angles.x;
            targetDistance = distance;
        }

        private void LateUpdate()
        {
            // Mouse Drag Orbit (ignore when dragging UI sliders)
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButton(0) && !isOverUI)
            {
                targetX += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                targetY -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                targetY = ClampAngle(targetY, yMinLimit, yMaxLimit);
            }
            else if (autoRotate)
            {
                targetX += autoRotateSpeed * Time.deltaTime;
            }

            // Mouse Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f && !isOverUI)
            {
                targetDistance -= scroll * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
            }

            // Smooth Interpolation
            currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * damping);
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * damping);
            distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * damping);

            Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
            Vector3 targetCenter = target != null ? target.position + focalPoint : focalPoint;
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + targetCenter;

            transform.rotation = rotation;
            transform.position = position;
        }

        public void FocusFullBody()
        {
            focalPoint = new Vector3(-0.32f, 0.95f, 0f);
            targetDistance = 2.70f;
            targetY = 4f;
        }

        public void FocusUpperBody()
        {
            focalPoint = new Vector3(-0.32f, 1.25f, 0f);
            targetDistance = 1.6f;
            targetY = 2f;
        }

        public void FocusHead()
        {
            focalPoint = new Vector3(-0.32f, 1.55f, 0f);
            targetDistance = 0.95f;
            targetY = 0f;
        }

        public void FocusHands()
        {
            focalPoint = new Vector3(0.12f, 0.90f, 0f);
            targetDistance = 0.90f;
            targetY = 6f;
        }

        public void FocusFeet()
        {
            focalPoint = new Vector3(-0.32f, 0.15f, 0f);
            targetDistance = 1.1f;
            targetY = 12f;
        }

        public void ToggleAutoRotate()
        {
            autoRotate = !autoRotate;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F) angle += 360F;
            if (angle > 360F) angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}

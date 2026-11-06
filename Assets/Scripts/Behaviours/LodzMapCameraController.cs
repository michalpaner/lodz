using UnityEngine;

namespace SanAndreasUnity.Behaviours
{
    [RequireComponent(typeof(Camera))]
    public class LodzMapCameraController : MonoBehaviour
    {
        [SerializeField] private float minZoom = 500f;
        [SerializeField] private float maxZoom = 30000f;
        [SerializeField] private float zoomSensitivity = 1800f;
        [SerializeField] private float panSpeed = 3000f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Update()
        {
            float zoom = Input.mouseScrollDelta.y;
            _camera.orthographicSize = Mathf.Clamp(
                _camera.orthographicSize - zoom * zoomSensitivity,
                minZoom,
                maxZoom);

            float scale = Mathf.Max(0.25f, _camera.orthographicSize / 10000f);
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            transform.position += new Vector3(horizontal, 0f, vertical)
                * (panSpeed * scale * Time.deltaTime);
        }
    }
}
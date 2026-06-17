using UnityEngine;
using UnityEngine.XR.Hands;

public class QuitGesture : MonoBehaviour
{
    [Tooltip("Durée de maintien avant de quitter (secondes)")]
    [SerializeField] private float confirmDuration = 0.8f;

    private float _holdTimer = 0f;
    private bool  _holding   = false;

    private void OnEnable()  => GestureDetector.OnGestureChanged += OnGestureChanged;
    private void OnDisable() => GestureDetector.OnGestureChanged -= OnGestureChanged;

    private void OnGestureChanged(GestureDetector.Gesture gesture, Handedness _)
    {
        if (gesture == GestureDetector.Gesture.MiddleFinger)
        {
            if (!_holding) _holdTimer = 0f;
            _holding = true;
        }
        else
        {
            _holding = false;
        }
    }

    private void Update()
    {
        if (!_holding) return;
        _holdTimer += Time.deltaTime;
        if (_holdTimer >= confirmDuration)
        {
            Debug.Log("[QuitGesture] Quitter via geste majeur.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
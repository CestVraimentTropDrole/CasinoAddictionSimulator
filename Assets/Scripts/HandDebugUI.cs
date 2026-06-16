using TMPro;
using UnityEngine.XR.Hands;
using UnityEngine;

/// <summary>
/// Affiche en temps réel le geste détecté par GestureDetector
/// sur un Canvas World-Space flottant devant le joueur.
///
/// Setup :
///   1. Créer un Canvas (Render Mode = World Space).
///   2. Ajouter un enfant TextMeshProUGUI.
///   3. Attacher ce script sur le Canvas, assigner le TMP dans l'Inspector.
///   4. Scale du Canvas : 0.001 — Position : ~0.5 m devant la caméra.
/// </summary>
public class HandDebugUI : MonoBehaviour
{
    [Header("Références UI")]
    [SerializeField] private TextMeshProUGUI gestureLabel;

    [Header("Position relative à la caméra")]
    [SerializeField] private Vector3 offsetFromCamera = new Vector3(0f, -0.15f, 0.6f);
    [SerializeField] private bool    followCamera     = true;

    [Header("Couleurs par geste")]
    [SerializeField] private Color colorUnknown  = Color.gray;
    [SerializeField] private Color colorRock     = new Color(0.9f, 0.4f, 0.2f);
    [SerializeField] private Color colorPaper    = new Color(0.2f, 0.8f, 0.4f);
    [SerializeField] private Color colorScissors = new Color(0.2f, 0.6f, 1.0f);

    private Camera _mainCam;

    private static readonly string[] GestureDisplay =
    {
        "❓  Inconnu",
        "✊  Pierre",
        "✋  Feuille",
        "✌️  Ciseaux",
    };

    private void Awake()
    {
        _mainCam = Camera.main;

        if (gestureLabel == null)
            gestureLabel = GetComponentInChildren<TextMeshProUGUI>();

        SetGesture(GestureDetector.Gesture.Unknown);
    }

    private void OnEnable()  => GestureDetector.OnGestureChanged += HandleGestureChanged;
    private void OnDisable() => GestureDetector.OnGestureChanged -= HandleGestureChanged;

    private void LateUpdate()
    {
        if (!followCamera || _mainCam == null) return;

        transform.position = _mainCam.transform.TransformPoint(offsetFromCamera);
        transform.rotation = Quaternion.LookRotation(
            transform.position - _mainCam.transform.position,
            _mainCam.transform.up);
    }

    private void HandleGestureChanged(GestureDetector.Gesture gesture, Handedness _)
        => SetGesture(gesture);

    private void SetGesture(GestureDetector.Gesture gesture)
    {
        if (gestureLabel == null) return;

        gestureLabel.text  = GestureDisplay[(int)gesture];
        gestureLabel.color = gesture switch
        {
            GestureDetector.Gesture.Rock     => colorRock,
            GestureDetector.Gesture.Paper    => colorPaper,
            GestureDetector.Gesture.Scissors => colorScissors,
            _                                => colorUnknown,
        };
    }
}
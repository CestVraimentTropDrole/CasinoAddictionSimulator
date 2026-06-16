using System;
using UnityEngine;
using UnityEngine.XR.Hands;

public class GestureDetector : MonoBehaviour
{
    public enum Gesture { Unknown, Rock, Paper, Scissors }

    public static event Action<Gesture, Handedness> OnGestureChanged;

    [Header("Main surveillée")]
    [SerializeField] private Handedness watchedHand = Handedness.Right;

    [Header("Seuils (distance tip→paume, en mètres)")]
    [Tooltip("En dessous : doigt replié")]
    [SerializeField] private float curledDistance   = 0.06f;
    [Tooltip("Au dessus : doigt étendu (Ciseaux/Pierre)")]
    [SerializeField] private float extendedDistance = 0.10f;
    [Tooltip("Seuil plus bas pour la Feuille — compense le tracking imprécis des doigts tendus")]
    [SerializeField] private float paperDistance    = 0.08f;

    [Header("Stabilisation")]
    [SerializeField, Min(0f)] private float holdDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;
    [HideInInspector] public float dbgIndex, dbgMiddle, dbgRing, dbgPinky;

    private XRHandSubsystem _subsystem;
    private Gesture         _lastConfirmedGesture = Gesture.Unknown;
    private Gesture         _candidateGesture     = Gesture.Unknown;
    private float           _candidateTimer       = 0f;

    private static readonly (XRHandJointID mcp, XRHandJointID tip)[] Fingers =
    {
        (XRHandJointID.IndexProximal,  XRHandJointID.IndexTip ),
        (XRHandJointID.MiddleProximal, XRHandJointID.MiddleTip),
        (XRHandJointID.RingProximal,   XRHandJointID.RingTip  ),
        (XRHandJointID.LittleProximal, XRHandJointID.LittleTip),
    };

    private void OnEnable()
    {
        var list = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(list);
        if (list.Count > 0) _subsystem = list[0];
        else Debug.LogWarning("[GestureDetector] Aucun XRHandSubsystem trouvé.");
    }

    private void Update()
    {
        if (_subsystem == null) return;

        XRHand hand = watchedHand == Handedness.Right
            ? _subsystem.rightHand : _subsystem.leftHand;

        if (!hand.isTracked) { ResetCandidate(); return; }

        if (!hand.GetJoint(XRHandJointID.Wrist        ).TryGetPose(out Pose wrist ) ||
            !hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out Pose midMcp))
        { ResetCandidate(); return; }

        Vector3 palmCenter = (wrist.position + midMcp.position) * 0.5f;

        float[] dist = new float[4];
        for (int i = 0; i < 4; i++)
            dist[i] = hand.GetJoint(Fingers[i].tip).TryGetPose(out Pose tip)
                ? Vector3.Distance(tip.position, palmCenter)
                : extendedDistance;

        dbgIndex = dist[0]; dbgMiddle = dist[1];
        dbgRing  = dist[2]; dbgPinky  = dist[3];

        // Règles strictes (seuils normaux)
        bool indexExt  = dist[0] >= extendedDistance;
        bool middleExt = dist[1] >= extendedDistance;
        bool ringCurl  = dist[2] <= curledDistance;
        bool pinkyCurl = dist[3] <= curledDistance;
        bool indexCurl = dist[0] <= curledDistance;
        bool middleCurl= dist[1] <= curledDistance;

        // Règles souples pour la feuille (seuil abaissé)
        bool indexPaper  = dist[0] >= paperDistance;
        bool middlePaper = dist[1] >= paperDistance;
        bool ringPaper   = dist[2] >= paperDistance;
        bool pinkyPaper  = dist[3] >= paperDistance;

        Gesture detected;

        // Ciseaux en premier (le plus spécifique)
        if (indexExt && middleExt && ringCurl && pinkyCurl)
            detected = Gesture.Scissors;
        // Pierre : tous repliés
        else if (indexCurl && middleCurl && ringCurl && pinkyCurl)
            detected = Gesture.Rock;
        // Feuille : tous "assez" étendus avec le seuil souple
        else if (indexPaper && middlePaper && ringPaper && pinkyPaper)
            detected = Gesture.Paper;
        else
            detected = Gesture.Unknown;

        Stabilize(detected);
    }

    private void Stabilize(Gesture detected)
    {
        if (detected == _candidateGesture)
        {
            _candidateTimer += Time.deltaTime;
            if (_candidateTimer >= holdDuration && detected != _lastConfirmedGesture)
            {
                _lastConfirmedGesture = detected;
                if (logToConsole)
                    Debug.Log($"[GestureDetector] {watchedHand} → {detected}");
                OnGestureChanged?.Invoke(detected, watchedHand);
            }
        }
        else
        {
            _candidateGesture = detected;
            _candidateTimer   = 0f;
        }
    }

    private void ResetCandidate()
    {
        _candidateGesture = Gesture.Unknown;
        _candidateTimer   = 0f;
    }
}
using System;
using UnityEngine;
using UnityEngine.XR.Hands;

public class GestureDetector : MonoBehaviour
{
    public enum Gesture { Unknown, Rock, Paper, Scissors, MiddleFinger }

    public static event Action<Gesture, Handedness> OnGestureChanged;

    [Header("Main surveillée")]
    [SerializeField] private Handedness watchedHand = Handedness.Invalid; // Invalid = les deux

    [Header("Seuils (distance tip->paume, metres)")]
    [SerializeField] private float curledDistance   = 0.06f;
    [SerializeField] private float extendedDistance = 0.10f;
    [SerializeField] private float paperDistance    = 0.08f;

    [Header("Stabilisation")]
    [SerializeField, Min(0f)] private float holdDuration = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool logToConsole = true;
    [HideInInspector] public float dbgIndex, dbgMiddle, dbgRing, dbgPinky;

    private XRHandSubsystem _subsystem;

    // État par main
    private Gesture _lastRight = Gesture.Unknown, _candidateRight = Gesture.Unknown;
    private Gesture _lastLeft  = Gesture.Unknown, _candidateLeft  = Gesture.Unknown;
    private float   _timerRight, _timerLeft;

    private static readonly (XRHandJointID proximal, XRHandJointID tip)[] Fingers =
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

        // Surveille la main droite si pas de filtre ou filtre = Right
        if (watchedHand == Handedness.Invalid || watchedHand == Handedness.Right)
            ProcessHand(_subsystem.rightHand, Handedness.Right);

        // Surveille la main gauche si pas de filtre ou filtre = Left
        if (watchedHand == Handedness.Invalid || watchedHand == Handedness.Left)
            ProcessHand(_subsystem.leftHand, Handedness.Left);
    }

    private void ProcessHand(XRHand hand, Handedness side)
    {
        if (!hand.isTracked)
        {
            ResetCandidate(side);
            return;
        }

        Gesture detected = DetectGesture(hand, side);
        Stabilize(detected, side);
    }

    private Gesture DetectGesture(XRHand hand, Handedness side)
    {
        if (!hand.GetJoint(XRHandJointID.Wrist        ).TryGetPose(out Pose wrist ) ||
            !hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out Pose midMcp))
            return Gesture.Unknown;

        Vector3 palmCenter = (wrist.position + midMcp.position) * 0.5f;

        float[] dist = new float[4];
        for (int i = 0; i < 4; i++)
            dist[i] = hand.GetJoint(Fingers[i].tip).TryGetPose(out Pose tip)
                ? Vector3.Distance(tip.position, palmCenter)
                : extendedDistance;

        // Debug uniquement sur la main droite pour éviter le spam
        if (side == Handedness.Right)
        {
            dbgIndex = dist[0]; dbgMiddle = dist[1];
            dbgRing  = dist[2]; dbgPinky  = dist[3];
        }

        bool indexExt   = dist[0] >= extendedDistance;
        bool middleExt  = dist[1] >= extendedDistance;
        bool ringCurl   = dist[2] <= curledDistance;
        bool pinkyCurl  = dist[3] <= curledDistance;
        bool indexCurl  = dist[0] <= curledDistance;
        bool middleCurl = dist[1] <= curledDistance;

        // Pouce étendu pour le majeur
        bool thumbExt = false;
        if (hand.GetJoint(XRHandJointID.ThumbTip     ).TryGetPose(out Pose thumbTip) &&
            hand.GetJoint(XRHandJointID.ThumbProximal ).TryGetPose(out Pose thumbBase))
            thumbExt = Vector3.Distance(thumbTip.position, palmCenter) >= extendedDistance;

        // ── Majeur (quitter) : majeur + pouce étendus, index/annulaire/auriculaire repliés ──
        if (middleExt && thumbExt && indexCurl && ringCurl && pinkyCurl)
            return Gesture.MiddleFinger;

        // ── Ciseaux ──
        if (indexExt && middleExt && ringCurl && pinkyCurl)
            return Gesture.Scissors;

        // ── Pierre ──
        if (indexCurl && middleCurl && ringCurl && pinkyCurl)
            return Gesture.Rock;

        // ── Feuille (seuil souple) ──
        bool paperAll = dist[0] >= paperDistance && dist[1] >= paperDistance &&
                        dist[2] >= paperDistance && dist[3] >= paperDistance;
        if (paperAll)
            return Gesture.Paper;

        return Gesture.Unknown;
    }

    private void Stabilize(Gesture detected, Handedness side)
    {
        ref Gesture last      = ref (side == Handedness.Right ? ref _lastRight      : ref _lastLeft);
        ref Gesture candidate = ref (side == Handedness.Right ? ref _candidateRight : ref _candidateLeft);
        ref float   timer     = ref (side == Handedness.Right ? ref _timerRight     : ref _timerLeft);

        if (detected == candidate)
        {
            timer += Time.deltaTime;
            if (timer >= holdDuration && detected != last)
            {
                last = detected;
                if (logToConsole)
                    Debug.Log($"[GestureDetector] {side} -> {detected}");
                OnGestureChanged?.Invoke(detected, side);
            }
        }
        else
        {
            candidate = detected;
            timer     = 0f;
        }
    }

    private void ResetCandidate(Handedness side)
    {
        if (side == Handedness.Right) { _candidateRight = Gesture.Unknown; _timerRight = 0f; }
        else                          { _candidateLeft  = Gesture.Unknown; _timerLeft  = 0f; }
    }
}
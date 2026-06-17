using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public const int MaxLives         = 3;
    public const int CountdownSeconds = 3;
    public const float GestureWindow  = 3.5f; // secondes pour faire un geste

    public static event Action<GameState>                         OnStateChanged;
    public static event Action<int>                               OnCountdownTick;
    public static event Action<GestureDetector.Gesture,
                               GestureDetector.Gesture,
                               RoundResult>                       OnRoundResolved;
    public static event Action<int, int, int>                     OnStatsUpdated;
    public static event Action<bool>                              OnGameOver;

    public enum GameState  { Idle, Countdown, WaitingGesture, Resolving, GameOver }
    public enum RoundResult { Win, Lose, Draw }

    public GameState CurrentState { get; private set; } = GameState.Idle;
    public int Score  { get; private set; }
    public int Round  { get; private set; }
    public int Lives  { get; private set; } = MaxLives;

    // Geste courant — mis à jour en permanence par GestureDetector
    private GestureDetector.Gesture _currentGesture = GestureDetector.Gesture.Unknown;
    private bool _gestureLockedIn = false;
    private GestureDetector.Gesture _lockedGesture  = GestureDetector.Gesture.Unknown;

    private void OnEnable()  => GestureDetector.OnGestureChanged += OnGestureChanged;
    private void OnDisable() => GestureDetector.OnGestureChanged -= OnGestureChanged;

    private void Start() => StartGame();

    public void StartGame()
    {
        Score = 0; Round = 0; Lives = MaxLives;
        NotifyStats();
        StartCoroutine(RoundLoop());
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        StartGame();
    }

    private IEnumerator RoundLoop()
    {
        while (Lives > 0)
        {
            Round++;
            NotifyStats();

            // ── Compte à rebours ──
            SetState(GameState.Countdown);
            for (int i = CountdownSeconds; i >= 1; i--)
            {
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSeconds(1f);
            }

            // ── Fenêtre de geste ──
            SetState(GameState.WaitingGesture);
            _gestureLockedIn = false;
            _lockedGesture   = GestureDetector.Gesture.Unknown;

            // Snapshot immédiat : si un geste valide est déjà en cours, on le prend
            if (_currentGesture != GestureDetector.Gesture.Unknown)
            {
                _lockedGesture   = _currentGesture;
                _gestureLockedIn = true;
            }

            float elapsed = 0f;
            while (elapsed < GestureWindow)
            {
                if (_gestureLockedIn) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // ── Résolution ──
            SetState(GameState.Resolving);

            GestureDetector.Gesture playerMove = _gestureLockedIn
                ? _lockedGesture
                : GestureDetector.Gesture.Unknown;

            GestureDetector.Gesture cpuMove = RandomGesture();
            RoundResult result = Resolve(playerMove, cpuMove);

            switch (result)
            {
                case RoundResult.Win:  Score++; break;
                case RoundResult.Lose: Lives--; break;
            }

            NotifyStats();
            OnRoundResolved?.Invoke(playerMove, cpuMove, result);

            yield return new WaitForSeconds(2.5f);
        }

        SetState(GameState.GameOver);
        OnGameOver?.Invoke(false);
    }

    // Appelé à chaque changement de geste (les deux mains)
    private void OnGestureChanged(GestureDetector.Gesture gesture, Handedness _)
    {
        // Mémorise toujours le geste courant (utile pour snapshot au début de WaitingGesture)
        _currentGesture = gesture;

        // Verrouille uniquement pendant la fenêtre de jeu
        if (CurrentState != GameState.WaitingGesture) return;
        if (gesture == GestureDetector.Gesture.Unknown) return;
        if (_gestureLockedIn) return;

        _lockedGesture   = gesture;
        _gestureLockedIn = true;
    }

    private static RoundResult Resolve(GestureDetector.Gesture player, GestureDetector.Gesture cpu)
    {
        if (player == GestureDetector.Gesture.Unknown) return RoundResult.Lose;
        if (player == cpu)                             return RoundResult.Draw;

        bool win =
            (player == GestureDetector.Gesture.Rock     && cpu == GestureDetector.Gesture.Scissors) ||
            (player == GestureDetector.Gesture.Paper    && cpu == GestureDetector.Gesture.Rock    ) ||
            (player == GestureDetector.Gesture.Scissors && cpu == GestureDetector.Gesture.Paper   );

        return win ? RoundResult.Win : RoundResult.Lose;
    }

    private static GestureDetector.Gesture RandomGesture() =>
        (GestureDetector.Gesture)Random.Range(1, 4);

    private void SetState(GameState s) { CurrentState = s; OnStateChanged?.Invoke(s); }
    private void NotifyStats() => OnStatsUpdated?.Invoke(Score, Round, Lives);
}
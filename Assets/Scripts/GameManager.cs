using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Hands;
using Random = UnityEngine.Random;

/// <summary>
/// Gère le déroulement du jeu Pierre-Feuille-Ciseaux.
///
/// Setup :
///   1. Attacher ce script sur un GameObject vide "GameManager".
///   2. S'assurer que GestureDetector est dans la scène.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Constantes ───────────────────────────────────────────────────────────

    public const int MaxLives  = 3;
    public const int CountdownSeconds = 3;

    // ── Events (écoutés par GameUI) ──────────────────────────────────────────

    public static event Action<GameState>                          OnStateChanged;
    public static event Action<int>                                OnCountdownTick;   // 3,2,1
    public static event Action<GestureDetector.Gesture,            // joueur
                               GestureDetector.Gesture,            // ordi
                               RoundResult>                        OnRoundResolved;
    public static event Action<int, int, int>                      OnStatsUpdated;    // score, manche, vies
    public static event Action<bool>                               OnGameOver;        // true = victoire (jamais ici), false = défaite

    // ── Types ────────────────────────────────────────────────────────────────

    public enum GameState { Idle, Countdown, WaitingGesture, Resolving, GameOver }
    public enum RoundResult { Win, Lose, Draw }

    // ── État ─────────────────────────────────────────────────────────────────

    public GameState CurrentState { get; private set; } = GameState.Idle;
    public int Score  { get; private set; }
    public int Round  { get; private set; }
    public int Lives  { get; private set; } = MaxLives;

    private GestureDetector.Gesture _playerGesture = GestureDetector.Gesture.Unknown;
    private bool _gestureLockedIn = false;

    // ── Unity ────────────────────────────────────────────────────────────────

    private void OnEnable()  => GestureDetector.OnGestureChanged += OnGestureChanged;
    private void OnDisable() => GestureDetector.OnGestureChanged -= OnGestureChanged;

    private void Start() => StartGame();

    // ── API publique ─────────────────────────────────────────────────────────

    public void StartGame()
    {
        Score  = 0;
        Round  = 0;
        Lives  = MaxLives;
        NotifyStats();
        StartCoroutine(RoundLoop());
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        StartGame();
    }

    // ── Boucle de jeu ────────────────────────────────────────────────────────

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

            // ── Fenêtre de détection du geste ──
            SetState(GameState.WaitingGesture);
            _playerGesture  = GestureDetector.Gesture.Unknown;
            _gestureLockedIn = false;

            // On attend 1.5 s max ; si un geste valide arrive avant, on le verrouille
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                if (_gestureLockedIn) break;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // ── Résolution ──
            SetState(GameState.Resolving);

            GestureDetector.Gesture playerMove = _gestureLockedIn
                ? _playerGesture
                : GestureDetector.Gesture.Unknown;

            GestureDetector.Gesture cpuMove = RandomGesture();
            RoundResult result = Resolve(playerMove, cpuMove);

            switch (result)
            {
                case RoundResult.Win:  Score++;          break;
                case RoundResult.Lose: Lives--;          break;
                case RoundResult.Draw: /* rien */        break;
            }

            NotifyStats();
            OnRoundResolved?.Invoke(playerMove, cpuMove, result);

            // Pause d'affichage avant la prochaine manche
            yield return new WaitForSeconds(2.5f);
        }

        // ── Game Over ──
        SetState(GameState.GameOver);
        OnGameOver?.Invoke(false);
    }

    // ── Callbacks ────────────────────────────────────────────────────────────

    private void OnGestureChanged(GestureDetector.Gesture gesture, Handedness _)
    {
        if (CurrentState != GameState.WaitingGesture) return;
        if (gesture == GestureDetector.Gesture.Unknown) return;

        _playerGesture   = gesture;
        _gestureLockedIn = true;
    }

    // ── Logique PFC ──────────────────────────────────────────────────────────

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

    private static GestureDetector.Gesture RandomGesture()
    {
        int r = Random.Range(1, 4); // 1=Rock, 2=Paper, 3=Scissors
        return (GestureDetector.Gesture)r;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetState(GameState s)
    {
        CurrentState = s;
        OnStateChanged?.Invoke(s);
    }

    private void NotifyStats() => OnStatsUpdated?.Invoke(Score, Round, Lives);
}
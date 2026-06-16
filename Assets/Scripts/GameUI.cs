using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Affiche toute l'UI du jeu sur un Canvas Screen Space Overlay.
///
/// Setup :
///   1. Créer un Canvas (Screen Space - Overlay).
///   2. Créer la hiérarchie suivante et assigner dans l'Inspector :
///
///   Canvas
///   ├── PanelStats          (haut de l'écran)
///   │   ├── TxtScore        TextMeshProUGUI  "Score : 0"
///   │   ├── TxtRound        TextMeshProUGUI  "Manche : 1"
///   │   └── TxtLives        TextMeshProUGUI  "❤️❤️❤️"
///   ├── PanelCountdown      (centre)
///   │   └── TxtCountdown    TextMeshProUGUI  "3"
///   ├── PanelResult         (centre, sous countdown)
///   │   ├── TxtPlayer       TextMeshProUGUI  "Vous : ✊"
///   │   ├── TxtVS           TextMeshProUGUI  "VS"
///   │   ├── TxtCPU          TextMeshProUGUI  "CPU : ✋"
///   │   └── TxtResultLabel  TextMeshProUGUI  "GAGNÉ !"
///   ├── PanelWaiting        (centre)
///   │   └── TxtWaiting      TextMeshProUGUI  "Faites votre geste !"
///   └── PanelGameOver       (plein écran)
///       ├── TxtGameOver     TextMeshProUGUI  "GAME OVER"
///       ├── TxtFinalScore   TextMeshProUGUI  "Score final : 0"
///       └── BtnRestart      Button           "Rejouer"
/// </summary>
public class GameUI : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Stats (haut)")]
    [SerializeField] private TextMeshProUGUI txtScore;
    [SerializeField] private TextMeshProUGUI txtRound;
    [SerializeField] private TextMeshProUGUI txtLives;

    [Header("Countdown")]
    [SerializeField] private GameObject      panelCountdown;
    [SerializeField] private TextMeshProUGUI txtCountdown;

    [Header("Attente geste")]
    [SerializeField] private GameObject      panelWaiting;
    [SerializeField] private TextMeshProUGUI txtWaiting;

    [Header("Résultat de manche")]
    [SerializeField] private GameObject      panelResult;
    [SerializeField] private TextMeshProUGUI txtPlayer;
    [SerializeField] private TextMeshProUGUI txtCPU;
    [SerializeField] private TextMeshProUGUI txtResultLabel;

    [Header("Game Over")]
    [SerializeField] private GameObject      panelGameOver;
    [SerializeField] private TextMeshProUGUI txtGameOver;
    [SerializeField] private TextMeshProUGUI txtFinalScore;
    [SerializeField] private Button          btnRestart;

    // ── Référence ─────────────────────────────────────────────────────────────

    private GameManager _gm;

    // ── Emojis ───────────────────────────────────────────────────────────────

    private static string GestureEmoji(GestureDetector.Gesture g) => g switch
    {
        GestureDetector.Gesture.Rock     => "Pierre",
        GestureDetector.Gesture.Paper    => "Feuille",
        GestureDetector.Gesture.Scissors => "Ciseaux",
        _                                => "Non-Détecté",
    };

    private static string LivesString(int lives)
    {
        return lives.ToString();
    }

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _gm = FindAnyObjectByType<GameManager>();
        btnRestart?.onClick.AddListener(() => _gm?.RestartGame());
        HideAll();
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged   += HandleState;
        GameManager.OnCountdownTick  += HandleCountdown;
        GameManager.OnRoundResolved  += HandleRoundResolved;
        GameManager.OnStatsUpdated   += HandleStats;
        GameManager.OnGameOver       += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged   -= HandleState;
        GameManager.OnCountdownTick  -= HandleCountdown;
        GameManager.OnRoundResolved  -= HandleRoundResolved;
        GameManager.OnStatsUpdated   -= HandleStats;
        GameManager.OnGameOver       -= HandleGameOver;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void HandleState(GameManager.GameState state)
    {
        HideAll();
        switch (state)
        {
            case GameManager.GameState.Countdown:
                panelCountdown?.SetActive(true);
                break;

            case GameManager.GameState.WaitingGesture:
                panelWaiting?.SetActive(true);
                if (txtWaiting) txtWaiting.text = "Faites votre geste !";
                break;

            case GameManager.GameState.Resolving:
                panelResult?.SetActive(true);
                break;

            case GameManager.GameState.GameOver:
                panelGameOver?.SetActive(true);
                break;
        }
    }

    private void HandleCountdown(int value)
    {
        if (txtCountdown) txtCountdown.text = value.ToString();
    }

    private void HandleRoundResolved(
        GestureDetector.Gesture player,
        GestureDetector.Gesture cpu,
        GameManager.RoundResult result)
    {
        if (txtPlayer) txtPlayer.text = $"Vous\n{GestureEmoji(player)}";
        if (txtCPU   ) txtCPU.text    = $"CPU\n{GestureEmoji(cpu)}";

        if (txtResultLabel)
        {
            (txtResultLabel.text, txtResultLabel.color) = result switch
            {
                GameManager.RoundResult.Win  => ("Victoire",  Color.green),
                GameManager.RoundResult.Lose => ("Défaite",  Color.red  ),
                GameManager.RoundResult.Draw => ("Égalité", Color.white ),
                _                            => ("",            Color.white ),
            };
        }
    }

    private void HandleStats(int score, int round, int lives)
    {
        if (txtScore) txtScore.text = $"Score : {score}";
        if (txtRound) txtRound.text = $"Manche : {round}";
        if (txtLives) txtLives.text = LivesString(lives);
    }

    private void HandleGameOver(bool victory)
    {
        if (txtGameOver   ) txtGameOver.text    = victory ? "VICTOIRE !" : "GAME OVER";
        if (txtFinalScore ) txtFinalScore.text  = $"Score final : {_gm?.Score ?? 0}";
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void HideAll()
    {
        panelCountdown?.SetActive(false);
        panelWaiting  ?.SetActive(false);
        panelResult   ?.SetActive(false);
        panelGameOver ?.SetActive(false);
    }
}
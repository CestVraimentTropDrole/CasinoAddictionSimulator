using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Caméra")]
    [SerializeField] private Camera  xrCamera;
    [SerializeField] private Vector3 offsetFromCamera = new Vector3(0f, 0f, 1.2f);

    [Header("Stats")]
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

    private GameManager _gm;

    private static string GestureLabel(GestureDetector.Gesture g) => g switch
    {
        GestureDetector.Gesture.Rock     => "Pierre",
        GestureDetector.Gesture.Paper    => "Feuille",
        GestureDetector.Gesture.Scissors => "Ciseaux",
        _                                => "Inconnu",
    };

    private static string LivesString(int lives)
    {
        string s = "";
        for (int i = 0; i < GameManager.MaxLives; i++)
            s += i < lives ? "♥ " : "♡ ";
        return s.TrimEnd();
    }

    private void Awake()
    {
        _gm = FindAnyObjectByType<GameManager>();
        btnRestart?.onClick.AddListener(() => _gm?.RestartGame());
        if (xrCamera == null) xrCamera = Camera.main;
        HideAll();
    }

    private void OnEnable()
    {
        GameManager.OnStateChanged  += HandleState;
        GameManager.OnCountdownTick += HandleCountdown;
        GameManager.OnRoundResolved += HandleRoundResolved;
        GameManager.OnStatsUpdated  += HandleStats;
        GameManager.OnGameOver      += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnStateChanged  -= HandleState;
        GameManager.OnCountdownTick -= HandleCountdown;
        GameManager.OnRoundResolved -= HandleRoundResolved;
        GameManager.OnStatsUpdated  -= HandleStats;
        GameManager.OnGameOver      -= HandleGameOver;
    }

    private void LateUpdate()
    {
        if (xrCamera == null) return;
        transform.position = xrCamera.transform.TransformPoint(offsetFromCamera);
        transform.rotation = Quaternion.LookRotation(
            transform.position - xrCamera.transform.position,
            xrCamera.transform.up);
    }

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
        if (txtPlayer) txtPlayer.text = $"Vous\n{GestureLabel(player)}";
        if (txtCPU)    txtCPU.text    = $"CPU\n{GestureLabel(cpu)}";

        if (txtResultLabel)
        {
            (txtResultLabel.text, txtResultLabel.color) = result switch
            {
                GameManager.RoundResult.Win  => ("GAGNE !",  Color.green),
                GameManager.RoundResult.Lose => ("PERDU !",  Color.red  ),
                GameManager.RoundResult.Draw => ("EGALITE",  Color.white),
                _                            => ("",          Color.white),
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
        if (txtGameOver)   txtGameOver.text   = victory ? "VICTOIRE !" : "GAME OVER";
        if (txtFinalScore) txtFinalScore.text = $"Score final : {_gm?.Score ?? 0}";
    }

    private void HideAll()
    {
        panelCountdown?.SetActive(false);
        panelWaiting  ?.SetActive(false);
        panelResult   ?.SetActive(false);
        panelGameOver ?.SetActive(false);
    }
}
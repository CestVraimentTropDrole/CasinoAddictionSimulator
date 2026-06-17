using UnityEngine;

/// <summary>
/// Gère tout le sound design du jeu.
///
/// Setup :
///   1. Attacher ce script sur un GameObject "SoundManager".
///   2. Assigner les AudioClips dans l'Inspector.
///   3. La musique de fond joue en boucle automatiquement au démarrage.
///
/// Slots AudioClip à remplir :
///   - sfxWin        : son joué quand le joueur gagne une manche
///   - sfxLose       : son joué quand le joueur perd une manche
///   - sfxDraw       : son joué en cas d'égalité
///   - sfxCountdown  : tick joué à chaque chiffre du compte à rebours
///   - sfxGameOver   : son de fin de partie (game over)
///   - musicBackground : musique de fond en boucle
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("SFX Manches")]
    [SerializeField] private AudioClip sfxWin;
    [SerializeField] private AudioClip sfxLose;
    [SerializeField] private AudioClip sfxDraw;

    [Header("SFX Divers")]
    [SerializeField] private AudioClip sfxCountdown;
    [SerializeField] private AudioClip sfxGameOver;

    [Header("Musique de fond")]
    [SerializeField] private AudioClip musicBackground;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume   = 1.0f;

    // Sources séparées pour ne pas couper la musique quand un SFX joue
    private AudioSource _sfxSource;
    private AudioSource _musicSource;

    private void Awake()
    {
        // Source SFX
        _sfxSource         = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.volume   = sfxVolume;

        // Source musique
        _musicSource           = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop      = true;
        _musicSource.volume    = musicVolume;
    }

    private void OnEnable()
    {
        GameManager.OnRoundResolved += HandleRoundResolved;
        GameManager.OnCountdownTick += HandleCountdown;
        GameManager.OnGameOver      += HandleGameOver;
        GameManager.OnStateChanged  += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameManager.OnRoundResolved -= HandleRoundResolved;
        GameManager.OnCountdownTick -= HandleCountdown;
        GameManager.OnGameOver      -= HandleGameOver;
        GameManager.OnStateChanged  -= HandleStateChanged;
    }

    private void Start() => PlayMusic();

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void HandleRoundResolved(
        GestureDetector.Gesture _player,
        GestureDetector.Gesture _cpu,
        GameManager.RoundResult result)
    {
        AudioClip clip = result switch
        {
            GameManager.RoundResult.Win  => sfxWin,
            GameManager.RoundResult.Lose => sfxLose,
            GameManager.RoundResult.Draw => sfxDraw,
            _                            => null,
        };
        PlaySFX(clip);
    }

    private void HandleCountdown(int _) => PlaySFX(sfxCountdown);

    private void HandleGameOver(bool _)
    {
        StopMusic();
        PlaySFX(sfxGameOver);
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        // Reprend la musique si une nouvelle partie démarre
        if (state == GameManager.GameState.Countdown && !_musicSource.isPlaying)
            PlayMusic();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void PlayMusic()
    {
        if (musicBackground == null || _musicSource == null) return;
        _musicSource.clip = musicBackground;
        _musicSource.Play();
    }

    private void StopMusic()
    {
        if (_musicSource == null) return;
        _musicSource.Stop();
    }
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 전역 사운드 매니저. 싱글턴 패턴.
/// Inspector에서 배경음악, 점프, 피격 등의 사운드를 직관적으로 1:1 할당할 수 있습니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("=== 배경음악 (BGM) ===")]
    [Tooltip("배경음악 파일 할당")]
    public AudioClip 배경음악;

    [Tooltip("BGM 볼륨")]
    [Range(0f, 1f)]
    public float BGM볼륨 = 0.5f;

    [Header("=== 효과음 (SFX) ===")]
    [Tooltip("점프할 때 나는 소리")]
    public AudioClip 점프소리;

    [Tooltip("장애물에 부딪혔을 때 나는 소리")]
    public AudioClip 피격소리;

    [Tooltip("슬라이딩할 때 나는 소리")]
    public AudioClip 슬라이딩소리;

    [Tooltip("효과음 전체 볼륨")]
    [Range(0f, 1f)]
    public float 효과음볼륨 = 1.0f;

    [Header("=== 시스템 설정 ===")]
    [Tooltip("동시에 재생 가능한 효과음 최대 수")]
    public int 최대동시효과음 = 10;

    // ── 내부 변수 ──
    private AudioSource _bgmSource;
    private List<AudioSource> _sfxSources = new List<AudioSource>();

    void Awake()
    {
        // 싱글턴 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 넘어가도 소리 유지

        SetupAudioSources();
    }

    void Start()
    {
        // 게임 시작 시 배경음악 자동 재생
        if (배경음악 != null)
        {
            BGM재생(배경음악);
        }
    }

    private void SetupAudioSources()
    {
        // BGM 전용 AudioSource
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.volume = BGM볼륨;
        _bgmSource.playOnAwake = false;

        // 효과음용 AudioSource 풀(Pool) 생성
        for (int i = 0; i < 최대동시효과음; i++)
        {
            AudioSource sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.loop = false;
            sfx.volume = 효과음볼륨;
            _sfxSources.Add(sfx);
        }
    }

    // ───────────────────────────────────────────
    //  재생 API
    // ───────────────────────────────────────────

    public void BGM재생(AudioClip clip)
    {
        if (clip == null) return;
        _bgmSource.clip = clip;
        _bgmSource.volume = BGM볼륨;
        _bgmSource.Play();
    }

    /// <summary>점프 소리 재생</summary>
    public void 점프소리재생() => 클립재생(점프소리);

    /// <summary>피격 소리 재생</summary>
    public void 피격소리재생() => 클립재생(피격소리);

    /// <summary>슬라이딩 소리 재생</summary>
    public void 슬라이딩소리재생() => 클립재생(슬라이딩소리);

    /// <summary>
    /// 비어있는 AudioSource를 찾아 사운드를 재생하는 핵심 함수
    /// </summary>
    public void 클립재생(AudioClip clip)
    {
        if (clip == null) return;

        // 비어있는 소스 찾기
        foreach (var src in _sfxSources)
        {
            if (!src.isPlaying)
            {
                src.clip = clip;
                src.volume = 효과음볼륨;
                src.Play();
                return;
            }
        }

        // 전부 사용 중이면 가장 오래된 첫 번째 소스를 뺏어서 사용
        _sfxSources[0].clip = clip;
        _sfxSources[0].volume = 효과음볼륨;
        _sfxSources[0].Play();
    }
}

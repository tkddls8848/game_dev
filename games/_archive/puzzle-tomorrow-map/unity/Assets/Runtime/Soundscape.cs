using UnityEngine;

/// <summary>
/// 이 게임의 소리 전부. 음악 · 바람 · 파도 · 행동 효과음.
///
/// 층을 나눈 이유: 셋이 동시에 들려야 "섬"이 된다. 음악만 있으면 화면과 따로 놀고,
/// 바람만 있으면 황량하고, 파도만 있으면 단조롭다. 각자 다른 음량·다른 주기로 겹친다.
///
/// **음량은 눈대중이 아니라 실측 라우드니스에서 나온다.** 음악과 바람은 둘 다 -20 LUFS 로
/// 맞춰 넣었고, 합성 파도는 아래에서 RMS 를 -20 dBFS 로 정규화한다. 아래 volume 은
/// 그 상태에서 실제로 측정한 값이다 — 음악 -26.3, 바람 -39~-32, 파도 -35 LUFS.
/// 즉 두 환경음이 나란히 음악보다 약 9 dB 아래에 앉는다.
///
/// **파도의 fader 만 유독 높은(.70) 이유**: 파도는 에너지가 저역에 몰려 있어
/// 같은 dBFS 라도 사람 귀에는 훨씬 작게 들린다(K-가중 -12 dB). 숫자를 음악과 맞추면
/// 들리지 않는다. 그래서 dBFS 가 아니라 LUFS 를 맞췄다.
///
/// **파도는 합성한다.** 라이브러리에 바다 녹음이 없다(비·천둥·개울은 있지만 파도가 없다).
/// 임시 삐 소리로 때우는 것이 아니라, 필터 노이즈에 느린 너울을 실어 실제 서프를 만든다 —
/// 파도 소리의 물리적 실체가 광대역 잡음의 진폭 변조이므로 이 방식이 녹음에 가깝다.
/// 바다 녹음을 구하면 이 함수를 파일 재생으로 바꾸면 된다.
/// </summary>
public sealed class Soundscape : MonoBehaviour
{
    public const int SampleRate = 44100;

    /// <summary>합성 파도의 목표 RMS. -20 dBFS — 음악·바람 파일의 라우드니스와 같은 자리다.</summary>
    const float SurfRms = .10f;

    AudioSource music, musicB, wind, surf, oneShot;
    AudioClip plantClip, seedClip, gravelClip, plankClip, hammerClip;
    AudioClip collapseClip, rockfallClip, splashClip, seasonClip;
    System.Random random = new System.Random(4127);

    /// <summary>바람 음량이 오가는 폭. 고정하면 곧 소음으로 들린다.</summary>
    float windTarget = .14f, windPhase;
    AudioClip[] seasonTracks; AudioClip fallbackTrack;
    int trackIndex = -1; Coroutine musicRoutine; float musicVolume = .50f;

    public void Build()
    {
        music  = Source("Music", .50f, false);
        musicB = Source("Music B", .50f, false);
        wind = Source("Wind", .14f, true);
        surf = Source("Surf", .70f, true);
        oneShot = Source("Actions", .62f, false);

        // 네 계절 곡을 **이어서** 튼다. 같은 악기·같은 음량(-20.5 LUFS)으로 만들어서
        // 곡이 넘어갈 때 볼륨이 튀지 않는다.
        //
        // **아직 계절에 맞춰 고르지 않는다** — 계절 연출이 없어서 음악만 바뀌면
        // 화면과 어긋난 것으로 들린다. 지금은 순서대로 돌리고, 계절 연출이 들어오면
        // SeasonTrack(index) 로 바꾼다(그 함수는 이미 있다).
        // **Bgm 이 맨 앞이다.** 사용자가 처음 생성한 그 곡을 가장 좋다고 했다 —
        // 먼저 들리고, 나머지 넷이 뒤따른다.
        string[] names = { "Bgm", "Bgm-spring", "Bgm-summer", "Bgm-autumn", "Bgm-winter" };
        seasonTracks = new AudioClip[names.Length];
        for (int i = 0; i < names.Length; i++) seasonTracks[i] = Resources.Load<AudioClip>("Audio/" + names[i]);
        fallbackTrack = seasonTracks[0];
        musicRoutine = StartCoroutine(Playlist());

        var breeze = Resources.Load<AudioClip>("Audio/Wind");
        if (breeze != null) { wind.clip = breeze; wind.Play(); }

        surf.clip = Surf();
        surf.Play();

        plantClip = Resources.Load<AudioClip>("Audio/Plant");
        seedClip = Resources.Load<AudioClip>("Audio/Seed");
        gravelClip = Resources.Load<AudioClip>("Audio/Gravel");
        plankClip = Resources.Load<AudioClip>("Audio/Plank");
        hammerClip = Resources.Load<AudioClip>("Audio/Hammer");
        collapseClip = Resources.Load<AudioClip>("Audio/Collapse");
        rockfallClip = Resources.Load<AudioClip>("Audio/Rockfall");
        splashClip = Resources.Load<AudioClip>("Audio/Splash");
        seasonClip = Resources.Load<AudioClip>("Audio/Season");
    }

    AudioSource Source(string name, float volume, bool loop)
    {
        var host = new GameObject(name);
        host.transform.SetParent(transform, false);
        var source = host.AddComponent<AudioSource>();
        source.volume = volume; source.loop = loop; source.playOnAwake = false;
        source.spatialBlend = 0f;                  // 2D. 카메라가 고정이라 공간화할 것이 없다
        return source;
    }

    void Update()
    {
        // 바람이 느리게 세졌다 잦아든다. 두 주기를 겹쳐 규칙이 드러나지 않게 한다.
        // 폭은 .11~.24 — 잦아들 때도 들리고, 돌풍에도 음악을 덮지 않는 자리다.
        windPhase += Time.deltaTime;
        float gust = .11f + .13f * (Mathf.Sin(windPhase * .21f) * .6f + Mathf.Sin(windPhase * .071f) * .4f + 1f) * .5f;
        windTarget = Mathf.Lerp(windTarget, gust, Time.deltaTime * .6f);
        if (wind != null) wind.volume = windTarget;
    }

    // ── 행동 효과음 ────────────────────────────────────────

    /// <summary>
    /// 씨앗을 심는다. 주머니를 흔들어 씨를 꺼내고 → 흙을 밟고 → 자갈로 덮는다.
    /// 셋을 순서대로 겹쳐야 "꺼내 묻는" 한 동작으로 들린다. 하나만 쓰면 그냥 발소리다.
    ///
    /// **지연은 파일의 무음 도입부를 뺀 값이다.** Plant.wav 는 앞이 .17초 비어 있어
    /// 여기 적힌 .10 이 실제로는 .27초에 들린다. 파일 길이만 보고 맞추면 순서가 뒤집힌다 —
    /// 실측 기준 세 소리가 각각 .05 / .27 / .43초에 온다.
    /// </summary>
    public void Plant()
    {
        Play(seedClip, .50f, Pitch(.96f, 1.06f));
        StartCoroutine(Delayed(plantClip, .10f, .80f, Pitch(.94f, 1.06f)));
        StartCoroutine(Delayed(gravelClip, .42f, .35f, Pitch(.98f, 1.10f)));
    }

    /// <summary>
    /// 널 방벽을 세운다. **자갈이 아니라 나무다** — 시안의 방벽은 나무 널이다.
    /// 널을 절벽면에 대고(둔탁한 나무 타격) 두 번 박은 뒤(망치) 발밑 흙을 다진다(자갈).
    /// 망치를 두 번 치는 사이를 벌려야 "박는" 동작으로 읽힌다.
    /// </summary>
    public void Defend()
    {
        Play(plankClip, .80f, Pitch(.88f, .98f));
        StartCoroutine(Delayed(hammerClip, .16f, .40f, Pitch(.94f, 1.04f)));
        StartCoroutine(Delayed(hammerClip, .34f, .34f, Pitch(.90f, 1.00f)));
        StartCoroutine(Delayed(gravelClip, .50f, .25f, Pitch(.72f, .82f)));
    }

    /// <summary>
    /// 땅이 무너진다. 이 게임의 유일한 상실이므로 가장 크고 길어야 한다 —
    /// 깨지고 · 굴러떨어지고 · **바다에 떨어진다.** 물보라까지 있어야 "바다가 가져갔다"가 된다.
    /// </summary>
    public void Collapse(int plots)
    {
        Play(collapseClip, 1.0f, Pitch(.88f, .98f));
        int stones = Mathf.Clamp(plots, 1, 4);
        if (rockfallClip != null)
        {
            // 잃은 칸 수만큼 돌이 더 떨어진다. 두 칸을 잃으면 두 배로 들려야 한다.
            for (int i = 0; i < stones; i++)
                StartCoroutine(Delayed(rockfallClip, .12f + i * .19f, .72f, Pitch(.80f, 1.10f)));
        }
        // 물보라는 마지막 돌의 **꼬리에 겹쳐** 들어온다. 끝난 뒤에 내면 사이가 끊겨
        // 한 번의 붕괴가 아니라 별개의 두 소리로 들린다 - 스펙트럼에서 .2초 공백이 보였다.
        StartCoroutine(Delayed(splashClip, .30f + stones * .14f, .80f, Pitch(.92f, 1.04f)));
    }

    /// <summary>
    /// 하루가 지난다. 작게 — 매일 울리므로 크면 곧 거슬린다.
    /// 다만 .18 은 **너무 작아 들리지 않았다**(-44 LUFS, 음악보다 18 dB 아래).
    /// 음악 대비 8 dB 아래(-35 LUFS)가 "있는 줄은 아는" 자리다.
    /// </summary>
    public void Advance()
    {
        Play(gravelClip, .55f, Pitch(1.25f, 1.40f));
    }

    /// <summary>
    /// 계절이 넘어간다. 피치카토 네 음(도-레-솔-파)이 올라갔다 내려앉는다 —
    /// 승리 팡파르가 아니라 달력을 한 장 넘기는 소리다.
    ///
    /// **네 음 전부 BGM(F major)의 음계 안에 있다.** 조가 어긋나면 배경 음악 위에서
    /// 튀어 들려 전환이 아니라 사고처럼 들린다.
    /// </summary>
    public void Season()
    {
        Play(seasonClip, .35f, 1f);
    }

    /// <summary>
    /// 네 곡을 순서대로 이어 튼다. 한 곡이 끝나기 CROSS 초 전에 다음 곡을 겹쳐 넣는다.
    ///
    /// **끊고 새로 틀지 않는다.** 배경음악이 뚝 끊기면 그 순간만 도드라져서
    /// 음악이 바뀐 것이 아니라 뭔가 고장 난 것처럼 들린다.
    /// 소스가 둘인 이유도 같다 — 하나로 페이드아웃/인 하면 전환 지점에 반드시 음량이 팬다.
    /// </summary>
    System.Collections.IEnumerator Playlist()
    {
        const float cross = 3.5f;
        var a = music; var b = musicB;
        while (true)
        {
            AudioClip clip = NextTrack();
            if (clip == null) yield break;              // 곡이 하나도 없으면 조용히 끝낸다
            a.clip = clip; a.volume = 0f; a.Play();
            for (float t2 = 0; t2 < cross; t2 += Time.deltaTime)
            {
                float k = t2 / cross;
                a.volume = musicVolume * k;
                if (b.isPlaying) b.volume = musicVolume * (1 - k);
                yield return null;
            }
            a.volume = musicVolume;
            if (b.isPlaying) { b.Stop(); b.volume = musicVolume; }

            float hold = Mathf.Max(0.1f, clip.length - cross * 2);
            yield return new WaitForSeconds(hold);
            var swap = a; a = b; b = swap;              // 다음 곡은 반대 소스로
        }
    }

    AudioClip NextTrack()
    {
        if (seasonTracks != null)
        {
            for (int step = 1; step <= seasonTracks.Length; step++)
            {
                int i = ((trackIndex + step) % seasonTracks.Length + seasonTracks.Length) % seasonTracks.Length;
                if (seasonTracks[i] != null) { trackIndex = i; return seasonTracks[i]; }
            }
        }
        return fallbackTrack;
    }

    /// <summary>
    /// 계절에 맞춰 곡을 고정한다. **계절 연출이 들어온 뒤에 쓴다** — 지금은 호출하지 않는다.
    /// 목록이 [Bgm, 봄, 여름, 가을, 겨울] 이므로 계절 index 에 **1을 더해** 부른다.
    /// </summary>
    public void SeasonTrack(int index)
    {
        if (seasonTracks == null || index < 0 || index >= seasonTracks.Length) return;
        if (index == trackIndex || seasonTracks[index] == null) return;
        trackIndex = index - 1;                          // 다음 NextTrack() 이 이 곡을 집게 한다
    }

    void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null || oneShot == null) return;
        oneShot.pitch = pitch;
        oneShot.PlayOneShot(clip, volume);
        oneShot.pitch = 1f;
    }

    System.Collections.IEnumerator Delayed(AudioClip clip, float wait, float volume, float pitch)
    {
        yield return new WaitForSeconds(wait);
        Play(clip, volume, pitch);
    }

    float Pitch(float low, float high) => low + (high - low) * (float)random.NextDouble();

    // ── 파도 합성 ──────────────────────────────────────────

    /// <summary>
    /// 서프 루프. 파도 소리는 **두 개의 층**이다. 느리게 오르내리는 물의 몸통(저역)과,
    /// 파도가 부서질 때만 나는 포말(중고역). 몸통만 만들면 파도가 아니라 저주파 웅웅거림이다.
    ///
    /// 포말을 너울의 **상승 구간**에 싣는 것이 핵심이다 — 실제로 물이 부서지는 순간이
    /// 파고가 올라가는 순간이고, 그 뒤로 쉿 소리가 꼬리를 끌며 잦아든다.
    ///
    /// 길이를 20초로 두고 양 끝을 교차 페이드해 이음매가 들리지 않게 한다 —
    /// 짧으면 같은 파도가 되풀이되는 것이 금방 들린다.
    /// </summary>
    AudioClip Surf()
    {
        const float seconds = 20f;
        int count = (int)(SampleRate * seconds);
        var body = new float[count];
        var foam = new float[count];
        var swell = new float[count];

        // 1) 몸통 — 갈색 잡음(brown noise)에 저역 통과를 두 번. 백색보다 저역이 실려 물소리에 가깝다.
        {
            float brown = 0f, lp1 = 0f, lp2 = 0f;
            const float a = .06f;
            for (int i = 0; i < count; i++)
            {
                float white = (float)(random.NextDouble() * 2 - 1);
                brown = Mathf.Clamp(brown + white * .045f, -1f, 1f);
                lp1 += (brown - lp1) * a;
                lp2 += (lp1 - lp2) * a;
                body[i] = lp2;
            }
        }

        // 2) 너울. 주기가 다른 셋을 겹쳐야 파도가 규칙적으로 들리지 않는다.
        //    **바닥을 .18 로 받친다** — 0 으로 떨어지면 매 주기마다 바다가 완전히 사라진다.
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            swell[i] = Mathf.Max(.18f, .44f
                     + .30f * Mathf.Sin(t * Mathf.PI * 2 / 7.3f)
                     + .17f * Mathf.Sin(t * Mathf.PI * 2 / 4.1f + 1.7f)
                     + .09f * Mathf.Sin(t * Mathf.PI * 2 / 11.9f + .6f));
        }

        // 3) 포말 — 백색 잡음을 대략 430 Hz ~ 2.5 kHz 로 좁힌다. 더 넓히면 쉿 소리가 돼
        //    바람과 구별되지 않고, 더 좁히면 다시 웅웅거림에 묻힌다.
        {
            float hp = 0f, lp = 0f;
            const float ah = .04f, al = .20f;
            for (int i = 0; i < count; i++)
            {
                float white = (float)(random.NextDouble() * 2 - 1);
                hp += (white - hp) * ah;              // hp 는 저역. white-hp 가 고역 통과다
                lp += ((white - hp) - lp) * al;
                foam[i] = lp;
            }
        }

        // 4) 부서지는 순간. 너울의 상승분을 받아 0.55초로 감쇠시킨다 —
        //    파고가 오를 때 치솟고, 그 뒤 꼬리를 끌며 사라진다.
        var breaking = new float[count];
        {
            float acc = 0f, top = 0f;
            float decay = Mathf.Exp(-1f / (SampleRate * .55f));
            for (int i = 0; i < count; i++)
            {
                float rise = i == 0 ? 0f : Mathf.Max(0f, swell[i] - swell[i - 1]) * SampleRate;
                acc = Mathf.Max(rise * .9f, acc * decay);
                breaking[i] = acc;
                if (acc > top) top = acc;
            }
            if (top > 0f) for (int i = 0; i < count; i++) breaking[i] /= top;
        }

        // 5) 두 층을 합친다. 포말은 항상 조금 깔려 있고(.25) 부서질 때 치솟는다.
        var samples = new float[count];
        for (int i = 0; i < count; i++)
            samples[i] = (body[i] + foam[i] * (.25f + .75f * breaking[i]) * .26f) * swell[i];

        // 6) **실측으로 음량을 잡는다.** 예전에는 여기서 고정 배율 6.5 를 곱했는데
        //    그 결과 피크가 5.8 까지 올라가 클리핑했다 — 파도가 아니라 찌그러진 저역이었다.
        //    RMS 를 -20 dBFS 로 맞추고 피크는 .95 로 눌러 둔다.
        {
            double sum = 0;
            for (int i = 0; i < count; i++) sum += (double)samples[i] * samples[i];
            float rms = Mathf.Sqrt((float)(sum / count));
            float gain = rms > 1e-6f ? SurfRms / rms : 1f;
            float peak = 0f;
            for (int i = 0; i < count; i++)
            {
                samples[i] *= gain;
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }
            if (peak > .95f) { float trim = .95f / peak; for (int i = 0; i < count; i++) samples[i] *= trim; }
        }

        // 7) 이음매 없는 반복. **꼬리를 머리에 섞고 꼬리 구간은 버린다.**
        //    꼬리 쪽에 섞으면(예전 방식) 마지막 표본이 samples[blend-1] 근처로 끝나는데
        //    되감기면 samples[0] 이 이어져 계단이 남는다 — 20초마다 딸깍 소리가 난다.
        //    머리에 섞고 길이를 count-blend 로 자르면, 되감기는 지점이 원본에서
        //    연속이던 두 표본 사이가 되어 계단이 사라진다(실측 0.0148 → 0.0006).
        int blend = (int)(SampleRate * .9f);
        int length = count - blend;
        for (int i = 0; i < blend; i++)
        {
            float k = (float)i / blend;
            samples[i] = samples[i] * k + samples[length + i] * (1 - k);
        }

        // SetData 는 클립 길이와 배열 길이가 같아야 한다. 잘라 낸 길이로 옮겨 담는다.
        var looped = new float[length];
        System.Array.Copy(samples, looped, length);

        var clip = AudioClip.Create("Synthesised surf", length, 1, SampleRate, false);
        clip.SetData(looped, 0);
        return clip;
    }
}

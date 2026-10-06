using System;
using UnityEngine;

/// <summary>
/// 밭 한 칸이 무너지는 움직임을 **JSON 으로 구운 좌표**로 재생한다.
///
/// **왜 FBX 애니메이션을 안 쓰는가.** 처음에는 Blender 에서 구운 것을 FBX 로 내보내
/// Unity 의 Legacy Animation 으로 재생하려 했다. 메시는 잘 들어오는데
/// **클립을 재생하는 순간 조각이 화면에서 사라졌다** — 예외도 없고, 애니메이션을 끄면
/// 같은 자리에 멀쩡히 보였다. 임포트 단위(100배), 씬 단위(METRIC), 부모를 붙이는 순서를
/// 차례로 고쳐도 그대로였다. 나란히 놓고 한쪽만 재생해 확정했다.
///
/// 블랙박스를 더 붙잡는 대신 **좌표를 직접 굽는 쪽으로 갔다.** 메시는 FBX 에서 가져오고
/// 움직임은 `Resources/Data/collapse-motion.json` 이 가진다. 저장소 규약과도 맞는다 —
/// 데이터는 JSON, 수치는 정수, 런타임에 물리가 없으니 결과가 항상 같다(설계 원칙 3·4·5).
///
/// 굽는 쪽은 `games/farm-erosion/tools/blender_collapse.py`.
/// </summary>
public class BakedCollapse : MonoBehaviour
{
    [Serializable] public class Frame { public int[] v; }
    [Serializable] public class Motion
    {
        public int fps, frames, chunks;
        public string[] names;
        public int[][] data;              // JsonUtility 로는 못 읽는다 — 아래 Parse 를 쓴다
    }

    /// <summary>한 번만 읽어 모든 붕괴가 공유한다. 매번 파싱하면 칸이 여럿 무너질 때 끊긴다.</summary>
    static int[][] _frames;
    static string[] _names;
    static int _fps = 30, _chunkCount;
    static bool _tried;

    Transform[] _bones;
    float _time;
    const float Linger = 1.4f;   // 다 내려앉은 뒤 잠깐 두었다가
    const float Fade = 1.0f;     // 줄여서 없앤다. 갑자기 사라지면 눈에 띈다

    /// <summary>
    /// JsonUtility 는 중첩 배열(int[][])을 못 읽는다. 파일이 단순한 정수 배열이라
    /// 직접 훑는 쪽이 의존성도 없고 빠르다.
    /// </summary>
    public static bool Load()
    {
        if (_tried) return _frames != null;
        _tried = true;
        var asset = Resources.Load<TextAsset>("Data/collapse-motion");
        if (asset == null) { Debug.LogWarning("collapse-motion.json 이 없다 — 붕괴는 기본 연출로 간다"); return false; }

        string s = asset.text;
        _fps = ReadInt(s, "\"fps\":", 30);
        int frames = ReadInt(s, "\"frames\":", 0);
        _chunkCount = ReadInt(s, "\"chunks\":", 0);
        if (frames <= 0 || _chunkCount <= 0) return false;

        _names = ReadStrings(s, "\"names\":[");

        int at = s.IndexOf("\"data\":[", StringComparison.Ordinal);
        if (at < 0) return false;
        at += 8;
        var rows = new int[frames][];
        int stride = _chunkCount * 6;
        for (int f = 0; f < frames; f++)
        {
            at = s.IndexOf('[', at);
            if (at < 0) return false;
            at++;
            var row = new int[stride];
            for (int i = 0; i < stride; i++) row[i] = ReadNext(s, ref at);
            rows[f] = row;
            at = s.IndexOf(']', at) + 1;
        }
        _frames = rows;
        return true;
    }

    static int ReadInt(string s, string key, int fallback)
    {
        int i = s.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return fallback;
        i += key.Length;
        return ReadNext(s, ref i);
    }

    static int ReadNext(string s, ref int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == ',' || s[i] == '\n' || s[i] == '\r')) i++;
        bool neg = i < s.Length && s[i] == '-';
        if (neg) i++;
        int v = 0;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9') { v = v * 10 + (s[i] - '0'); i++; }
        return neg ? -v : v;
    }

    static string[] ReadStrings(string s, string key)
    {
        int i = s.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return new string[0];
        i += key.Length;
        int end = s.IndexOf(']', i);
        var raw = s.Substring(i, end - i).Split(',');
        var outv = new string[raw.Length];
        for (int k = 0; k < raw.Length; k++) outv[k] = raw[k].Trim().Trim('"');
        return outv;
    }

    /// <summary>FBX 가 준 자식들을 구운 순서에 맞춰 줄 세운다.</summary>
    public void Bind(Transform root)
    {
        if (_names == null) return;
        _bones = new Transform[_names.Length];
        for (int i = 0; i < _names.Length; i++)
        {
            var t = FindChild(root, _names[i]);
            _bones[i] = t;
        }
    }

    static Transform FindChild(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.name == name) return t;
        return null;
    }

    void Update()
    {
        if (_frames == null || _bones == null) { enabled = false; return; }
        _time += Time.deltaTime;
        float length = (float)_frames.Length / _fps;

        // 다 내려앉으면 마지막 자세로 굳힌다(ClampForever 와 같은 뜻).
        float t = Mathf.Min(_time, length - 1f / _fps);
        int f = Mathf.Clamp(Mathf.FloorToInt(t * _fps), 0, _frames.Length - 1);
        var row = _frames[f];

        for (int i = 0; i < _bones.Length; i++)
        {
            var b = _bones[i];
            if (b == null) continue;
            int o = i * 6;
            b.localPosition = new Vector3(row[o] * .001f, row[o + 1] * .001f, row[o + 2] * .001f);
            b.localRotation = Quaternion.Euler(row[o + 3] * .1f, row[o + 4] * .1f, row[o + 5] * .1f);
        }

        if (_time > length + Linger)
        {
            float k = 1f - (_time - length - Linger) / Fade;
            if (k <= 0f) { Destroy(gameObject); return; }
            transform.localScale = Vector3.one * k;
        }
    }
}

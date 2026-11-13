using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum GamePhase { Menu, Playing, Paused, Complete }

/// <summary>Owns the complete journey, puzzle state, interaction, save data and ending.</summary>
public sealed partial class AetherGame : MonoBehaviour
{
    public static AetherGame I;
    public GamePhase Phase = GamePhase.Menu;
    public bool[] Solved = new bool[3];
    public bool[] RelicFound = new bool[3];
    public int[] RingValues = { 0, 0, 0 };
    public int[] LightValues = { 1, 0, 0 };
    public int SelectedRing;
    public int SequenceProgress;
    public int PreviewTone = -1;
    public bool IsPlayingSequence => playingSequence;
    public bool IsSmokeMode => smokeMode;
    public int ActivePuzzle = -1;
    public float PlaySeconds;
    public string Prompt = "", Toast = "";
    public float ToastUntil;
    public bool ReducedMotion;
    public float Volume = .65f;
    public AetherPlayer Player;
    public Vector3 LastSafePosition = new Vector3(0, .15f, -10);
    public int SolvedCount { get { int n = 0; foreach (bool b in Solved) if (b) n++; return n; } }
    public int RelicCount { get { int n = 0; foreach (bool b in RelicFound) if (b) n++; return n; } }
    public bool HasSave => ReadSave() != null;
    public string PuzzleTitle => ActivePuzzle == 0 ? "천구의 정렬" : ActivePuzzle == 1 ? "메아리의 기억" : "빛의 순환";
    public string PuzzleDescription => ActivePuzzle == 0 ? "오래된 관측 기록을 따라 세 개의 고리를 정렬하세요."
        : ActivePuzzle == 1 ? "정원이 부르는 네 개의 음을 듣고, 같은 순서로 응답하세요."
        : "하나의 스위치는 자신의 빛과 다음 빛을 함께 바꿉니다. 세 빛을 모두 밝히세요.";
    static readonly int[] Melody = { 0, 2, 1, 3 };
    static readonly Vector3[] Stations = { new Vector3(-10, 0, 1), new Vector3(10, 0, 3), new Vector3(0, 0, 16) };
    static readonly Vector3[] RelicPositions = { new Vector3(-13, 1.15f, -7), new Vector3(13, 1.15f, 13), new Vector3(-7, 1.15f, 19) };
    static readonly string[] Names = { "천구의 정렬", "메아리의 기억", "빛의 순환" };
    static readonly string[] Memories = {
        "기록 01  /  우리는 하늘에 정원을 지었다. 누군가 돌아올 수 있도록.",
        "기록 02  /  빛은 사라진 것이 아니다. 서로를 기다리고 있을 뿐.",
        "기록 03  /  마지막 문 너머에는, 새로운 아침이 있다."
    };
    readonly List<Transform> animated = new List<Transform>();
    readonly List<Vector3> animatedBases = new List<Vector3>();
    readonly Transform[] rings = new Transform[3];
    readonly Renderer[] lamps = new Renderer[3];
    readonly Transform[] relics = new Transform[3];
    readonly Transform[] stationCores = new Transform[3];
    readonly Light[] stationLights = new Light[3];
    readonly LineRenderer[] beams = new LineRenderer[3];
    readonly List<Transform> bridgeSteps = new List<Transform>();
    readonly List<int> sequence = new List<int>();
    Camera menuCamera;
    Material gold, cyan, stone, dark, inactive;
    int nearby = -1;
    bool bridgeReady, playingSequence, smokeMode;
    float bridgeTime = -20, saveTimer, menuTime;
    Coroutine melodyRoutine;
    const string SaveKey = "Aether.Journey.v1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<AetherGame>() == null) new GameObject("AETHER · Journey").AddComponent<AetherGame>();
    }

    void Awake()
    {
        I = this;
        smokeMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-aether-smoke") >= 0;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        Volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Aether.Volume", .65f));
        ReducedMotion = PlayerPrefs.GetInt("Aether.ReducedMotion", 0) == 1;
        AetherWorld.Build();
        AetherFrontierWorld.Build();
        AetherHorizonWorld.Build();
        gold = AetherWorld.Mat("Signal · gold", new Color(1, .68f, .28f), 1.2f);
        cyan = AetherWorld.Mat("Signal · ice", new Color(.22f, .91f, .89f), 1.1f);
        stone = AetherWorld.Mat("Puzzle · porcelain", new Color(.77f, .81f, .73f));
        dark = AetherWorld.Mat("Puzzle · basalt", new Color(.10f, .23f, .24f));
        inactive = AetherWorld.Mat("Signal · dormant", new Color(.22f, .35f, .35f));
        BuildStations(); BuildRelics(); BuildBridge();
        Player = new GameObject("The traveller").AddComponent<AetherPlayer>(); Player.Initialize();
        menuCamera = new GameObject("Sanctuary · title camera").AddComponent<Camera>();
        menuCamera.fieldOfView = 56; menuCamera.farClipPlane = 500; menuCamera.allowHDR = true;
        menuCamera.allowMSAA = true;
        gameObject.AddComponent<AetherAudio>(); AetherAudio.I.SetVolume(Volume);
        InitializeExpansion();
        gameObject.AddComponent<AetherUI>();
        SetCameraState(); RefreshCursor();
        if (smokeMode) StartCoroutine(SmokeJourney());
    }

    public static bool RingsSolved(int[] values) => values != null && values.Length == 3 && values[0] == 2 && values[1] == 5 && values[2] == 3;
    public static bool IsSequenceCorrect(int[] values)
    {
        if (values == null || values.Length != Melody.Length) return false;
        for (int i = 0; i < Melody.Length; i++) if (values[i] != Melody[i]) return false;
        return true;
    }
    public static int LightMaskAfterToggle(int mask, int index)
    {
        if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
        return (mask ^ (1 << index) ^ (1 << ((index + 1) % 3))) & 7;
    }

    void Update()
    {
        AnimateWorld();
        if (Phase == GamePhase.Menu)
        {
            menuTime += ReducedMotion ? 0 : Time.deltaTime * .065f;
            menuCamera.transform.position = new Vector3(21 + Mathf.Sin(menuTime) * 1.5f, 13.5f, -24);
            menuCamera.transform.LookAt(new Vector3(0, 2.2f, 7));
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (PhotoMode) TogglePhotoMode();
            else if (JournalOpen || MapOpen) { JournalOpen = MapOpen = false; RefreshCursor(); }
            else if (AetherExpansion.I != null && AetherExpansion.I.IsOpen) AetherExpansion.I.Close();
            else if (AetherFrontiers.I != null && AetherFrontiers.I.IsOpen) AetherFrontiers.I.Close();
            else if (AetherHorizons.I != null && AetherHorizons.I.IsOpen) AetherHorizons.I.Close();
            else if (Phase == GamePhase.Paused) Resume();
            else if (ActivePuzzle >= 0) ClosePuzzle();
            else if (Phase == GamePhase.Playing) Pause();
            return;
        }
        if (Phase != GamePhase.Playing) return;
        if (!JournalOpen && !MapOpen && !PhotoMode) PlaySeconds += Time.deltaTime;
        if (UpdateExpansionInput()) return;
        DiscoverNearbyWaystones();
        saveTimer += Time.deltaTime;
        if (saveTimer >= 6)
        {
            saveTimer = 0;
            if (Player.Body.isGrounded && ValidPosition(Player.transform.position, SolvedCount == 3))
                LastSafePosition = Player.transform.position + Vector3.up * .08f;
            Save();
        }
        if (ActivePuzzle >= 0) { PuzzleKeys(); return; }
        UpdateInteraction();
        UpdateExtraInteraction();
        UpdateFrontierInteraction();
        UpdateHorizonInteraction();
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (nearby >= 0) Interact(nearby);
            else if (!CollectNearbyExtraRelic() && !CollectNearbyFrontierRecord() && !CollectNearbyHorizonRecord())
            {
                AetherExpansion.I.TryInteract();
                if(!HasOpenSidePuzzle)AetherFrontiers.I.TryInteract();
                if(!HasOpenSidePuzzle)AetherHorizons.I.TryInteract();
            }
        }
    }

    void PuzzleKeys()
    {
        for (int i = 0; i < 4; i++)
        {
            if (!Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) continue;
            if (ActivePuzzle == 0 && i < 3) SelectedRing = i;
            else if (ActivePuzzle == 1) PressTone(i);
            else if (ActivePuzzle == 2 && i < 3) ToggleLight(i);
        }
        if (ActivePuzzle == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q)) RotateRing(SelectedRing, -1);
            if (Input.GetKeyDown(KeyCode.E)) RotateRing(SelectedRing, 1);
        }
        if (Input.GetKeyDown(KeyCode.H)) Hint();
        if (ActivePuzzle == 1 && Input.GetKeyDown(KeyCode.R)) ReplaySequence();
    }

    void UpdateInteraction()
    {
        nearby = -1; Prompt = ""; float closest = 3.8f;
        for (int i = 0; i < 7; i++)
        {
            if (i >= 3 && i < 6 && RelicFound[i - 3]) continue;
            Vector3 target = i < 3 ? Stations[i] + Vector3.up : i < 6 ? RelicPositions[i - 3] : new Vector3(0, 1.5f, 33);
            float distance = Vector3.Distance(Player.transform.position + Vector3.up, target);
            float dot = Vector3.Dot(Player.View.transform.forward, (target - Player.View.transform.position).normalized);
            if (distance < closest && dot > -.05f) { nearby = i; closest = distance; }
        }
        if (nearby < 0) return;
        if (nearby < 3) Prompt = Solved[nearby] ? Names[nearby] + " · 복구 완료" : Names[nearby] + " 살펴보기";
        else if (nearby < 6) Prompt = "남겨진 기록 읽기";
        else Prompt = TotalSolved == TotalPuzzles ? "새로운 아침으로 걸어가기" : "마지막 문 · 깨어난 빛 " + TotalSolved + " / " + TotalPuzzles;
    }

    public void Interact(int id)
    {
        if (Phase != GamePhase.Playing || ActivePuzzle >= 0 || HasOpenSidePuzzle) return;
        if (id < 0 || id > 6) return;
        if (id < 3)
        {
            if (Solved[id]) { Notify("이 장치에는 다시 빛이 흐르고 있습니다."); return; }
            ActivePuzzle = id; SelectedRing = 0; Prompt = ""; RefreshCursor();
            AetherAudio.I?.Click();
            if (id == 1) ReplaySequence();
        }
        else if (id < 6)
        {
            int r = id - 3; if (RelicFound[r]) return;
            RelicFound[r] = true; relics[r].gameObject.SetActive(false);
            AetherAudio.I?.Chime(r); Notify(Memories[r], 9); Save();
        }
        else if (TotalSolved == TotalPuzzles && bridgeReady) Complete();
        else Notify("아홉 섬의 27개 장치를 복구해야 마지막 문이 열립니다. M 지도에서 남은 성소를 확인하세요.");
    }

    public void Begin(bool resume)
    {
        StopMelody(); ActivePuzzle = -1;
        JournalOpen = MapOpen = false;
        if (PhotoMode) TogglePhotoMode();
        SaveData data = resume ? ReadSave() : null;
        ResetExpansion(data != null);
        Solved = new bool[3]; RelicFound = new bool[3]; RingValues = new int[3]; LightValues = new[] { 1, 0, 0 };
        SequenceProgress = 0; sequence.Clear(); PlaySeconds = 0;
        LastSafePosition = new Vector3(0, .12f, -10);
        if (data != null)
        {
            Solved = data.solved; RelicFound = data.relics; RingValues = data.rings; LightValues = data.lights;
            PlaySeconds = Mathf.Max(0, data.seconds);
            LastSafePosition = ValidPosition(data.position, SolvedCount == 3) ? data.position : LastSafePosition;
        }
        Player.Teleport(LastSafePosition, data != null ? data.yaw : 0);
        ResetWaystones(data != null);
        for (int i = 0; i < 3; i++) { relics[i].gameObject.SetActive(!RelicFound[i]); ApplyStationState(i); }
        bridgeReady = SolvedCount == 3; bridgeTime = bridgeReady ? Time.time - 8 : -20;
        foreach (Transform step in bridgeSteps) step.gameObject.SetActive(bridgeReady);
        Phase = GamePhase.Playing; SetCameraState(); RefreshCursor();
        Notify(data == null ? "아홉 섬에 남겨진 27개의 빛을 깨우세요. 비석 발견 후 M 지도에서 빠르게 이동할 수 있습니다." : "돌아온 발걸음을 기억합니다. 남쪽 지평선의 세 섬과 새로운 이야기가 기다립니다.", 7);
        Save();
    }

    public void Pause() { if (Phase != GamePhase.Playing) return; if(PhotoMode) TogglePhotoMode(); JournalOpen=MapOpen=false; Phase = GamePhase.Paused; StopMelody(); Save(); RefreshCursor(); }
    public void Resume() { Phase = GamePhase.Playing; RefreshCursor(); }
    public void ReturnToMenu() { if(PhotoMode) TogglePhotoMode(); StopMelody(); Save(); ActivePuzzle = -1; AetherExpansion.I.Close(); AetherFrontiers.I.Close(); AetherHorizons.I.Close(); JournalOpen=MapOpen=false; Phase = GamePhase.Menu; SetCameraState(); RefreshCursor(); }
    public void Restart() { Begin(false); }
    public void SetVolume(float value) { Volume = Mathf.Clamp01(value); AetherAudio.I?.SetVolume(Volume); if (!smokeMode) { PlayerPrefs.SetFloat("Aether.Volume", Volume); PlayerPrefs.Save(); } }
    public void SetReducedMotion(bool value) { ReducedMotion = value; if (!smokeMode) { PlayerPrefs.SetInt("Aether.ReducedMotion", value ? 1 : 0); PlayerPrefs.Save(); } }
    public void ClosePuzzle() { StopMelody(); ActivePuzzle = -1; sequence.Clear(); SequenceProgress = 0; Save(); RefreshCursor(); }
    public void RefreshCursor() { bool locked = Phase == GamePhase.Playing && ActivePuzzle < 0 && !JournalOpen && !MapOpen && !HasOpenSidePuzzle; Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
    void SetCameraState() { Player.View.enabled = Phase != GamePhase.Menu; menuCamera.enabled = Phase == GamePhase.Menu; }
    void OnApplicationFocus(bool focus) { if (!focus && Phase == GamePhase.Playing && !smokeMode) Pause(); }
    void OnApplicationQuit() { Save(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    public void RotateRing(int index, int delta)
    {
        if (Phase != GamePhase.Playing || ActivePuzzle != 0 || index < 0 || index >= 3) return;
        RingValues[index] = (RingValues[index] + delta % 8 + 8) % 8;
        AetherAudio.I?.Chime(index);
        if (RingsSolved(RingValues)) Solve(0);
    }
    public void PressTone(int tone)
    {
        if (Phase != GamePhase.Playing || ActivePuzzle != 1 || playingSequence || tone < 0 || tone > 3) return;
        AetherAudio.I?.Chime(tone);
        if (tone != Melody[SequenceProgress])
        {
            sequence.Clear(); SequenceProgress = 0; Notify("메아리가 흩어졌습니다. 첫 음부터 다시 시도하세요.", 3); return;
        }
        sequence.Add(tone); SequenceProgress = sequence.Count;
        if (SequenceProgress == 4 && IsSequenceCorrect(sequence.ToArray())) Solve(1);
    }
    public void ToggleLight(int index)
    {
        if (Phase != GamePhase.Playing || ActivePuzzle != 2 || index < 0 || index > 2) return;
        int mask = LightValues[0] | (LightValues[1] << 1) | (LightValues[2] << 2);
        mask = LightMaskAfterToggle(mask, index);
        for (int i = 0; i < 3; i++) { LightValues[i] = (mask >> i) & 1; lamps[i].sharedMaterial = LightValues[i] == 1 ? gold : inactive; }
        AetherAudio.I?.Chime(index);
        if (mask == 7) Solve(2);
    }
    public void ReplaySequence()
    {
        if (ActivePuzzle != 1 || Phase != GamePhase.Playing) return;
        StopMelody(); sequence.Clear(); SequenceProgress = 0; melodyRoutine = StartCoroutine(PlaySequence());
    }
    IEnumerator PlaySequence()
    {
        playingSequence = true;
        Notify("귀를 기울이세요…", 1);
        yield return new WaitForSeconds(.4f);
        foreach (int note in Melody)
        {
            PreviewTone = note;
            Notify("정원의 메아리  ·  " + new[] { "I", "II", "III", "IV" }[note], .9f);
            AetherAudio.I?.Chime(note); yield return new WaitForSeconds(.85f);
        }
        PreviewTone = -1; playingSequence = false; melodyRoutine = null; Notify("당신의 차례입니다. 같은 순서로 응답하세요.", 3);
    }
    void StopMelody() { if (melodyRoutine != null) StopCoroutine(melodyRoutine); melodyRoutine = null; playingSequence = false; PreviewTone = -1; }
    public string GetPuzzleHint() => ActivePuzzle == 0 ? "관측 기록: 바깥 고리 2 · 가운데 고리 5 · 안쪽 고리 3."
        : ActivePuzzle == 1 ? "메아리의 순서: I → III → II → IV. 소리를 꺼도 숫자를 따라갈 수 있습니다."
        : "각 스위치는 두 빛을 바꿉니다. 시작 상태에서는 II를 한 번 누르면 됩니다. 현재 상태가 다르면, 꺼진 두 빛을 함께 바꾸는 스위치를 찾으세요.";
    public void Hint() { Notify(GetPuzzleHint(), 12); }
    public void Notify(string message, float seconds = 5) { Toast = message; ToastUntil = Time.unscaledTime + seconds; }

    void Solve(int id)
    {
        if (Solved[id]) return;
        Solved[id] = true; ApplyStationState(id); AetherAudio.I?.Solve();
        ClosePuzzle();
        Notify(Names[id] + " · 빛 복구 완료  /  " + SolvedCount + " : 3", 6);
        LastSafePosition = Player.transform.position + Vector3.up * .1f;
        if (SolvedCount == 3)
        {
            bridgeTime = Time.time;
            foreach (Transform step in bridgeSteps) step.gameObject.SetActive(true);
            Notify(TotalSolved==TotalPuzzles ? "27개의 빛이 깨어났습니다. 북쪽 새벽의 문으로 향하세요." : "새벽의 길이 열렸습니다. M 지도에서 아홉 섬의 남은 성소를 찾아보세요.", 9);
        }
        Save();
    }
    void Complete()
    {
        Phase = GamePhase.Complete; ActivePuzzle = -1; Save(); RefreshCursor(); AetherAudio.I?.Solve();
        Player.View.transform.LookAt(new Vector3(0, 3.5f, 36));
    }

    void BuildStations()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = Stations[i];
            AetherWorld.Shape(PrimitiveType.Cylinder, "Device plinth " + i, p + Vector3.up * .18f, new Vector3(2.5f, .18f, 2.5f), dark);
            AetherWorld.Shape(PrimitiveType.Cylinder, "Porcelain console " + i, p + Vector3.up * .62f, new Vector3(1.7f, .4f, 1.7f), stone);
            var core = AetherWorld.Shape(PrimitiveType.Sphere, "Sleeping lumen " + i, p + Vector3.up * 1.7f, Vector3.one * .32f, inactive, null, false);
            stationCores[i] = core.transform;
            stationLights[i] = core.AddComponent<Light>(); stationLights[i].color = new Color(.35f, 1, .89f); stationLights[i].range = 5; stationLights[i].intensity = .5f;
            TextLabel("0" + (i + 1), p + new Vector3(0, .65f, -.88f), .12f, new Color(.17f, .31f, .30f));
            TextLabel(new[] { "CELESTIAL ALIGNMENT", "THE ECHO ARCHIVE", "CIRCUIT OF LIGHT" }[i], p + new Vector3(0, .12f, -1.7f), .09f, new Color(.88f, .80f, .56f), true);
            beams[i] = Line("Restored energy " + i, new[] { p + Vector3.up * 2, new Vector3(0, 5.8f, 36) }, cyan, .045f);
            beams[i].gameObject.SetActive(false);
        }
        for (int r = 0; r < 3; r++)
        {
            Vector3 center = Stations[0] + Vector3.up * 1.8f;
            var pivot = new GameObject("Celestial ring " + r); pivot.transform.position = center; rings[r] = pivot.transform;
            var points = new Vector3[65]; float radius = .67f + r * .21f;
            for (int k = 0; k <= 64; k++) { float a = k * Mathf.PI / 32; points[k] = new Vector3(Mathf.Sin(a) * radius, Mathf.Cos(a) * radius, 0); }
            var ring = Line("Engraved orbit", points, gold, .026f); ring.useWorldSpace = false; ring.transform.SetParent(pivot.transform, false);
            AetherWorld.Shape(PrimitiveType.Cube, "Alignment marker", center + Vector3.up * radius, new Vector3(.1f, .14f, .08f), cyan, pivot.transform, false);
        }
        TextLabel("II  ·  V  ·  III", Stations[0] + new Vector3(0, 1.04f, -.86f), .13f, new Color(.17f, .29f, .29f));
        for (int n = 0; n < 4; n++)
        {
            Vector3 p = Stations[1] + new Vector3((n - 1.5f) * .35f, 1.4f, 0);
            var crystal = AetherWorld.Shape(PrimitiveType.Cube, "Echo crystal " + n, p, new Vector3(.18f, .48f + n * .12f, .18f), cyan, null, false);
            crystal.transform.rotation = Quaternion.Euler(0, 45, 15); animated.Add(crystal.transform); animatedBases.Add(p);
        }
        for (int n = 0; n < 3; n++)
        {
            Vector3 p = Stations[2] + new Vector3((n - 1) * .52f, 1.22f, -.18f);
            lamps[n] = AetherWorld.Shape(PrimitiveType.Sphere, "Circuit lumen " + n, p, Vector3.one * .3f, n == 0 ? gold : inactive, null, false).GetComponent<Renderer>();
        }
    }

    void BuildRelics()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = RelicPositions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder, "Archive stone", new Vector3(p.x, .25f, p.z), new Vector3(.8f, .25f, .8f), stone);
            var relic = AetherWorld.Shape(PrimitiveType.Cube, "Memory shard " + i, p, new Vector3(.23f, .48f, .23f), gold, null, false);
            relic.transform.rotation = Quaternion.Euler(0, 45, 25); relics[i] = relic.transform;
            animated.Add(relic.transform); animatedBases.Add(p);
            var light = relic.AddComponent<Light>(); light.type = LightType.Point; light.range = 3; light.intensity = .7f; light.color = new Color(1, .7f, .3f);
        }
    }
    void BuildBridge()
    {
        for (int i = 0; i < 10; i++)
        {
            var step = AetherWorld.Shape(PrimitiveType.Cube, "Awakening bridge " + i, new Vector3(0, -.22f, 22 + i * .7f), new Vector3(3.3f, .4f, .77f), stone);
            var edge = AetherWorld.Shape(PrimitiveType.Cube, "Golden bridge seam", new Vector3(0, -.012f, 22 + i * .7f), new Vector3(3.1f, .013f, .04f), gold, step.transform, false);
            bridgeSteps.Add(step.transform); step.SetActive(false);
        }
    }
    void ApplyStationState(int i)
    {
        stationCores[i].GetComponent<Renderer>().sharedMaterial = Solved[i] ? cyan : inactive;
        stationLights[i].intensity = Solved[i] ? 2.2f : .4f;
        beams[i].gameObject.SetActive(Solved[i]);
        if (AetherWorld.BeaconTops != null && AetherWorld.BeaconTops.Length > i && AetherWorld.BeaconTops[i] != null)
        {
            var renderer = AetherWorld.BeaconTops[i].GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = Solved[i] ? cyan : inactive;
        }
        if (i == 2) for (int n = 0; n < 3; n++) lamps[n].sharedMaterial = LightValues[n] == 1 ? gold : inactive;
    }
    void AnimateWorld()
    {
        float t = Time.time;
        if (!ReducedMotion)
            for (int i = 0; i < animated.Count; i++)
            {
                animated[i].position = animatedBases[i] + Vector3.up * Mathf.Sin(t * 1.3f + i) * .075f;
                animated[i].Rotate(0, 15 * Time.deltaTime, 0, Space.World);
            }
        for (int r = 0; r < 3; r++) rings[r].localRotation = Quaternion.Slerp(rings[r].localRotation, Quaternion.Euler(0, 0, -RingValues[r] * 45), Time.deltaTime * 5);
        if (SolvedCount == 3)
        {
            for (int i = 0; i < bridgeSteps.Count; i++)
            {
                float f = Mathf.Clamp01((t - bridgeTime - i * .13f) / 1.1f);
                f = f * f * (3 - 2 * f);
                bridgeSteps[i].position = new Vector3(0, Mathf.Lerp(-4, -.22f, f), 22 + i * .7f);
            }
            bridgeReady = t - bridgeTime > 2.4f;
        }
        if (AetherWorld.Portal != null && !ReducedMotion && SolvedCount == 3)
            AetherWorld.Portal.Rotate(0, 0, 4 * Time.deltaTime, Space.Self);
    }
    static LineRenderer Line(string name, Vector3[] points, Material mat, float width)
    {
        var line = new GameObject(name).AddComponent<LineRenderer>();
        line.generateLightingData = true;
        line.sharedMaterial = mat; line.positionCount = points.Length; line.SetPositions(points); line.widthMultiplier = width;
        line.numCapVertices = 4; line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return line;
    }
    static void TextLabel(string text, Vector3 position, float size, Color color, bool floor = false)
    {
        var go = new GameObject("Inscription · " + text); go.transform.position = position;
        if (floor) go.transform.rotation = Quaternion.Euler(80, 0, 0);
        var label = go.AddComponent<TextMesh>(); label.text = text; label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center; label.fontSize = 56; label.characterSize = size; label.color = color;
    }

    [Serializable] class SaveData
    {
        public int version = 1;
        public bool[] solved, relics;
        public int[] rings, lights;
        public float seconds, yaw;
        public Vector3 position;
    }
    SaveData ReadSave()
    {
        if (!AetherSaveStore.HasKey(SaveKey)) return null;
        try
        {
            var s = JsonUtility.FromJson<SaveData>(AetherSaveStore.GetString(SaveKey));
            if (s == null || s.version != 1 || s.solved == null || s.solved.Length != 3 || s.relics == null || s.relics.Length != 3 || s.rings == null || s.rings.Length != 3 || s.lights == null || s.lights.Length != 3) return null;
            for (int i = 0; i < 3; i++) if (s.rings[i] < 0 || s.rings[i] > 7 || s.lights[i] < 0 || s.lights[i] > 1) return null;
            int lit = s.lights[0] + s.lights[1] + s.lights[2];
            if (lit % 2 != 1 || s.solved[0] != RingsSolved(s.rings) || s.solved[2] != (lit == 3)) return null;
            if (float.IsNaN(s.seconds) || float.IsInfinity(s.seconds) || float.IsNaN(s.yaw) || float.IsInfinity(s.yaw)) return null;
            return s;
        }
        catch { return null; }
    }
    static bool ValidPosition(Vector3 p, bool finalUnlocked)
    {
        if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) return false;
        if (p.y < -.1f || p.y > 2) return false;
        bool main = Mathf.Abs(p.x) < 16 && p.z > -14 && p.z < 21;
        bool sides = Mathf.Abs(p.x) > 29 && Mathf.Abs(p.x) < 56 && p.z > -9 && p.z < 19;
        bool sideBridges = Mathf.Abs(p.x) >= 16 && Mathf.Abs(p.x) <= 30 && p.z > -4.5f && p.z < -1.5f;
        bool bridge = finalUnlocked && Mathf.Abs(p.x) < 1.4f && p.z >= 21 && p.z < 30;
        bool final = finalUnlocked && new Vector2(p.x, p.z - 35).magnitude < 6;
        bool southernIslands = p.z > -61 && p.z < -31 && (Mathf.Abs(p.x)<13 || Mathf.Abs(p.x)>30 && Mathf.Abs(p.x)<56);
        bool southernBridges = p.z >= -31 && p.z < -10 && (Mathf.Abs(p.x)<1.65f || Mathf.Abs(Mathf.Abs(p.x)-43)<1.65f);
        bool southernMainEntrance = Mathf.Abs(p.x)<1.65f && p.z>=-16 && p.z<=-14;
        bool horizonIslands = p.z > -113 && p.z < -83 && (Mathf.Abs(p.x)<13 || Mathf.Abs(p.x)>30 && Mathf.Abs(p.x)<56);
        bool horizonBridges = p.z >= -83 && p.z <= -60 && (Mathf.Abs(p.x)<1.65f || Mathf.Abs(Mathf.Abs(p.x)-43)<1.65f);
        return main || bridge || final || sides || sideBridges || southernIslands || southernBridges || southernMainEntrance || horizonIslands || horizonBridges;
    }
    void Save()
    {
        if (Phase == GamePhase.Menu || Player == null) return;
        Vector3 position = Player.Body.isGrounded && ValidPosition(Player.transform.position, SolvedCount == 3) ? Player.transform.position + Vector3.up * .05f : LastSafePosition;
        var s = new SaveData { solved = Solved, relics = RelicFound, rings = RingValues, lights = LightValues, seconds = PlaySeconds, position = position, yaw = Player.Yaw };
        AetherSaveStore.SetString(SaveKey, JsonUtility.ToJson(s));
        SaveExpansion();
    }

    IEnumerator SmokeJourney()
    {
        string[] args = Environment.GetCommandLineArgs(); int ai = Array.IndexOf(args, "-aether-report");
        string dir = ai >= 0 && ai + 1 < args.Length ? args[ai + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(dir);
        var checks = new List<string>();
        yield return new WaitForSeconds(2);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "01-title.png"));
        yield return new WaitForSeconds(.5f);
        Begin(false);
        yield return new WaitForSeconds(1);
        SetQualityLevel(0);
        yield return new WaitForSeconds(1.1f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "00-no-postprocess.png"));
        yield return new WaitForSeconds(.4f);
        SetQualityLevel(2);
        yield return new WaitForSeconds(1.1f);
        Check(Phase == GamePhase.Playing && ActivePuzzle < 0, "new journey", checks);
        Check(Player.Body.isGrounded && Player.transform.position.y > -.1f, "spawn collision", checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "02-exploration.png"));
        yield return new WaitForSeconds(.5f);
        Pause(); Check(!Player.CanMove && Phase == GamePhase.Paused, "pause movement", checks); Resume();
        Interact(0); Check(ActivePuzzle == 0 && !Player.CanMove, "puzzle locks movement", checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "03-puzzle.png"));
        yield return new WaitForSeconds(.4f);
        RotateRing(0, 1); Check(!Solved[0], "wrong alignment stays closed", checks);
        RotateRing(0, 1); RotateRing(1, 5); RotateRing(2, 3); Check(Solved[0], "celestial puzzle", checks);
        Interact(1); yield return new WaitForSeconds(4.1f);
        PressTone(3); Check(SequenceProgress == 0 && !Solved[1], "wrong echo resets", checks);
        foreach (int tone in Melody) PressTone(tone);
        Check(Solved[1], "echo puzzle", checks);
        Interact(2); ToggleLight(0); Check(!Solved[2], "wrong circuit stays closed", checks); ToggleLight(0); ToggleLight(1);
        Check(Solved[2] && SolvedCount == 3, "light puzzle", checks);
        yield return new WaitForSeconds(3);
        Check(bridgeReady && bridgeSteps[5].GetComponent<Collider>().enabled && bridgeSteps[5].gameObject.activeSelf, "bridge activation", checks);
        for (int r = 3; r < 6; r++) Interact(r); Check(RelicCount == 3, "all memories", checks);
        Player.Teleport(new Vector3(0, .15f, 24), 0, -5); yield return new WaitForSeconds(.6f);
        Check(Player.Body.isGrounded && Player.transform.position.y > -.1f, "bridge walkable", checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "04-awakened.png"));
        yield return new WaitForSeconds(.6f);
        yield return SmokeExpansion(checks, dir);
        // Old release saves contain the original nine lights and no frontier board.
        Save();
        AetherSaveStore.SetString("Aether.Frontiers.v1", "");
        Begin(true);
        Check(TotalSolved==9 && TotalRelics==9 && AetherFrontiers.I.SolvedCount==0,"old journey keeps its nine lights and adds new content",checks);
        yield return SmokeSouthernCrossings(checks);
        yield return AetherFrontiers.I.ValidateJourney(checks,dir);
        yield return SmokeFrontierRecords(checks,dir);
        Save();
        AetherSaveStore.SetString("Aether.Horizons.v1", "");
        AetherSaveStore.SetString("Aether.HorizonRecords.v1", "");
        AetherSaveStore.SetString("Aether.Waystones.v1", "");
        Begin(true);
        Check(TotalSolved==18 && TotalRelics==24 && AetherHorizons.I.SolvedCount==0, "earlier save retains eighteen lights and twenty-four records", checks);
        Check(VisitedWaystones==6, "legacy completed regions receive six waystones", checks);
        Interact(6); Check(Phase==GamePhase.Playing, "eighteen lights do not bypass horizon finale", checks);
        yield return SmokeHorizonCrossings(checks);
        yield return AetherHorizons.I.ValidateJourney(checks,dir);
        yield return SmokeHorizonRecordsAndTravel(checks,dir);
        Save();
        Begin(true);
        yield return new WaitForSeconds(.7f);
        Check(TotalSolved==TotalPuzzles && TotalRelics==TotalRecords && VisitedWaystones==9 && bridgeReady, "saved journey restores nine regions, thirty-nine records and nine waystones", checks);
        Player.Teleport(new Vector3(0, .15f, 32), 0, -5); yield return new WaitForSeconds(.6f);
        Interact(6); Check(Phase == GamePhase.Complete, "ending", checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "05-ending.png"));
        yield return new WaitForSeconds(.7f);
        Begin(false); Check(TotalSolved == 0 && TotalRelics == 0 && VisitedWaystones==1 && !bridgeSteps[0].gameObject.activeSelf, "restart resets journey and keeps only starting waystone", checks);
        Interact(0);RotateRing(0,1);ClosePuzzle();Save();Begin(true);
        Check(RingValues[0]==1 && SolvedCount==0,"unfinished puzzle survives save and resume",checks);
        string validSave = AetherSaveStore.GetString(SaveKey);
        var damaged = JsonUtility.FromJson<SaveData>(validSave);
        damaged.lights = new[] { 0, 0, 0 };
        AetherSaveStore.SetString(SaveKey, JsonUtility.ToJson(damaged));
        Check(!HasSave, "unreachable corrupt puzzle save is rejected", checks);
        AetherSaveStore.SetString(SaveKey, validSave);
        Screen.SetResolution(1280, 720, false);
        yield return new WaitForSeconds(1);
        ToggleMap();
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "11-atlas-1280.png"));
        yield return new WaitForSeconds(.6f);
        ToggleMap(); Pause();
        ScreenCapture.CaptureScreenshot(Path.Combine(dir, "12-settings-1280.png"));
        yield return new WaitForSeconds(.6f);
        var report = new SmokeReport { passed = !checks.Exists(s => s.StartsWith("FAIL")), checks = checks.ToArray(), unity = Application.unityVersion };
        File.WriteAllText(Path.Combine(dir, "runtime-validation.json"), JsonUtility.ToJson(report, true));
        Debug.Log("AETHER_SMOKE " + (report.passed ? "PASS" : "FAIL"));
        Application.Quit(report.passed ? 0 : 1);
    }
    static void Check(bool pass, string label, List<string> checks) { checks.Add((pass ? "PASS " : "FAIL ") + label); if (!pass) Debug.LogError("AETHER_SMOKE: " + label); }
    [Serializable] class SmokeReport { public bool passed; public string unity; public string[] checks; }
}

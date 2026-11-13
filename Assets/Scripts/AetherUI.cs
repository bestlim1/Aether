using UnityEngine;

/// <summary>Resolution independent interface for Aether's built-in rendering project.</summary>
public sealed class AetherUI : MonoBehaviour
{
    private readonly Color ivory = new Color(0.94f, 0.94f, 0.86f);
    private readonly Color muted = new Color(0.62f, 0.73f, 0.73f);
    private readonly Color cyan = new Color(0.45f, 0.88f, 0.85f);
    private readonly Color gold = new Color(0.90f, 0.76f, 0.46f);
    private readonly Color deep = new Color(0.025f, 0.075f, 0.091f, 0.96f);
    private Font bodyFont, displayFont;
    private GUIStyle textStyle, buttonStyle;
    private Texture2D white, circle, disc, shade, hudShade, vignette;
    private bool initialized, showSettings, showControls, hintVisible;
    private int lastPuzzle = -1;
    private int lastExpansionPuzzle = -1;
    private int lastFrontierPuzzle = -1;
    private int lastHorizonPuzzle = -1;
    private int journalSelection;
    private int journalPage;
    private Vector2 journalScroll;
    private int mapSelection;
    private bool wasMapOpen, mapSelectionExplicit;
    private int lastPressedTone = -1;
    private float toneFlashUntil;
    private float width, height, scale;
    private Camera sceneCamera;
    private float nextCameraCheck;
    private bool wasPhotoMode;
    private float photoHintUntil, hideInterfaceUntil;

    private void Update()
    {
        AetherGame game = AetherGame.I;
        if (game != null)
        {
            if (game.MapOpen && !wasMapOpen)
            {
                if (!mapSelectionExplicit && game.Player != null)
                {
                    float bestDistance = float.MaxValue;
                    for (int i = 0; i < AetherGame.WaystonePositions.Length; i++)
                    {
                        float distance = (AetherGame.WaystonePositions[i] - game.Player.transform.position).sqrMagnitude;
                        if (distance < bestDistance) { bestDistance = distance; mapSelection = i; }
                    }
                }
                mapSelectionExplicit = false;
            }
            wasMapOpen = game.MapOpen;
            if (game.PhotoMode && !wasPhotoMode) photoHintUntil = Time.unscaledTime + 9f;
            wasPhotoMode = game.PhotoMode;
            if (Input.GetKeyDown(KeyCode.F12)) hideInterfaceUntil = Time.unscaledTime + 0.65f;
        }
        if (AetherGame.I != null && AetherGame.I.Phase == GamePhase.Menu && Input.GetKeyDown(KeyCode.Escape))
        {
            showSettings = false;
            showControls = false;
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        bodyFont = Font.CreateDynamicFontFromOSFont(new string[] { "Malgun Gothic", "Noto Sans CJK KR", "Arial" }, 24);
        displayFont = Font.CreateDynamicFontFromOSFont(new string[] { "Georgia", "Times New Roman", "Arial" }, 80);
        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.font = bodyFont;
        textStyle.padding = new RectOffset(0, 0, 0, 0);
        textStyle.margin = new RectOffset(0, 0, 0, 0);
        textStyle.richText = false;
        buttonStyle = new GUIStyle(GUIStyle.none);
        white = Texture2D.whiteTexture;
        circle = CircleTexture(false);
        disc = CircleTexture(true);
        shade = new Texture2D(128, 2, TextureFormat.RGBA32, false);
        shade.wrapMode = TextureWrapMode.Clamp;
        shade.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < 128; x++)
        {
            float a = x == 127 ? 0f : Mathf.SmoothStep(0.96f, 0f, (float)x / 127f);
            shade.SetPixel(x, 0, new Color(0.015f, 0.055f, 0.067f, a));
            shade.SetPixel(x, 1, new Color(0.015f, 0.055f, 0.067f, a));
        }
        shade.Apply();
        hudShade = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        hudShade.wrapMode = TextureWrapMode.Clamp;
        hudShade.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 128; x++)
        {
            float horizontal = Mathf.SmoothStep(1f, 0f, x / 127f);
            float vertical = Mathf.SmoothStep(0f, 1f, y / 63f);
            hudShade.SetPixel(x, y, new Color(0.015f, 0.055f, 0.067f, horizontal * vertical * 0.88f));
        }
        hudShade.Apply();
        vignette = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        vignette.wrapMode = TextureWrapMode.Clamp;
        vignette.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 128; y++)
        for (int x = 0; x < 128; x++)
        {
            float nx = (x / 127f - 0.5f) * 2f;
            float ny = (y / 127f - 0.5f) * 2f;
            float a = Mathf.Clamp01((Mathf.Sqrt(nx * nx + ny * ny) - 0.48f) * 0.37f);
            vignette.SetPixel(x, y, new Color(0.015f, 0.04f, 0.06f, a));
        }
        vignette.Apply();
    }

    private static Texture2D CircleTexture(bool filled)
    {
        const int size = 128;
        Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
        result.wrapMode = TextureWrapMode.Clamp;
        result.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64f, 64f));
            float a = filled ? Mathf.Clamp01(62f - d) : Mathf.Clamp01(1.7f - Mathf.Abs(d - 60f));
            result.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        AetherGame game = AetherGame.I;
        if (game == null) return;
        if (Time.unscaledTime < hideInterfaceUntil) return;
        Initialize();
        scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
        if (scale <= 0f) return;
        width = Screen.width / scale;
        height = Screen.height / scale;
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
        GUI.depth = -100;
        GUI.color = Color.white;
        if (!game.PhotoMode) Texture(new Rect(0, 0, width, height), vignette, Color.white);

        if (game.ActivePuzzle != lastPuzzle)
        {
            hintVisible = false;
            lastPuzzle = game.ActivePuzzle;
        }
        AetherExpansion expansion = AetherExpansion.I;
        int expansionPuzzle = expansion != null ? expansion.ActivePuzzle : -1;
        if (expansionPuzzle != lastExpansionPuzzle)
        {
            hintVisible = false;
            lastExpansionPuzzle = expansionPuzzle;
        }
        AetherFrontiers frontier = AetherFrontiers.I;
        int frontierPuzzle = frontier != null ? frontier.ActivePuzzle : -1;
        if (frontierPuzzle != lastFrontierPuzzle)
        {
            hintVisible = false;
            lastFrontierPuzzle = frontierPuzzle;
        }
        AetherHorizons horizon = AetherHorizons.I;
        int horizonPuzzle = horizon != null ? horizon.ActivePuzzle : -1;
        if (horizonPuzzle != lastHorizonPuzzle)
        {
            hintVisible = false;
            lastHorizonPuzzle = horizonPuzzle;
        }

        if (game.Phase == GamePhase.Menu)
        {
            MainMenu(game);
            if (showSettings) Settings(game, true);
            else if (showControls) Controls(game);
        }
        else if (game.Phase == GamePhase.Complete) Completion(game);
        else
        {
            bool clearView = game.ActivePuzzle < 0 && !game.HasOpenSidePuzzle && !game.JournalOpen && !game.MapOpen;
            if (game.PhotoMode)
            {
                float alpha = Mathf.Clamp01(photoHintUntil - Time.unscaledTime) * 0.7f;
                if (alpha > 0) Label(new Rect(width * 0.5f - 680, height - 51, 1360, 29), "WASD  이동     Q · E  높이     마우스 휠  화각     F12  사진 저장     F  사진 모드 끝내기", 14, WithAlpha(ivory, alpha), FontStyle.Normal, TextAnchor.MiddleCenter);
                GUI.matrix = previous;
                GUI.color = Color.white;
                return;
            }
            if (game.Phase == GamePhase.Playing && clearView) Hud(game);
            if (game.Phase == GamePhase.Paused) Settings(game, false);
            else if (game.JournalOpen) Journal(game);
            else if (game.MapOpen) Map(game);
            else if (game.ActivePuzzle >= 0) Puzzle(game);
            else if (expansionPuzzle >= 0) ExpansionPuzzle(game, expansion);
            else if (frontierPuzzle >= 0) FrontierPuzzle(game, frontier);
            else if (horizonPuzzle >= 0) HorizonPuzzle(game, horizon);
            Toast(game);
        }
        GUI.matrix = previous;
        GUI.color = Color.white;
    }

    private void MainMenu(AetherGame game)
    {
        bool hasSave = game.HasSave;
        Texture(new Rect(0, 0, width * 0.68f, height), shade, Color.white);
        RectFill(new Rect(0, 0, width, 4), WithAlpha(cyan, 0.55f));
        float x = 112f;
        float y = height * 0.20f;
        Emblem(new Vector2(x + 27, y - 51), 26f, gold);
        Label(new Rect(x + 74, y - 66, 470, 32), "AN ATMOSPHERIC EXPLORATION", 15, muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        SpacedText(new Vector2(x - 5, y - 11), "AETHER", 98, 14f, ivory, displayFont);
        Label(new Rect(x + 3, y + 119, 560, 43), "잊힌 빛의 정원", 32, ivory);
        Line(new Vector2(x + 4, y + 187), new Vector2(x + 58, y + 187), gold, 2);
        Label(new Rect(x + 4, y + 213, 530, 84), (game.TotalPuzzles / 3) + "개의 섬, " + game.TotalPuzzles + "개의 잠든 빛.\n잊힌 정원의 기억을 깨워 주세요.", 21, muted);

        bool overlay = showSettings || showControls;
        GUI.enabled = !overlay;
        float by = y + 335f;
        string start = hasSave ? "여정 이어가기" : "정원으로 들어가기";
        if (Button(new Rect(x, by, 414, 66), start, true, hasSave ? "CONTINUE" : "BEGIN JOURNEY")) game.Begin(hasSave);
        if (hasSave)
        {
            if (Button(new Rect(x, by + 82, 414, 56), "새로운 여정", false, "NEW JOURNEY")) game.Begin(false);
            by += 70;
        }
        if (TextButton(new Rect(x, by + 91, 100, 36), "설정")) showSettings = true;
        if (TextButton(new Rect(x + 126, by + 91, 130, 36), "플레이 방법")) showControls = true;
        if (TextButton(new Rect(x + 294, by + 91, 120, 36), "게임 종료")) Application.Quit();
        GUI.enabled = true;

        float footerY = height - 115;
        Line(new Vector2(x, footerY), new Vector2(width - 112, footerY), WithAlpha(ivory, 0.15f), 1);
        Label(new Rect(x, footerY + 26, 840, 26), (game.TotalPuzzles / 3) + "개의 지역   /   " + game.TotalPuzzles + "개의 환경 퍼즐   /   " + game.TotalRecords + "개의 기억", 16, muted);
        Label(new Rect(width - 610, footerY + 26, 498, 26), "서두르지 않아도 괜찮습니다.", 16, muted, FontStyle.Normal, TextAnchor.MiddleRight);
        RectFill(new Rect(width - 405, 83, 293, 71), new Color(0.02f, 0.075f, 0.08f, 0.28f));
        Diamond(new Vector2(width - 374, 118), 5, gold);
        Label(new Rect(width - 353, 93, 225, 45), "THE FORGOTTEN GARDEN", 13, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
    }

    private void Hud(AetherGame game)
    {
        Texture(new Rect(0, 0, 590, 288), hudShade, Color.white);
        Emblem(new Vector2(80, 79), 18, gold);
        SpacedText(new Vector2(112, 53), "AETHER", 26, 6, ivory, displayFont);
        Label(new Rect(113, 96, 420, 26), game.CurrentRegion, 15, muted);
        Line(new Vector2(61, 146), new Vector2(94, 146), WithAlpha(gold, 0.8f), 2);
        Label(new Rect(112, 129, 450, 32), game.TotalSolved == game.TotalPuzzles ? "북쪽의 문으로 향하세요" : "섬들을 탐험하며 " + game.TotalPuzzles + "개의 빛을 깨우세요", 17, ivory);

        float right = width - 70;
        Label(new Rect(right - 276, 49, 270, 29), "깨어난 성소", 16, muted, FontStyle.Normal, TextAnchor.MiddleRight);
        for (int i = 0; i < game.TotalPuzzles; i++)
        {
            bool solved = IsStationSolved(game, i);
            Vector2 p = new Vector2(right - 264 + (i % 9) * 31, 123 + (i / 9) * 29);
            Diamond(p, 7, solved ? cyan : WithAlpha(ivory, 0.26f));
            if (solved) Diamond(p, 3.5f, cyan, true);
        }
        Label(new Rect(right - 170, 75, 158, 35), game.TotalSolved + " / " + game.TotalPuzzles, 24, ivory, FontStyle.Normal, TextAnchor.MiddleRight);
        float recordY = 125 + Mathf.CeilToInt(game.TotalPuzzles / 9f) * 29;
        Label(new Rect(right - 310, recordY, 302, 28), "기억의 조각   " + game.TotalRelics + " / " + game.TotalRecords, 15, muted, FontStyle.Normal, TextAnchor.MiddleRight);
        Compass();

        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        Texture(new Rect(center.x - 2, center.y - 2, 4, 4), disc, WithAlpha(ivory, 0.78f));
        Line(new Vector2(center.x - 9, center.y), new Vector2(center.x - 6, center.y), WithAlpha(ivory, 0.3f), 1);
        Line(new Vector2(center.x + 6, center.y), new Vector2(center.x + 9, center.y), WithAlpha(ivory, 0.3f), 1);

        if (!string.IsNullOrEmpty(game.Prompt))
        {
            float py = height - 211;
            RectFill(new Rect(width * 0.5f - 305, py, 610, 66), new Color(0.025f, 0.065f, 0.075f, 0.81f));
            Line(new Vector2(width * 0.5f - 305, py), new Vector2(width * 0.5f + 305, py), WithAlpha(cyan, 0.42f), 1);
            Keycap(new Rect(width * 0.5f - 264, py + 15, 36, 36), "E");
            Label(new Rect(width * 0.5f - 207, py + 13, 480, 39), game.Prompt, 21, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
        }

        ControlsStrip(height - 71);
        Label(new Rect(113, 174, 470, 24), "J  기록     M  지도     F  사진 모드", 12, muted);
        if (game.PlaySeconds < 9f)
        {
            float alpha = Mathf.Clamp01(game.PlaySeconds / 1.4f) * Mathf.Clamp01((9f - game.PlaySeconds) / 2f);
            Label(new Rect(width * 0.5f - 500, height * 0.29f, 1000, 27), "CHAPTER 01", 15, WithAlpha(gold, alpha), FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(new Rect(width * 0.5f - 500, height * 0.29f + 46, 1000, 65), "침묵의 끝에서", 37, WithAlpha(ivory, alpha), FontStyle.Normal, TextAnchor.MiddleCenter);
        }
    }

    private void Compass()
    {
        if (Time.unscaledTime >= nextCameraCheck)
        {
            sceneCamera = Camera.main;
            nextCameraCheck = Time.unscaledTime + 1f;
        }
        if (sceneCamera == null) return;
        float bearing = sceneCamera.transform.eulerAngles.y;
        float cx = width * 0.5f;
        for (int angle = 0; angle < 360; angle += 15)
        {
            float delta = Mathf.DeltaAngle(bearing, angle);
            if (Mathf.Abs(delta) > 60) continue;
            float x = cx + delta * 3.2f;
            float opacity = Mathf.Clamp01(1f - Mathf.Abs(delta) / 65f);
            bool cardinal = angle % 90 == 0;
            Line(new Vector2(x, 78), new Vector2(x, cardinal ? 87 : 82), WithAlpha(muted, opacity * 0.7f), 1);
            if (cardinal)
            {
                string[] labels = { "N", "E", "S", "W" };
                Label(new Rect(x - 20, 42, 40, 30), labels[angle / 90], 14, WithAlpha(ivory, opacity), FontStyle.Normal, TextAnchor.MiddleCenter);
            }
        }
        Diamond(new Vector2(cx, 97), 3, gold, true);
    }

    private void ControlsStrip(float y)
    {
        float x = width * 0.5f - 471;
        RectFill(new Rect(x - 23, y - 10, 980, 50), new Color(0.018f, 0.055f, 0.065f, 0.62f));
        ControlPair(x, y, "WASD", "이동", 79);
        ControlPair(x + 180, y, "마우스", "시점", 78);
        ControlPair(x + 360, y, "SHIFT", "달리기", 80);
        ControlPair(x + 550, y, "SPACE", "점프", 81);
        ControlPair(x + 735, y, "ESC", "메뉴", 62);
    }

    private void ControlPair(float x, float y, string key, string label, float keyWidth)
    {
        Keycap(new Rect(x, y, keyWidth, 30), key, 12);
        Label(new Rect(x + keyWidth + 13, y, 90, 30), label, 14, muted, FontStyle.Normal, TextAnchor.MiddleLeft);
    }

    private void Settings(AetherGame game, bool fromMenu)
    {
        Backdrop();
        Rect box = new Rect(width * 0.5f - 390, height * 0.5f - 446, 780, 892);
        Panel(box);
        float x = box.x + 58;
        Label(new Rect(x, box.y + 44, 500, 29), fromMenu ? "PREFERENCES" : "TAKE A BREATH", 14, gold);
        Label(new Rect(x, box.y + 92, 560, 58), fromMenu ? "나에게 맞는 여정" : "잠시 쉬어가기", 35, ivory);
        Label(new Rect(x, box.y + 159, 664, 40), "발견한 빛과 기억은 자동으로 저장됩니다.", 17, muted);
        Line(new Vector2(x, box.y + 211), new Vector2(box.xMax - 58, box.y + 211), WithAlpha(ivory, 0.13f), 1);

        Label(new Rect(x, box.y + 235, 350, 38), "소리", 20, ivory);
        Label(new Rect(box.xMax - 158, box.y + 238, 100, 30), Mathf.RoundToInt(game.Volume * 100) + "%", 18, gold, FontStyle.Normal, TextAnchor.MiddleRight);
        float volume = Slider(new Rect(x, box.y + 288, 664, 24), game.Volume);
        if (Mathf.Abs(volume - game.Volume) > 0.001f) game.SetVolume(volume);

        Label(new Rect(x, box.y + 332, 350, 38), "시점 감도", 20, ivory);
        Label(new Rect(box.xMax - 158, box.y + 335, 100, 30), game.Sensitivity.ToString("0.0"), 18, gold, FontStyle.Normal, TextAnchor.MiddleRight);
        float sensitivity = Mathf.Lerp(0.5f, 5f, Slider(new Rect(x, box.y + 385, 664, 24), Mathf.InverseLerp(0.5f, 5f, game.Sensitivity)));
        if (Mathf.Abs(sensitivity - game.Sensitivity) > 0.001f) game.SetSensitivity(sensitivity);
        Label(new Rect(x, box.y + 436, 440, 37), "화면 움직임 줄이기", 20, ivory);
        Label(new Rect(x, box.y + 478, 480, 33), "카메라 흔들림과 시야 변화가 줄어듭니다.", 15, muted);
        if (Toggle(new Rect(box.xMax - 139, box.y + 437, 81, 35), game.ReducedMotion)) game.SetReducedMotion(!game.ReducedMotion);
        Label(new Rect(x, box.y + 538, 400, 33), "그래픽 품질", 20, ivory);
        string[] qualityNames = { "가볍게", "균형 있게", "아름답게" };
        for (int i = 0; i < 3; i++)
            if (Button(new Rect(x + i * 226, box.y + 590, 212, 48), qualityNames[i], game.QualityLevel == i, null, 17)) game.SetQualityLevel(i);
        Line(new Vector2(x, box.y + 683), new Vector2(box.xMax - 58, box.y + 683), WithAlpha(ivory, 0.13f), 1);
        if (Button(new Rect(x, box.y + 721, 664, 64), fromMenu ? "설정 완료" : "여정 계속하기", true, fromMenu ? "SAVE & RETURN" : "RESUME"))
        {
            showSettings = false;
            if (!fromMenu) game.Resume();
        }
        if (!fromMenu)
        {
            if (TextButton(new Rect(x, box.y + 817, 280, 39), "처음 화면으로")) game.ReturnToMenu();
            Label(new Rect(box.xMax - 266, box.y + 819, 208, 31), "ESC  계속하기", 15, muted, FontStyle.Normal, TextAnchor.MiddleRight);
        }
        else Label(new Rect(x, box.y + 820, 664, 30), "설정은 다음 여정에도 유지됩니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Controls(AetherGame game)
    {
        Backdrop();
        Rect box = new Rect(width * 0.5f - 440, height * 0.5f - 381, 880, 762);
        Panel(box);
        float x = box.x + 62;
        Label(new Rect(x, box.y + 47, 600, 30), "A SMALL GUIDE TO THE GARDEN", 14, gold);
        Label(new Rect(x, box.y + 91, 710, 54), "정원의 목소리를 따라", 35, ivory);
        Label(new Rect(x, box.y + 169, 750, 68), (game.TotalPuzzles / 3) + "개 지역을 탐험하고 " + game.TotalPuzzles + "개의 퍼즐을 풀어 보세요.\n모든 빛을 깨운 뒤 북쪽의 문을 통과하면 여정이 완성됩니다.", 19, muted);
        string[] keys = { "W A S D", "마우스", "SHIFT / SPACE", "E", "J / M / F", "ESC" };
        string[] explanations = { "정원 탐험하기", "주변 둘러보기", "달리기 / 점프", "장치 살펴보기 · 단서 읽기 · 조각 수집", "기억의 기록 / 섬 지도 / 사진 모드", "장치 닫기 / 잠시 쉬어가기" };
        for (int i = 0; i < keys.Length; i++)
        {
            float y = box.y + 261 + i * 53;
            Keycap(new Rect(x, y, 171, 41), keys[i], 15);
            Label(new Rect(x + 202, y, 559, 41), explanations[i], 18, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
        }
        Label(new Rect(x, box.y + 600, 750, 28), "길을 잃었다면, 하늘로 올라가는 빛줄기를 찾아보세요.", 17, gold);
        if (Button(new Rect(x, box.y + 658, 756, 58), "준비됐어요", true)) showControls = false;
    }

    private void Puzzle(AetherGame game)
    {
        Backdrop(0.63f);
        Rect box = new Rect(width * 0.5f - 620, height * 0.5f - 381, 1240, 762);
        Panel(box);
        float left = box.x + 52;
        string[] names = { "THE CELESTIAL ALIGNMENT", "THE MEMORY OF SOUND", "THE PATH OF LIGHT" };
        int puzzle = Mathf.Clamp(game.ActivePuzzle, 0, 2);
        Label(new Rect(left, box.y + 41, 950, 28), "SANCTUARY  0" + (puzzle + 1) + "   /   " + names[puzzle], 14, gold);
        if (TextButton(new Rect(box.xMax - 137, box.y + 35, 85, 39), "닫기  ×")) game.ClosePuzzle();
        Label(new Rect(left, box.y + 99, 1080, 60), game.PuzzleTitle, 36, ivory);
        Line(new Vector2(left, box.y + 181), new Vector2(box.xMax - 52, box.y + 181), WithAlpha(ivory, 0.14f), 1);

        Label(new Rect(left, box.y + 224, 400, 32), "장치에 남겨진 기록", 16, gold);
        Label(new Rect(left, box.y + 277, 391, 132), game.PuzzleDescription, 20, ivory);
        string[] observations = {
            "각 고리는 여덟 개의 위치를 가집니다.\n주변의 각인을 살펴보고 바깥 고리부터\n올바른 위치로 정렬하세요.",
            "성소가 들려주는 네 개의 음을 기억하세요.\n같은 순서로 울리면 잠든 기억이 깨어납니다.",
            "하나의 렌즈를 누르면 그 렌즈와\n바로 다음 렌즈가 함께 바뀝니다.\n세 렌즈에 모두 빛을 채워 주세요."
        };
        Label(new Rect(left, box.y + 426, 391, 112), observations[puzzle], 16, muted);
        if (Button(new Rect(left, box.y + 583, 351, 49), hintVisible ? "단서 다시 읽기" : "단서 떠올리기", false, "HINT"))
        {
            hintVisible = true;
            game.Hint();
        }
        if (hintVisible)
        {
            Label(new Rect(left, box.y + 644, 389, 80), game.GetPuzzleHint(), 15, gold);
        }
        else Label(new Rect(left, box.y + 652, 390, 47), "해답은 언제나 정원 어딘가에 있습니다.", 15, muted);
        Line(new Vector2(box.x + 485, box.y + 219), new Vector2(box.x + 485, box.y + 709), WithAlpha(ivory, 0.10f), 1);

        Rect area = new Rect(box.x + 537, box.y + 215, 651, 476);
        if (puzzle == 0) Rings(game, area);
        else if (puzzle == 1) Tones(game, area);
        else Lights(game, area);
        Label(new Rect(area.x, box.y + 709, area.width, 25), "ESC  장치에서 물러나기", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Rings(AetherGame game, Rect area)
    {
        string[] ringNames = { "바깥 고리", "가운데 고리", "안쪽 고리" };
        for (int i = 0; i < 3; i++)
        {
            float y = area.y + 14 + i * 144;
            bool selected = game.SelectedRing == i;
            Rect row = new Rect(area.x, y, area.width, 128);
            RectFill(row, new Color(0.09f, 0.20f, 0.22f, selected ? 0.55f : 0.26f));
            if (selected) RectFill(new Rect(row.x, row.y, 3, row.height), cyan);
            Label(new Rect(row.x + 25, y + 26, 150, 35), ringNames[i], 19, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
            Label(new Rect(row.x + 26, y + 72, 135, 29), "RING  0" + (i + 1), 12, muted);
            if (GUI.Button(new Rect(row.x + 7, row.y + 3, 181, row.height - 6), GUIContent.none, buttonStyle))
            {
                game.SelectedRing = i;
                SoundClick();
            }
            int value = game.RingValues != null && game.RingValues.Length > i ? game.RingValues[i] : 0;
            if (Button(new Rect(row.x + 204, y + 36, 61, 56), "‹", false, null, 34)) game.RotateRing(i, -1);
            Vector2 center = new Vector2(row.x + 359, y + 64);
            RingDial(center, 50, value, selected ? cyan : muted);
            Label(new Rect(center.x - 43, center.y - 26, 86, 52), value.ToString(), 34, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (Button(new Rect(row.x + 454, y + 36, 61, 56), "›", false, null, 34)) game.RotateRing(i, 1);
            Label(new Rect(row.x + 550, y + 45, 66, 41), "0 – 7", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        Label(new Rect(area.x, area.y + 459, area.width, 33), "1 · 2 · 3  고리 선택     Q / E  회전", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void RingDial(Vector2 center, float radius, int value, Color color)
    {
        Texture(new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2), circle, WithAlpha(color, 0.35f));
        for (int tick = 0; tick < 8; tick++)
        {
            float angle = (tick * 45 - 90) * Mathf.Deg2Rad;
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Diamond(p, value == tick ? 5 : 2.8f, value == tick ? color : WithAlpha(color, 0.30f), value == tick);
        }
    }

    private void Tones(AetherGame game, Rect area)
    {
        bool playing = game.IsPlayingSequence;
        Label(new Rect(area.x, area.y + 16, area.width, 34), playing ? "성소가 부르는 네 개의 음에 귀를 기울이세요." : "울림을 듣고, 기억하고, 되돌려 주세요.", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        if (Button(new Rect(area.center.x - 161, area.y + 81, 322, 54), playing ? "처음부터 다시 듣기" : "성소의 울림 듣기", false, "LISTEN")) game.ReplaySequence();

        string[] numerals = { "I", "II", "III", "IV" };
        for (int i = 0; i < 4; i++)
        {
            Rect key = new Rect(area.x + i * 165, area.y + 191, 155, 183);
            bool hover = !playing && key.Contains(Event.current.mousePosition);
            bool sounding = playing ? game.PreviewTone == i : lastPressedTone == i && Time.unscaledTime < toneFlashUntil;
            Color accent = Color.Lerp(cyan, gold, i / 3f);
            RectFill(key, sounding ? new Color(accent.r * 0.23f, accent.g * 0.23f, accent.b * 0.23f, 1f) : new Color(0.09f, 0.18f, 0.19f, hover ? 0.94f : 0.55f));
            Border(key, WithAlpha(accent, sounding ? 1f : hover ? 0.8f : 0.27f));
            Line(new Vector2(key.x + 23, key.y + 1), new Vector2(key.xMax - 23, key.y + 1), WithAlpha(accent, 0.7f), 2);
            if (sounding) Texture(new Rect(key.center.x - 50, key.y + 23, 100, 100), circle, WithAlpha(accent, 0.32f));
            Label(new Rect(key.x, key.y + 31, key.width, 79), numerals[i], 48, sounding ? ivory : accent, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Label(new Rect(key.x, key.y + 128, key.width, 26), sounding ? "울리는 중" : "TONE  0" + (i + 1), sounding ? 13 : 12, sounding ? accent : muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !playing;
            if (GUI.Button(key, GUIContent.none, buttonStyle))
            {
                lastPressedTone = i;
                toneFlashUntil = Time.unscaledTime + 0.36f;
                game.PressTone(i);
            }
            GUI.enabled = enabled;
        }
        Label(new Rect(area.x, area.y + 405, area.width, 28), playing ? "듣기가 끝나면 응답할 수 있습니다." : "기억의 흐름     /     1 · 2 · 3 · 4  응답", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 4; i++)
        {
            Vector2 p = new Vector2(area.center.x - 66 + i * 44, area.y + 465);
            if (i < 3) Line(new Vector2(p.x + 8, p.y), new Vector2(p.x + 36, p.y), WithAlpha(ivory, 0.12f), 1);
            bool filled = i < game.SequenceProgress;
            Diamond(p, 7, filled ? cyan : WithAlpha(ivory, 0.23f), filled);
        }
    }

    private void Lights(AetherGame game, Rect area)
    {
        Label(new Rect(area.x, area.y + 13, area.width, 34), "세 개의 렌즈를 하나의 빛으로", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        string[] numerals = { "I", "II", "III" };
        for (int i = 0; i < 3; i++)
        {
            float cx = area.x + 110 + i * 215;
            float cy = area.y + 211;
            bool on = game.LightValues != null && game.LightValues.Length > i && game.LightValues[i] != 0;
            Color color = on ? gold : muted;
            if (i < 2)
            {
                Line(new Vector2(cx + 75, cy), new Vector2(cx + 137, cy), WithAlpha(cyan, 0.22f), 1);
                Label(new Rect(cx + 86, cy - 20, 45, 40), "›", 27, WithAlpha(cyan, 0.6f), FontStyle.Normal, TextAnchor.MiddleCenter);
            }
            if (on) Texture(new Rect(cx - 72, cy - 72, 144, 144), disc, WithAlpha(gold, 0.10f));
            Texture(new Rect(cx - 76, cy - 76, 152, 152), circle, WithAlpha(color, on ? 0.8f : 0.34f));
            Texture(new Rect(cx - 57, cy - 57, 114, 114), circle, WithAlpha(color, on ? 0.42f : 0.17f));
            Diamond(new Vector2(cx, cy), 20, color, on);
            Label(new Rect(cx - 78, cy + 108, 156, 43), numerals[i], 29, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Label(new Rect(cx - 78, cy + 162, 156, 30), on ? "빛이 깨어남" : "잠든 렌즈", 15, color, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(cx - 86, cy - 90, 172, 296), GUIContent.none, buttonStyle))
            {
                game.ToggleLight(i);
                SoundClick();
            }
        }
        Label(new Rect(area.x, area.y + 437, area.width, 40), "렌즈 클릭 또는 1 · 2 · 3      /      III 다음은 I", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void ExpansionPuzzle(AetherGame game, AetherExpansion puzzle)
    {
        Backdrop(0.68f);
        Rect box = new Rect(width * 0.5f - 620, height * 0.5f - 395, 1240, 790);
        Panel(box);
        float left = box.x + 52;
        int id = Mathf.Clamp(puzzle.ActivePuzzle, 0, 5);
        Label(new Rect(left, box.y + 41, 1000, 28), (id < 3 ? "THE LUNAR ARCHIVE" : "THE SOLAR OBSERVATORY") + "   /   TRIAL  0" + (id + 4), 14, gold);
        if (TextButton(new Rect(box.xMax - 137, box.y + 35, 85, 39), "닫기  ×")) { puzzle.Close(); return; }
        Label(new Rect(left, box.y + 99, 1080, 60), puzzle.PuzzleTitle, 36, ivory);
        Line(new Vector2(left, box.y + 181), new Vector2(box.xMax - 52, box.y + 181), WithAlpha(ivory, 0.14f), 1);
        Label(new Rect(left, box.y + 224, 400, 32), "장치에 남겨진 기록", 16, gold);
        Label(new Rect(left, box.y + 274, 391, 165), puzzle.PuzzleDescription, 19, ivory);
        string[] instructions = {
            "빈칸과 맞닿은 타일을 누르면 움직입니다.\n첫 줄부터 왼쪽에서 오른쪽으로\n숫자의 흐름을 되돌려 주세요.",
            "각 렌즈는 다음 렌즈와 연결되어 있습니다.\n마지막 렌즈 다음은 첫 번째 렌즈입니다.\n다섯 빛을 모두 켜 주세요.",
            "여섯 번의 울림을 기억하세요.\n듣기 버튼으로 언제든 다시 들을 수 있습니다.\n소리를 끈 상태에서도 빛으로 확인하세요.",
            "각 추는 0부터 6까지 조절할 수 있습니다.\n주변 기록의 관계를 만족하는\n세 개의 무게를 찾으세요.",
            "두 개의 룬을 차례로 선택하면\n서로 자리를 바꿉니다.\n석판에 새겨진 순서를 따라 주세요.",
            "거울을 누를 때마다 90도 회전합니다.\n북쪽부터 시계 방향으로\nN · E · S · W를 가리킵니다."
        };
        Label(new Rect(left, box.y + 452, 391, 108), instructions[id], 16, muted);
        if (Button(new Rect(left, box.y + 596, 351, 49), hintVisible ? "단서 다시 읽기" : "단서 떠올리기", false, "HINT"))
        {
            hintVisible = true;
            game.Notify(puzzle.GetHint(), 12);
        }
        if (hintVisible) Label(new Rect(left, box.y + 661, 391, 96), puzzle.GetHint(), 15, gold);
        else Label(new Rect(left, box.y + 665, 391, 58), "잠시 물러나 주변을 살펴보아도 좋습니다.", 15, muted);
        Line(new Vector2(box.x + 485, box.y + 219), new Vector2(box.x + 485, box.y + 737), WithAlpha(ivory, 0.10f), 1);
        Rect area = new Rect(box.x + 537, box.y + 215, 651, 502);
        switch (id)
        {
            case 0: SlidingTiles(puzzle, area); break;
            case 1: FiveLights(puzzle, area); break;
            case 2: ExtendedTones(puzzle, area); break;
            case 3: Balance(puzzle, area); break;
            case 4: Runes(puzzle, area); break;
            case 5: Mirrors(puzzle, area); break;
        }
        Label(new Rect(area.x, box.y + 739, area.width, 25), "ESC  장치에서 물러나기", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void SlidingTiles(AetherExpansion puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        const float tile = 124f, gap = 10f;
        float x = area.center.x - 196;
        float y = area.y + 32;
        for (int i = 0; i < 9; i++)
        {
            int value = values[i];
            Rect cell = new Rect(x + (i % 3) * (tile + gap), y + (i / 3) * (tile + gap), tile, tile);
            bool correct = value == i + 1;
            if (value == 0)
            {
                Border(cell, WithAlpha(cyan, 0.11f));
                Diamond(cell.center, 10, WithAlpha(cyan, 0.15f));
                continue;
            }
            bool hover = cell.Contains(Event.current.mousePosition);
            RectFill(cell, hover ? new Color(0.12f, 0.29f, 0.31f) : new Color(0.09f, 0.18f, 0.20f));
            Border(cell, WithAlpha(correct ? gold : cyan, hover ? 0.8f : 0.31f));
            Label(cell, value.ToString(), 47, correct ? gold : ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (correct) Diamond(new Vector2(cell.xMax - 15, cell.y + 15), 2.5f, gold, true);
            if (GUI.Button(cell, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(area.x, area.y + 459, area.width, 33), "1 → 8  순서로 정렬    ·    마지막 자리는 빈칸", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void FiveLights(AetherExpansion puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Vector2 center = new Vector2(area.center.x, area.y + 235);
        Texture(new Rect(center.x - 166, center.y - 166, 332, 332), circle, WithAlpha(cyan, 0.11f));
        Vector2[] nodes = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            float angle = (i * 72f - 90) * Mathf.Deg2Rad;
            nodes[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 159;
        }
        for (int i = 0; i < 5; i++) Line(nodes[i], nodes[(i + 1) % 5], WithAlpha(cyan, 0.22f), 1.4f);
        int lit = 0;
        string[] marks = { "I", "II", "III", "IV", "V" };
        for (int i = 0; i < 5; i++)
        {
            bool on = values[i] != 0;
            if (on) lit++;
            Color accent = on ? gold : muted;
            Rect lens = new Rect(nodes[i].x - 46, nodes[i].y - 46, 92, 92);
            Texture(lens, disc, on ? new Color(0.25f, 0.23f, 0.14f) : new Color(0.05f, 0.11f, 0.13f));
            Texture(lens, circle, WithAlpha(accent, on ? 0.95f : 0.34f));
            Label(lens, marks[i], 27, on ? ivory : muted, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (GUI.Button(lens, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(center.x - 78, center.y - 25, 156, 50), lit + " / 5", 30, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
        Label(new Rect(area.x, area.y + 470, area.width, 30), "한 번의 선택, 두 개의 변화", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void ExtendedTones(AetherExpansion puzzle, Rect area)
    {
        Label(new Rect(area.x, area.y + 14, area.width, 34), puzzle.IsPreviewing ? "빛의 흐름을 따라 여섯 음을 기억하세요." : "오래된 정원이 다시 노래합니다.", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        if (Button(new Rect(area.center.x - 161, area.y + 77, 322, 54), "기억의 울림 듣기", false, "LISTEN")) puzzle.Replay();
        string[] numerals = { "I", "II", "III", "IV" };
        for (int i = 0; i < 4; i++)
        {
            Rect key = new Rect(area.x + i * 165, area.y + 190, 155, 182);
            bool active = puzzle.Highlight == i;
            Color accent = Color.Lerp(cyan, gold, i / 3f);
            RectFill(key, active ? new Color(accent.r * 0.27f, accent.g * 0.27f, accent.b * 0.27f) : new Color(0.07f, 0.15f, 0.17f));
            Border(key, WithAlpha(accent, active ? 1 : 0.25f));
            if (active) Texture(new Rect(key.center.x - 49, key.y + 25, 98, 98), circle, WithAlpha(accent, 0.4f));
            Label(new Rect(key.x, key.y + 31, key.width, 79), numerals[i], 48, active ? ivory : accent, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Label(new Rect(key.x, key.y + 128, key.width, 26), active ? "울리는 중" : "TONE  0" + (i + 1), 12, active ? accent : muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !puzzle.IsPreviewing;
            if (GUI.Button(key, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
            GUI.enabled = enabled;
        }
        Label(new Rect(area.x, area.y + 409, area.width, 28), puzzle.IsPreviewing ? "듣기가 끝나면 응답할 수 있습니다." : "여섯 개의 기억을 같은 순서로 이어 주세요.", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 6; i++)
        {
            Vector2 point = new Vector2(area.center.x - 110 + i * 44, area.y + 469);
            if (i < 5) Line(point + Vector2.right * 8, point + Vector2.right * 36, WithAlpha(ivory, 0.14f), 1);
            Diamond(point, 7, i < puzzle.Progress ? cyan : WithAlpha(ivory, 0.24f), i < puzzle.Progress);
        }
    }

    private void Balance(AetherExpansion puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 10, area.width, 35), "서로의 무게가 균형을 이룹니다.", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        string[] names = { "A", "B", "C" };
        for (int i = 0; i < 3; i++)
        {
            float cx = area.x + 110 + i * 215;
            Label(new Rect(cx - 75, area.y + 67, 150, 46), names[i], 32, gold, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            float baseline = area.y + 280;
            Line(new Vector2(cx - 73, baseline), new Vector2(cx + 73, baseline), WithAlpha(ivory, 0.3f), 2);
            for (int n = 0; n < 6; n++)
            {
                Rect weight = new Rect(cx - 39, baseline - 18 - n * 23, 78, 16);
                bool filled = n < values[i];
                RectFill(weight, WithAlpha(filled ? gold : cyan, filled ? 0.55f : 0.08f));
                Border(weight, WithAlpha(filled ? gold : cyan, filled ? 0.58f : 0.12f));
            }
            Label(new Rect(cx - 45, area.y + 300, 90, 62), values[i].ToString(), 42, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (Button(new Rect(cx - 78, area.y + 389, 65, 51), "−", false, null, 26)) puzzle.DoAction(i, -1);
            if (Button(new Rect(cx + 13, area.y + 389, 65, 51), "+", false, null, 26)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 468, area.width, 30), "각 추의 무게  0 – 6", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Runes(AetherExpansion puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 16, area.width, 34), "하나를 고르고, 자리를 바꿀 룬을 고르세요.", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        string[] names = { "씨앗", "물결", "산", "태양", "별" };
        string[] numerals = { "I", "II", "III", "IV", "V" };
        for (int i = 0; i < 5; i++)
        {
            int value = Mathf.Clamp(values[i], 0, 4);
            Rect card = new Rect(area.x + i * 132, area.y + 126, 122, 251);
            bool selected = puzzle.SelectedIndex == i;
            RectFill(card, selected ? new Color(0.12f, 0.27f, 0.28f) : new Color(0.07f, 0.15f, 0.17f));
            Border(card, WithAlpha(selected ? gold : cyan, selected ? 0.9f : 0.25f));
            Label(new Rect(card.x, card.y + 15, card.width, 28), "0" + (i + 1), 12, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            RuneIcon(new Vector2(card.center.x, card.y + 106), value, 30, selected ? gold : cyan);
            Label(new Rect(card.x, card.y + 170, card.width, 31), names[value], 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(new Rect(card.x, card.y + 214, card.width, 26), numerals[value], 17, gold, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (GUI.Button(card, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(area.x, area.y + 425, area.width, 55), puzzle.SelectedIndex >= 0 ? "선택한 룬과 자리를 바꿀 다른 룬을 눌러 주세요." : "정원의 문자는 저마다 하나의 기억을 품고 있습니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void RuneIcon(Vector2 center, int rune, float size, Color color)
    {
        if (rune == 0)
        {
            Texture(new Rect(center.x - size * 0.65f, center.y - size * 0.65f, size * 1.3f, size * 1.3f), circle, color);
            Line(center, center + new Vector2(size * 0.55f, -size), color, 2);
        }
        else if (rune == 1)
        {
            for (int row = -1; row <= 1; row++)
            for (int part = 0; part < 6; part++)
            {
                float x1 = -size + part * size / 3f;
                float x2 = -size + (part + 1) * size / 3f;
                Line(center + new Vector2(x1, row * 15 + Mathf.Sin(part * Mathf.PI / 3) * 5), center + new Vector2(x2, row * 15 + Mathf.Sin((part + 1) * Mathf.PI / 3) * 5), color, 2);
            }
        }
        else if (rune == 2)
        {
            Line(center + new Vector2(-size, size * 0.75f), center + new Vector2(0, -size), color, 2);
            Line(center + new Vector2(0, -size), center + new Vector2(size, size * 0.75f), color, 2);
            Line(center + new Vector2(-size, size * 0.75f), center + new Vector2(size, size * 0.75f), color, 2);
        }
        else if (rune == 3)
        {
            Texture(new Rect(center.x - size * 0.48f, center.y - size * 0.48f, size * 0.96f, size * 0.96f), circle, color);
            for (int n = 0; n < 8; n++)
            {
                Vector2 direction = new Vector2(Mathf.Sin(n * Mathf.PI / 4), Mathf.Cos(n * Mathf.PI / 4));
                Line(center + direction * size * 0.7f, center + direction * size, color, 2);
            }
        }
        else
        {
            Diamond(center, size, color);
            Line(center + Vector2.left * size, center + Vector2.right * size, WithAlpha(color, 0.6f), 1);
            Line(center + Vector2.up * size, center + Vector2.down * size, WithAlpha(color, 0.6f), 1);
            Diamond(center, size * 0.22f, color, true);
        }
    }

    private void Mirrors(AetherExpansion puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        string[] compass = { "N", "E", "S", "W" };
        for (int i = 0; i < 4; i++)
        {
            Vector2 center = new Vector2(area.x + 172 + (i % 2) * 310, area.y + 121 + (i / 2) * 249);
            Rect card = new Rect(center.x - 133, center.y - 106, 266, 220);
            RectFill(card, new Color(0.07f, 0.15f, 0.17f));
            Border(card, WithAlpha(cyan, 0.18f));
            Label(new Rect(card.x + 12, card.y + 8, 77, 24), "0" + (i + 1), 12, muted);
            Texture(new Rect(center.x - 69, center.y - 69, 138, 138), circle, WithAlpha(cyan, 0.23f));
            for (int n = 0; n < 4; n++)
            {
                Vector2 direction = new Vector2(Mathf.Sin(n * Mathf.PI * 0.5f), -Mathf.Cos(n * Mathf.PI * 0.5f));
                Vector2 point = center + direction * 86;
                Label(new Rect(point.x - 15, point.y - 13, 30, 26), compass[n], 13, n == values[i] ? gold : muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            }
            float angle = values[i] * Mathf.PI * 0.5f;
            Vector2 ray = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
            Line(center - ray * 36, center + ray * 53, gold, 3);
            Vector2 normal = new Vector2(-ray.y, ray.x);
            Line(center + ray * 53, center + ray * 37 + normal * 11, gold, 2);
            Line(center + ray * 53, center + ray * 37 - normal * 11, gold, 2);
            Diamond(center, 8, ivory, true);
            if (GUI.Button(card, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
    }

    private void FrontierPuzzle(AetherGame game, AetherFrontiers puzzle)
    {
        Backdrop(0.68f);
        Rect box = new Rect(width * 0.5f - 620, height * 0.5f - 395, 1240, 790);
        Panel(box);
        int id = Mathf.Clamp(puzzle.ActivePuzzle, 0, 8);
        float left = box.x + 52;
        string[] regions = { "THE RAIN ARBORETUM", "THE TIDAL CLOISTER", "THE WIND ATELIER" };
        Label(new Rect(left, box.y + 41, 800, 28), regions[id / 3] + "   /   TRIAL  " + (id + 10).ToString("00"), 14, gold);
        if (TextButton(new Rect(box.xMax - 335, box.y + 35, 181, 39), "장치 초기화")) puzzle.Replay();
        if (TextButton(new Rect(box.xMax - 137, box.y + 35, 85, 39), "닫기  ×")) { puzzle.Close(); return; }
        Label(new Rect(left, box.y + 99, 1080, 60), puzzle.PuzzleTitle, 36, ivory);
        Line(new Vector2(left, box.y + 181), new Vector2(box.xMax - 52, box.y + 181), WithAlpha(ivory, 0.14f), 1);
        Label(new Rect(left, box.y + 224, 400, 32), "장치에 남겨진 기록", 16, gold);
        Label(new Rect(left, box.y + 274, 391, 165), puzzle.PuzzleDescription, 19, ivory);
        string[] instructions = {
            "수로를 누르면 시계 방향으로 회전합니다.\n왼쪽 입구에서 오른쪽 출구까지\n아홉 개의 수로를 모두 이어 주세요.",
            "각 칸에 0부터 4까지의 수를 놓으세요.\n세 행과 세 열의 합이\n가장자리에 새겨진 목표와 같아야 합니다.",
            "첫 씨앗의 값은 바꿀 수 없습니다.\n앞의 수를 두 배로 만든 뒤\n7로 나눈 나머지를 이어 놓으세요.",
            "오른쪽 화살표는 다음 수문으로,\n왼쪽 화살표는 거꾸로 물을 옮깁니다.\n마지막 수문은 첫 수문과 이어집니다.",
            "이웃한 칸을 눌러 항로를 이으세요.\n마지막 선택 칸을 다시 누르면 되돌립니다.\n초기화 버튼으로 처음부터 시작할 수 있습니다.",
            "각 공명기의 진폭을 1 이상으로 맞추세요.\n진폭의 합과 주파수를 곱한 합,\n두 조건을 함께 만족해야 합니다.",
            "한 톱니를 돌리면 다음 톱니가\n반대 방향으로 한 칸 움직입니다.\n금색 표식은 맞춰야 할 위치입니다.",
            "붉은빛, 초록빛, 푸른빛을 섞어\n기록된 새벽의 빛을 되살리세요.\n각 채널은 0부터 4까지 조절할 수 있습니다.",
            "A부터 F까지 바람 날개를 켜고 끄세요.\n각 날개 아래의 관계가\n모두 참이 되는 상태를 찾으세요."
        };
        Label(new Rect(left, box.y + 452, 391, 111), instructions[id], 16, muted);
        if (Button(new Rect(left, box.y + 596, 351, 49), hintVisible ? "단서 다시 읽기" : "단서 떠올리기", false, "HINT"))
        {
            hintVisible = true;
            game.Notify(puzzle.GetHint(), 12);
        }
        Label(new Rect(left, box.y + 661, 391, 98), hintVisible ? puzzle.GetHint() : "차분히 관찰하면, 모든 장치는 하나의 규칙을 들려줍니다.", 15, hintVisible ? gold : muted);
        Line(new Vector2(box.x + 485, box.y + 219), new Vector2(box.x + 485, box.y + 737), WithAlpha(ivory, 0.10f), 1);
        Rect area = new Rect(box.x + 537, box.y + 215, 651, 502);
        switch (id)
        {
            case 0: RainPipes(puzzle, area); break;
            case 1: GardenSums(puzzle, area); break;
            case 2: SeedLaw(puzzle, area); break;
            case 3: WaterGates(puzzle, area); break;
            case 4: TidalRoute(puzzle, area); break;
            case 5: Resonance(puzzle, area); break;
            case 6: CoupledGears(puzzle, area); break;
            case 7: DawnColor(puzzle, area); break;
            case 8: WindLogic(puzzle, area); break;
        }
        Label(new Rect(area.x, box.y + 739, area.width, 25), "R  장치 초기화     ESC  물러나기", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void RainPipes(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        int reached = AetherFrontiers.PipeReachable(values);
        float x = area.center.x - 176, y = area.y + 40;
        Vector2[] centers = new Vector2[9];
        int flowing = 0;
        for (int i = 0; i < 9; i++)
        {
            Rect tile = new Rect(x + (i % 3) * 121, y + (i / 3) * 121, 110, 110);
            centers[i] = tile.center;
            bool wet = (reached & (1 << i)) != 0;
            if (wet) flowing++;
            Color water = wet ? cyan : WithAlpha(muted, 0.58f);
            RectFill(tile, wet ? new Color(0.06f, 0.22f, 0.23f) : new Color(0.06f, 0.13f, 0.15f));
            Border(tile, WithAlpha(water, 0.36f));
            int mask = AetherFrontiers.PipeMask(i, values[i]);
            Vector2[] directions = { Vector2.up * -1, Vector2.right, Vector2.down * -1, Vector2.left };
            for (int d = 0; d < 4; d++)
                if ((mask & (1 << d)) != 0) Line(tile.center, tile.center + directions[d] * 57, water, 7);
            Texture(new Rect(tile.center.x - 9, tile.center.y - 9, 18, 18), disc, wet ? ivory : water);
            Label(new Rect(tile.x + 8, tile.y + 7, 35, 20), (i + 1).ToString(), 11, muted);
            if (GUI.Button(tile, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        for (int i = 0; i < 9; i++)
        {
            int mask = AetherFrontiers.PipeMask(i, values[i]);
            Color water = (reached & (1 << i)) != 0 ? cyan : WithAlpha(muted, 0.58f);
            if (i % 3 < 2 && (mask & 2) != 0 && (AetherFrontiers.PipeMask(i + 1, values[i + 1]) & 8) != 0)
                Line(centers[i] + new Vector2(55, 0), centers[i + 1] - new Vector2(55, 0), water, 7);
            if (i < 6 && (mask & 4) != 0 && (AetherFrontiers.PipeMask(i + 3, values[i + 3]) & 1) != 0)
                Line(centers[i] + new Vector2(0, 55), centers[i + 3] - new Vector2(0, 55), water, 7);
        }
        Line(new Vector2(x - 37, y + 55), new Vector2(x - 2, y + 55), cyan, 7);
        Label(new Rect(x - 79, y + 13, 70, 27), "입구", 13, cyan, FontStyle.Normal, TextAnchor.MiddleCenter);
        Line(new Vector2(x + 354, y + 297), new Vector2(x + 389, y + 297), gold, 7);
        Label(new Rect(x + 364, y + 322, 70, 27), "출구", 13, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.x, area.y + 449, area.width, 39), "물이 닿은 수로  " + flowing + " / 9", 17, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void GardenSums(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        float x = area.center.x - 223, y = area.y + 17;
        for (int row = 0; row < 3; row++)
        {
            int sum = 0;
            for (int col = 0; col < 3; col++)
            {
                int i = row * 3 + col;
                sum += values[i];
                Rect tile = new Rect(x + col * 119, y + row * 119, 108, 108);
                RectFill(tile, new Color(0.07f, 0.16f, 0.18f));
                Border(tile, WithAlpha(cyan, 0.23f));
                Label(new Rect(tile.x, tile.y + 8, tile.width, 54), values[i].ToString(), 33, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
                if (Button(new Rect(tile.x + 9, tile.y + 70, 39, 28), "−", false, null, 17)) puzzle.DoAction(i, -1);
                if (Button(new Rect(tile.x + 60, tile.y + 70, 39, 28), "+", false, null, 17)) puzzle.DoAction(i, 1);
            }
            int target = AetherFrontiers.GridRowTargets[row];
            Label(new Rect(x + 371, y + row * 119 + 24, 97, 35), sum + " / " + target, 21, sum == target ? cyan : gold, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(new Rect(x + 371, y + row * 119 + 65, 97, 26), "행의 합", 12, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        for (int col = 0; col < 3; col++)
        {
            int sum = values[col] + values[col + 3] + values[col + 6];
            int target = AetherFrontiers.GridColumnTargets[col];
            Label(new Rect(x + col * 119, y + 371, 108, 38), sum + " / " + target, 21, sum == target ? cyan : gold, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(new Rect(x + col * 119, y + 410, 108, 25), "열의 합", 12, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        Label(new Rect(area.x, area.y + 475, area.width, 25), "현재 합 / 목표 합     ·     모든 합이 맞으면 정원이 깨어납니다.", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void SeedLaw(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 24, area.width, 41), "이전 값 × 2 → 7로 나눈 나머지", 21, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 6; i++)
        {
            Rect card = new Rect(area.x + i * 110, area.y + 130, 100, 216);
            RectFill(card, new Color(0.065f, 0.16f, 0.16f));
            Border(card, WithAlpha(i == 0 ? gold : cyan, 0.3f));
            Label(new Rect(card.x, card.y + 19, card.width, 25), "SEED " + (i + 1), 11, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Texture(new Rect(card.center.x - 21, card.y + 63, 42, 42), circle, WithAlpha(cyan, 0.3f));
            Label(new Rect(card.x, card.y + 95, card.width, 69), values[i].ToString(), 42, i == 0 ? gold : ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Label(new Rect(card.x, card.y + 172, card.width, 27), i == 0 ? "첫 번째 씨앗" : "0 – 6", 12, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (i == 0) continue;
            if (Button(new Rect(card.x, area.y + 369, 45, 43), "−", false, null, 22)) puzzle.DoAction(i, -1);
            if (Button(new Rect(card.x + 55, area.y + 369, 45, 43), "+", false, null, 22)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 462, area.width, 31), "하나의 씨앗이 다음 씨앗의 형태를 결정합니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void WaterGates(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 12, area.width, 37), "모든 수문의 수위를 3으로 맞추세요.", 19, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        string[] names = { "I", "II", "III", "IV" };
        for (int i = 0; i < 4; i++)
        {
            float cx = area.x + 79 + i * 165;
            Label(new Rect(cx - 60, area.y + 72, 120, 33), names[i], 24, gold, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Rect tank = new Rect(cx - 52, area.y + 128, 104, 186);
            RectFill(tank, new Color(0.025f, 0.09f, 0.13f));
            Border(tank, WithAlpha(cyan, 0.4f));
            float fill = values[i] / 6f * (tank.height - 6);
            RectFill(new Rect(tank.x + 3, tank.yMax - fill - 3, tank.width - 6, fill), WithAlpha(cyan, 0.46f));
            Line(new Vector2(tank.x - 7, tank.center.y), new Vector2(tank.xMax + 7, tank.center.y), gold, 1);
            Label(new Rect(cx - 60, area.y + 328, 120, 41), values[i] + " / 3", 24, values[i] == 3 ? cyan : ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (Button(new Rect(cx - 65, area.y + 390, 59, 46), "←", false, null, 22)) puzzle.DoAction(i, -1);
            if (Button(new Rect(cx + 6, area.y + 390, 59, 46), "→", false, null, 22)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 470, area.width, 26), "물의 총량은 12     ·     IV 다음은 I", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void TidalRoute(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        int length = Mathf.Clamp(puzzle.Progress, 1, values.Length);
        Vector2[] nodes = new Vector2[9];
        float x = area.center.x - 136, y = area.y + 97;
        for (int i = 0; i < 9; i++) nodes[i] = new Vector2(x + (i % 3) * 136, y + (i / 3) * 136);
        for (int i = 0; i < 9; i++)
        {
            if (i % 3 < 2) Line(nodes[i], nodes[i + 1], WithAlpha(cyan, 0.12f), 1);
            if (i < 6) Line(nodes[i], nodes[i + 3], WithAlpha(cyan, 0.12f), 1);
        }
        for (int i = 1; i < length; i++)
            if (values[i - 1] >= 0 && values[i] >= 0) Line(nodes[values[i - 1]], nodes[values[i]], cyan, 4);
        for (int i = 0; i < 9; i++)
        {
            bool visited = false;
            for (int j = 0; j < length; j++) if (values[j] == i) visited = true;
            bool checkpoint = i == 2 || i == 4 || i == 8;
            Rect node = new Rect(nodes[i].x - 43, nodes[i].y - 43, 86, 86);
            Texture(node, disc, visited ? new Color(0.12f, 0.28f, 0.27f) : new Color(0.055f, 0.12f, 0.15f));
            Texture(node, circle, checkpoint ? gold : visited ? cyan : WithAlpha(muted, 0.5f));
            Label(node, (i + 1).ToString(), 28, checkpoint ? gold : ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (i == 0) Label(new Rect(node.x, node.y - 28, node.width, 23), "출발", 12, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (GUI.Button(node, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(area.x, area.y + 443, area.width, 30), "체크포인트  3 → 5 → 9     ·     항로  " + length + " / 7칸", 16, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Resonance(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        int total = 0, weighted = 0;
        for (int i = 0; i < 4; i++) { total += values[i]; weighted += values[i] * AetherFrontiers.FrequencyWeights[i]; }
        Label(new Rect(area.x, area.y + 10, area.width * 0.5f, 45), "진폭의 합  " + total + " / 6", 21, total == 6 ? cyan : ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.center.x, area.y + 10, area.width * 0.5f, 45), "가중합  " + weighted + " / 24", 21, weighted == 24 ? cyan : ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 4; i++)
        {
            float cx = area.x + 79 + i * 165;
            RectFill(new Rect(cx - 67, area.y + 91, 134, 250), new Color(0.06f, 0.14f, 0.17f));
            Label(new Rect(cx - 65, area.y + 111, 130, 31), "주파수 " + AetherFrontiers.FrequencyWeights[i], 16, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
            for (int n = 0; n < 3; n++)
            {
                float by = area.y + 266 - n * 35;
                RectFill(new Rect(cx - 37, by, 74, 24), WithAlpha(cyan, n < values[i] ? 0.64f : 0.075f));
            }
            Label(new Rect(cx - 60, area.y + 298, 120, 38), values[i].ToString(), 25, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (Button(new Rect(cx - 66, area.y + 369, 59, 46), "−", false, null, 23)) puzzle.DoAction(i, -1);
            if (Button(new Rect(cx + 7, area.y + 369, 59, 46), "+", false, null, 23)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 455, area.width, 40), "모든 진폭은 1 이상     ·     가중합 = 진폭 × 주파수의 합", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void CoupledGears(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        float cy = area.y + 213;
        Line(new Vector2(area.x + 109, cy), new Vector2(area.x + 541, cy), WithAlpha(cyan, 0.17f), 2);
        for (int i = 0; i < 3; i++)
        {
            Vector2 center = new Vector2(area.x + 110 + i * 215, cy);
            Label(new Rect(center.x - 80, area.y + 65, 160, 34), "GEAR " + (i + 1), 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            RingDial(center, 72, values[i], cyan);
            float target = (AetherFrontiers.GearTargets[i] * 45 - 90) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(target), Mathf.Sin(target));
            Line(center + direction * 80, center + direction * 95, gold, 3);
            Label(new Rect(center.x - 50, center.y - 34, 100, 68), values[i].ToString(), 40, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (Button(new Rect(center.x - 78, area.y + 364, 66, 53), "‹", false, null, 31)) puzzle.DoAction(i, -1);
            if (Button(new Rect(center.x + 12, area.y + 364, 66, 53), "›", false, null, 31)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 460, area.width, 34), "한 톱니는 시계 방향으로, 다음 톱니는 반대 방향으로.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void DawnColor(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        int[] target = AetherFrontiers.ColorTarget;
        Color mixed = new Color(values[0] / 4f, values[1] / 4f, values[2] / 4f);
        Color desired = new Color(target[0] / 4f, target[1] / 4f, target[2] / 4f);
        Rect current = new Rect(area.center.x - 232, area.y + 52, 206, 130);
        Rect reference = new Rect(area.center.x + 26, area.y + 52, 206, 130);
        RectFill(current, mixed); Border(current, WithAlpha(ivory, 0.35f));
        RectFill(reference, desired); Border(reference, WithAlpha(gold, 0.8f));
        Label(new Rect(current.x, area.y + 12, current.width, 29), "지금의 빛", 16, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(reference.x, area.y + 12, reference.width, 29), "기록된 새벽", 16, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Color[] channels = { new Color(0.94f, 0.40f, 0.37f), new Color(0.44f, 0.87f, 0.55f), new Color(0.42f, 0.62f, 0.96f) };
        string[] names = { "R  붉은빛", "G  초록빛", "B  푸른빛" };
        for (int i = 0; i < 3; i++)
        {
            float y = area.y + 224 + i * 79;
            Label(new Rect(area.x + 20, y, 152, 47), names[i], 17, channels[i], FontStyle.Normal, TextAnchor.MiddleLeft);
            for (int n = 0; n < 4; n++) RectFill(new Rect(area.x + 184 + n * 53, y + 16, 43, 17), WithAlpha(channels[i], n < values[i] ? 0.8f : 0.12f));
            Label(new Rect(area.x + 405, y, 55, 47), values[i].ToString(), 22, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(area.x + 475, y, 66, 47), "−", false, null, 23)) puzzle.DoAction(i, -1);
            if (Button(new Rect(area.x + 553, y, 66, 47), "+", false, null, 23)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x, area.y + 476, area.width, 26), "세 채널을 섞어 같은 색을 만드세요.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void WindLogic(AetherFrontiers puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        string[] rules = { "언제나 켜짐", "A와 반대", "A와 같음", "A와 C 모두 켜짐", "D와 반대", "D와 E 중 하나만 켜짐" };
        for (int i = 0; i < 6; i++)
        {
            Rect tile = new Rect(area.x + (i % 3) * 221, area.y + 35 + (i / 3) * 218, 209, 197);
            bool on = values[i] != 0;
            Color accent = on ? cyan : muted;
            RectFill(tile, on ? new Color(0.09f, 0.23f, 0.22f) : new Color(0.06f, 0.12f, 0.14f));
            Border(tile, WithAlpha(accent, on ? 0.57f : 0.23f));
            Label(new Rect(tile.x + 14, tile.y + 11, 55, 41), ((char)('A' + i)).ToString(), 27, on ? gold : muted, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Vector2 center = new Vector2(tile.center.x, tile.y + 80);
            for (int blade = 0; blade < 3; blade++)
            {
                float angle = (blade * 120 + (on ? 20 : 80)) * Mathf.Deg2Rad;
                Vector2 tip = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 27;
                Line(center, tip, accent, 3);
            }
            Texture(new Rect(center.x - 5, center.y - 5, 10, 10), disc, ivory);
            Label(new Rect(tile.x, tile.y + 118, tile.width, 26), on ? "켜짐" : "꺼짐", 17, accent, FontStyle.Normal, TextAnchor.MiddleCenter);
            Label(new Rect(tile.x + 8, tile.y + 159, tile.width - 16, 27), rules[i], 13, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (GUI.Button(tile, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(area.x, area.y + 479, area.width, 25), "모든 날개의 약속이 동시에 참이어야 합니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void HorizonPuzzle(AetherGame game, AetherHorizons puzzle)
    {
        Backdrop(0.68f);
        Rect box = new Rect(width * 0.5f - 620, height * 0.5f - 395, 1240, 790);
        Panel(box);
        int id = Mathf.Clamp(puzzle.ActivePuzzle, 0, 8);
        float left = box.x + 52;
        string[] regions = { "THE FROST CONSERVATORY", "THE STARLIT CAVERN", "THE DAWN SANCTUARY" };
        Label(new Rect(left, box.y + 41, 800, 28), regions[id / 3] + "   /   TRIAL  " + (id + 19).ToString("00"), 14, gold);
        if (TextButton(new Rect(box.xMax - 335, box.y + 35, 181, 39), "장치 초기화")) puzzle.Replay();
        if (TextButton(new Rect(box.xMax - 137, box.y + 35, 85, 39), "닫기  ×")) { puzzle.Close(); return; }
        Label(new Rect(left, box.y + 99, 1080, 60), puzzle.PuzzleTitle, 36, ivory);
        Line(new Vector2(left, box.y + 181), new Vector2(box.xMax - 52, box.y + 181), WithAlpha(ivory, 0.14f), 1);
        Label(new Rect(left, box.y + 224, 400, 32), "장치에 남겨진 기록", 16, gold);
        Label(new Rect(left, box.y + 274, 391, 165), puzzle.PuzzleDescription, 19, ivory);
        string[] instructions = {
            "금색 숫자는 고정된 단서입니다.\n빈칸을 눌러 1부터 4까지 채우세요.\n같은 행·열·작은 사각형에 중복은 없습니다.",
            "각 숫자는 연속으로 칠해야 할 칸의 수입니다.\n가로와 세로의 단서를 함께 읽고\n얼음 속의 무늬를 찾아 주세요.",
            "사공은 한 번에 하나만 함께 태웁니다.\n사공이 없는 강가에 여우와 새,\n또는 새와 씨앗을 함께 두면 안 됩니다.",
            "각 행에는 하나의 별이 있습니다.\n다른 칸을 눌러 별을 옮기세요.\n같은 세로줄과 대각선에 별이 겹치면 안 됩니다.",
            "일반적인 연산 우선순위를 쓰지 않습니다.\n왼쪽부터 차례대로 계산하세요.\n나눗셈은 나누어떨어져야 합니다.",
            "원반이 있는 기둥을 고른 뒤\n옮길 기둥을 선택하세요. 큰 원반을\n작은 원반 위에 놓을 수는 없습니다.",
            "5리터와 3리터 항아리를 채우고,\n비우고, 서로 옮겨 담으세요.\n큰 항아리에 정확히 4리터를 남겨야 합니다.",
            "등불이 있는 쪽에서 한 명 또는 두 명을\n선택해 건너세요. 걸리는 시간은\n함께 건너는 사람 중 느린 쪽과 같습니다.",
            "각 기록은 암호를 추측한 흔적입니다.\n정위치는 숫자와 자리가 모두 맞은 것,\n다른 자리는 숫자만 맞은 것입니다."
        };
        Label(new Rect(left, box.y + 452, 391, 111), instructions[id], 16, muted);
        if (Button(new Rect(left, box.y + 596, 351, 49), hintVisible ? "단서 다시 읽기" : "단서 떠올리기", false, "HINT"))
        {
            hintVisible = true;
            game.Notify(puzzle.GetHint(), 12);
        }
        Label(new Rect(left, box.y + 661, 391, 98), hintVisible ? puzzle.GetHint() : "한 번의 시도가 다음 발견의 단서가 됩니다.", 15, hintVisible ? gold : muted);
        Line(new Vector2(box.x + 485, box.y + 219), new Vector2(box.x + 485, box.y + 737), WithAlpha(ivory, 0.10f), 1);
        Rect area = new Rect(box.x + 537, box.y + 215, 651, 502);
        switch (id)
        {
            case 0: FrostSudoku(puzzle, area); break;
            case 1: IceNonogram(puzzle, area); break;
            case 2: RiverCrossing(puzzle, area); break;
            case 3: FourStars(puzzle, area); break;
            case 4: StarEquation(puzzle, area); break;
            case 5: LightTowers(puzzle, area); break;
            case 6: DawnJars(puzzle, area); break;
            case 7: DawnBridge(puzzle, area); break;
            case 8: ObserverCode(puzzle, area); break;
        }
        Label(new Rect(area.x, box.y + 739, area.width, 25), "R  장치 초기화     ESC  물러나기", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void FrostSudoku(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        float x = area.center.x - 177, y = area.y + 43;
        for (int i = 0; i < 16; i++)
        {
            bool given = AetherHorizons.SudokuGivens[i] != 0;
            Rect cell = new Rect(x + (i % 4) * 89, y + (i / 4) * 89, 84, 84);
            RectFill(cell, given ? new Color(0.16f, 0.17f, 0.13f) : new Color(0.07f, 0.17f, 0.20f));
            Border(cell, WithAlpha(given ? gold : cyan, 0.20f));
            if (puzzle.SelectedIndex == i) Border(new Rect(cell.x - 1, cell.y - 1, cell.width + 2, cell.height + 2), cyan);
            Label(cell, values[i] == 0 ? "·" : values[i].ToString(), values[i] == 0 ? 22 : 35, given ? gold : ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !given;
            if (GUI.Button(cell, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
            GUI.enabled = enabled;
        }
        Line(new Vector2(x + 175.5f, y - 5), new Vector2(x + 175.5f, y + 356), WithAlpha(gold, 0.65f), 2);
        Line(new Vector2(x - 5, y + 175.5f), new Vector2(x + 356, y + 175.5f), WithAlpha(gold, 0.65f), 2);
        Label(new Rect(area.x, area.y + 435, area.width, 35), "각 행 · 각 열 · 2×2 구역에 1부터 4까지 한 번씩", 16, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.x, area.y + 477, area.width, 23), "방향키  칸 선택     SPACE  숫자 바꾸기", 13, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void IceNonogram(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        float x = area.center.x - 115, y = area.y + 121;
        for (int row = 0; row < 4; row++)
            Label(new Rect(x - 95, y + row * 79, 75, 72), string.Join("  ", AetherHorizons.NonogramRows[row]), 23, gold, FontStyle.Normal, TextAnchor.MiddleRight, displayFont);
        for (int col = 0; col < 4; col++)
            Label(new Rect(x + col * 79, y - 84, 72, 67), string.Join("\n", AetherHorizons.NonogramColumns[col]), 23, gold, FontStyle.Normal, TextAnchor.LowerCenter, displayFont);
        for (int i = 0; i < 16; i++)
        {
            Rect cell = new Rect(x + (i % 4) * 79, y + (i / 4) * 79, 72, 72);
            bool on = values[i] != 0;
            RectFill(cell, on ? new Color(0.44f, 0.78f, 0.81f) : new Color(0.045f, 0.12f, 0.16f));
            Border(cell, WithAlpha(cyan, on ? 0.8f : 0.22f));
            if (puzzle.SelectedIndex == i) Border(new Rect(cell.x - 2, cell.y - 2, cell.width + 4, cell.height + 4), gold);
            if (on) Diamond(cell.center, 10, new Color(0.1f, 0.28f, 0.31f));
            if (GUI.Button(cell, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
        }
        Label(new Rect(area.x, area.y + 463, area.width, 32), "숫자는 이어진 칸의 길이     ·     칸을 눌러 칠하거나 지우기", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void RiverCrossing(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Rect left = new Rect(area.x + 18, area.y + 74, 246, 241);
        Rect right = new Rect(area.x + 387, area.y + 74, 246, 241);
        RectFill(left, new Color(0.09f, 0.17f, 0.17f)); RectFill(right, new Color(0.13f, 0.18f, 0.16f));
        Border(left, WithAlpha(cyan, 0.18f)); Border(right, WithAlpha(gold, 0.25f));
        Label(new Rect(left.x, area.y + 24, left.width, 32), "이쪽 강가", 18, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(right.x, area.y + 24, right.width, 32), "저쪽 강가", 18, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int wave = 0; wave < 7; wave++)
        {
            float sy = area.y + 83 + wave * 33;
            Line(new Vector2(area.center.x - 37, sy), new Vector2(area.center.x + 37, sy + 11), WithAlpha(cyan, 0.14f), 2);
        }
        string[] names = { "사공", "여우", "새", "씨앗" };
        for (int i = 0; i < 4; i++)
        {
            Rect bank = values[i] == 0 ? left : right;
            Rect token = new Rect(bank.x + 22, bank.y + 19 + i * 51, bank.width - 44, 37);
            RectFill(token, i == 0 ? WithAlpha(gold, 0.11f) : WithAlpha(cyan, 0.08f));
            Diamond(new Vector2(token.x + 18, token.center.y), 5, i == 0 ? gold : cyan, i == 0);
            Label(new Rect(token.x + 43, token.y, token.width - 54, token.height), names[i], 18, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
        }
        string[] actions = { "혼자 건너기", "여우와 건너기", "새와 건너기", "씨앗과 건너기" };
        for (int i = 0; i < 4; i++)
            if (Button(new Rect(area.x + 18 + (i % 2) * 315, area.y + 349 + (i / 2) * 58, 298, 47), actions[i], false, null, 17)) puzzle.DoAction(i);
        Label(new Rect(area.x, area.y + 474, area.width, 26), "건넌 횟수  " + values[4] + "     ·     모두 안전하게 저쪽 강가로", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void FourStars(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        float x = area.center.x - 180, y = area.y + 41;
        bool[] conflict = new bool[4];
        for (int i = 0; i < 4; i++)
        for (int j = i + 1; j < 4; j++)
            if (values[i] == values[j] || Mathf.Abs(values[i] - values[j]) == j - i) conflict[i] = conflict[j] = true;
        for (int row = 0; row < 4; row++)
        for (int col = 0; col < 4; col++)
        {
            Rect cell = new Rect(x + col * 91, y + row * 91, 87, 87);
            RectFill(cell, (row + col) % 2 == 0 ? new Color(0.11f, 0.15f, 0.23f) : new Color(0.075f, 0.11f, 0.18f));
            Border(cell, WithAlpha(cyan, 0.11f));
            if (values[row] == col) RuneIcon(cell.center, 4, 24, conflict[row] ? new Color(0.89f, 0.54f, 0.45f) : gold);
            if (GUI.Button(cell, GUIContent.none, buttonStyle))
            {
                int delta = col - values[row];
                if (delta != 0) puzzle.DoAction(row, delta);
            }
        }
        Label(new Rect(area.x, area.y + 450, area.width, 41), "같은 열과 대각선을 피하세요. 붉은 별은 서로 겹친 별입니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void StarEquation(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 23, area.width, 41), "왼쪽부터 차례로, 마지막은 28", 21, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        int[] numbers = { 6, 2, 3, 4 };
        for (int i = 0; i < 4; i++)
        {
            float x = area.x + 12 + i * 151;
            Label(new Rect(x, area.y + 132, 69, 90), numbers[i].ToString(), 42, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (i == 3) continue;
            Rect op = new Rect(x + 76, area.y + 142, 64, 70);
            if (Button(op, AetherHorizons.OperatorSymbols[values[i]], true, null, 31)) puzzle.DoAction(i);
            if (Button(new Rect(op.x, area.y + 246, 27, 39), "‹", false, null, 23)) puzzle.DoAction(i, -1);
            if (Button(new Rect(op.x + 37, area.y + 246, 27, 39), "›", false, null, 23)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x + 553, area.y + 138, 97, 82), "= 28", 28, gold, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
        int result;
        bool valid = AetherHorizons.EvaluateExpression(values, out result);
        Label(new Rect(area.x, area.y + 349, area.width, 53), valid ? "현재 결과  " + result : "나누어떨어지는 연산을 선택하세요.", valid ? 27 : 19, valid && result == 28 ? cyan : ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.x, area.y + 454, area.width, 41), "+  더하기     −  빼기     ×  곱하기     ÷  나누기", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void LightTowers(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 18, area.width, 38), "세 원반을 모두 세 번째 기둥으로", 20, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Color[] diskColors = { cyan, new Color(0.62f, 0.64f, 0.93f), gold };
        for (int pillar = 0; pillar < 3; pillar++)
        {
            float cx = area.x + 110 + pillar * 215;
            Rect hit = new Rect(cx - 96, area.y + 100, 192, 282);
            bool selected = puzzle.SelectedIndex == pillar;
            RectFill(hit, new Color(0.06f, 0.14f, 0.17f, selected ? 0.95f : 0.50f));
            if (selected) Border(hit, WithAlpha(cyan, 0.8f));
            Label(new Rect(cx - 84, area.y + 106, 168, 34), "기둥 " + (pillar + 1), 17, pillar == 2 ? gold : muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            Line(new Vector2(cx, area.y + 168), new Vector2(cx, area.y + 329), WithAlpha(ivory, 0.30f), 6);
            Line(new Vector2(cx - 80, area.y + 332), new Vector2(cx + 80, area.y + 332), gold, 3);
            int level = 0;
            for (int disk = 2; disk >= 0; disk--)
            {
                if (values[disk] != pillar) continue;
                float diskWidth = 61 + disk * 39;
                Rect rect = new Rect(cx - diskWidth * 0.5f, area.y + 305 - level * 30, diskWidth, 24);
                RectFill(rect, WithAlpha(diskColors[disk], 0.78f));
                Border(rect, diskColors[disk]);
                level++;
            }
            if (GUI.Button(hit, GUIContent.none, buttonStyle)) puzzle.DoAction(pillar);
        }
        Label(new Rect(area.x, area.y + 435, area.width, 52), puzzle.SelectedIndex < 0 ? "먼저 원반을 가져올 기둥을 선택하세요." : "원반을 옮길 기둥을 선택하세요.", 18, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void DawnJars(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        int[] capacities = { 5, 3 };
        for (int i = 0; i < 2; i++)
        {
            float cx = area.center.x - 163 + i * 326;
            Label(new Rect(cx - 130, area.y + 8, 260, 35), capacities[i] + "리터 항아리", 18, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
            Rect jar = new Rect(cx - 75, area.y + 73, 150, 185);
            RectFill(jar, new Color(0.05f, 0.105f, 0.14f));
            Border(jar, WithAlpha(cyan, 0.46f));
            float fill = values[i] / (float)capacities[i] * (jar.height - 6);
            RectFill(new Rect(jar.x + 3, jar.yMax - 3 - fill, jar.width - 6, fill), WithAlpha(cyan, 0.49f));
            if (i == 0) Line(new Vector2(jar.x - 9, jar.yMax - jar.height * 0.8f), new Vector2(jar.xMax + 9, jar.yMax - jar.height * 0.8f), gold, 2);
            Label(new Rect(cx - 100, area.y + 265, 200, 44), values[i] + " L", 27, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
        }
        string[] actions = { "5L 항아리 채우기", "3L 항아리 채우기", "5L 항아리 비우기", "3L 항아리 비우기", "5L에서 3L로 옮기기", "3L에서 5L로 옮기기" };
        for (int i = 0; i < 6; i++)
            if (Button(new Rect(area.x + 12 + (i % 2) * 326, area.y + 329 + (i / 2) * 53, 301, 43), actions[i], false, null, 16)) puzzle.DoAction(i);
        Label(new Rect(area.x, area.y + 480, area.width, 24), "금색 선은 큰 항아리의 목표, 4리터입니다.", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void DawnBridge(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x, area.y + 12, area.width, 40), "지나간 시간  " + values[5] + " / 17분", 23, values[5] <= 17 ? gold : new Color(0.9f, 0.5f, 0.4f), FontStyle.Normal, TextAnchor.MiddleCenter);
        Rect[] banks = { new Rect(area.x + 8, area.y + 94, 274, 236), new Rect(area.x + 369, area.y + 94, 274, 236) };
        Line(new Vector2(banks[0].xMax, area.y + 206), new Vector2(banks[1].x, area.y + 206), WithAlpha(gold, 0.30f), 12);
        for (int bank = 0; bank < 2; bank++)
        {
            RectFill(banks[bank], new Color(0.09f, 0.16f, 0.16f));
            Border(banks[bank], WithAlpha(bank == values[4] ? gold : cyan, 0.31f));
            Label(new Rect(banks[bank].x, area.y + 57, banks[bank].width, 28), bank == 0 ? "출발 지점" : "도착 지점", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (bank == values[4]) Diamond(new Vector2(banks[bank].xMax - 20, banks[bank].y + 17), 6, gold, true);
        }
        int selectedCount = 0;
        for (int i = 0; i < 4; i++)
        {
            bool selected = (values[6] & (1 << i)) != 0;
            if (selected) selectedCount++;
            Rect bank = banks[Mathf.Clamp(values[i], 0, 1)];
            Rect person = new Rect(bank.x + 17, bank.y + 33 + i * 43, bank.width - 34, 35);
            RectFill(person, WithAlpha(selected ? gold : cyan, selected ? 0.25f : 0.06f));
            Diamond(new Vector2(person.x + 17, person.center.y), 5, selected ? gold : muted, selected);
            Label(new Rect(person.x + 38, person.y, person.width - 53, person.height), "여행자 " + (i + 1) + "    " + AetherHorizons.CrossingTimes[i] + "분", 16, ivory, FontStyle.Normal, TextAnchor.MiddleLeft);
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && values[i] == values[4];
            if (GUI.Button(person, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
            GUI.enabled = enabled;
        }
        Label(new Rect(area.x, area.y + 349, area.width, 31), "선택한 여행자  " + selectedCount + " / 2", 16, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && selectedCount >= 1 && selectedCount <= 2;
        if (Button(new Rect(area.center.x - 191, area.y + 398, 382, 58), "선택한 이들과 건너기", true, null, 20)) puzzle.DoAction(4);
        GUI.enabled = wasEnabled;
        Label(new Rect(area.x, area.y + 477, area.width, 25), "등불이 있어야 건널 수 있습니다.", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void ObserverCode(AetherHorizons puzzle, Rect area)
    {
        int[] values = puzzle.Values;
        Label(new Rect(area.x + 14, area.y + 16, 286, 35), "네 번의 관측 기록", 18, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 4; i++)
        {
            Rect row = new Rect(area.x + 14, area.y + 79 + i * 91, 286, 77);
            RectFill(row, new Color(0.065f, 0.15f, 0.17f));
            Label(new Rect(row.x + 16, row.y + 5, 254, 35), string.Join("   ", AetherHorizons.CodeGuesses[i]), 26, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            Label(new Rect(row.x + 10, row.y + 45, 266, 24), "정위치 " + AetherHorizons.CodeExact[i] + "     다른 자리 " + AetherHorizons.CodeMisplaced[i], 13, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        }
        Label(new Rect(area.x + 329, area.y + 69, 308, 35), "세 자리 암호", 18, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        for (int i = 0; i < 3; i++)
        {
            Rect digit = new Rect(area.x + 330 + i * 106, area.y + 156, 96, 119);
            RectFill(digit, new Color(0.09f, 0.20f, 0.22f));
            Border(digit, WithAlpha(cyan, 0.39f));
            Label(digit, values[i].ToString(), 44, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
            if (GUI.Button(digit, GUIContent.none, buttonStyle)) puzzle.DoAction(i);
            if (Button(new Rect(digit.x, area.y + 299, 43, 43), "−", false, null, 21)) puzzle.DoAction(i, -1);
            if (Button(new Rect(digit.x + 53, area.y + 299, 43, 43), "+", false, null, 21)) puzzle.DoAction(i, 1);
        }
        Label(new Rect(area.x + 330, area.y + 376, 308, 67), "각 자리는 0부터 5까지.\n네 기록이 모두 맞는 암호를 찾으세요.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.x, area.y + 477, area.width, 25), "기록에 없는 가정 없이, 모든 단서를 함께 읽어 보세요.", 14, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private bool IsStationSolved(AetherGame game, int station)
    {
        if (station < 3) return game.Solved != null && game.Solved.Length > station && game.Solved[station];
        if (station >= 18)
        {
            AetherHorizons horizon = AetherHorizons.I;
            return horizon != null && horizon.Solved != null && horizon.Solved.Length > station - 18 && horizon.Solved[station - 18];
        }
        if (station >= 9)
        {
            AetherFrontiers frontier = AetherFrontiers.I;
            return frontier != null && frontier.Solved != null && frontier.Solved.Length > station - 9 && frontier.Solved[station - 9];
        }
        AetherExpansion expansion = AetherExpansion.I;
        return expansion != null && expansion.Solved != null && expansion.Solved.Length > station - 3 && expansion.Solved[station - 3];
    }

    public void SelectJournalEntry(int index)
    {
        int count = AetherGame.I != null ? AetherGame.I.TotalRecords : 39;
        journalSelection = Mathf.Clamp(index, 0, Mathf.Max(0, count - 1));
        journalPage = journalSelection / 8;
        journalScroll = Vector2.zero;
    }

    private void Journal(AetherGame game)
    {
        Backdrop(0.79f);
        Rect box = new Rect(width * 0.5f - 650, height * 0.5f - 414, 1300, 828);
        Panel(box);
        Label(new Rect(box.x + 52, box.y + 43, 700, 28), "FRAGMENTS OF A FORGOTTEN WORLD", 14, gold);
        Label(new Rect(box.x + 52, box.y + 94, 860, 55), "정원이 간직한 기억", 35, ivory);
        if (TextButton(new Rect(box.xMax - 150, box.y + 39, 98, 39), "닫기  ×")) game.ToggleJournal();
        Line(new Vector2(box.x + 52, box.y + 181), new Vector2(box.xMax - 52, box.y + 181), WithAlpha(ivory, 0.14f), 1);
        string[] entries = game.GetJournalEntries();
        int count = entries != null ? entries.Length : 0;
        journalSelection = Mathf.Clamp(journalSelection, 0, Mathf.Max(0, count - 1));
        const int recordsPerPage = 8;
        int pageCount = Mathf.Max(1, Mathf.CeilToInt(count / (float)recordsPerPage));
        journalPage = Mathf.Clamp(journalPage, 0, pageCount - 1);
        for (int rowIndex = 0; rowIndex < recordsPerPage; rowIndex++)
        {
            int i = journalPage * recordsPerPage + rowIndex;
            if (i >= count) break;
            Rect row = new Rect(box.x + 52, box.y + 218 + rowIndex * 54, 286, 44);
            bool selected = i == journalSelection;
            bool found = !string.IsNullOrEmpty(entries[i]) && !entries[i].Contains("미발견");
            if (selected) RectFill(row, new Color(0.10f, 0.22f, 0.24f));
            Diamond(new Vector2(row.x + 22, row.center.y), 4, found ? gold : muted, found);
            Label(new Rect(row.x + 45, row.y, 173, row.height), "기억  " + (i + 1).ToString("00"), 18, selected ? ivory : muted, FontStyle.Normal, TextAnchor.MiddleLeft);
            if (!found) Label(new Rect(row.xMax - 76, row.y, 66, row.height), "미발견", 12, muted, FontStyle.Normal, TextAnchor.MiddleRight);
            if (GUI.Button(row, GUIContent.none, buttonStyle)) { journalSelection = i; journalScroll = Vector2.zero; SoundClick(); }
        }
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && journalPage > 0;
        if (Button(new Rect(box.x + 52, box.y + 680, 65, 43), "‹", false, null, 25)) { journalPage--; journalSelection = journalPage * recordsPerPage; journalScroll = Vector2.zero; }
        GUI.enabled = enabled;
        Label(new Rect(box.x + 125, box.y + 680, 140, 43), (journalPage + 1) + " / " + pageCount, 17, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        GUI.enabled = enabled && journalPage + 1 < pageCount;
        if (Button(new Rect(box.x + 273, box.y + 680, 65, 43), "›", false, null, 25)) { journalPage++; journalSelection = journalPage * recordsPerPage; journalScroll = Vector2.zero; }
        GUI.enabled = enabled;
        Line(new Vector2(box.x + 374, box.y + 218), new Vector2(box.x + 374, box.y + 732), WithAlpha(ivory, 0.12f), 1);
        Rect page = new Rect(box.x + 426, box.y + 218, 804, 514);
        RectFill(page, new Color(0.07f, 0.14f, 0.15f, 0.50f));
        Label(new Rect(page.x + 39, page.y + 35, 720, 29), "ARCHIVE  /  " + (journalSelection + 1).ToString("00"), 13, gold);
        Label(new Rect(page.x + 37, page.y + 88, 120, 92), "“", 88, WithAlpha(cyan, 0.27f), FontStyle.Normal, TextAnchor.UpperLeft, displayFont);
        string entry = count > 0 ? entries[journalSelection] : "아직 발견한 기억이 없습니다. 정원에 남겨진 조각을 찾아 주세요.";
        Rect readingArea = new Rect(page.x + 47, page.y + 167, page.width - 94, 286);
        textStyle.font = bodyFont;
        textStyle.fontSize = 24;
        textStyle.wordWrap = true;
        float contentHeight = Mathf.Max(readingArea.height, textStyle.CalcHeight(new GUIContent(entry), readingArea.width - 25) + 18);
        journalScroll = GUI.BeginScrollView(readingArea, journalScroll, new Rect(0, 0, readingArea.width - 25, contentHeight), false, contentHeight > readingArea.height);
        Label(new Rect(0, 0, readingArea.width - 25, contentHeight), entry, 24, ivory);
        GUI.EndScrollView();
        Label(new Rect(box.x + 52, box.y + 760, 500, 28), "발견한 기억  " + game.TotalRelics + " / " + game.TotalRecords, 15, muted);
        Label(new Rect(box.xMax - 500, box.y + 760, 448, 28), "J / ESC  기록 닫기", 14, muted, FontStyle.Normal, TextAnchor.MiddleRight);
    }

    public void SelectMapRegion(int index)
    {
        mapSelection = Mathf.Clamp(index, 0, 8);
        mapSelectionExplicit = true;
    }

    private void Map(AetherGame game)
    {
        Backdrop(0.79f);
        Rect box = new Rect(width * 0.5f - 750, height * 0.5f - 465, 1500, 930);
        Panel(box);
        Label(new Rect(box.x + 52, box.y + 38, 800, 28), "ATLAS OF THE FLOATING GARDEN", 14, gold);
        Label(new Rect(box.x + 52, box.y + 86, 800, 55), "빛으로 이어진 섬들", 34, ivory);
        if (TextButton(new Rect(box.xMax - 150, box.y + 39, 98, 39), "닫기  ×")) game.ToggleMap();
        Label(new Rect(box.xMax - 534, box.y + 98, 482, 31), "현재 위치  /  " + game.CurrentRegion, 16, muted, FontStyle.Normal, TextAnchor.MiddleRight);
        Rect map = new Rect(box.x + 52, box.y + 174, 986, 648);
        for (int x = -60; x <= 60; x += 10) Line(MapPoint(map, x, -119), MapPoint(map, x, 46), WithAlpha(cyan, 0.045f), 1);
        for (int z = -110; z <= 40; z += 10) Line(MapPoint(map, -64, z), MapPoint(map, 64, z), WithAlpha(cyan, 0.045f), 1);
        MapBridge(map, new Vector2(-28, -3), new Vector2(-18, -3));
        MapBridge(map, new Vector2(18, -3), new Vector2(28, -3));
        MapBridge(map, new Vector2(0, 22), new Vector2(0, 28));
        MapBridge(map, new Vector2(-43, -31), new Vector2(-43, -11));
        MapBridge(map, new Vector2(0, -31), new Vector2(0, -16));
        MapBridge(map, new Vector2(43, -31), new Vector2(43, -11));
        MapBridge(map, new Vector2(-43, -83), new Vector2(-43, -63));
        MapBridge(map, new Vector2(0, -83), new Vector2(0, -63));
        MapBridge(map, new Vector2(43, -83), new Vector2(43, -63));
        MapIsland(map, -18, 18, -16, 22, "빛의 정원", cyan, true, 0);
        MapIsland(map, -58, -28, -11, 21, "달빛 기록보관소", new Color(0.60f, 0.65f, 0.94f), true, 1);
        MapIsland(map, 28, 58, -11, 21, "태양 관측소", gold, true, 2);
        MapIsland(map, -7, 7, 28, 42, "새벽의 문", gold, false);
        MapIsland(map, -58, -28, -63, -31, "비의 수목원", new Color(0.47f, 0.85f, 0.65f), true, 3);
        MapIsland(map, -15, 15, -63, -31, "조수의 회랑", new Color(0.38f, 0.71f, 0.94f), true, 4);
        MapIsland(map, 28, 58, -63, -31, "바람의 공방", new Color(0.91f, 0.62f, 0.47f), true, 5);
        MapIsland(map, -58, -28, -115, -83, "서리의 온실", new Color(0.66f, 0.88f, 0.96f), true, 6);
        MapIsland(map, -15, 15, -115, -83, "별빛의 동굴", new Color(0.77f, 0.65f, 0.95f), true, 7);
        MapIsland(map, 28, 58, -115, -83, "새벽의 성소", new Color(0.96f, 0.78f, 0.56f), true, 8);
        Vector3[] stations = new Vector3[27];
        stations[0] = new Vector3(-10, 0, 1); stations[1] = new Vector3(10, 0, 3); stations[2] = new Vector3(0, 0, 16);
        System.Array.Copy(AetherExpansion.Positions, 0, stations, 3, 6);
        System.Array.Copy(AetherFrontiers.Positions, 0, stations, 9, 9);
        System.Array.Copy(AetherHorizons.Positions, 0, stations, 18, 9);
        for (int i = 0; i < stations.Length; i++)
        {
            Vector2 p = MapPoint(map, stations[i].x, stations[i].z);
            bool solved = IsStationSolved(game, i);
            Diamond(p, 8, solved ? gold : cyan, solved);
            Label(new Rect(p.x + 14, p.y - 12, 45, 24), (i + 1).ToString("00"), 12, solved ? gold : muted);
        }
        for (int i = 0; i < AetherGame.WaystonePositions.Length; i++)
        {
            if (game.WaystonesFound == null || i >= game.WaystonesFound.Length || !game.WaystonesFound[i]) continue;
            Vector3 position = AetherGame.WaystonePositions[i];
            Vector2 point = MapPoint(map, position.x, position.z);
            Texture(new Rect(point.x - 8, point.y - 8, 16, 16), circle, cyan);
            Texture(new Rect(point.x - 3, point.y - 3, 6, 6), disc, cyan);
        }
        if (game.Player != null)
        {
            Vector3 player = game.Player.transform.position;
            Vector2 p = MapPoint(map, player.x, player.z);
            float yaw = game.Player.transform.eulerAngles.y * Mathf.Deg2Rad;
            Vector2 direction = (MapPoint(map, player.x + Mathf.Sin(yaw), player.z + Mathf.Cos(yaw)) - p).normalized;
            Texture(new Rect(p.x - 15, p.y - 15, 30, 30), circle, WithAlpha(ivory, 0.5f));
            Texture(new Rect(p.x - 5, p.y - 5, 10, 10), disc, ivory);
            Line(p + direction * 8, p + direction * 25, ivory, 2);
        }
        Vector2 north = new Vector2(map.x + 30, map.y + 23);
        Label(new Rect(north.x - 20, north.y - 32, 40, 29), "N", 17, gold, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
        Line(north + new Vector2(0, 4), north + new Vector2(0, 33), gold, 1);
        Diamond(north, 5, gold, true);
        Rect travel = new Rect(box.x + 1074, box.y + 180, 374, 644);
        if (WaystonePanel(game, travel)) return;
        Line(new Vector2(box.x + 52, box.y + 866), new Vector2(box.xMax - 52, box.y + 866), WithAlpha(ivory, 0.12f), 1);
        Diamond(new Vector2(box.x + 63, box.y + 901), 6, cyan);
        Label(new Rect(box.x + 84, box.y + 887, 158, 27), "잠든 장치", 14, muted);
        Diamond(new Vector2(box.x + 237, box.y + 901), 6, gold, true);
        Label(new Rect(box.x + 258, box.y + 887, 170, 27), "깨어난 장치", 14, muted);
        Texture(new Rect(box.x + 425, box.y + 896, 10, 10), disc, ivory);
        Label(new Rect(box.x + 450, box.y + 887, 135, 27), "나의 위치", 14, muted);
        Label(new Rect(box.x + 598, box.y + 887, 390, 27), "발견한 이동 비석  " + game.VisitedWaystones + " / 9", 14, cyan);
        Label(new Rect(box.xMax - 402, box.y + 887, 350, 27), "M / ESC  지도 닫기", 14, muted, FontStyle.Normal, TextAnchor.MiddleRight);
    }

    private bool WaystonePanel(AetherGame game, Rect area)
    {
        mapSelection = Mathf.Clamp(mapSelection, 0, 8);
        bool found = game.WaystonesFound != null && game.WaystonesFound.Length > mapSelection && game.WaystonesFound[mapSelection];
        RectFill(area, new Color(0.06f, 0.135f, 0.15f, 0.75f));
        Border(area, WithAlpha(cyan, 0.14f));
        float x = area.x + 24, w = area.width - 48;
        Label(new Rect(x, area.y + 24, w, 26), "WAYSTONES  /  " + (mapSelection + 1).ToString("00"), 13, gold);
        Label(new Rect(x, area.y + 65, w, 52), AetherGame.WaystoneNames[mapSelection], 25, ivory);
        Label(new Rect(x, area.y + 133, w, 31), "깨어난 성소     " + game.RegionPuzzleCount(mapSelection) + " / 3", 17, cyan);
        Label(new Rect(x, area.y + 174, w, 31), "기억의 조각     " + game.RegionRecordCount(mapSelection) + " / " + game.RegionRecordTotal(mapSelection), 17, muted);
        Line(new Vector2(x, area.y + 227), new Vector2(x + w, area.y + 227), WithAlpha(ivory, 0.12f), 1);
        Label(new Rect(x, area.y + 250, w, 48), found ? "발견한 비석의 빛이\n돌아오는 길을 기억합니다." : "이 지역의 비석을 가까이에서\n발견하면 빠르게 돌아올 수 있습니다.", 15, muted);
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && found;
        bool travel = Button(new Rect(x, area.y + 327, w, 59), found ? "이 지역으로 이동" : "비석 미발견", found, null, 19);
        GUI.enabled = enabled;
        if (travel && game.TravelToWaystone(mapSelection)) return true;
        Label(new Rect(x, area.y + 414, w, 25), "지역 선택", 14, gold);
        string[] shortNames = { "빛의 정원", "달빛 서고", "태양 관측소", "비의 수목원", "조수의 회랑", "바람의 공방", "서리의 온실", "별빛의 동굴", "새벽의 성소" };
        for (int i = 0; i < 9; i++)
        {
            Rect button = new Rect(x + (i % 3) * 112, area.y + 455 + (i / 3) * 53, 102, 44);
            if (Button(button, shortNames[i], i == mapSelection, null, 12)) mapSelection = i;
        }
        return false;
    }

    private Vector2 MapPoint(Rect map, float x, float z)
    {
        return new Vector2(map.x + (x + 65) / 130f * map.width, map.y + (46 - z) / 167f * map.height);
    }

    private void MapBridge(Rect map, Vector2 from, Vector2 to)
    {
        Line(MapPoint(map, from.x, from.y), MapPoint(map, to.x, to.y), new Color(0.13f, 0.24f, 0.24f), 22);
        Line(MapPoint(map, from.x, from.y), MapPoint(map, to.x, to.y), WithAlpha(gold, 0.18f), 2);
    }

    private void MapIsland(Rect map, float x1, float x2, float z1, float z2, string title, Color accent, bool titleBelow = true, int region = -1)
    {
        Vector2 topLeft = MapPoint(map, x1, z2);
        Vector2 bottomRight = MapPoint(map, x2, z1);
        Rect rect = new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
        RectFill(rect, new Color(accent.r * 0.10f, accent.g * 0.10f, accent.b * 0.10f, 1));
        Border(rect, WithAlpha(accent, region == mapSelection ? 0.95f : 0.33f));
        Border(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), WithAlpha(accent, 0.07f));
        float ty = titleBelow ? rect.yMax + 7 : rect.y + 10;
        Label(new Rect(rect.center.x - 150, ty, 300, 27), title, 13, accent, FontStyle.Normal, TextAnchor.MiddleCenter);
        if (region >= 0 && GUI.Button(rect, GUIContent.none, buttonStyle)) { mapSelection = region; SoundClick(); }
    }

    private void Toast(AetherGame game)
    {
        if (string.IsNullOrEmpty(game.Toast) || Time.unscaledTime >= game.ToastUntil) return;
        float alpha = Mathf.Clamp01((game.ToastUntil - Time.unscaledTime) / 0.45f);
        if (game.Phase == GamePhase.Paused || game.JournalOpen || game.MapOpen) return;
        float y = game.ActivePuzzle >= 0 || game.HasOpenSidePuzzle ? height - 87 : height - 320;
        float toastWidth = Mathf.Min(1050, width - 140);
        Rect box = new Rect((width - toastWidth) * 0.5f, y, toastWidth, 72);
        RectFill(box, new Color(0.02f, 0.075f, 0.085f, 0.93f * alpha));
        RectFill(new Rect(box.x, box.y, 3, box.height), WithAlpha(gold, alpha));
        Label(new Rect(box.x + 30, box.y + 11, box.width - 60, 50), game.Toast, 19, WithAlpha(ivory, alpha), FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Completion(AetherGame game)
    {
        Backdrop(0.73f);
        float cx = width * 0.5f;
        float top = height * 0.14f;
        Emblem(new Vector2(cx, top + 43), 39, gold);
        Label(new Rect(cx - 500, top + 119, 1000, 32), "THE GARDEN REMEMBERS", 17, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(cx - 700, top + 181, 1400, 79), game.EndingTitle, 48, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(cx - 610, top + 290, 1220, 82), game.EndingDescription, 22, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(cx - 400, top + 379, 800, 25), game.JourneyRank, 14, gold, FontStyle.Normal, TextAnchor.MiddleCenter);
        float statsY = top + 443;
        Line(new Vector2(cx - 391, statsY - 35), new Vector2(cx + 391, statsY - 35), WithAlpha(ivory, 0.16f), 1);
        Stat(new Rect(cx - 413, statsY, 250, 111), "깨어난 성소", game.TotalSolved + " / " + game.TotalPuzzles);
        Stat(new Rect(cx - 125, statsY, 250, 111), "기억의 조각", game.TotalRelics + " / " + game.TotalRecords);
        int seconds = Mathf.Max(0, Mathf.RoundToInt(game.PlaySeconds));
        Stat(new Rect(cx + 163, statsY, 250, 111), "정원에서 보낸 시간", (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00"));
        if (Button(new Rect(cx - 285, top + 634, 570, 66), "처음 화면으로", true, "RETURN TO AETHER")) game.ReturnToMenu();
        Label(new Rect(cx - 400, height - 82, 800, 30), "여정의 끝에는, 언제나 또 다른 시작이 있습니다.", 15, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Stat(Rect area, string label, string value)
    {
        Label(new Rect(area.x, area.y, area.width, 32), label, 16, muted, FontStyle.Normal, TextAnchor.MiddleCenter);
        Label(new Rect(area.x, area.y + 52, area.width, 52), value, 33, ivory, FontStyle.Normal, TextAnchor.MiddleCenter, displayFont);
    }

    private float Slider(Rect rect, float value)
    {
        // A wide invisible native slider preserves keyboard and drag behaviour.
        GUIStyle trackStyle = new GUIStyle(GUI.skin.horizontalSlider);
        GUIStyle thumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
        trackStyle.normal.background = null;
        trackStyle.hover.background = null;
        trackStyle.active.background = null;
        trackStyle.fixedHeight = 24;
        thumbStyle.normal.background = null;
        thumbStyle.hover.background = null;
        thumbStyle.active.background = null;
        thumbStyle.fixedWidth = 24;
        thumbStyle.fixedHeight = 24;
        float result = GUI.HorizontalSlider(rect, value, 0f, 1f, trackStyle, thumbStyle);
        float cx = rect.x + 12 + (rect.width - 24) * result;
        RectFill(new Rect(rect.x + 12, rect.center.y - 2, rect.width - 24, 4), WithAlpha(ivory, 0.16f));
        RectFill(new Rect(rect.x + 12, rect.center.y - 2, cx - rect.x - 12, 4), cyan);
        Texture(new Rect(cx - 10, rect.center.y - 10, 20, 20), disc, ivory);
        return result;
    }

    private bool Toggle(Rect rect, bool value)
    {
        RectFill(rect, value ? WithAlpha(cyan, 0.2f) : WithAlpha(ivory, 0.08f));
        Border(rect, value ? WithAlpha(cyan, 0.6f) : WithAlpha(ivory, 0.18f));
        RectFill(new Rect(value ? rect.xMax - 29 : rect.x + 7, rect.y + 7, 22, 21), value ? cyan : muted);
        bool clicked = GUI.Button(rect, GUIContent.none, buttonStyle);
        if (clicked) SoundClick();
        return clicked;
    }

    private void Backdrop(float opacity = 0.77f)
    {
        RectFill(new Rect(0, 0, width, height), new Color(0.012f, 0.031f, 0.04f, opacity));
    }

    private void Panel(Rect rect)
    {
        RectFill(new Rect(rect.x + 12, rect.y + 19, rect.width, rect.height), new Color(0, 0, 0, 0.18f));
        RectFill(rect, deep);
        Border(rect, WithAlpha(cyan, 0.17f));
        Line(new Vector2(rect.x + 1, rect.y), new Vector2(rect.x + 131, rect.y), gold, 2);
        Line(new Vector2(rect.xMax - 130, rect.yMax), new Vector2(rect.xMax, rect.yMax), WithAlpha(cyan, 0.5f), 2);
    }

    private bool Button(Rect rect, string label, bool primary, string sub = null, int fontSize = 20)
    {
        bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
        Color fill = primary ? (hover ? new Color(0.69f, 0.94f, 0.86f) : cyan) : (hover ? new Color(0.10f, 0.24f, 0.26f, 0.92f) : new Color(0.09f, 0.17f, 0.19f, 0.75f));
        Color foreground = primary ? new Color(0.02f, 0.10f, 0.12f) : ivory;
        RectFill(rect, fill);
        if (!primary) Border(rect, WithAlpha(hover ? cyan : ivory, hover ? 0.6f : 0.19f));
        if (string.IsNullOrEmpty(sub)) Label(rect, label, fontSize, foreground, FontStyle.Normal, TextAnchor.MiddleCenter);
        else
        {
            Label(new Rect(rect.x + 24, rect.y, rect.width * 0.63f, rect.height), label, fontSize, foreground, FontStyle.Normal, TextAnchor.MiddleLeft);
            Label(new Rect(rect.x + rect.width * 0.51f, rect.y, rect.width * 0.49f - 24, rect.height), sub, 10, primary ? WithAlpha(foreground, 0.70f) : muted, FontStyle.Normal, TextAnchor.MiddleRight);
        }
        bool clicked = GUI.Button(rect, GUIContent.none, buttonStyle);
        if (clicked) SoundClick();
        return clicked;
    }

    private bool TextButton(Rect rect, string label)
    {
        bool hover = GUI.enabled && rect.Contains(Event.current.mousePosition);
        Label(rect, label, 17, hover ? cyan : muted, FontStyle.Normal, TextAnchor.MiddleLeft);
        bool clicked = GUI.Button(rect, GUIContent.none, buttonStyle);
        if (clicked) SoundClick();
        return clicked;
    }

    private void Keycap(Rect rect, string text, int size = 17)
    {
        RectFill(rect, new Color(0.025f, 0.07f, 0.075f, 0.64f));
        Border(rect, WithAlpha(ivory, 0.26f));
        Label(rect, text, size, ivory, FontStyle.Normal, TextAnchor.MiddleCenter);
    }

    private void Label(Rect rect, string text, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft, Font font = null)
    {
        textStyle.font = font != null ? font : bodyFont;
        textStyle.fontSize = size;
        textStyle.fontStyle = style;
        textStyle.alignment = alignment;
        textStyle.normal.textColor = color;
        textStyle.wordWrap = true;
        textStyle.clipping = TextClipping.Clip;
        GUI.Label(rect, text ?? string.Empty, textStyle);
    }

    private void SpacedText(Vector2 origin, string text, int size, float spacing, Color color, Font font)
    {
        float x = origin.x;
        for (int i = 0; i < text.Length; i++)
        {
            string letter = text[i].ToString();
            Label(new Rect(x, origin.y, size * 1.15f, size * 1.45f), letter, size, color, FontStyle.Normal, TextAnchor.UpperLeft, font);
            x += textStyle.CalcSize(new GUIContent(letter)).x + spacing;
        }
    }

    private void Emblem(Vector2 center, float radius, Color color)
    {
        Diamond(center, radius, color);
        Diamond(center, radius * 0.53f, WithAlpha(color, 0.55f));
        Line(new Vector2(center.x, center.y - radius * 1.36f), new Vector2(center.x, center.y + radius * 1.36f), WithAlpha(color, 0.72f), 1);
        Texture(new Rect(center.x - radius * 0.18f, center.y - radius * 0.18f, radius * 0.36f, radius * 0.36f), disc, color);
    }

    private void Diamond(Vector2 center, float radius, Color color, bool filled = false)
    {
        if (filled)
        {
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.TRS(new Vector3(center.x, center.y, 0f), Quaternion.Euler(0f, 0f, 45f), Vector3.one);
            float side = radius * 1.41421356f;
            RectFill(new Rect(-side * 0.5f, -side * 0.5f, side, side), color);
            GUI.matrix = previous;
            return;
        }
        Vector2 top = center + Vector2.up * radius;
        Vector2 right = center + Vector2.right * radius;
        Vector2 bottom = center + Vector2.down * radius;
        Vector2 left = center + Vector2.left * radius;
        Line(top, right, color, 1.2f);
        Line(right, bottom, color, 1.2f);
        Line(bottom, left, color, 1.2f);
        Line(left, top, color, 1.2f);
    }

    private void Border(Rect rect, Color color)
    {
        RectFill(new Rect(rect.x, rect.y, rect.width, 1), color);
        RectFill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
        RectFill(new Rect(rect.x, rect.y, 1, rect.height), color);
        RectFill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
    }

    private void Line(Vector2 from, Vector2 to, Color color, float thickness)
    {
        Matrix4x4 previous = GUI.matrix;
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
        // Compose in reference-screen coordinates before applying the outer resolution scale.
        GUI.matrix = previous * Matrix4x4.TRS(new Vector3(from.x, from.y, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
        RectFill(new Rect(0f, -thickness * 0.5f, Vector2.Distance(from, to), thickness), color);
        GUI.matrix = previous;
    }

    private void RectFill(Rect rect, Color color)
    {
        Texture(rect, white, color);
    }

    private static void Texture(Rect rect, Texture2D texture, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
        GUI.color = previous;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void SoundClick()
    {
        if (AetherAudio.I != null) AetherAudio.I.Click();
    }

    private void OnDestroy()
    {
        if (circle != null) Destroy(circle);
        if (disc != null) Destroy(disc);
        if (shade != null) Destroy(shade);
        if (hudShade != null) Destroy(hudShade);
        if (vignette != null) Destroy(vignette);
        if (bodyFont != null) Destroy(bodyFont);
        if (displayFont != null) Destroy(displayFont);
    }
}

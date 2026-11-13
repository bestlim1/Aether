using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class AetherGame
{
    public int TotalPuzzles => 27;
    public int TotalSolved => SolvedCount + (AetherExpansion.I != null ? AetherExpansion.I.SolvedCount : 0) + (AetherFrontiers.I != null ? AetherFrontiers.I.SolvedCount : 0) + (AetherHorizons.I != null ? AetherHorizons.I.SolvedCount : 0);
    public int TotalRelics { get { int n = RelicCount; foreach (bool b in ExtraRelicFound) if (b) n++; foreach(bool b in FrontierRecords) if(b)n++; foreach(bool b in HorizonRecords) if(b)n++; return n; } }
    public bool JournalOpen, MapOpen, PhotoMode;
    public float Sensitivity = 2.1f;
    public int QualityLevel = 2;
    public bool[] ExtraRelicFound = new bool[6];
    public string CurrentRegion => Player == null ? "잊힌 빛의 정원" : RegionAt(Player.transform.position);
    public string EndingTitle => TotalRelics == TotalRecords ? "아홉 섬의 아침" : "지평선 너머, 아침";
    public string EndingDescription => TotalRelics == TotalRecords
        ? "당신은 27개의 빛과 39개의 기억을 되찾았습니다.\n지평선의 정원사가 되어, 아홉 섬에 당신의 이야기를 심어 주세요."
        : "27개의 빛이 지평선에 새로운 길을 그립니다.\n아홉 섬은 깨어났습니다. 남겨진 기억은 다음 발걸음을 기다립니다.";
    public string JourneyRank => TotalRelics == TotalRecords ? "지평선의 정원사" : TotalRelics >= 24 ? "별빛의 기록자" : TotalRelics >= 12 ? "기억의 수호자" : "새벽을 연 여행자";
    readonly Transform[] extraRelics = new Transform[6];
    static readonly Vector3[] ExtraRelicPositions = {
        new Vector3(-54,1.1f,-6), new Vector3(-31,1.1f,14), new Vector3(-53,1.1f,17),
        new Vector3(53,1.1f,-7), new Vector3(31,1.1f,15), new Vector3(54,1.1f,18)
    };
    static readonly string[] ExtraMemories = {
        "04 · 남겨진 자리\n관측자 세린은 마지막 배에 오르지 않았다. 책장 사이에 남아, 돌아올 이들을 위한 길을 기록했다. 달빛은 잉크처럼 고요히 번졌다.",
        "05 · 기억의 배열\n우리는 같은 풍경을 보고도 다른 것을 기억한다. 부서진 기억을 다시 놓을 때, 빈칸까지도 하나의 자리라는 것을 잊지 말 것.",
        "06 · 마지막 메아리\n여섯 음은 구조 요청이 아니었다. 세린이 매일 정원에게 건넨 인사였다. 오늘도, 여기, 누군가가 있다고.",
        "07 · 태양의 무게\n빛에 무게가 있다면, 기다림의 무게와 같을까. 관측자 노엘은 저울 위에 세 개의 돌을 올리고는, 다시 해를 바라보았다.",
        "08 · 두 관측자\n달은 기억을 보관하고 태양은 내일을 계산했다. 서로 다른 일을 했지만, 두 사람의 기록은 언제나 같은 문장으로 끝났다. 돌아오는 길을 비워 둘 것.",
        "09 · 정원의 약속\n이 글을 읽는 당신에게. 정원은 우리를 위해 남겨 둔 기념비가 아니다. 당신이 다시 이름을 붙이고, 새로운 이야기를 심을 수 있는 빈 정원이다."
    };
    Camera photoCamera;
    Vector2 photoAngles;
    string lastRegion = "";
    int nearbyExtraRelic = -1;

    void InitializeExpansion()
    {
        Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("Aether.Sensitivity", 2.1f), .5f, 5);
        QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt("Aether.Quality", 2), 0, 2);
        gameObject.AddComponent<AetherExpansion>();
        for (int i = 0; i < extraRelics.Length; i++)
        {
            Vector3 p = ExtraRelicPositions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder, "Archive fragment pedestal " + (i + 4), new Vector3(p.x,.22f,p.z), new Vector3(.8f,.22f,.8f),stone);
            var shard = AetherWorld.Shape(PrimitiveType.Cube, "Record " + (i + 4), p, new Vector3(.22f,.46f,.22f),gold,null,false);
            shard.transform.rotation=Quaternion.Euler(0,45,25); extraRelics[i]=shard.transform;
            animated.Add(shard.transform); animatedBases.Add(p);
        }
        InitializeFrontiers();
        InitializeHorizons();
        photoCamera = new GameObject("AETHER · Photo camera").AddComponent<Camera>();
        photoCamera.CopyFrom(Player.View); photoCamera.enabled=false;
        ApplyQuality();
    }

    bool UpdateExpansionInput()
    {
        if (Input.GetKeyDown(KeyCode.F12)) StartCoroutine(CapturePhoto());
        if (ActivePuzzle < 0 && !HasOpenSidePuzzle)
        {
            if (Input.GetKeyDown(KeyCode.J)) ToggleJournal();
            if (Input.GetKeyDown(KeyCode.M)) ToggleMap();
            if (Input.GetKeyDown(KeyCode.F) && !JournalOpen && !MapOpen) TogglePhotoMode();
        }
        if (PhotoMode)
        {
            if (Cursor.lockState == CursorLockMode.Locked && !smokeMode)
            {
                photoAngles.x += Input.GetAxisRaw("Mouse X") * Sensitivity;
                photoAngles.y = Mathf.Clamp(photoAngles.y - Input.GetAxisRaw("Mouse Y") * Sensitivity,-85,85);
                photoCamera.transform.rotation=Quaternion.Euler(photoAngles.y,photoAngles.x,0);
                float rise=(Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0);
                Vector3 move=photoCamera.transform.forward*Input.GetAxisRaw("Vertical")+photoCamera.transform.right*Input.GetAxisRaw("Horizontal")+Vector3.up*rise;
                photoCamera.transform.position+=move*Time.deltaTime*(Input.GetKey(KeyCode.LeftShift)?14:6);
                photoCamera.fieldOfView=Mathf.Clamp(photoCamera.fieldOfView-Input.mouseScrollDelta.y*3,25,95);
            }
            return true;
        }
        if (JournalOpen || MapOpen || HasOpenSidePuzzle) return true;
        if (CurrentRegion != lastRegion)
        {
            lastRegion=CurrentRegion;
            if (PlaySeconds>3) Notify("✦  " + lastRegion,4);
        }
        return false;
    }
    public void ToggleJournal()
    {
        if (Phase!=GamePhase.Playing || ActivePuzzle>=0 || HasOpenSidePuzzle || PhotoMode) return;
        JournalOpen=!JournalOpen; MapOpen=false; RefreshCursor();
    }
    public void ToggleMap()
    {
        if (Phase!=GamePhase.Playing || ActivePuzzle>=0 || HasOpenSidePuzzle || PhotoMode) return;
        MapOpen=!MapOpen; JournalOpen=false; RefreshCursor();
    }
    public void TogglePhotoMode()
    {
        if (!PhotoMode && (Phase!=GamePhase.Playing || ActivePuzzle>=0 || HasOpenSidePuzzle || JournalOpen || MapOpen)) return;
        PhotoMode=!PhotoMode;
        if (PhotoMode)
        {
            photoCamera.transform.SetPositionAndRotation(Player.View.transform.position,Player.View.transform.rotation);
            photoCamera.fieldOfView=Player.View.fieldOfView;
            photoAngles=new Vector2(Player.Yaw,Player.Pitch);
        }
        Player.View.enabled=!PhotoMode; photoCamera.enabled=PhotoMode; RefreshCursor();
    }
    public void SetSensitivity(float value)
    {
        Sensitivity=Mathf.Clamp(value,.5f,5);
        if (!smokeMode) { PlayerPrefs.SetFloat("Aether.Sensitivity",Sensitivity); PlayerPrefs.Save(); }
    }
    public void SetQualityLevel(int value)
    {
        QualityLevel=Mathf.Clamp(value,0,2); ApplyQuality();
        if (!smokeMode) { PlayerPrefs.SetInt("Aether.Quality",QualityLevel); PlayerPrefs.Save(); }
    }
    void ApplyQuality()
    {
        QualitySettings.antiAliasing=QualityLevel==0?0:QualityLevel==1?2:4;
        QualitySettings.shadowDistance=QualityLevel==0?35:QualityLevel==1?65:100;
        QualitySettings.shadowResolution=QualityLevel==2?ShadowResolution.High:ShadowResolution.Medium;
        QualitySettings.pixelLightCount=QualityLevel==0?2:QualityLevel==1?4:8;
        foreach (var finish in FindObjectsOfType<AetherAtmosphericFinish>()) finish.enabled=QualityLevel>0;
    }
    IEnumerator CapturePhoto()
    {
        string folder=Path.Combine(Application.persistentDataPath,"Photos");
        Directory.CreateDirectory(folder);
        yield return new WaitForEndOfFrame();
        string path=Path.Combine(folder,"AETHER-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png");
        ScreenCapture.CaptureScreenshot(path);
        Notify("사진을 저장했습니다. " + folder,6);
    }
    void UpdateExtraInteraction()
    {
        nearbyExtraRelic=-1;
        if (nearby>=0) return;
        float closest=3.2f;
        for(int i=0;i<6;i++)
        {
            if(ExtraRelicFound[i]) continue;
            float d=Vector3.Distance(Player.transform.position+Vector3.up,ExtraRelicPositions[i]);
            float dot=Vector3.Dot(Player.View.transform.forward,(ExtraRelicPositions[i]-Player.View.transform.position).normalized);
            if(d<closest&&dot>-.05f) { closest=d; nearbyExtraRelic=i; }
        }
        Prompt=nearbyExtraRelic>=0?"남겨진 기록 " +(nearbyExtraRelic+4).ToString("00")+" 읽기":AetherExpansion.I.NearbyPrompt;
    }
    bool CollectNearbyExtraRelic()
    {
        if(nearbyExtraRelic<0) return false;
        int id=nearbyExtraRelic; ExtraRelicFound[id]=true; extraRelics[id].gameObject.SetActive(false);
        AetherAudio.I?.Chime(id%4); Notify(ExtraMemories[id].Split('\n')[0]+" · J 일지에서 이야기를 읽어 보세요.",7); SaveExpansion();
        if(TotalRelics==TotalRecords) Notify("모든 기록을 되찾았습니다. 마지막 문에서 정원의 진실이 이어집니다.",8);
        return true;
    }
    public string[] GetJournalEntries()
    {
        var entries=new string[TotalRecords];
        for(int i=0;i<3;i++) entries[i]=RelicFound[i]?Memories[i]:"기록 "+(i+1).ToString("00")+" · 미발견\n중앙 정원의 길과 가장자리를 살펴보세요.";
        for(int i=0;i<6;i++) entries[i+3]=ExtraRelicFound[i]?ExtraMemories[i]:"기록 "+(i+4).ToString("00")+" · 미발견\n"+(i<3?"달빛 기록보관소":"태양 관측소")+"의 조용한 자리에서 기다리고 있습니다.";
        for(int i=0;i<15;i++)entries[i+9]=FrontierRecords[i]?FrontierMemories[i]:"기록 "+(i+10).ToString("00")+" · 미발견\n"+RegionAt(FrontierRecordPositions[i])+"의 산책길과 가장자리를 살펴보세요.";
        for(int i=0;i<15;i++)entries[i+24]=HorizonRecords[i]?HorizonMemories[i]:"기록 "+(i+25).ToString("00")+" · 미발견\n"+RegionAt(HorizonRecordPositions[i])+"의 갈림길과 조용한 구석을 살펴보세요.";
        return entries;
    }
    public void OnExpansionSolved()
    {
        Save();
        if(TotalSolved==TotalPuzzles) Notify("27개의 빛이 깨어났습니다. 북쪽 새벽의 문으로 향하세요.",9);
    }
    void ResetExpansion(bool resume)
    {
        AetherExpansion.I.ResetJourney(resume);
        ExtraRelicFound=new bool[6];
        if(resume&&AetherSaveStore.HasKey("Aether.ExtraRecords.v1"))
        {
            string bits=AetherSaveStore.GetString("Aether.ExtraRecords.v1");
            if(bits.Length==6) for(int i=0;i<6;i++) ExtraRelicFound[i]=bits[i]=='1';
        }
        for(int i=0;i<6;i++) extraRelics[i].gameObject.SetActive(!ExtraRelicFound[i]);
        ResetFrontiers(resume);
        ResetHorizons(resume);
    }
    void SaveExpansion()
    {
        AetherExpansion.I.Save();
        string bits="";foreach(bool b in ExtraRelicFound) bits+=b?"1":"0";
        AetherSaveStore.SetString("Aether.ExtraRecords.v1",bits);
        SaveFrontiers();
        SaveHorizons();
    }

    IEnumerator SmokeExpansion(List<string> checks,string dir)
    {
        var expansion=AetherExpansion.I;
        foreach(string check in AetherExpansion.RunSelfTests()) checks.Add(check);
        Player.Teleport(new Vector3(-23,.15f,-3),270,0);
        yield return new WaitForSeconds(.7f);
        Check(Player.Body.isGrounded&&Player.transform.position.y>-.1f,"western bridge collision",checks);
        Player.Teleport(new Vector3(-43,.15f,-8),0,-10);
        yield return new WaitForSeconds(.7f);
        Check(Player.Body.isGrounded&&Player.transform.position.y>-.1f,"archive island collision",checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"06-moon-archive.png"));
        yield return new WaitForSeconds(.5f);
        for(int id=0;id<6;id++)
        {
            Player.Teleport(AetherExpansion.Positions[id]+new Vector3(0,.15f,-2.7f),0,8);
            yield return new WaitForSeconds(.5f);
            expansion.TryInteract();
            Check(expansion.ActivePuzzle==id&&!Player.CanMove,"open expansion puzzle "+id,checks);
            if(id==0)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,"07-constellation-puzzle.png"));
                yield return new WaitForSeconds(.4f);
                int[] moves=FindSlidingSolution(expansion.Values);
                Check(moves!=null,"sliding board has solution",checks);
                if(moves!=null)foreach(int move in moves)expansion.DoAction(move);
            }
            else if(id==1) { expansion.DoAction(1);expansion.DoAction(3); }
            else if(id==2)
            {
                yield return new WaitForSeconds(5.8f);
                expansion.DoAction(3);Check(expansion.Progress==0,"long echo wrong note resets",checks);
                foreach(int note in new[]{0,3,1,2,0,2})expansion.DoAction(note);
            }
            else if(id==3) { expansion.DoAction(0,3);expansion.DoAction(1,1);expansion.DoAction(2,4); }
            else if(id==4)
            {
                int[] goal={4,1,3,0,2};
                for(int i=0;i<5&&expansion.IsOpen;i++)
                {
                    int from=Array.IndexOf(expansion.Values,goal[i]);
                    if(from!=i) { expansion.DoAction(i);expansion.DoAction(from); }
                }
            }
            else { int[] goal={1,3,2,0};for(int i=0;i<4&&expansion.IsOpen;i++)expansion.DoAction(i,goal[i]-expansion.Values[i]); }
            Check(expansion.Solved[id],"expansion puzzle "+id+" solved",checks);
        }
        Player.Teleport(new Vector3(23,.15f,-3),90,0);yield return new WaitForSeconds(.7f);
        Check(Player.Body.isGrounded&&Player.transform.position.y>-.1f,"eastern bridge collision",checks);
        Player.Teleport(new Vector3(43,.15f,-8),0,-15);yield return new WaitForSeconds(.7f);
        Check(Player.Body.isGrounded&&Player.transform.position.y>-.1f,"observatory island collision",checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"08-solar-observatory.png"));yield return new WaitForSeconds(.4f);
        for(int i=0;i<6;i++)
        {
            nearbyExtraRelic=i;CollectNearbyExtraRelic();
        }
        Check(TotalRelics==9,"nine records collected",checks);
        ToggleJournal();Check(!Player.CanMove&&JournalOpen,"journal movement lock",checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"09-journal.png"));yield return new WaitForSeconds(.4f);ToggleJournal();
        ToggleMap();Check(!Player.CanMove&&MapOpen,"atlas movement lock",checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"10-atlas.png"));yield return new WaitForSeconds(.4f);ToggleMap();
        TogglePhotoMode();Check(PhotoMode&&!Player.CanMove&&photoCamera.enabled,"photo mode camera",checks);TogglePhotoMode();
        Interact(6);
        Check(TotalSolved==9 && Phase==GamePhase.Playing,"original nine lights do not bypass expanded final gate",checks);
    }
    static int[] FindSlidingSolution(int[] start)
    {
        string initial=string.Concat(start);const string target="123456780";
        var queue=new Queue<string>();var previous=new Dictionary<string,string>();var actions=new Dictionary<string,int>();
        queue.Enqueue(initial);previous[initial]=null;
        while(queue.Count>0)
        {
            string state=queue.Dequeue();
            if(state==target)
            {
                var result=new List<int>();while(previous[state]!=null){result.Add(actions[state]);state=previous[state];}result.Reverse();return result.ToArray();
            }
            int blank=state.IndexOf('0');
            for(int cell=0;cell<9;cell++)
            {
                if(Mathf.Abs(blank/3-cell/3)+Mathf.Abs(blank%3-cell%3)!=1)continue;
                char[] chars=state.ToCharArray();chars[blank]=chars[cell];chars[cell]='0';string next=new string(chars);
                if(previous.ContainsKey(next))continue;previous[next]=state;actions[next]=cell;queue.Enqueue(next);
            }
        }
        return null;
    }
}

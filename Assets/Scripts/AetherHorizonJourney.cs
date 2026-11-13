using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class AetherGame
{
    public bool[] HorizonRecords = new bool[15];
    public bool[] WaystonesFound = new bool[9];
    public int VisitedWaystones { get { int n=0; foreach(bool b in WaystonesFound) if(b)n++; return n; } }
    public static readonly string[] WaystoneNames = {
        "잊힌 빛의 정원", "달빛 기록보관소", "태양 관측소", "비의 수목원", "조수의 회랑", "바람의 공방", "서리의 온실", "별빛의 동굴", "새벽의 성소"
    };
    public static readonly Vector3[] WaystonePositions = {
        new Vector3(0,0,-8),new Vector3(-43,0,-6),new Vector3(43,0,-6),
        new Vector3(-43,0,-56),new Vector3(0,0,-56),new Vector3(43,0,-56),
        new Vector3(-43,0,-108),new Vector3(0,0,-108),new Vector3(43,0,-108)
    };
    static readonly Vector3[] HorizonRecordPositions = {
        new Vector3(-55,1.1f,-109),new Vector3(-31,1.1f,-109),new Vector3(-54,1.1f,-97),new Vector3(-31,1.1f,-88),new Vector3(-55,1.1f,-86),
        new Vector3(-12,1.1f,-110),new Vector3(12,1.1f,-110),new Vector3(-12,1.1f,-95),new Vector3(12,1.1f,-86),new Vector3(-9,1.1f,-86),
        new Vector3(31,1.1f,-110),new Vector3(55,1.1f,-109),new Vector3(55,1.1f,-94),new Vector3(31,1.1f,-87),new Vector3(55,1.1f,-86)
    };
    static readonly string[] HorizonMemories = {
        "25 · 겨울을 보관하는 집\n식물학자 미라는 추위를 몰아내려고 온실을 짓지 않았다. 겨울잠을 자야만 꽃을 피우는 씨앗들이 있었다. 유리 지붕 아래, 그녀는 봄을 서두르지 않는 법을 배웠다.",
        "26 · 다른 모양의 눈\n아이들은 눈송이가 모두 다르다는 말을 믿지 않았다. 미라는 검은 천을 펼쳐 눈을 받았다. 금세 녹아 사라지는 것들을 들여다보느라, 아이들은 누구의 것이 더 아름다운지 묻지 않았다.",
        "27 · 얼음 밑의 흐름\n연못은 하얗게 얼었지만 바닥의 물은 흐르고 있었다. 미라는 얼음 위에 귀를 대고, 아주 낮은 소리를 기록했다. 멈춰 보인다는 이유만으로 멈췄다고 적어서는 안 되었다.",
        "28 · 작은 배의 자리\n떠나는 날, 미라는 씨앗 상자를 한 번에 모두 옮길 수 없었다. 바람에 약한 것은 안쪽에, 습기를 좋아하는 것은 물가에 두었다. 돌봄이란 때로 올바른 순서로 여러 번 돌아오는 일이었다.",
        "29 · 봄에게 남긴 쪽지\n아직 싹트지 않은 화분마다 이름표를 꽂았다. 죽은 것이 아니라 기다리는 것이라고. 미라는 마지막 이름표를 빈 화분에 꽂았다. 누구든 돌아와 자신이 가져온 씨앗을 심을 수 있도록.",
        "30 · 낮에도 남아 있는 별\n천문학자 린은 밝은 날에도 별이 사라지지 않는다고 말했다. 사람들은 보이지 않는 것을 어떻게 믿느냐고 물었다. 린은 어둠을 기다리는 대신, 어두운 동굴 안에 작은 하늘을 만들었다.",
        "31 · 빈 자리의 별자리\n린의 첫 별자리 지도에는 점 하나가 빠져 있었다. 스승은 틀린 지도라고 했지만 린은 지우지 않았다. 나중에 돌아올 누군가가 그 자리에 자신의 별을 발견할지도 몰랐다.",
        "32 · 돌이 내는 대답\n수정에 빛을 비추자 벽 너머까지 가느다란 선이 이어졌다. 린은 질문을 한쪽 벽에, 답을 반대편에 적었다. 사이를 걸어가는 시간 동안 질문이 달라지는 날도 있었다.",
        "33 · 먼 빛의 나이\n우리가 오늘 보는 빛은 오래전에 출발했다고 린은 적었다. 세린은 그 문장 옆에 답을 남겼다. 그렇다면 누군가의 오래된 친절도 아직 우리에게 오는 중일 수 있겠군요.",
        "34 · 세 겹의 밤\n가장 작은 수정부터 큰 수정 위로 옮기며 관측 장치를 접었다. 린은 서두르다가 한 번 무너뜨렸고, 다시 처음부터 쌓았다. 밤은 충분히 길었다. 다시 시작할 만큼의 어둠이 남아 있었다.",
        "35 · 아침을 맡은 사람\n성소의 관리인 온은 해를 뜨게 할 수 없었다. 대신 해가 뜰 때 사람들이 쉴 자리를 닦고 물을 채웠다. 아무도 그의 이름을 몰라도, 누구든 아침을 맞을 수 있으면 되었다.",
        "36 · 정확한 한 잔\n가뭄이 들자 온은 크기가 다른 두 항아리로 물을 나누었다. 넘치는 물도 버리지 않고 다시 모았다. 공평함은 모두에게 같은 그릇을 주는 일이 아니라, 필요한 만큼 담아 주는 일이었다.",
        "37 · 함께 건너는 속도\n바람이 거세던 저녁, 마지막 네 사람이 작은 등불 하나를 들고 다리를 건넜다. 가장 빠른 사람은 혼자 달리지 않았다. 돌아가 등불을 건네는 길까지가 그의 몫이었다.",
        "38 · 문에 적힌 숫자\n성소의 문에는 암호가 있었지만 비밀은 없었다. 필요한 단서는 모두 문 옆에 남겨 두었다. 온은 답을 아는 사람보다, 차분히 읽고 생각하는 사람이 이곳을 발견하기를 바랐다.",
        "39 · 당신이 돌아올 곳\n겨울을 지킨 미라, 어둠을 읽은 린, 아침을 준비한 온. 아홉 섬의 길이 다시 이어지자 비석마다 불이 들어왔다. 이 정원의 마지막 약속은 멀리 가라는 말이 아니었다. 언제든 돌아올 곳이 있다는 말이었다."
    };
    const string HorizonRecordKey="Aether.HorizonRecords.v1", WaystoneKey="Aether.Waystones.v1";
    readonly Transform[] horizonShards=new Transform[15];
    readonly Renderer[] waystoneLights=new Renderer[9];
    int nearbyHorizonRecord=-1;

    void InitializeHorizons()
    {
        gameObject.AddComponent<AetherHorizons>();
        for(int i=0;i<15;i++)
        {
            Vector3 p=HorizonRecordPositions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder,"Horizon archive pedestal "+(i+25),new Vector3(p.x,.22f,p.z),new Vector3(.8f,.22f,.8f),stone);
            var shard=AetherWorld.Shape(PrimitiveType.Cube,"Record "+(i+25),p,new Vector3(.22f,.46f,.22f),gold,null,false);
            shard.transform.rotation=Quaternion.Euler(0,45,25);horizonShards[i]=shard.transform;
            animated.Add(shard.transform);animatedBases.Add(p);
        }
        for(int i=0;i<9;i++)
        {
            Vector3 p=WaystonePositions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder,"Waystone footing "+i,p+Vector3.up*.06f,new Vector3(1.8f,.06f,1.8f),dark,null,false);
            AetherWorld.Shape(PrimitiveType.Cube,"Waystone pillar "+i,p+Vector3.up*.58f,new Vector3(.52f,1.1f,.35f),stone,null,false);
            var crystal=AetherWorld.Shape(PrimitiveType.Cube,"Waystone beacon "+i,p+Vector3.up*1.4f,new Vector3(.32f,.52f,.32f),inactive,null,false);
            crystal.transform.rotation=Quaternion.Euler(0,45,20);waystoneLights[i]=crystal.GetComponent<Renderer>();
            animated.Add(crystal.transform);animatedBases.Add(p+Vector3.up*1.4f);
            var circle=new Vector3[49];for(int k=0;k<circle.Length;k++){float a=k*Mathf.PI/24;circle[k]=p+new Vector3(Mathf.Cos(a)*1.12f,.04f,Mathf.Sin(a)*1.12f);}
            Line("Waystone compass circle "+i,circle,cyan,.02f);
        }
    }
    void ResetHorizons(bool resume)
    {
        AetherHorizons.I.ResetJourney(resume);
        HorizonRecords=ReadRecordBits(HorizonRecordKey,15,resume);
        for(int i=0;i<15;i++)horizonShards[i].gameObject.SetActive(!HorizonRecords[i]);
        nearbyHorizonRecord=-1;
    }
    static bool[] ReadRecordBits(string key,int length,bool resume)
    {
        var result=new bool[length];if(!resume)return result;
        string bits=AetherSaveStore.GetString(key);
        if(bits.Length!=length)return result;
        for(int i=0;i<length;i++)if(bits[i]!='0'&&bits[i]!='1')return result;
        for(int i=0;i<length;i++)result[i]=bits[i]=='1';return result;
    }
    static void WriteRecordBits(string key,bool[] flags)
    {
        char[] chars=new char[flags.Length];for(int i=0;i<flags.Length;i++)chars[i]=flags[i]?'1':'0';
        AetherSaveStore.SetString(key,new string(chars));
    }
    void SaveHorizons()
    {
        AetherHorizons.I.Save();WriteRecordBits(HorizonRecordKey,HorizonRecords);WriteRecordBits(WaystoneKey,WaystonesFound);
    }
    void ResetWaystones(bool resume)
    {
        WaystonesFound=ReadRecordBits(WaystoneKey,9,resume);
        // Existing completed work proves an old region was visited. This also repairs a missing waystone save.
        if(resume)for(int i=0;i<9;i++)if(RegionPuzzleCount(i)>0||RegionRecordCount(i)>0)WaystonesFound[i]=true;
        WaystonesFound[0]=true;
        RefreshWaystones();
    }
    void RefreshWaystones()
    {
        for(int i=0;i<9;i++)if(waystoneLights[i]!=null)waystoneLights[i].sharedMaterial=WaystonesFound[i]?cyan:inactive;
    }
    void DiscoverNearbyWaystones()
    {
        if(Phase!=GamePhase.Playing||Player==null||!Player.Body.isGrounded||HasOpenSidePuzzle||ActivePuzzle>=0||MapOpen||JournalOpen||PhotoMode)return;
        for(int i=0;i<9;i++)
        {
            if(WaystonesFound[i]||Vector3.Distance(Player.transform.position,WaystonePositions[i])>4.5f)continue;
            WaystonesFound[i]=true;RefreshWaystones();Save();AetherAudio.I?.Chime(i%4);
            Notify(WaystoneNames[i]+" · 이동 비석 발견 ("+VisitedWaystones+" / 9)\nM 지도에서 이곳으로 돌아올 수 있습니다.",7);
        }
    }
    public bool IsWaystoneUnlocked(int index)=>index>=0&&index<9&&WaystonesFound[index];
    public bool TravelToWaystone(int index)
    {
        if(!IsWaystoneUnlocked(index)||Phase!=GamePhase.Playing||!MapOpen||Player==null||ActivePuzzle>=0||HasOpenSidePuzzle||JournalOpen||PhotoMode)return false;
        Vector3 destination=WaystonePositions[index]+new Vector3(0,.16f,-2.1f);
        if(!ValidPosition(destination,SolvedCount==3))return false;
        MapOpen=false;LastSafePosition=destination;Player.Teleport(destination,0,0);RefreshCursor();Save();
        AetherAudio.I?.Chime(index%4);Notify(WaystoneNames[index]+" · 비석의 빛을 따라 돌아왔습니다.",4);return true;
    }
    public int RegionPuzzleCount(int region)
    {
        if(region<0||region>8)return 0;
        bool[] flags=region==0?Solved:region<3?AetherExpansion.I?.Solved:region<6?AetherFrontiers.I?.Solved:AetherHorizons.I?.Solved;
        int start=region==0?0:region<3?(region-1)*3:region<6?(region-3)*3:(region-6)*3;
        int n=0;if(flags!=null)for(int i=start;i<start+3;i++)if(flags[i])n++;return n;
    }
    public int RegionRecordTotal(int region)=>region>=0&&region<3?3:region>=3&&region<9?5:0;
    public int RegionRecordCount(int region)
    {
        if(region<0||region>8)return 0;
        bool[] flags=region==0?RelicFound:region<3?ExtraRelicFound:region<6?FrontierRecords:HorizonRecords;
        int start=region==0?0:region<3?(region-1)*3:region<6?(region-3)*5:(region-6)*5;
        int n=0;for(int i=start;i<start+RegionRecordTotal(region);i++)if(flags[i])n++;return n;
    }
    void UpdateHorizonInteraction()
    {
        nearbyHorizonRecord=-1;if(nearby>=0||nearbyExtraRelic>=0||nearbyFrontierRecord>=0)return;
        float closest=3.2f;
        for(int i=0;i<15;i++)
        {
            if(HorizonRecords[i])continue;Vector3 p=HorizonRecordPositions[i];
            float distance=Vector3.Distance(Player.transform.position+Vector3.up,p);
            float facing=Vector3.Dot(Player.View.transform.forward,(p-Player.View.transform.position).normalized);
            if(distance<closest&&facing>-.05f){closest=distance;nearbyHorizonRecord=i;}
        }
        if(nearbyHorizonRecord>=0)Prompt="남겨진 기록 "+(nearbyHorizonRecord+25).ToString("00")+" 읽기";
        else if(!string.IsNullOrEmpty(AetherHorizons.I.NearbyPrompt))Prompt=AetherHorizons.I.NearbyPrompt;
    }
    bool CollectNearbyHorizonRecord()
    {
        if(nearbyHorizonRecord<0)return false;int id=nearbyHorizonRecord;
        HorizonRecords[id]=true;horizonShards[id].gameObject.SetActive(false);AetherAudio.I?.Chime(id%4);Save();
        Notify(TotalRelics==TotalRecords?"39개의 기억을 모두 되찾았습니다. 마지막 문에서 아홉 섬의 아침을 맞이하세요.":HorizonMemories[id].Split('\n')[0]+" · J 일지에서 이야기를 읽어 보세요.",7);
        nearbyHorizonRecord=-1;return true;
    }
    public void OnHorizonSolved(int index)
    {
        Save();if(TotalSolved==TotalPuzzles)Notify("27개의 빛이 깨어났습니다. M 지도에서 중앙 정원으로 돌아가 북쪽 새벽의 문을 여세요.",10);
    }

    IEnumerator SmokeHorizonCrossings(List<string> checks)
    {
        foreach(float x in new[]{-43f,0f,43f})
        {
            Player.Teleport(new Vector3(x,.15f,-56),180,0);yield return new WaitForSeconds(.3f);
            for(int step=0;step<160&&Player.transform.position.z>-84.5f;step++){Player.Body.Move(Vector3.back*.22f);yield return null;}
            Check(Player.transform.position.z<-84&&Mathf.Abs(Player.transform.position.x-x)<1&&Player.transform.position.y>-.15f,"walk entire horizon bridge at x="+x,checks);
        }
    }
    IEnumerator SmokeHorizonRecordsAndTravel(List<string> checks,string dir)
    {
        // Locked travel cannot move the player, even with the atlas open.
        WaystonesFound[8]=false;ToggleMap();Vector3 before=Player.transform.position;
        Check(!TravelToWaystone(8)&&MapOpen&&(Player.transform.position-before).sqrMagnitude<.01f,"undiscovered waystone rejects travel",checks);ToggleMap();
        for(int i=0;i<9;i++)
        {
            Player.Teleport(WaystonePositions[i]+new Vector3(0,.15f,-2.1f),0,0);yield return new WaitForSeconds(.35f);DiscoverNearbyWaystones();
            Check(WaystonesFound[i]&&Player.Body.isGrounded,"waystone "+i+" can be reached and discovered",checks);
        }
        Check(VisitedWaystones==9,"all nine waystones discovered",checks);
        for(int i=0;i<9;i++)
        {
            ToggleMap();Check(TravelToWaystone(i)&&!MapOpen&&Player.CanMove,"atlas travel to region "+i,checks);
            yield return new WaitForSeconds(.25f);
            Check(Player.Body.isGrounded&&Vector3.Distance(Player.transform.position,WaystonePositions[i])<3,"waystone "+i+" arrival has safe ground",checks);
        }
        // Returning to a southern island survives a complete save/reload.
        Save();Begin(true);yield return new WaitForSeconds(.3f);
        Check(VisitedWaystones==9&&Player.transform.position.z<-100&&Player.Body.isGrounded,"horizon position and waystones survive resume",checks);
        for(int i=0;i<15;i++)
        {
            Player.Teleport(HorizonRecordPositions[i]+new Vector3(0,-.95f,-1.6f),0,0);yield return new WaitForSeconds(.22f);
            UpdateInteraction();UpdateExtraInteraction();UpdateFrontierInteraction();UpdateHorizonInteraction();
            Check(Player.Body.isGrounded&&nearbyHorizonRecord==i&&CollectNearbyHorizonRecord(),"horizon record "+(i+25)+" reachable and collected",checks);
        }
        Check(TotalRelics==39&&GetJournalEntries().Length==39,"all thirty-nine records collected",checks);
        for(int i=0;i<9;i++)Check(RegionPuzzleCount(i)==3&&RegionRecordCount(i)==RegionRecordTotal(i),"region "+i+" completion counters",checks);
        ToggleJournal();GetComponent<AetherUI>().SelectJournalEntry(38);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"29-horizon-journal.png"));yield return new WaitForSeconds(.45f);ToggleJournal();
        ToggleMap();GetComponent<AetherUI>().SelectMapRegion(8);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"30-nine-region-atlas.png"));yield return new WaitForSeconds(.45f);
        Pause();Check(!TravelToWaystone(0),"paused state rejects fast travel",checks);Resume();
        TogglePhotoMode();photoCamera.transform.position=new Vector3(85,114,-160);photoCamera.transform.LookAt(new Vector3(0,0,-39));photoCamera.fieldOfView=66;
        var ui=GetComponent<AetherUI>();ui.enabled=false;yield return new WaitForSeconds(.6f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"31-nine-islands-panorama.png"));yield return new WaitForSeconds(.45f);ui.enabled=true;TogglePhotoMode();
        Check(Phase==GamePhase.Playing&&!HasOpenSidePuzzle&&!PhotoMode,"expanded journey returns to exploration",checks);
    }
}

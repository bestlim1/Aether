using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class AetherGame
{
    public int TotalRecords => 39;
    public bool HasOpenSidePuzzle => (AetherExpansion.I != null && AetherExpansion.I.IsOpen) || (AetherFrontiers.I != null && AetherFrontiers.I.IsOpen) || (AetherHorizons.I != null && AetherHorizons.I.IsOpen);
    public bool[] FrontierRecords = new bool[15];
    readonly Transform[] frontierShards = new Transform[15];
    int nearbyFrontierRecord = -1;
    const string FrontierRecordKey = "Aether.FrontierRecords.v1";
    static readonly Vector3[] FrontierRecordPositions = {
        new Vector3(-55,1.1f,-57),new Vector3(-31,1.1f,-57),new Vector3(-54,1.1f,-45),new Vector3(-31,1.1f,-36),new Vector3(-55,1.1f,-34),
        new Vector3(-12,1.1f,-58),new Vector3(12,1.1f,-58),new Vector3(-12,1.1f,-43),new Vector3(12,1.1f,-34),new Vector3(-9,1.1f,-34),
        new Vector3(31,1.1f,-58),new Vector3(55,1.1f,-57),new Vector3(55,1.1f,-42),new Vector3(31,1.1f,-35),new Vector3(55,1.1f,-34)
    };
    static readonly string[] FrontierMemories = {
        "10 · 비를 심는 사람\n정원사 이안은 구름을 물통에 담을 수 없다는 것을 알았다. 대신 잎이 빗방울을 받아 서로에게 건네도록 온실을 설계했다. 가장 작은 잎에도 해야 할 일이 있었다.",
        "11 · 뿌리의 편지\n온실 아래의 뿌리는 서로 다른 나무를 연결했다. 한 그루가 햇빛을 받으면 그늘 속 나무에게도 양분이 흘렀다. 이안은 그것을 정원이 쓰는 편지라고 불렀다.",
        "12 · 마른 계절\n마지막 비가 내린 뒤 백 일을 셌다. 사람들은 저수조의 물을 아끼자고 했다. 이안은 묘목 하나에 물을 주었다. 모두가 떠나도 이곳에 계절이 남기를 바라며.",
        "13 · 씨앗의 계산\n씨앗의 수를 세는 일은 내일을 세는 일이었다. 하나가 둘이 되고, 둘이 넷이 되는 것을 보며 아이들은 덧셈보다 먼저 기다리는 법을 배웠다.",
        "14 · 돌아온 비\n온실 유리에 처음 물방울이 맺힌 날, 이안은 기록장에 아무것도 쓰지 않았다. 빗소리를 듣느라 손을 멈추었기 때문이다. 빈 페이지에도 기쁜 날이 있다.",
        "15 · 물이 기억하는 길\n항해사 루아는 배가 떠난 뒤에도 수문을 열었다. 물은 가장 낮은 곳을 찾아 흐르지만, 사람들이 낸 길을 오래 기억한다. 회랑은 강을 위한 작은 지도였다.",
        "16 · 서로 다른 시계\n태양의 시계와 조수의 시계는 같은 속도로 걷지 않았다. 노엘은 그것을 고장이라 불렀고, 루아는 대화라 불렀다. 두 사람이 함께 만든 표에는 두 종류의 시간이 적혔다.",
        "17 · 이름 없는 항구\n이곳을 지나간 배의 이름을 모두 벽에 새길 수는 없었다. 그래서 루아는 항구를 비워 두었다. 아직 오지 않은 배들이 자신의 이름을 가져올 수 있도록.",
        "18 · 깊은 울림\n회랑 아래에서 들려오는 낮은 소리는 바다의 목소리가 아니었다. 물이 빈 관을 지날 때 생기는 울림이었다. 세린은 그 음을 기억의 장치에 옮겨, 멀리서도 항구를 찾게 했다.",
        "19 · 건너지 않은 다리\n루아는 마지막 배를 놓친 것이 아니었다. 돌아오는 배를 맞을 사람이 필요했기에 남았다. 남겨진 것과 남은 것은 같은 모양이어도 다른 선택이다.",
        "20 · 바람을 빌려 쓰다\n공방의 장인 테오는 바람을 잡을 수 있다고 말하지 않았다. 잠깐 힘을 빌린 뒤 다시 보내 주는 것이라고 했다. 그래서 모든 날개 뒤에는 빈 하늘이 남아 있었다.",
        "21 · 반대로 도는 것들\n첫 번째 톱니가 오른쪽으로 돌면 두 번째는 왼쪽으로 돌았다. 테오는 그것을 다툼이라 생각하지 않았다. 서로 다르게 움직여야 함께 한 바퀴를 완성할 수 있었다.",
        "22 · 세 가지 빛\n노엘이 황금빛만 남기려 하자 테오는 붉고 푸른 렌즈를 가져왔다. 새로운 색은 하나를 없애서 생기는 것이 아니라, 다른 빛이 만날 때 생긴다고 말했다.",
        "23 · 멈춘 날개의 노래\n바람이 멎은 밤에는 아이들이 공방의 날개를 손으로 돌렸다. 날개는 도시를 움직일 만큼 빠르지 않았다. 그래도 그날 밤에는 누구도 정원이 멈췄다고 느끼지 않았다.",
        "24 · 여섯 섬의 아침\n세린은 기억을, 노엘은 빛을, 이안은 씨앗을, 루아는 물길을, 테오는 바람길을 남겼다. 마지막 자리는 비워 두었다. 여섯 번째 정원사는 이 기록을 읽고 있는 당신이다."
    };

    string RegionAt(Vector3 p)
    {
        if (p.z < -76) return p.x < -24 ? "서리의 온실" : p.x > 24 ? "새벽의 성소" : "별빛의 동굴";
        if (p.z < -23) return p.x < -24 ? "비의 수목원" : p.x > 24 ? "바람의 공방" : "조수의 회랑";
        return p.x < -24 ? "달빛 기록보관소" : p.x > 24 ? "태양 관측소" : p.z > 25 ? "새벽의 문" : "잊힌 빛의 정원";
    }
    void InitializeFrontiers()
    {
        gameObject.AddComponent<AetherFrontiers>();
        for(int i=0;i<frontierShards.Length;i++)
        {
            Vector3 p=FrontierRecordPositions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder,"Frontier archive pedestal "+(i+10),new Vector3(p.x,.22f,p.z),new Vector3(.8f,.22f,.8f),stone);
            var shard=AetherWorld.Shape(PrimitiveType.Cube,"Record "+(i+10),p,new Vector3(.22f,.46f,.22f),gold,null,false);
            shard.transform.rotation=Quaternion.Euler(0,45,25);
            frontierShards[i]=shard.transform; animated.Add(shard.transform);animatedBases.Add(p);
        }
    }
    void ResetFrontiers(bool resume)
    {
        AetherFrontiers.I.ResetJourney(resume);
        FrontierRecords=new bool[15];
        if(resume&&AetherSaveStore.HasKey(FrontierRecordKey))
        {
            string bits=AetherSaveStore.GetString(FrontierRecordKey);
            if(bits.Length==15)for(int i=0;i<15;i++)FrontierRecords[i]=bits[i]=='1';
        }
        for(int i=0;i<15;i++)frontierShards[i].gameObject.SetActive(!FrontierRecords[i]);
        nearbyFrontierRecord=-1;
    }
    void SaveFrontiers()
    {
        AetherFrontiers.I.Save();
        string bits="";foreach(bool found in FrontierRecords)bits+=found?"1":"0";
        AetherSaveStore.SetString(FrontierRecordKey,bits);
    }
    void UpdateFrontierInteraction()
    {
        nearbyFrontierRecord=-1;
        if(nearby>=0||nearbyExtraRelic>=0)return;
        float closest=3.2f;
        for(int i=0;i<15;i++)
        {
            if(FrontierRecords[i])continue;
            Vector3 point=FrontierRecordPositions[i];
            float distance=Vector3.Distance(Player.transform.position+Vector3.up,point);
            float facing=Vector3.Dot(Player.View.transform.forward,(point-Player.View.transform.position).normalized);
            if(distance<closest&&facing>-.05f){closest=distance;nearbyFrontierRecord=i;}
        }
        if(nearbyFrontierRecord>=0)Prompt="남겨진 기록 "+(nearbyFrontierRecord+10).ToString("00")+" 읽기";
        else if(!string.IsNullOrEmpty(AetherFrontiers.I.NearbyPrompt))Prompt=AetherFrontiers.I.NearbyPrompt;
    }
    bool CollectNearbyFrontierRecord()
    {
        if(nearbyFrontierRecord<0)return false;
        int id=nearbyFrontierRecord;
        FrontierRecords[id]=true;frontierShards[id].gameObject.SetActive(false);
        AetherAudio.I?.Chime(id%4);Save();
        Notify(TotalRelics==TotalRecords?"39개의 기억이 모두 이어졌습니다. 마지막 문에서 정원의 약속을 완성하세요.":FrontierMemories[id].Split('\n')[0]+" · 기록을 수집했습니다. J 일지에서 이야기를 읽어 보세요.",7);
        nearbyFrontierRecord=-1;return true;
    }
    public void OnFrontierSolved()
    {
        Save();
        if(TotalSolved==TotalPuzzles)Notify("27개의 빛이 깨어났습니다. 북쪽 새벽의 문으로 향하세요.",9);
    }
    IEnumerator SmokeSouthernCrossings(List<string> checks)
    {
        foreach(float x in new[]{-43f,0f,43f})
        {
            Player.Teleport(new Vector3(x,.15f,x==0?-13:-9),180,0);
            yield return new WaitForSeconds(.4f);
            for(int step=0;step<132&&Player.transform.position.z>-33.5f;step++)
            {
                Player.Body.Move(Vector3.back*.22f);
                yield return null;
            }
            Check(Player.transform.position.z<-33&&Mathf.Abs(Player.transform.position.x-x)<1&&Player.transform.position.y>-.15f,"walk entire southern bridge at x="+x,checks);
        }
    }
    IEnumerator SmokeFrontierRecords(List<string> checks,string dir)
    {
        // Use the same proximity and pickup code as keyboard interaction.
        for(int i=0;i<15;i++)
        {
            Player.Teleport(FrontierRecordPositions[i]+new Vector3(0,-.95f,-1.6f),0,0);
            yield return new WaitForSeconds(.25f);
            UpdateInteraction();UpdateExtraInteraction();UpdateFrontierInteraction();
            Check(nearbyFrontierRecord==i&&CollectNearbyFrontierRecord(),"frontier record "+(i+10)+" reachable and collected",checks);
        }
        Check(TotalRelics==24,"first twenty-four records collected",checks);
        ToggleJournal();
        GetComponent<AetherUI>().SelectJournalEntry(23);
        Check(GetJournalEntries().Length==TotalRecords&&!Player.CanMove,"expanded journal entries and movement lock",checks);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"17-expanded-journal.png"));
        yield return new WaitForSeconds(.5f);ToggleJournal();
        ToggleMap();ScreenCapture.CaptureScreenshot(Path.Combine(dir,"18-six-region-atlas.png"));
        yield return new WaitForSeconds(.5f);ToggleMap();
        TogglePhotoMode();
        photoCamera.transform.position=new Vector3(66,75,-115);
        photoCamera.transform.LookAt(new Vector3(0,0,-13));
        photoCamera.fieldOfView=65;
        var ui=GetComponent<AetherUI>();ui.enabled=false;
        yield return new WaitForSeconds(.7f);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"19-six-islands-panorama.png"));
        yield return new WaitForSeconds(.5f);
        ui.enabled=true;TogglePhotoMode();
    }
}

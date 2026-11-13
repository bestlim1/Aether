using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Nine independent instruments in the rain, tide and wind districts.</summary>
public sealed class AetherFrontiers : MonoBehaviour
{
    public static AetherFrontiers I;
    public static readonly Vector3[] Positions = {
        new Vector3(-50,0,-53),new Vector3(-36,0,-47),new Vector3(-49,0,-37),
        new Vector3(-8,0,-53),new Vector3(8,0,-46),new Vector3(0,0,-36),
        new Vector3(36,0,-53),new Vector3(50,0,-47),new Vector3(40,0,-36)
    };
    public static readonly string[] Titles = {
        "빗물의 길","정원의 합","씨앗의 법칙","네 개의 수문","해류의 항로","조수의 공명","맞물린 시간","새벽의 색","바람의 약속"
    };
    public static readonly string[] Descriptions = {
        "관을 돌려 왼쪽 위의 빗물을 오른쪽 아래로 보내세요. 아홉 칸을 모두 지나는 하나의 길이 필요합니다.",
        "아홉 화분의 값을 조절해 모든 행과 열의 합을 맞추세요. 행 6·9·3, 열 5·7·6입니다.",
        "첫 씨앗은 1. 다음 씨앗은 앞선 값의 두 배를 7로 나눈 나머지입니다. 여섯 단계의 성장을 이어 주세요.",
        "수문을 누르면 자신의 물 한 칸을 다음 수문으로 보냅니다. 총 열두 칸의 물을 네 곳에 똑같이 나누세요.",
        "첫 칸에서 시작해 3 → 5 → 9번 표지를 차례로 지나세요. 상하좌우로 이동하고, 일곱 칸으로 도착해야 합니다.",
        "네 진동자를 모두 켜세요. 진폭의 합은 6, 가중 진동수는 24가 되어야 합니다. 각 배율은 2·3·5·7입니다.",
        "기어 하나를 돌리면 다음 기어가 반대로 움직입니다. 세 바늘을 2·5·1의 표식에 맞추세요.",
        "빨강·초록·파랑의 빛을 섞어 견본의 색을 만드세요. 각 빛은 0부터 4까지 계량할 수 있습니다.",
        "A는 켜짐, B는 A의 반대, C는 A와 같음. D는 A와 C가 함께 켜질 때, E는 D의 반대, F는 D와 E 중 하나만 켜질 때 켜집니다."
    };
    public static readonly int[] PipeBaseMasks = {10,10,12,6,10,9,3,10,10};
    public static readonly int[] GridRowTargets = {6,9,3};
    public static readonly int[] GridColumnTargets = {5,7,6};
    public static readonly int[] FrequencyWeights = {2,3,5,7};
    public static readonly int[] GearTargets = {2,5,1};
    public static readonly int[] ColorTarget = {2,3,1};
    public static readonly int[] GrowthTarget = {1,2,4,1,2,4};
    static readonly int[] WindTarget = {1,0,1,1,0,1};
    static readonly int[] RowExample = {1,2,3,4,3,2,0,2,1};
    static readonly int[] RouteExample = {0,1,2,5,4,7,8};
    static readonly int[] ResonanceTarget = {2,1,2,1};
    static readonly int[] Lengths = {9,9,6,4,7,4,3,3,6};
    const string SaveKey = "Aether.Frontiers.v1";

    public int ActivePuzzle = -1;
    public bool[] Solved = new bool[9];
    public bool IsOpen => ActivePuzzle >= 0 && ActivePuzzle < 9;
    public int SolvedCount { get { int result=0; foreach(bool value in Solved) if(value) result++; return result; } }
    public int[] Values => IsOpen ? boards[ActivePuzzle] : Array.Empty<int>();
    public int SelectedIndex = -1, Progress, Highlight = -1;
    public bool IsPreviewing;
    public string NearbyPrompt = "";
    public string PuzzleTitle => IsOpen ? Titles[ActivePuzzle] : "새로운 정원의 성소";
    public string PuzzleDescription => IsOpen ? Descriptions[ActivePuzzle] : "";

    int[][] boards;
    int nearest=-1;
    Material stone,basalt,gold,cyan,dormant,mixedColor;
    readonly Renderer[] hearts=new Renderer[9];
    readonly Light[] lights=new Light[9];
    readonly LineRenderer[] beaconRays=new LineRenderer[9];
    readonly List<Renderer>[] pieces=new List<Renderer>[9];
    readonly TextMesh[] numberTiles=new TextMesh[9];
    readonly LineRenderer[] pipeStrokes=new LineRenderer[9];
    readonly Transform[] gears=new Transform[3];
    readonly Vector3[] heartHomes=new Vector3[9];
    readonly List<Transform> stationLabels=new List<Transform>(18);
    Camera labelCamera;
    LineRenderer routeStroke;

    void Awake()
    {
        I=this; InitializeBoards();
        stone=AetherWorld.Mat("Frontier · warm porcelain",new Color(.77f,.79f,.68f));
        basalt=AetherWorld.Mat("Frontier · deep slate",new Color(.12f,.24f,.27f));
        gold=AetherWorld.Mat("Frontier · Gold",new Color(1,.72f,.26f),.75f);
        cyan=AetherWorld.Mat("Frontier · running water",new Color(.24f,.88f,.93f),.85f);
        dormant=AetherWorld.Mat("Frontier · dormant instrument",new Color(.28f,.39f,.38f));
        mixedColor=AetherWorld.Mat("Frontier · mixed chroma",new Color(0,0,0),.1f);
        BuildStations(); RefreshAll();
    }
    void Update()
    {
        var game=AetherGame.I;
        if(game==null) return;
        if(!game.ReducedMotion)
            for(int i=0;i<9;i++) if(hearts[i]!=null)
                hearts[i].transform.position=heartHomes[i]+Vector3.up*Mathf.Sin(Time.time*.9f+i)*.055f;
        if(game.Phase!=GamePhase.Playing || game.JournalOpen || game.MapOpen || game.PhotoMode)
        { nearest=-1;NearbyPrompt="";return; }
        if(!IsOpen) { FindNearest();return; }
        NearbyPrompt="";
        if(game.IsSmokeMode) return;
        int keys=ActivePuzzle==0||ActivePuzzle==1||ActivePuzzle==4?9:Lengths[ActivePuzzle];
        for(int i=0;i<keys;i++)
            if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))
                DoAction(i,Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?-1:1);
        if(Input.GetKeyDown(KeyCode.H)) game.Notify(GetHint(),12);
        if(Input.GetKeyDown(KeyCode.R)) Replay();
    }
    void LateUpdate()
    {
        if(labelCamera==null||!labelCamera.isActiveAndEnabled)
        {
            var player=AetherGame.I==null?null:AetherGame.I.Player;
            labelCamera=player!=null&&player.View.isActiveAndEnabled?player.View:null;
            if(labelCamera==null)
                foreach(var camera in Camera.allCameras)
                    if(camera.isActiveAndEnabled){labelCamera=camera;break;}
        }
        if(labelCamera==null)return;
        foreach(var label in stationLabels)
        {
            Vector3 forward=label.position-labelCamera.transform.position;forward.y=0;
            if(forward.sqrMagnitude>.001f)label.rotation=Quaternion.LookRotation(forward,Vector3.up);
        }
    }
    bool CanInteract()
    {
        var game=AetherGame.I;
        return game!=null&&game.Phase==GamePhase.Playing&&game.Player!=null&&game.ActivePuzzle<0
            &&!game.JournalOpen&&!game.MapOpen&&!game.PhotoMode
            &&(AetherExpansion.I==null||!AetherExpansion.I.IsOpen)
            &&(AetherHorizons.I==null||!AetherHorizons.I.IsOpen);
    }
    void FindNearest()
    {
        nearest=-1;NearbyPrompt="";
        if(!CanInteract()||IsOpen)return;
        var player=AetherGame.I.Player;float best=3.8f;
        for(int i=0;i<9;i++)
        {
            Vector3 target=Positions[i]+Vector3.up*1.3f;
            float distance=Vector3.Distance(player.transform.position+Vector3.up,target);
            float facing=Vector3.Dot(player.View.transform.forward,(target-player.View.transform.position).normalized);
            if(distance<best&&facing>-.05f){nearest=i;best=distance;}
        }
        if(nearest>=0)NearbyPrompt=Titles[nearest]+(Solved[nearest]?" · 복구 완료":" 살펴보기");
    }
    public void TryInteract()
    {
        if(!CanInteract()||IsOpen)return;
        FindNearest();if(nearest<0)return;
        if(Solved[nearest]){AetherGame.I.Notify("이 장치의 빛은 이미 정원으로 이어지고 있습니다.");return;}
        ActivePuzzle=nearest;SelectedIndex=-1;NearbyPrompt="";UpdateReadout();
        AetherGame.I.RefreshCursor();AetherAudio.I?.Click();
    }
    public void Close()
    {
        ActivePuzzle=-1;SelectedIndex=-1;Progress=0;Highlight=-1;IsPreviewing=false;NearbyPrompt="";
        Save();AetherGame.I?.RefreshCursor();
    }
    public void Replay()
    {
        if(!IsOpen||Solved[ActivePuzzle]||AetherGame.I==null||AetherGame.I.Phase!=GamePhase.Playing)return;
        boards[ActivePuzzle]=InitialBoard(ActivePuzzle);SelectedIndex=-1;
        UpdateReadout();RefreshStation(ActivePuzzle);Save();
        AetherAudio.I?.Click();AetherGame.I.Notify("장치를 처음 상태로 되돌렸습니다.",3);
    }
    public void DoAction(int index,int delta=1)
    {
        if(!CanInteract()||!IsOpen||Solved[ActivePuzzle]||delta==0)return;
        int puzzle=ActivePuzzle;int[] values=boards[puzzle];bool changed=false;
        if(index<0||index>=(puzzle==4?9:values.Length))return;
        switch(puzzle)
        {
            case 0: values[index]=Mod(values[index]+delta,4);changed=true;break;
            case 1: values[index]=Mod(values[index]+delta,5);changed=true;break;
            case 2:
                if(index==0){AetherGame.I.Notify("첫 씨앗의 1은 고정된 시작점입니다.",3);return;}
                values[index]=Mod(values[index]+delta,7);changed=true;break;
            case 3:
                int next=(index+1)%4;int from=delta>0?index:next;int to=delta>0?next:index;
                if(values[from]==0||values[to]==6){AetherGame.I.Notify("빈 수문에서는 물을 보낼 수 없고, 가득 찬 수문은 받을 수 없습니다.",3);return;}
                values[from]--;values[to]++;changed=true;break;
            case 4: changed=RouteAction(values,index,delta);break;
            case 5: values[index]=Mod(values[index]+delta,4);changed=true;break;
            case 6: values[index]=Mod(values[index]+delta,8);values[(index+1)%3]=Mod(values[(index+1)%3]-delta,8);changed=true;break;
            case 7: values[index]=Mod(values[index]+delta,5);changed=true;break;
            case 8: values[index]=1-values[index];changed=true;break;
        }
        if(!changed)return;
        SelectedIndex=index;UpdateReadout();RefreshStation(puzzle);AetherAudio.I?.Chime(index%4);
        if(PuzzleSolved(puzzle,values))
        {
            Solved[puzzle]=true;RefreshStation(puzzle);Close();
            AetherAudio.I?.Solve();AetherGame.I.Notify(Titles[puzzle]+" · 빛 복구 완료",6);
            AetherGame.I.OnFrontierSolved();
        }
        else Save();
    }
    bool RouteAction(int[] values,int node,int delta)
    {
        int length=RouteLength(values);
        if(delta<0||(length>1&&node==values[length-1]))
        {if(length<=1)return false;values[length-1]=-1;return true;}
        if(length>=7){AetherGame.I.Notify("일곱 칸을 사용했습니다. 마지막 칸을 다시 눌러 되돌아갈 수 있습니다.",4);return false;}
        if(Array.IndexOf(values,node)>=0||!Adjacent(values[length-1],node))
        {AetherGame.I.Notify("다시 밟지 않은 상하좌우의 칸을 골라 주세요.",3);return false;}
        if(node==4&&Array.IndexOf(values,2)<0)
        {AetherGame.I.Notify("5번보다 먼저 3번 표지를 지나야 합니다.",3);return false;}
        if(node==8&&(length!=6||Array.IndexOf(values,2)<0||Array.IndexOf(values,4)<0))
        {AetherGame.I.Notify("3번과 5번을 거친 뒤, 일곱 번째 칸으로 9번에 도착하세요.",4);return false;}
        values[length]=node;return true;
    }
    void UpdateReadout()
    {
        if(!IsOpen){Progress=0;Highlight=-1;return;}
        int[] values=Values;Progress=0;Highlight=-1;
        if(ActivePuzzle==0)Progress=CountBits(PipeReachable(values));
        else if(ActivePuzzle==1)
        {for(int i=0;i<3;i++){if(values[i*3]+values[i*3+1]+values[i*3+2]==GridRowTargets[i])Progress++;if(values[i]+values[i+3]+values[i+6]==GridColumnTargets[i])Progress++;}}
        else if(ActivePuzzle==4){Progress=RouteLength(values);Highlight=values[Progress-1];}
        else if(ActivePuzzle==5){foreach(int value in values)Progress+=value;}
        else
        {
            int[] target=ActivePuzzle==2?GrowthTarget:ActivePuzzle==3?new[]{3,3,3,3}:ActivePuzzle==6?GearTargets:ActivePuzzle==7?ColorTarget:WindTarget;
            for(int i=0;i<values.Length;i++)if(values[i]==target[i])Progress++;
        }
    }
    public string GetHint()
    {
        switch(ActivePuzzle)
        {
            case 0:return "물이 지나는 칸은 1→2→3→6→5→4→7→8→9. 윗줄은 오른쪽, 가운데 줄은 왼쪽, 아랫줄은 오른쪽으로 이어집니다.";
            case 1:return "가능한 배치 하나는 윗줄 1·2·3, 가운데 4·3·2, 아랫줄 0·2·1. 모든 행과 열의 합만 맞으면 다른 배치도 정답입니다.";
            case 2:return "1을 두 배 하면 2, 다시 두 배 하면 4, 8을 7로 나눈 나머지는 1. 전체 성장 순서는 1·2·4·1·2·4입니다.";
            case 3:return "각 수문에 물 3칸이 필요합니다. 초기 상태라면 1번과 3번 수문을 각각 세 번 누르세요. Shift+숫자는 흐름을 거꾸로 보냅니다. R로 초기화할 수 있습니다.";
            case 4:return "경로는 1→2→3→6→5→8→9. 마지막 칸을 다시 누르면 한 칸 되돌아갑니다. R은 출발점으로 돌아갑니다.";
            case 5:return "네 진폭은 2·1·2·1입니다. 합은 6, 가중합은 2×2+3×1+5×2+7×1=24가 됩니다.";
            case 6:
                int first=Mod(GearTargets[0]-Values[0],8),second=Mod(GearTargets[0]-Values[0]+GearTargets[1]-Values[1],8);
                return "현재 상태에서 왼쪽 기어를 "+first+"번, 가운데 기어를 "+second+"번 정방향으로 돌리면 2·5·1에 맞습니다. 숫자키로 회전하고 Shift를 누르면 반대입니다.";
            case 7:return "빨강 2, 초록 3, 파랑 1을 섞으세요. 초록이 가장 많고, 파랑이 가장 적은 연두색입니다.";
            case 8:return "켜진 날개는 A·C·D·F, 꺼진 날개는 B·E입니다. F는 두 입력 D와 E의 상태가 다를 때 켜집니다.";
            default:return "장치의 기록을 읽고 숫자 또는 버튼으로 조작해 보세요.";
        }
    }

    public static int[] InitialBoard(int puzzle)
    {
        switch(puzzle)
        {
            case 0:return new[]{1,0,2,1,3,2,0,1,2};
            case 1:return new int[9];
            case 2:return new[]{1,0,0,0,0,0};
            case 3:return new[]{6,0,6,0};
            case 4:return new[]{0,-1,-1,-1,-1,-1,-1};
            case 5:return new int[4];
            case 6:return new int[3];
            case 7:return new int[3];
            case 8:return new int[6];
            default:throw new ArgumentOutOfRangeException(nameof(puzzle));
        }
    }
    void InitializeBoards(){Solved=new bool[9];boards=new int[9][];for(int i=0;i<9;i++)boards[i]=InitialBoard(i);}
    public void ResetJourney(bool resume)
    {
        ActivePuzzle=-1;SelectedIndex=-1;Progress=0;Highlight=-1;IsPreviewing=false;nearest=-1;NearbyPrompt="";
        InitializeBoards();SaveData saved=resume?ReadSave():null;
        if(saved!=null){Solved=(bool[])saved.solved.Clone();for(int i=0;i<9;i++)boards[i]=(int[])saved.boards[i].values.Clone();}
        RefreshAll();Save();AetherGame.I?.RefreshCursor();
    }
    public void Save()
    {
        if(boards==null||AetherGame.I==null)return;
        var data=new SaveData{solved=(bool[])Solved.Clone(),boards=new Board[9]};
        for(int i=0;i<9;i++)data.boards[i]=new Board{values=(int[])boards[i].Clone()};
        AetherSaveStore.SetString(SaveKey,JsonUtility.ToJson(data));
    }
    SaveData ReadSave()
    {
        if(!AetherSaveStore.HasKey(SaveKey))return null;
        try
        {
            var data=JsonUtility.FromJson<SaveData>(AetherSaveStore.GetString(SaveKey));
            if(data==null||data.version!=1||data.solved==null||data.solved.Length!=9||data.boards==null||data.boards.Length!=9)return null;
            for(int i=0;i<9;i++)
                if(data.boards[i]==null||!BoardValid(i,data.boards[i].values)||data.solved[i]!=PuzzleSolved(i,data.boards[i].values))return null;
            return data;
        }
        catch{return null;}
    }
    [Serializable]class Board{public int[] values;}
    [Serializable]class SaveData{public int version=1;public bool[] solved;public Board[] boards;}

    public static bool BoardValid(int puzzle,int[] values)
    {
        if(puzzle<0||puzzle>=9||values==null||values.Length!=Lengths[puzzle])return false;
        if(puzzle==4)return RouteValid(values);
        int max=puzzle==0||puzzle==5?3:puzzle==1||puzzle==7?4:puzzle==2||puzzle==3?6:puzzle==6?7:1;
        foreach(int value in values)if(value<0||value>max)return false;
        if(puzzle==2&&values[0]!=1)return false;
        if(puzzle==3&&values[0]+values[1]+values[2]+values[3]!=12)return false;
        if(puzzle==6&&Mod(values[0]+values[1]+values[2],8)!=0)return false;
        return true;
    }
    public static bool PuzzleSolved(int puzzle,int[] values)
    {
        if(!BoardValid(puzzle,values))return false;
        switch(puzzle)
        {
            case 0:return PipesSolved(values);
            case 1:
                for(int i=0;i<3;i++)if(values[i*3]+values[i*3+1]+values[i*3+2]!=GridRowTargets[i]||values[i]+values[i+3]+values[i+6]!=GridColumnTargets[i])return false;
                return true;
            case 2:for(int i=1;i<6;i++)if(values[i]!=(values[i-1]*2)%7)return false;return true;
            case 3:foreach(int value in values)if(value!=3)return false;return true;
            case 4:return RouteLength(values)==7&&values[6]==8&&Array.IndexOf(values,2)<Array.IndexOf(values,4)&&Array.IndexOf(values,2)>0;
            case 5:
                int sum=0,frequency=0;
                for(int i=0;i<4;i++){if(values[i]==0)return false;sum+=values[i];frequency+=values[i]*FrequencyWeights[i];}
                return sum==6&&frequency==24;
            case 6:return Equal(values,GearTargets);
            case 7:return Equal(values,ColorTarget);
            case 8:return values[0]==1&&values[1]==1-values[0]&&values[2]==values[0]&&values[3]==(values[0]&values[2])&&values[4]==1-values[3]&&values[5]==(values[3]^values[4]);
            default:return false;
        }
    }
    public static int PipeMask(int cell,int rotation)
    {
        if(cell<0||cell>=9)return 0;
        int mask=PipeBaseMasks[cell];int turns=Mod(rotation,4);
        return ((mask<<turns)|(mask>>(4-turns)))&15;
    }
    public static int PipeReachable(int[] values)
    {
        if(values==null||values.Length!=9||(PipeMask(0,values[0])&8)==0)return 0;
        int seen=1;var queue=new Queue<int>();queue.Enqueue(0);
        int[] dr={-1,0,1,0},dc={0,1,0,-1};
        while(queue.Count>0)
        {
            int cell=queue.Dequeue(),mask=PipeMask(cell,values[cell]);
            for(int dir=0;dir<4;dir++)
            {
                if((mask&(1<<dir))==0)continue;
                int row=cell/3+dr[dir],column=cell%3+dc[dir];if(row<0||row>2||column<0||column>2)continue;
                int other=row*3+column;
                if((seen&(1<<other))==0&&(PipeMask(other,values[other])&(1<<((dir+2)%4)))!=0){seen|=1<<other;queue.Enqueue(other);}
            }
        }
        return seen;
    }
    public static bool PipesSolved(int[] values)
    {
        return values!=null&&values.Length==9&&PipeReachable(values)==511&&(PipeMask(8,values[8])&2)!=0;
    }
    static bool RouteValid(int[] values)
    {
        if(values==null||values.Length!=7||values[0]!=0)return false;
        int seen=0;bool ended=false;
        for(int i=0;i<values.Length;i++)
        {
            int node=values[i];if(node==-1){ended=true;continue;}
            if(ended||node<0||node>8||(seen&(1<<node))!=0||(i>0&&!Adjacent(values[i-1],node)))return false;
            if(node==4&&(seen&(1<<2))==0)return false;
            if(node==8&&(i!=6||(seen&(1<<2))==0||(seen&(1<<4))==0))return false;
            seen|=1<<node;
        }
        return true;
    }
    static int RouteLength(int[] values){int count=0;while(count<values.Length&&values[count]>=0)count++;return count;}
    static bool Adjacent(int a,int b)=>Math.Abs(a/3-b/3)+Math.Abs(a%3-b%3)==1;
    static int Mod(int value,int modulo)=>(value%modulo+modulo)%modulo;
    static int CountBits(int value){int count=0;while(value!=0){count+=value&1;value>>=1;}return count;}
    static bool Equal(int[] a,int[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}

    void BuildStations()
    {
        for(int puzzle=0;puzzle<9;puzzle++)
        {
            pieces[puzzle]=new List<Renderer>();Vector3 p=Positions[puzzle];
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[puzzle]+" · footing",p+Vector3.up*.13f,new Vector3(2.85f,.13f,2.85f),basalt);
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[puzzle]+" · instrument table",p+Vector3.up*.62f,new Vector3(2.25f,.34f,2.25f),stone);
            AetherWorld.Shape(PrimitiveType.Cylinder,"Instrument · fine gold rim",p+Vector3.up*.968f,new Vector3(2.18f,.018f,2.18f),gold,null,false);
            var heart=AetherWorld.Shape(PrimitiveType.Sphere,"Frontier heart "+(puzzle+10),p+new Vector3(0,2.15f,.48f),Vector3.one*.29f,dormant,null,false);
            hearts[puzzle]=heart.GetComponent<Renderer>();heartHomes[puzzle]=heart.transform.position;
            lights[puzzle]=heart.AddComponent<Light>();lights[puzzle].color=new Color(.36f,.88f,1);lights[puzzle].range=4.6f;lights[puzzle].intensity=.25f;
            beaconRays[puzzle]=MakeLine("Frontier restored light "+puzzle,new[]{p+Vector3.up*2.2f,p+Vector3.up*9.5f},cyan,.045f);
            beaconRays[puzzle].gameObject.SetActive(false);
            Label((puzzle+10).ToString("00"),p+new Vector3(0,.61f,-1.15f),.16f,true);
            string[] inscriptions={"RAIN / ALL NINE","ROWS 6 9 3 / COLS 5 7 6","GROWTH x2 / MOD 7","TWELVE / FOUR","3 > 5 > 9 / SEVEN STEPS","2 3 5 7 / TOTAL 24","II / V / I","RED 2 / GREEN 3 / BLUE 1","A = ON / FOLLOW THE WIND"};
            Label(inscriptions[puzzle],p+new Vector3(0,.87f,-1.14f),.052f,true);
            if(puzzle==0)
            {
                for(int i=0;i<9;i++)
                {
                    Vector3 cell=GridPoint(p,i,1.12f,.43f);
                    AddPiece(puzzle,PrimitiveType.Cube,"Water channel tile "+i,cell,new Vector3(.39f,.10f,.39f),basalt);
                    pipeStrokes[i]=MakeLine("Water bend "+i,new[]{cell,cell,cell},cyan,.035f);
                }
            }
            else if(puzzle==1)
            {
                for(int i=0;i<9;i++)
                {
                    Vector3 cell=GridPoint(p,i,1.12f,.44f);
                    AddPiece(puzzle,PrimitiveType.Cube,"Number planter "+i,cell,new Vector3(.38f,.12f,.38f),basalt);
                    numberTiles[i]=Label("0",cell+Vector3.up*.08f,.12f);
                    numberTiles[i].transform.rotation=Quaternion.Euler(90,0,0);
                    numberTiles[i].color=new Color(.93f,.86f,.64f);
                }
            }
            else if(puzzle==2)
            {
                for(int i=0;i<6;i++)
                {
                    Vector3 cell=p+new Vector3((i%3-1)*.48f,1.25f,(i/3-.5f)*.53f);
                    AddPiece(puzzle,PrimitiveType.Sphere,"Growing seed "+i,cell,Vector3.one*.22f,i==0?gold:cyan);
                    AetherWorld.Shape(PrimitiveType.Cylinder,"Seed cup",cell-Vector3.up*.15f,new Vector3(.32f,.06f,.32f),basalt,null,false);
                }
            }
            else if(puzzle==3)
            {
                for(int i=0;i<4;i++)
                {
                    Vector3 cell=p+new Vector3((i-1.5f)*.42f,1.12f,0);
                    AetherWorld.Shape(PrimitiveType.Cylinder,"Reservoir base",cell,new Vector3(.34f,.045f,.34f),basalt,null,false);
                    AddPiece(puzzle,PrimitiveType.Cylinder,"Reservoir water "+i,cell+Vector3.up*.2f,new Vector3(.25f,.2f,.25f),cyan);
                    Label((i+1).ToString(),p+new Vector3((i-1.5f)*.42f,1.10f,-.38f),.075f);
                }
            }
            else if(puzzle==4)
            {
                for(int i=0;i<9;i++)AddPiece(puzzle,PrimitiveType.Sphere,"Tidal marker "+i,GridPoint(p,i,1.15f,.45f),Vector3.one*.12f,(i==2||i==4||i==8)?gold:dormant);
                routeStroke=MakeLine("Chosen tidal route",new[]{GridPoint(p,0,1.2f,.45f)},cyan,.033f);
            }
            else if(puzzle==5)
            {
                for(int i=0;i<4;i++)
                {
                    Vector3 cell=p+new Vector3((i-1.5f)*.41f,1.25f,0);
                    AddPiece(puzzle,PrimitiveType.Cube,"Resonant bar "+i,cell,new Vector3(.20f,.2f,.20f),cyan);
                    Label(FrequencyWeights[i].ToString(),p+new Vector3((i-1.5f)*.41f,1.12f,-.37f),.085f);
                }
            }
            else if(puzzle==6)
            {
                for(int i=0;i<3;i++)
                {
                    Vector3 center=p+new Vector3((i-1)*.60f,1.12f,0);
                    var pivot=new GameObject("Coupled gear "+i).transform;pivot.position=center;gears[i]=pivot;
                    AetherWorld.Shape(PrimitiveType.Cylinder,"Gear hub",center,new Vector3(.40f,.055f,.40f),basalt,pivot,false);
                    for(int tooth=0;tooth<8;tooth++)
                    {
                        float angle=tooth*Mathf.PI/4;
                        var item=AetherWorld.Shape(PrimitiveType.Cube,"Gear tooth",center+new Vector3(Mathf.Sin(angle)*.23f,0,Mathf.Cos(angle)*.23f),new Vector3(.11f,.075f,.10f),gold,pivot,false);
                        item.transform.rotation=Quaternion.Euler(0,tooth*45,0);
                    }
                    AetherWorld.Shape(PrimitiveType.Cube,"Gear index",center+new Vector3(0,.055f,.13f),new Vector3(.045f,.035f,.23f),cyan,pivot,false);
                }
            }
            else if(puzzle==7)
            {
                Material[] primaries={AetherWorld.Mat("Chroma red",new Color(.92f,.17f,.17f),.45f),AetherWorld.Mat("Chroma green",new Color(.20f,.90f,.31f),.45f),AetherWorld.Mat("Chroma blue",new Color(.22f,.38f,1),.45f)};
                for(int i=0;i<3;i++)AddPiece(puzzle,PrimitiveType.Sphere,"Color measure "+i,p+new Vector3((i-1)*.51f,1.2f,-.15f),Vector3.one*.23f,primaries[i]);
                AetherWorld.Shape(PrimitiveType.Sphere,"Mixed light",p+new Vector3(-.27f,1.48f,.33f),Vector3.one*.29f,mixedColor,null,false);
                AetherWorld.Shape(PrimitiveType.Sphere,"Color reference",p+new Vector3(.27f,1.48f,.33f),Vector3.one*.29f,AetherWorld.Mat("Target chroma",new Color(.5f,.75f,.25f),.1f),null,false);
            }
            else
            {
                for(int i=0;i<6;i++)
                {
                    Vector3 cell=p+new Vector3((i%3-1)*.54f,1.21f,(i/3-.5f)*.54f);
                    AddPiece(puzzle,PrimitiveType.Cube,"Logic vane "+(char)('A'+i),cell,new Vector3(.34f,.07f,.12f),dormant);
                    Label(((char)('A'+i)).ToString(),cell+new Vector3(0,-.07f,-.16f),.07f);
                }
            }
        }
    }
    void AddPiece(int puzzle,PrimitiveType shape,string name,Vector3 position,Vector3 scale,Material material)
    {pieces[puzzle].Add(AetherWorld.Shape(shape,name,position,scale,material,null,false).GetComponent<Renderer>());}
    static Vector3 GridPoint(Vector3 center,int index,float y,float spacing)=>center+new Vector3((index%3-1)*spacing,y,(1-index/3)*spacing);
    void RefreshAll(){for(int i=0;i<9;i++)RefreshStation(i);}
    void RefreshStation(int puzzle)
    {
        if(hearts[puzzle]==null)return;
        hearts[puzzle].sharedMaterial=Solved[puzzle]?cyan:dormant;lights[puzzle].intensity=Solved[puzzle]?1.75f:.25f;
        beaconRays[puzzle].gameObject.SetActive(Solved[puzzle]);
        int index=puzzle+9;
        if(AetherWorld.BeaconTops!=null&&AetherWorld.BeaconTops.Length>index&&AetherWorld.BeaconTops[index]!=null)
        {var renderer=AetherWorld.BeaconTops[index].GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=Solved[puzzle]?cyan:dormant;}
        int[] values=boards[puzzle];Vector3 p=Positions[puzzle];
        if(puzzle==0)
        {
            int reachable=PipeReachable(values);
            for(int i=0;i<9;i++)
            {
                Vector3 center=GridPoint(p,i,1.187f,.43f);int mask=PipeMask(i,values[i]);var ends=new List<Vector3>();
                for(int dir=0;dir<4;dir++)if((mask&(1<<dir))!=0)
                    ends.Add(center+new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left}[dir]*.215f);
                pipeStrokes[i].SetPositions(new[]{ends[0],center,ends[1]});
                pipeStrokes[i].sharedMaterial=(reachable&(1<<i))!=0?cyan:gold;
            }
        }
        else if(puzzle==1){for(int i=0;i<9;i++)numberTiles[i].text=values[i].ToString();}
        else if(puzzle==2){for(int i=0;i<6;i++)pieces[puzzle][i].transform.localScale=Vector3.one*(.10f+values[i]*.037f);}
        else if(puzzle==3)
        {
            for(int i=0;i<4;i++)
            {
                float half=.025f+values[i]*.039f;
                pieces[puzzle][i].transform.localScale=new Vector3(.25f,half,.25f);
                pieces[puzzle][i].transform.position=p+new Vector3((i-1.5f)*.42f,1.16f+half,0);
            }
        }
        else if(puzzle==4)
        {
            int count=RouteLength(values);routeStroke.positionCount=count;
            for(int i=0;i<count;i++)routeStroke.SetPosition(i,GridPoint(p,values[i],1.20f,.45f));
            for(int i=0;i<9;i++)pieces[puzzle][i].sharedMaterial=Array.IndexOf(values,i)>=0?cyan:(i==2||i==4||i==8)?gold:dormant;
        }
        else if(puzzle==5)
        {for(int i=0;i<4;i++){float height=.10f+values[i]*.17f;pieces[puzzle][i].transform.localScale=new Vector3(.2f,height,.2f);pieces[puzzle][i].transform.position=p+new Vector3((i-1.5f)*.41f,1.13f+height*.5f,0);}}
        else if(puzzle==6){for(int i=0;i<3;i++)gears[i].rotation=Quaternion.Euler(0,values[i]*45,0);}
        else if(puzzle==7)
        {
            Color color=new Color(values[0]/4f,values[1]/4f,values[2]/4f);
            mixedColor.color=color;mixedColor.SetColor("_EmissionColor",color*.10f);
            for(int i=0;i<3;i++)pieces[puzzle][i].transform.localScale=Vector3.one*(.13f+values[i]*.033f);
        }
        else if(puzzle==8)
        {for(int i=0;i<6;i++){pieces[puzzle][i].sharedMaterial=values[i]==1?cyan:dormant;pieces[puzzle][i].transform.rotation=Quaternion.Euler(0,values[i]==1?45:-45,0);}}
    }
    static LineRenderer MakeLine(string name,Vector3[] points,Material material,float width)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>();line.sharedMaterial=material;
        line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;
        line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
    }
    TextMesh Label(string text,Vector3 position,float size,bool faceCamera=false)
    {
        var go=new GameObject("Frontier inscription · "+text);go.transform.position=position;
        var label=go.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
        label.fontSize=48;label.characterSize=size;label.color=new Color(.16f,.29f,.31f);
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(font!=null){label.font=font;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
        if(faceCamera)stationLabels.Add(go.transform);
        return label;
    }

    public static List<string> RunSelfTests()
    {
        var checks=new List<string>();Action<bool,string> test=(ok,label)=>checks.Add((ok?"PASS ":"FAIL ")+label);
        for(int i=0;i<9;i++)test(BoardValid(i,InitialBoard(i))&&!PuzzleSolved(i,InitialBoard(i)),"frontier "+i+" starts valid and unfinished");
        int[][] targets={new int[9],RowExample,GrowthTarget,new[]{3,3,3,3},RouteExample,ResonanceTarget,GearTargets,ColorTarget,WindTarget};
        for(int i=0;i<9;i++)test(PuzzleSolved(i,targets[i])&&!PuzzleSolved(i,null),"frontier "+i+" target and null guard");
        var pipes=new int[9];pipes[0]=1;test(!PipesSolved(pipes)&&PipeReachable(pipes)==0,"pipe entrance rejects a broken connection");
        pipes[0]=2;test(PipesSolved(pipes),"straight pipes allow equivalent half rotations");
        test(PipeMask(2,0)==12&&PipeMask(2,1)==9&&PipeMask(2,4)==12,"pipe rotations preserve their openings");
        var badGrid=(int[])RowExample.Clone();badGrid[0]++;test(!PuzzleSolved(1,badGrid),"garden sum rejects a wrong row and column");
        test(!BoardValid(2,new[]{0,2,4,1,2,4})&&!PuzzleSolved(2,new[]{1,2,4,2,2,4}),"seed starting point and recurrence");
        test(!BoardValid(3,new[]{3,3,3,2})&&!BoardValid(3,new[]{7,0,3,2}),"water total and tank capacity are preserved");
        test(BoardValid(4,new[]{0,3,-1,-1,-1,-1,-1})&&!BoardValid(4,new[]{0,4,-1,-1,-1,-1,-1}),"tidal routes require orthogonal steps");
        test(!BoardValid(4,new[]{0,1,4,-1,-1,-1,-1})&&!BoardValid(4,new[]{0,1,2,5,8,-1,-1}),"tidal checkpoints and arrival length");
        test(!PuzzleSolved(5,new[]{3,1,1,1})&&!PuzzleSolved(5,new[]{0,2,2,2}),"resonance needs both total and four active voices");
        bool gearsValid=true;
        for(int a=0;a<8;a++)for(int b=0;b<8;b++)for(int c=0;c<8;c++)
            gearsValid&=BoardValid(6,new[]{a,b,c})==((a+b+c)%8==0);
        test(gearsValid,"all 512 gear states obey conserved rotation");
        test(!PuzzleSolved(7,new[]{2,2,1})&&!BoardValid(7,new[]{2,5,1}),"color target and channel limits");
        int windSolutions=0;for(int mask=0;mask<64;mask++){var state=new int[6];for(int i=0;i<6;i++)state[i]=(mask>>i)&1;if(PuzzleSolved(8,state))windSolutions++;}
        test(windSolutions==1,"wind logic has exactly one solution among all 64 states");
        return checks;
    }

    /// <summary>Exercises the actual station actions and persistence in the isolated smoke save store.</summary>
    public IEnumerator ValidateJourney(List<string> checks,string directory)
    {
        var game=AetherGame.I;
        if(game==null||!game.IsSmokeMode){Record(checks,false,"frontier validation requires isolated smoke mode");yield break;}
        Directory.CreateDirectory(directory);
        foreach(string check in RunSelfTests())checks.Add(check);
        Vector3[] bridgeCenters={new Vector3(-43,.12f,-21),new Vector3(0,.12f,-23.5f),new Vector3(43,.12f,-21)};
        Vector3[] views={new Vector3(-43,.12f,-33.8f),new Vector3(0,.12f,-33.5f),new Vector3(43,.12f,-33.8f)};
        string[] names={"rain arboretum","tidal gallery","wind workshop"};
        string[] screenshots={"13-rain-arboretum.png","14-tidal-gallery.png","15-wind-workshop.png"};
        for(int district=0;district<3;district++)
        {
            game.Player.Teleport(bridgeCenters[district],180,0);yield return new WaitForSeconds(.4f);
            Record(checks,game.Player.Body.isGrounded&&game.Player.transform.position.y>-.1f,names[district]+" bridge collision");
            game.Player.Teleport(views[district],180,-5);yield return new WaitForSeconds(.5f);
            Record(checks,game.Player.Body.isGrounded&&game.Player.transform.position.y>-.1f,names[district]+" ground collision");
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,screenshots[district]));yield return new WaitForSeconds(.25f);
        }
        for(int puzzle=0;puzzle<9;puzzle++)
        {
            game.Player.Teleport(Positions[puzzle]+new Vector3(0,.12f,-2.7f),0,0);
            yield return new WaitForSeconds(.4f);
            Record(checks,game.Player.Body.isGrounded&&game.Player.transform.position.y>-.1f,"frontier "+puzzle+" approach is walkable");
            TryInteract();
            Record(checks,ActivePuzzle==puzzle&&!game.Player.CanMove,"frontier "+puzzle+" opens through proximity and locks movement");
            if(ActivePuzzle!=puzzle)continue;
            if(puzzle==0||puzzle==3||puzzle==6)
            {
                yield return new WaitForSeconds(.15f);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,(16+puzzle/3).ToString("00")+"-frontier-puzzle-"+(puzzle+10)+".png"));
                yield return new WaitForSeconds(.25f);
            }
            if(puzzle==0)
            {
                var frozen=(int[])Values.Clone();game.Pause();DoAction(0);
                Record(checks,Equal(frozen,Values),"paused frontier rejects puzzle input");game.Resume();
            }
            DoAction(puzzle==2?1:puzzle==4?3:0);
            Record(checks,!Solved[puzzle]&&ActivePuzzle==puzzle,"frontier "+puzzle+" wrong action does not solve");
            var partial=(int[])Values.Clone();Save();Close();ResetJourney(true);TryInteract();
            Record(checks,ActivePuzzle==puzzle&&Equal(partial,Values)&&!Solved[puzzle],"frontier "+puzzle+" partial state survives save and reopen");
            if(ActivePuzzle!=puzzle)continue;
            Replay();Record(checks,Equal(Values,InitialBoard(puzzle)),"frontier "+puzzle+" reset restores its initial board");
            SolveByActions(puzzle);
            Record(checks,Solved[puzzle]&&!IsOpen,"frontier "+puzzle+" solved through real actions");
        }
        Save();string finished=AetherSaveStore.GetString(SaveKey);
        ResetJourney(true);Record(checks,SolvedCount==9&&!IsOpen,"all nine frontier instruments survive save and resume");
        AetherSaveStore.SetString(SaveKey,"{\"version\":999}");ResetJourney(true);
        Record(checks,SolvedCount==0,"damaged frontier save resets only frontier progress");
        AetherSaveStore.SetString(SaveKey,finished);ResetJourney(true);
        Record(checks,SolvedCount==9&&game.Phase==GamePhase.Playing&&!IsOpen,"frontier validation leaves nine solved and play active");
    }
    void SolveByActions(int puzzle)
    {
        if(puzzle==0){for(int i=0;i<9&&IsOpen;i++)for(int turn=0;turn<4&&IsOpen&&Values[i]!=0;turn++)DoAction(i);}
        else if(puzzle==1)SetByActions(RowExample);
        else if(puzzle==2)SetByActions(GrowthTarget);
        else if(puzzle==3){for(int i=0;i<3;i++)DoAction(0);for(int i=0;i<3;i++)DoAction(2);}
        else if(puzzle==4){for(int i=1;i<RouteExample.Length;i++)DoAction(RouteExample[i]);}
        else if(puzzle==5)SetByActions(ResonanceTarget);
        else if(puzzle==6){DoAction(0);DoAction(0);for(int i=0;i<7;i++)DoAction(1);}
        else if(puzzle==7)SetByActions(ColorTarget);
        else if(puzzle==8){DoAction(0);DoAction(2);DoAction(3);DoAction(5);}
    }
    void SetByActions(int[] target)
    {
        for(int i=0;i<target.Length&&IsOpen;i++)
            for(int step=0;step<8&&IsOpen&&Values[i]!=target[i];step++)DoAction(i);
    }
    static void Record(List<string> checks,bool passed,string description)
    {checks.Add((passed?"PASS ":"FAIL ")+description);if(!passed)Debug.LogError("AETHER_FRONTIERS: "+description);}
    void OnDestroy(){if(I==this)I=null;}
}

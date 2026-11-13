using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Nine self-contained logic instruments in the frost, stars and dawn districts.</summary>
public sealed class AetherHorizons : MonoBehaviour
{
    public static AetherHorizons I;
    public static readonly Vector3[] Positions={
        new Vector3(-50,0,-105),new Vector3(-36,0,-99),new Vector3(-49,0,-89),
        new Vector3(-8,0,-105),new Vector3(8,0,-98),new Vector3(0,0,-88),
        new Vector3(36,0,-105),new Vector3(50,0,-99),new Vector3(40,0,-88)
    };
    public static readonly string[] Titles={"서리의 문자","얼음의 자수","작은 나룻배","네 개의 별","별자리 연산식","빛의 탑","두 항아리","새벽의 다리","관측자의 암호"};
    public static readonly string[] Descriptions={
        "각 행, 각 열, 굵게 묶인 2×2 구역마다 1·2·3·4를 한 번씩 놓으세요. 서리로 새겨진 숫자는 바뀌지 않습니다.",
        "행과 열 바깥의 수는 연속해서 채워진 칸의 길이입니다. 숫자 사이에는 빈칸이 하나 이상 있어야 합니다. 얼음 속 무늬를 복원하세요.",
        "사공과 여우·새·씨앗을 오른쪽 강둑으로 옮기세요. 배에는 사공과 짐 하나만 탑니다. 사공이 없으면 여우는 새를, 새는 씨앗을 먹습니다.",
        "각 행의 별을 옮기세요. 어떤 두 별도 같은 열이나 같은 대각선에 있으면 안 됩니다. 네 별이 서로 가리지 않는 하늘을 찾으세요.",
        "6 □ 2 □ 3 □ 4 = 28. 연산 기호를 골라 왼쪽부터 차례로 계산하세요. 일반적인 곱셈 우선순위는 없고, 나눗셈은 나누어떨어져야 합니다.",
        "세 빛의 원반을 모두 세 번째 기둥으로 옮기세요. 출발 기둥과 도착 기둥을 차례로 누릅니다. 한 번에 맨 위 하나만, 큰 원반은 작은 원반 위에 놓을 수 없습니다.",
        "눈금 없는 5L 항아리와 3L 항아리로 정확히 4L를 만드세요. 채우기, 비우기, 한쪽이 비거나 다른 쪽이 찰 때까지 붓기만 가능합니다.",
        "1·2·5·10분이 걸리는 네 여행자가 등불 하나로 다리를 건넙니다. 한 번에 한두 명, 등불을 꼭 가지고 이동합니다. 느린 사람의 시간만큼 걸립니다. 모두 17분 안에 건너세요.",
        "세 자리 암호는 0부터 5까지입니다. 네 관측 기록의 '제자리'와 '다른 자리' 개수가 모두 맞는 암호를 찾으세요. 기록에 없는 숫자를 임의로 제외하지 마세요."
    };
    public static readonly int[] SudokuGivens={1,0,0,4,0,4,1,0,2,0,0,3,0,3,2,0};
    public static readonly int[] SudokuTarget={1,2,3,4,3,4,1,2,2,1,4,3,4,3,2,1};
    public static readonly int[][] NonogramRows={new[]{2},new[]{4},new[]{2},new[]{1}};
    public static readonly int[][] NonogramColumns={new[]{1},new[]{4},new[]{3},new[]{1}};
    public static readonly string[] OperatorSymbols={"+","−","×","÷"};
    public static readonly int[] CrossingTimes={1,2,5,10};
    public static readonly int[][] CodeGuesses={new[]{1,2,3},new[]{4,0,1},new[]{5,3,4},new[]{0,1,3}};
    public static readonly int[] CodeExact={1,1,0,0},CodeMisplaced={0,0,2,0};
    static readonly int[] Embroidery={0,1,1,0,1,1,1,1,0,1,1,0,0,1,0,0};
    static readonly int[] Lengths={16,16,5,4,3,3,2,7,3};
    const string SaveKey="Aether.Horizons.v1";

    public int ActivePuzzle=-1;
    public bool[] Solved=new bool[9];
    public bool IsOpen=>ActivePuzzle>=0&&ActivePuzzle<9;
    public int SolvedCount { get {int count=0;foreach(bool value in Solved)if(value)count++;return count;} }
    public int[] Values=>IsOpen?boards[ActivePuzzle]:Array.Empty<int>();
    public int SelectedIndex=-1,Progress,Highlight=-1;
    public bool IsPreviewing;
    public string NearbyPrompt="";
    public string PuzzleTitle=>IsOpen?Titles[ActivePuzzle]:"수평선의 성소";
    public string PuzzleDescription=>IsOpen?Descriptions[ActivePuzzle]:"";

    int[][] boards;
    int nearest=-1;
    Material porcelain,slate,gold,ice,resting;
    readonly Renderer[] hearts=new Renderer[9];
    readonly Light[] lanterns=new Light[9];
    readonly List<Renderer>[] pieces=new List<Renderer>[9];
    readonly List<TextMesh>[] readouts=new List<TextMesh>[9];
    readonly List<Transform> facingLabels=new List<Transform>();
    readonly Vector3[] heartHomes=new Vector3[9];
    readonly LineRenderer[] rays=new LineRenderer[9];
    Camera labelCamera;

    void Awake()
    {
        I=this;InitializeBoards();
        porcelain=AetherWorld.Mat("Horizon · pearl stone",new Color(.79f,.82f,.88f));
        slate=AetherWorld.Mat("Horizon · indigo stone",new Color(.14f,.20f,.31f));
        gold=AetherWorld.Mat("Horizon · sunrise brass",new Color(1,.75f,.35f),.65f);
        ice=AetherWorld.Mat("Horizon · glacial light",new Color(.35f,.84f,1),.7f);
        resting=AetherWorld.Mat("Horizon · dormant silver",new Color(.34f,.42f,.56f));
        BuildStations();RefreshAll();
    }
    void Update()
    {
        var game=AetherGame.I;if(game==null)return;
        if(!game.ReducedMotion)for(int i=0;i<9;i++)if(hearts[i]!=null)
            hearts[i].transform.position=heartHomes[i]+Vector3.up*Mathf.Sin(Time.time*.8f+i)*.045f;
        if(game.Phase!=GamePhase.Playing||game.JournalOpen||game.MapOpen||game.PhotoMode)
        {nearest=-1;NearbyPrompt="";return;}
        if(!IsOpen){FindNearest();return;}
        NearbyPrompt="";if(game.IsSmokeMode)return;
        int count=ActivePuzzle<2?9:ActivePuzzle==2?4:ActivePuzzle==6?6:ActivePuzzle==7?5:Lengths[ActivePuzzle];
        int direction=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift)?-1:1;
        for(int i=0;i<count;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))DoAction(i,direction);
        if(IsOpen&&ActivePuzzle<2)
        {
            if(SelectedIndex<0)SelectedIndex=0;
            if(Input.GetKeyDown(KeyCode.LeftArrow))SelectedIndex=Mod(SelectedIndex-1,16);
            if(Input.GetKeyDown(KeyCode.RightArrow))SelectedIndex=Mod(SelectedIndex+1,16);
            if(Input.GetKeyDown(KeyCode.UpArrow))SelectedIndex=Mod(SelectedIndex-4,16);
            if(Input.GetKeyDown(KeyCode.DownArrow))SelectedIndex=Mod(SelectedIndex+4,16);
            if(Input.GetKeyDown(KeyCode.Space))DoAction(SelectedIndex,direction);
        }
        if(Input.GetKeyDown(KeyCode.H))game.Notify(GetHint(),14);
        if(Input.GetKeyDown(KeyCode.R))Replay();
    }
    void LateUpdate()
    {
        if(labelCamera==null||!labelCamera.isActiveAndEnabled)
        {
            var player=AetherGame.I==null?null:AetherGame.I.Player;
            labelCamera=player!=null&&player.View.isActiveAndEnabled?player.View:null;
            if(labelCamera==null)foreach(var camera in Camera.allCameras)
                if(camera.isActiveAndEnabled){labelCamera=camera;break;}
        }
        if(labelCamera==null)return;
        foreach(var label in facingLabels)
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
            &&(AetherExpansion.I==null||!AetherExpansion.I.IsOpen)&&(AetherFrontiers.I==null||!AetherFrontiers.I.IsOpen);
    }
    void FindNearest()
    {
        nearest=-1;NearbyPrompt="";if(!CanInteract()||IsOpen)return;
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
        if(!CanInteract()||IsOpen)return;FindNearest();if(nearest<0)return;
        if(Solved[nearest]){AetherGame.I.Notify("이 장치는 이미 수평선의 빛을 되찾았습니다.");return;}
        ActivePuzzle=nearest;SelectedIndex=ActivePuzzle<2?0:-1;NearbyPrompt="";UpdateReadout();
        AetherGame.I.RefreshCursor();AetherAudio.I?.Click();
    }
    public void Close()
    {
        ActivePuzzle=-1;SelectedIndex=-1;Progress=0;Highlight=-1;IsPreviewing=false;NearbyPrompt="";
        Save();AetherGame.I?.RefreshCursor();
    }
    public void Replay()
    {
        if(!CanInteract()||!IsOpen||Solved[ActivePuzzle])return;
        boards[ActivePuzzle]=InitialBoard(ActivePuzzle);SelectedIndex=ActivePuzzle<2?0:-1;
        UpdateReadout();RefreshStation(ActivePuzzle);Save();AetherAudio.I?.Click();
        AetherGame.I.Notify("장치를 처음 상태로 되돌렸습니다.",3);
    }
    public void DoAction(int index,int delta=1)
    {
        if(!CanInteract()||!IsOpen||Solved[ActivePuzzle]||delta==0)return;
        int puzzle=ActivePuzzle;int[] values=boards[puzzle];bool changed=false;string reason="";
        int max=puzzle==2?4:puzzle==6?6:puzzle==7?5:values.Length;
        if(index<0||index>=max)return;
        switch(puzzle)
        {
            case 0:
                SelectedIndex=index;
                if(SudokuGivens[index]!=0){reason="서리로 새겨진 숫자는 고정된 단서입니다.";break;}
                values[index]=Mod(values[index]+delta,5);changed=true;break;
            case 1:SelectedIndex=index;values[index]=1-values[index];changed=true;break;
            case 2:changed=TryFerry(values,index);if(!changed)reason="그 짐이 같은 강둑에 있는지, 남겨진 새와 씨앗이 안전한지 확인하세요.";break;
            case 3:values[index]=Mod(values[index]+delta,4);changed=true;break;
            case 4:values[index]=Mod(values[index]+delta,4);changed=true;break;
            case 5:
                if(SelectedIndex<0)
                {if(TopDisk(values,index)<0)reason="빈 기둥에는 옮길 원반이 없습니다.";else SelectedIndex=index;}
                else if(SelectedIndex==index)SelectedIndex=-1;
                else if(TryMoveTower(values,SelectedIndex,index)){SelectedIndex=-1;changed=true;}
                else reason="큰 원반은 작은 원반 위에 놓을 수 없습니다. 다른 기둥을 골라 주세요.";
                break;
            case 6:changed=JugAction(values,index);if(!changed)reason="이미 가득 차거나 비어 있어 물의 양이 바뀌지 않습니다.";break;
            case 7:
                if(index<4)
                {
                    if(values[index]!=values[4]){reason="등불과 같은 쪽에 있는 여행자를 고르세요.";break;}
                    int mask=values[6]^(1<<index);
                    if(CountBits(mask)>2){reason="다리에는 한 번에 두 명까지만 오를 수 있습니다.";break;}
                    values[6]=mask;changed=true;
                }
                else {changed=TryCrossBridge(values);if(!changed)reason="등불과 함께 건널 여행자 한 명 또는 두 명을 먼저 고르세요.";}
                break;
            case 8:values[index]=Mod(values[index]+delta,6);changed=true;break;
        }
        if(!string.IsNullOrEmpty(reason))AetherGame.I.Notify(reason,4);
        if(!changed){AetherAudio.I?.Click();return;}
        UpdateReadout();RefreshStation(puzzle);AetherAudio.I?.Chime(index%4);
        if(PuzzleSolved(puzzle,values))
        {
            Solved[puzzle]=true;RefreshStation(puzzle);Close();AetherAudio.I?.Solve();
            AetherGame.I.Notify(Titles[puzzle]+" · 수평선의 빛 복구 완료",6);AetherGame.I.OnHorizonSolved(puzzle);
        }
        else
        {
            Save();if(puzzle==7&&values[5]>17)AetherGame.I.Notify("17분을 넘겼습니다. R 또는 초기화로 다시 계획해 보세요. 가장 빠른 두 명이 등불을 나누어 운반합니다.",7);
        }
    }
    void UpdateReadout()
    {
        Progress=0;Highlight=-1;if(!IsOpen)return;var v=Values;
        switch(ActivePuzzle)
        {
            case 0:foreach(int n in v)if(n>0)Progress++;break;
            case 1:foreach(int n in v)Progress+=n;break;
            case 2:for(int i=0;i<4;i++)Progress+=v[i];Highlight=v[0];break;
            case 3:Progress=QueenConflicts(v);break;
            case 4:int result;if(EvaluateExpression(v,out result))Progress=result;break;
            case 5:foreach(int n in v)if(n==2)Progress++;break;
            case 6:Progress=v[0];break;
            case 7:Progress=v[5];Highlight=v[4];break;
            case 8:for(int i=0;i<4;i++)if(CodeClueMatches(v,i))Progress++;break;
        }
    }
    public string GetHint()
    {
        switch(ActivePuzzle)
        {
            case 0:return "완성된 네 행은 1·2·3·4 / 3·4·1·2 / 2·1·4·3 / 4·3·2·1입니다. 빈칸을 눌러 값을 바꾸세요. 방향키와 Space로도 모든 칸을 조작할 수 있습니다.";
            case 1:return "위부터 .##. / #### / .##. / .#.. 입니다. #은 채움, 점은 빈칸입니다. 두 번째 열은 4칸 모두 채워집니다.";
            case 2:return "새와 건너기 → 혼자 돌아오기 → 여우와 건너기 → 새와 돌아오기 → 씨앗과 건너기 → 혼자 돌아오기 → 새와 건너기. 짐을 다시 데려와야 길이 열립니다.";
            case 3:return "위 행부터 2·4·1·3번째 열에 별을 놓아 보세요. 좌우가 뒤집힌 3·1·4·2도 정답입니다.";
            case 4:return "왼쪽부터 +, ×, +를 놓으면 (6+2)×3+4=28입니다. −, +, ×도 왼쪽부터 계산하면 28이 됩니다.";
            case 5:return "초기 상태의 이동 순서는 1→3, 1→2, 3→2, 1→3, 2→1, 2→3, 1→3입니다. 가장 큰 원반을 옮길 공간을 먼저 만드세요.";
            case 6:return "5L 채우기 → 5L에서 3L로 붓기 → 3L 비우기 → 5L에서 3L로 붓기 → 5L 채우기 → 5L에서 3L로 붓기. 큰 항아리에 4L가 남습니다.";
            case 7:return "초기 상태에서 1·2 함께 건너기(2) → 1 돌아오기(1) → 5·10 함께 건너기(10) → 2 돌아오기(2) → 1·2 함께 건너기(2). 합계 17분입니다.";
            case 8:return "013의 기록으로 0·1·3을 제외합니다. 123에서는 2가 제자리, 401에서는 4가 제자리입니다. 534의 두 숫자는 자리가 다릅니다. 암호는 425입니다.";
            default:return "장치의 단서를 읽어 보세요. H는 자세한 힌트, R은 이 장치의 초기화입니다.";
        }
    }

    public static int[] InitialBoard(int puzzle)
    {
        if(puzzle<0||puzzle>=9)throw new ArgumentOutOfRangeException(nameof(puzzle));
        return puzzle==0?(int[])SudokuGivens.Clone():new int[Lengths[puzzle]];
    }
    void InitializeBoards(){Solved=new bool[9];boards=new int[9][];for(int i=0;i<9;i++)boards[i]=InitialBoard(i);}
    public void ResetJourney(bool resume)
    {
        ActivePuzzle=-1;SelectedIndex=-1;Progress=0;Highlight=-1;IsPreviewing=false;nearest=-1;NearbyPrompt="";
        InitializeBoards();SaveData data=resume?ReadSave():null;
        if(data!=null){Solved=(bool[])data.solved.Clone();for(int i=0;i<9;i++)boards[i]=(int[])data.boards[i].values.Clone();}
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
            for(int i=0;i<9;i++)if(data.boards[i]==null||!BoardValid(i,data.boards[i].values)||data.solved[i]!=PuzzleSolved(i,data.boards[i].values))return null;
            return data;
        }
        catch{return null;}
    }
    [Serializable]class Board{public int[] values;}
    [Serializable]class SaveData{public int version=1;public bool[] solved;public Board[] boards;}

    public static bool BoardValid(int puzzle,int[] values)
    {
        if(puzzle<0||puzzle>=9||values==null||values.Length!=Lengths[puzzle])return false;
        for(int i=0;i<values.Length;i++)
        {
            int max=puzzle==0?4:puzzle==1?1:puzzle==2?(i==4?100000:1):puzzle==3||puzzle==4?3:puzzle==5?2:puzzle==6?(i==0?5:3):puzzle==7?(i<5?1:i==5?1000:15):5;
            if(values[i]<0||values[i]>max)return false;
            if(puzzle==0&&SudokuGivens[i]>0&&values[i]!=SudokuGivens[i])return false;
        }
        if(puzzle==2&&(!FerrySafe(values)||values[0]!=(values[4]%2)))return false;
        if(puzzle==7)
        {
            if(CountBits(values[6])>2)return false;
            for(int i=0;i<4;i++)if((values[6]&(1<<i))!=0&&values[i]!=values[4])return false;
        }
        return true;
    }
    public static bool PuzzleSolved(int puzzle,int[] values)
    {
        if(!BoardValid(puzzle,values))return false;
        switch(puzzle)
        {
            case 0:
                for(int i=0;i<4;i++)
                {
                    int row=0,col=0,box=0;
                    for(int j=0;j<4;j++){row|=1<<values[i*4+j];col|=1<<values[j*4+i];int k=(i/2)*8+(i%2)*2+(j/2)*4+j%2;box|=1<<values[k];}
                    if(row!=30||col!=30||box!=30)return false;
                }
                return true;
            case 1:
                for(int i=0;i<4;i++)if(!LineMatches(values,i*4,1,NonogramRows[i])||!LineMatches(values,i,4,NonogramColumns[i]))return false;
                return true;
            case 2:for(int i=0;i<4;i++)if(values[i]!=1)return false;return true;
            case 3:return QueenConflicts(values)==0;
            case 4:int result;return EvaluateExpression(values,out result)&&result==28;
            case 5:return values[0]==2&&values[1]==2&&values[2]==2;
            case 6:return values[0]==4;
            case 7:for(int i=0;i<5;i++)if(values[i]!=1)return false;return values[5]<=17&&values[6]==0;
            case 8:for(int i=0;i<4;i++)if(!CodeClueMatches(values,i))return false;return true;
            default:return false;
        }
    }
    static bool LineMatches(int[] values,int start,int stride,int[] expected)
    {
        var runs=new List<int>();int run=0;
        for(int i=0;i<4;i++){if(values[start+i*stride]!=0)run++;else if(run>0){runs.Add(run);run=0;}}
        if(run>0)runs.Add(run);return Equal(runs.ToArray(),expected);
    }
    public static int QueenConflicts(int[] values)
    {
        if(values==null||values.Length!=4)return -1;int count=0;
        for(int i=0;i<4;i++)for(int j=i+1;j<4;j++)if(values[i]==values[j]||Math.Abs(values[i]-values[j])==j-i)count++;
        return count;
    }
    public static bool EvaluateExpression(int[] values,out int result)
    {
        result=6;if(values==null||values.Length!=3)return false;int[] numbers={2,3,4};
        for(int i=0;i<3;i++)switch(values[i])
        {
            case 0:result+=numbers[i];break;case 1:result-=numbers[i];break;case 2:result*=numbers[i];break;
            case 3:if(result%numbers[i]!=0)return false;result/=numbers[i];break;default:return false;
        }
        return true;
    }
    public static bool FerrySafe(int[] values)
    {
        return values!=null&&values.Length==5&&!(values[1]==values[2]&&values[0]!=values[1])&&!(values[2]==values[3]&&values[0]!=values[2]);
    }
    public static bool TryFerry(int[] values,int cargo)
    {
        if(!BoardValid(2,values)||cargo<0||cargo>3||(cargo>0&&values[cargo]!=values[0])||values[4]>=100000)return false;
        int previous=values[0];values[0]=1-previous;if(cargo>0)values[cargo]=1-previous;
        if(!FerrySafe(values)){values[0]=previous;if(cargo>0)values[cargo]=previous;return false;}
        values[4]++;return true;
    }
    public static int TopDisk(int[] values,int pillar)
    {if(values==null||values.Length!=3||pillar<0||pillar>2)return -1;for(int i=0;i<3;i++)if(values[i]==pillar)return i;return -1;}
    public static bool TryMoveTower(int[] values,int from,int to)
    {
        if(!BoardValid(5,values)||from<0||from>2||to<0||to>2||from==to)return false;
        int top=TopDisk(values,from),destination=TopDisk(values,to);
        if(top<0||(destination>=0&&top>destination))return false;values[top]=to;return true;
    }
    public static bool JugAction(int[] values,int action)
    {
        if(!BoardValid(6,values)||action<0||action>5)return false;int a=values[0],b=values[1],amount;
        switch(action)
        {
            case 0:values[0]=5;break;case 1:values[1]=3;break;case 2:values[0]=0;break;case 3:values[1]=0;break;
            case 4:amount=Math.Min(values[0],3-values[1]);values[0]-=amount;values[1]+=amount;break;
            case 5:amount=Math.Min(values[1],5-values[0]);values[1]-=amount;values[0]+=amount;break;
        }
        return a!=values[0]||b!=values[1];
    }
    public static bool TryCrossBridge(int[] values)
    {
        if(!BoardValid(7,values))return false;int mask=values[6],count=CountBits(mask),duration=0;
        if(count<1||count>2)return false;
        for(int i=0;i<4;i++)if((mask&(1<<i))!=0)duration=Math.Max(duration,CrossingTimes[i]);
        if(values[5]+duration>1000)return false;
        for(int i=0;i<4;i++)if((mask&(1<<i))!=0)values[i]=1-values[i];
        values[4]=1-values[4];values[5]+=duration;values[6]=0;return true;
    }
    public static void CodeFeedback(int[] code,int[] guess,out int exact,out int misplaced)
    {
        exact=misplaced=0;if(code==null||guess==null||code.Length!=3||guess.Length!=3)return;
        int[] a=new int[6],b=new int[6];
        for(int i=0;i<3;i++)
        {if(code[i]<0||code[i]>5||guess[i]<0||guess[i]>5){exact=misplaced=-1;return;}if(code[i]==guess[i])exact++;a[code[i]]++;b[guess[i]]++;}
        for(int i=0;i<6;i++)misplaced+=Math.Min(a[i],b[i]);misplaced-=exact;
    }
    public static bool CodeClueMatches(int[] code,int clue)
    {
        if(clue<0||clue>=4)return false;int exact,misplaced;CodeFeedback(code,CodeGuesses[clue],out exact,out misplaced);
        return exact==CodeExact[clue]&&misplaced==CodeMisplaced[clue];
    }
    static int Mod(int value,int modulo)=>(value%modulo+modulo)%modulo;
    static int CountBits(int value){int n=0;while(value>0){n+=value&1;value>>=1;}return n;}
    static bool Equal(int[] a,int[] b){if(a==null||b==null||a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}

    void BuildStations()
    {
        string[] subtitles={"FOUR SIGNS / FOUR SEASONS","THE PATTERN UNDER ICE","EVERY PASSENGER MATTERS","STARS WITHOUT SHADOWS","READ FROM LEFT TO RIGHT","ONE LIGHT AT A TIME","MEASURE THE UNMEASURED","ONE LANTERN / SEVENTEEN MINUTES","FOUR RECORDS / ONE ANSWER"};
        for(int puzzle=0;puzzle<9;puzzle++)
        {
            pieces[puzzle]=new List<Renderer>();readouts[puzzle]=new List<TextMesh>();Vector3 p=Positions[puzzle];
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[puzzle]+" · foundation",p+Vector3.up*.13f,new Vector3(2.9f,.13f,2.9f),slate);
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[puzzle]+" · table",p+Vector3.up*.63f,new Vector3(2.3f,.35f,2.3f),porcelain);
            AetherWorld.Shape(PrimitiveType.Cylinder,"Horizon · rim",p+Vector3.up*.99f,new Vector3(2.22f,.018f,2.22f),gold,null,false);
            var heart=AetherWorld.Shape(PrimitiveType.Sphere,"Horizon heart "+(puzzle+19),p+new Vector3(0,2.12f,.48f),Vector3.one*.26f,resting,null,false);
            hearts[puzzle]=heart.GetComponent<Renderer>();heartHomes[puzzle]=heart.transform.position;
            lanterns[puzzle]=heart.AddComponent<Light>();lanterns[puzzle].color=new Color(.55f,.8f,1);lanterns[puzzle].range=4.5f;lanterns[puzzle].intensity=.2f;
            rays[puzzle]=Line("Horizon ray "+puzzle,new[]{p+Vector3.up*2.12f,p+Vector3.up*10},ice,.035f);rays[puzzle].gameObject.SetActive(false);
            Label((puzzle+19).ToString("00"),p+new Vector3(0,.62f,-1.16f),.15f,true);
            Label(subtitles[puzzle],p+new Vector3(0,.88f,-1.17f),.043f,true);
            if(puzzle==0||puzzle==1||puzzle==3)
            {
                for(int i=0;i<16;i++)
                {
                    Vector3 cell=p+new Vector3((i%4-1.5f)*.37f,1.05f,(i/4-1.5f)*.37f);
                    var tile=Piece(puzzle,PrimitiveType.Cube,"Logic tile "+i,cell,new Vector3(.33f,.055f,.33f),slate);
                    if(puzzle==0)
                    {var label=Label("",cell+Vector3.up*.034f,.065f,false);label.transform.rotation=Quaternion.Euler(90,0,0);readouts[puzzle].Add(label);}
                }
                if(puzzle==3)for(int i=0;i<4;i++)Piece(puzzle,PrimitiveType.Sphere,"Quiet star "+i,p+Vector3.up*1.2f,Vector3.one*.20f,gold);
                if(puzzle==0)
                {
                    Line("Sudoku box border X",new[]{p+new Vector3(-.77f,1.09f,0),p+new Vector3(.77f,1.09f,0)},gold,.035f);
                    Line("Sudoku box border Z",new[]{p+new Vector3(0,1.09f,-.77f),p+new Vector3(0,1.09f,.77f)},gold,.035f);
                }
            }
            else if(puzzle==2)
            {
                AetherWorld.Shape(PrimitiveType.Cube,"Ferry miniature river",p+Vector3.up*1.03f,new Vector3(.48f,.05f,1.3f),ice,null,false);
                for(int i=0;i<4;i++)Piece(puzzle,i==3?PrimitiveType.Cube:PrimitiveType.Sphere,"Ferry passenger "+i,p+Vector3.up*1.2f,Vector3.one*(i==0?.16f:.21f),i==0?gold:i==2?ice:slate);
                Piece(puzzle,PrimitiveType.Cube,"Miniature ferry",p+Vector3.up*1.1f,new Vector3(.30f,.08f,.56f),gold);
            }
            else if(puzzle==4)
            {
                for(int i=0;i<3;i++)
                {Piece(puzzle,PrimitiveType.Cube,"Arithmetic dial "+i,p+new Vector3((i-1)*.54f,1.15f,0),new Vector3(.40f,.16f,.44f),slate);readouts[puzzle].Add(Label("+",p+new Vector3((i-1)*.54f,1.46f,0),.16f,true));}
                Label("6    2    3    4   =   28",p+new Vector3(0,1.78f,.1f),.057f,true);
            }
            else if(puzzle==5)
            {
                for(int i=0;i<3;i++)AetherWorld.Shape(PrimitiveType.Cylinder,"Tower spindle "+i,p+new Vector3((i-1)*.65f,1.43f,0),new Vector3(.055f,.43f,.055f),gold,null,false);
                for(int i=0;i<3;i++)Piece(puzzle,PrimitiveType.Cylinder,"Light disk "+i,p+Vector3.up*1.1f,new Vector3(.26f+i*.14f,.055f,.26f+i*.14f),i==0?ice:i==1?gold:slate);
            }
            else if(puzzle==6)
            {
                for(int i=0;i<2;i++)
                {
                    float x=(i-.5f)*.85f;
                    AetherWorld.Shape(PrimitiveType.Cylinder,"Jug pedestal "+i,p+new Vector3(x,1.06f,0),new Vector3(.59f,.07f,.59f),slate,null,false);
                    Piece(puzzle,PrimitiveType.Cylinder,"Measured water "+i,p+new Vector3(x,1.14f,0),new Vector3(.45f,.07f,.45f),ice);
                    readouts[puzzle].Add(Label("0",p+new Vector3(x,1.9f,0),.13f,true));
                }
            }
            else if(puzzle==7)
            {
                AetherWorld.Shape(PrimitiveType.Cube,"Crossing bridge model",p+new Vector3(0,1.08f,0),new Vector3(1.4f,.07f,.22f),gold,null,false);
                for(int i=0;i<4;i++)Piece(puzzle,PrimitiveType.Capsule,"Traveler "+CrossingTimes[i],p+Vector3.up*1.2f,new Vector3(.13f,.14f,.13f),i<2?ice:slate);
                Piece(puzzle,PrimitiveType.Sphere,"Travelers' lantern",p+Vector3.up*1.4f,Vector3.one*.12f,gold);
                readouts[puzzle].Add(Label("0 / 17",p+new Vector3(0,1.85f,0),.10f,true));
            }
            else if(puzzle==8)
            {
                for(int i=0;i<3;i++)
                {Piece(puzzle,PrimitiveType.Cube,"Observation tumbler "+i,p+new Vector3((i-1)*.55f,1.22f,0),Vector3.one*.39f,slate);readouts[puzzle].Add(Label("0",p+new Vector3((i-1)*.55f,1.53f,0),.13f,true));}
            }
        }
    }
    Renderer Piece(int puzzle,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
    {var renderer=AetherWorld.Shape(type,name,position,scale,material,null,false).GetComponent<Renderer>();pieces[puzzle].Add(renderer);return renderer;}
    void RefreshAll(){for(int i=0;i<9;i++)RefreshStation(i);}
    void RefreshStation(int puzzle)
    {
        if(hearts[puzzle]==null)return;var v=boards[puzzle];Vector3 p=Positions[puzzle];
        hearts[puzzle].sharedMaterial=Solved[puzzle]?gold:ice;lanterns[puzzle].intensity=Solved[puzzle]?1:.23f;rays[puzzle].gameObject.SetActive(Solved[puzzle]);
        if(AetherWorld.BeaconTops!=null&&AetherWorld.BeaconTops.Length>puzzle+18&&AetherWorld.BeaconTops[puzzle+18]!=null)
        {
            var beacon=AetherWorld.BeaconTops[puzzle+18].GetComponent<Renderer>();
            if(beacon!=null)beacon.sharedMaterial=Solved[puzzle]?gold:resting;
        }
        if(puzzle==0)
        {
            for(int i=0;i<16;i++){pieces[puzzle][i].sharedMaterial=SudokuGivens[i]>0?slate:porcelain;readouts[puzzle][i].text=v[i]==0?"·":v[i].ToString();readouts[puzzle][i].color=SudokuGivens[i]>0?new Color(.85f,.94f,1):new Color(.10f,.22f,.35f);}
        }
        else if(puzzle==1)for(int i=0;i<16;i++)pieces[puzzle][i].sharedMaterial=v[i]==1?ice:slate;
        else if(puzzle==2)
        {
            for(int i=0;i<4;i++)pieces[puzzle][i].transform.position=p+new Vector3(v[i]==0?-.64f:.64f,1.19f,(i-1.5f)*.31f);
            pieces[puzzle][4].transform.position=p+new Vector3(v[0]==0?-.24f:.24f,1.1f,0);
        }
        else if(puzzle==3)for(int i=0;i<4;i++)pieces[puzzle][16+i].transform.position=p+new Vector3((v[i]-1.5f)*.37f,1.20f,(i-1.5f)*.37f);
        else if(puzzle==4)for(int i=0;i<3;i++)readouts[puzzle][i].text=OperatorSymbols[v[i]];
        else if(puzzle==5)
        {
            for(int i=0;i<3;i++){int below=0;for(int j=i+1;j<3;j++)if(v[j]==v[i])below++;pieces[puzzle][i].transform.position=p+new Vector3((v[i]-1)*.65f,1.10f+below*.12f,0);}
        }
        else if(puzzle==6)
        {
            for(int i=0;i<2;i++){float height=.03f+v[i]*.12f;pieces[puzzle][i].transform.localScale=new Vector3(.45f,height*.5f,.45f);pieces[puzzle][i].transform.position=p+new Vector3((i-.5f)*.85f,1.13f+height*.5f,0);readouts[puzzle][i].text=v[i]+" / "+(i==0?5:3);}
        }
        else if(puzzle==7)
        {
            for(int i=0;i<4;i++)pieces[puzzle][i].transform.position=p+new Vector3(v[i]==0?-.69f:.69f,1.20f,(i-1.5f)*.27f);
            pieces[puzzle][4].transform.position=p+new Vector3(v[4]==0?-.32f:.32f,1.43f,0);readouts[puzzle][0].text=v[5]+" / 17";
        }
        else if(puzzle==8)for(int i=0;i<3;i++)readouts[puzzle][i].text=v[i].ToString();
    }
    TextMesh Label(string text,Vector3 position,float size,bool facing)
    {
        var go=new GameObject("Horizon inscription · "+text);go.transform.position=position;
        var label=go.AddComponent<TextMesh>();label.text=text;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=48;label.characterSize=size;label.color=new Color(.13f,.23f,.35f);
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(font!=null){label.font=font;go.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
        if(facing)facingLabels.Add(go.transform);return label;
    }
    static LineRenderer Line(string name,Vector3[] points,Material material,float width)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=points.Length;line.SetPositions(points);line.widthMultiplier=width;line.numCapVertices=3;line.generateLightingData=true;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
    }

    public static List<string> RunSelfTests()
    {
        var checks=new List<string>();Action<bool,string> check=(ok,name)=>checks.Add((ok?"PASS ":"FAIL ")+"horizon "+name);
        int[][] targets={SudokuTarget,Embroidery,new[]{1,1,1,1,7},new[]{1,3,0,2},new[]{0,2,0},new[]{2,2,2},new[]{4,3},new[]{1,1,1,1,1,17,0},new[]{4,2,5}};
        for(int i=0;i<9;i++){check(BoardValid(i,InitialBoard(i))&&!PuzzleSolved(i,InitialBoard(i)),i+" starts valid and unfinished");check(PuzzleSolved(i,targets[i])&&!PuzzleSolved(i,null),i+" target and null guard");}
        var frost=(int[])SudokuTarget.Clone();frost[1]=1;check(!PuzzleSolved(0,frost),"sudoku duplicate rejected");frost[0]=2;check(!BoardValid(0,frost),"fixed sudoku clue cannot change");
        var art=(int[])Embroidery.Clone();art[0]=1;check(!PuzzleSolved(1,art),"nonogram wrong run rejected");
        var ferry=InitialBoard(2);check(!TryFerry(ferry,1)&&Equal(ferry,InitialBoard(2)),"unsafe ferry action is reversible without loss");
        foreach(int cargo in new[]{2,0,1,2,3,0,2})check(TryFerry(ferry,cargo)&&BoardValid(2,ferry),"safe ferry transition "+ferry[4]);check(PuzzleSolved(2,ferry),"ferry reaches other bank");
        check(PuzzleSolved(3,new[]{2,0,3,1})&&!PuzzleSolved(3,new[]{0,1,2,3}),"queens reflection accepted and diagonals rejected");
        int result;check(EvaluateExpression(new[]{1,0,2},out result)&&result==28&&!EvaluateExpression(new[]{0,3,0},out result),"left to right operators and integral division");
        var tower=InitialBoard(5);check(TryMoveTower(tower,0,2)&&!TryMoveTower(tower,0,2)&&Equal(tower,new[]{2,0,0}),"tower refuses large disk on small disk");
        int[,] moves={{0,1},{2,1},{0,2},{1,0},{1,2},{0,2}};for(int i=0;i<moves.GetLength(0);i++)check(TryMoveTower(tower,moves[i,0],moves[i,1]),"tower legal move "+i);check(PuzzleSolved(5,tower),"tower completes in seven moves");
        var jugs=InitialBoard(6);foreach(int action in new[]{0,4,3,4,0,4})check(JugAction(jugs,action)&&BoardValid(6,jugs),"jug transfer "+action);check(PuzzleSolved(6,jugs),"exact four litre measurement");
        var bridge=InitialBoard(7);foreach(int mask in new[]{3,1,12,2,3}){bridge[6]=mask;check(TryCrossBridge(bridge)&&BoardValid(7,bridge),"bridge legal crossing "+mask);}check(PuzzleSolved(7,bridge)&&bridge[5]==17,"bridge fastest cooperative solution");
        var late=new[]{1,1,1,1,1,19,0};check(!PuzzleSolved(7,late)&&BoardValid(7,late),"late bridge remains recoverable unfinished");
        int exact,misplaced;CodeFeedback(new[]{4,4,2},new[]{4,2,2},out exact,out misplaced);check(exact==2&&misplaced==0,"code duplicate counts are bounded");
        check(!BoardValid(2,new[]{1,0,0,0,1})&&!BoardValid(7,new[]{0,0,0,0,0,0,7}),"unsafe transport save states rejected");
        return checks;
    }
    public IEnumerator ValidateJourney(List<string> checks,string directory)
    {
        var game=AetherGame.I;
        if(game==null||!game.IsSmokeMode){Record(checks,false,"horizon validation requires isolated smoke save");yield break;}
        Directory.CreateDirectory(directory);foreach(string check in RunSelfTests())checks.Add(check);
        string[] areas={"frost conservatory","starlit cavern","dawn sanctuary"};
        for(int district=0;district<3;district++)
        {
            game.Player.Teleport(new Vector3((district-1)*43,.12f,-85.8f),180,-5);yield return new WaitForSeconds(.45f);
            Record(checks,game.Player.Body.isGrounded&&game.Player.transform.position.y>-.1f,areas[district]+" landing collision");
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,(20+district).ToString("00")+"-"+areas[district].Replace(' ','-')+".png"));yield return new WaitForSeconds(.25f);
        }
        for(int puzzle=0;puzzle<9;puzzle++)
        {
            game.Player.Teleport(Positions[puzzle]+new Vector3(0,.12f,-2.7f),0,0);yield return new WaitForSeconds(.4f);
            Record(checks,game.Player.Body.isGrounded&&game.Player.transform.position.y>-.1f,"horizon "+puzzle+" station ground collision");
            TryInteract();Record(checks,ActivePuzzle==puzzle&&!game.Player.CanMove,"horizon "+puzzle+" proximity opens and blocks walking");if(ActivePuzzle!=puzzle)continue;
            yield return new WaitForSeconds(.15f);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory,(23+puzzle).ToString("00")+"-horizon-puzzle-"+(puzzle+19)+".png"));
            yield return new WaitForSeconds(.25f);
            var original=(int[])Values.Clone();game.Pause();DoAction(puzzle==0?1:0);
            Record(checks,Equal(original,Values),"horizon "+puzzle+" pause prevents input");game.Resume();
            if(puzzle==0)DoAction(1);else if(puzzle==2)DoAction(2);else if(puzzle==5){DoAction(0);DoAction(2);}else DoAction(0);
            Record(checks,!Solved[puzzle]&&IsOpen&&!Equal(original,Values),"horizon "+puzzle+" partial action keeps puzzle unfinished");
            var partial=(int[])Values.Clone();Save();Close();ResetJourney(true);TryInteract();
            Record(checks,ActivePuzzle==puzzle&&Equal(partial,Values),"horizon "+puzzle+" partial board survives save and reopen");if(ActivePuzzle!=puzzle)continue;
            Replay();Record(checks,Equal(Values,InitialBoard(puzzle)),"horizon "+puzzle+" reset recovers initial state");
            if(puzzle==2){var before=(int[])Values.Clone();DoAction(1);Record(checks,Equal(before,Values),"horizon unsafe ferry move preserves passengers");}
            if(puzzle==7){var before=(int[])Values.Clone();DoAction(4);Record(checks,Equal(before,Values),"horizon empty bridge crossing rejected");}
            SolveByActions(puzzle);Record(checks,Solved[puzzle]&&!IsOpen,"horizon "+puzzle+" solved through actual actions");
        }
        Save();string completed=AetherSaveStore.GetString(SaveKey);ResetJourney(true);
        Record(checks,SolvedCount==9&&!IsOpen,"horizon nine completed boards survive resume");
        AetherSaveStore.SetString(SaveKey,"{\"version\":999}");ResetJourney(true);Record(checks,SolvedCount==0,"damaged horizon save resets only horizon boards");
        AetherSaveStore.SetString(SaveKey,completed);ResetJourney(true);
        Record(checks,SolvedCount==9&&!IsOpen&&game.Phase==GamePhase.Playing,"horizon validation leaves nine solved and playing");
    }
    void SolveByActions(int puzzle)
    {
        if(puzzle==0)SetByActions(SudokuTarget);
        else if(puzzle==1)SetByActions(Embroidery);
        else if(puzzle==2)foreach(int cargo in new[]{2,0,1,2,3,0,2})DoAction(cargo);
        else if(puzzle==3)SetByActions(new[]{1,3,0,2});
        else if(puzzle==4)SetByActions(new[]{0,2,0});
        else if(puzzle==5){int[,] moves={{0,2},{0,1},{2,1},{0,2},{1,0},{1,2},{0,2}};for(int i=0;i<moves.GetLength(0);i++){DoAction(moves[i,0]);DoAction(moves[i,1]);}}
        else if(puzzle==6)foreach(int action in new[]{0,4,3,4,0,4})DoAction(action);
        else if(puzzle==7)foreach(int mask in new[]{3,1,12,2,3}){for(int i=0;i<4;i++)if((mask&(1<<i))!=0)DoAction(i);DoAction(4);}
        else if(puzzle==8)SetByActions(new[]{4,2,5});
    }
    void SetByActions(int[] target)
    {for(int i=0;i<target.Length&&IsOpen;i++)for(int step=0;step<8&&IsOpen&&Values[i]!=target[i];step++)DoAction(i);}
    static void Record(List<string> checks,bool passed,string message)
    {checks.Add((passed?"PASS ":"FAIL ")+message);if(!passed)Debug.LogError("AETHER_HORIZONS: "+message);}
    void OnDestroy(){if(I==this)I=null;}
}

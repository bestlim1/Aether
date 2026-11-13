using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Six independent, persistent sanctums in the western and eastern gardens.</summary>
public sealed class AetherExpansion : MonoBehaviour
{
    public static AetherExpansion I;
    public static readonly Vector3[] Positions = {
        new Vector3(-46,0,-4), new Vector3(-36,0,5), new Vector3(-47,0,14),
        new Vector3(36,0,-4), new Vector3(47,0,5), new Vector3(39,0,15)
    };
    public int ActivePuzzle = -1;
    public bool[] Solved = new bool[6];
    public int[] Values => IsOpen ? boards[ActivePuzzle] : Array.Empty<int>();
    public int Progress;
    public int Highlight = -1;
    public int SelectedIndex = -1;
    public bool IsPreviewing;
    public bool IsOpen => ActivePuzzle >= 0 && ActivePuzzle < 6;
    public string NearbyPrompt = "";
    public int SolvedCount { get { int count = 0; foreach (bool solved in Solved) if (solved) count++; return count; } }
    public string PuzzleTitle => IsOpen ? Titles[ActivePuzzle] : "정원의 성소";
    public string PuzzleDescription => IsOpen ? Descriptions[ActivePuzzle] : "";

    static readonly string[] Titles = { "조각난 별자리", "다섯 빛의 정원", "긴 메아리의 노래", "기억의 저울", "시간의 서고", "새벽의 거울" };
    static readonly string[] Descriptions = {
        "빈칸 옆의 조각을 밀어, 1부터 8까지의 별을 차례로 이어 주세요.",
        "하나의 씨앗이 자신의 빛과 다음 빛을 바꿉니다. 다섯 개를 모두 깨워 주세요.",
        "여섯 번의 울림을 기억하고 같은 순서로 답하세요. 빛으로도 음을 읽을 수 있습니다.",
        "세 무게의 짝을 비교해 잊힌 값을 복원하세요. A+B=4 · B+C=5 · A+C=7.",
        "다섯 기록을 올바른 순서로 배치하세요. 두 기록을 차례로 누르면 자리를 바꿉니다.",
        "네 거울의 방향을 읽고, 새벽의 길을 연결하세요. 동쪽 · 서쪽 · 남쪽 · 북쪽."
    };
    static readonly int[] EchoTarget = { 0, 3, 1, 2, 0, 2 };
    static readonly int[] RuneTarget = { 4, 1, 3, 0, 2 };
    static readonly int[] MirrorTarget = { 1, 3, 2, 0 };
    static readonly int[] Lengths = { 9, 5, 6, 3, 5, 4 };
    const string SaveKey = "Aether.Expansion.v1";
    int[][] boards;
    int nearest = -1;
    Coroutine preview;
    Material stone, basalt, gold, cyan, dormant;
    readonly Renderer[] cores = new Renderer[6];
    readonly Light[] beacons = new Light[6];
    readonly List<Renderer>[] clues = new List<Renderer>[6];
    readonly List<Transform> moving = new List<Transform>();
    readonly List<Vector3> homes = new List<Vector3>();
    readonly LineRenderer[] rays = new LineRenderer[6];

    void Awake()
    {
        I = this;
        InitializeBoards();
        stone = AetherWorld.Mat("Expansion limestone", new Color(.76f,.78f,.64f));
        basalt = AetherWorld.Mat("Expansion basalt", new Color(.12f,.26f,.27f));
        gold = AetherWorld.Mat("Expansion Gold", new Color(1,.73f,.29f),1.1f);
        cyan = AetherWorld.Mat("Expansion light", new Color(.27f,.95f,.85f),1.1f);
        dormant = AetherWorld.Mat("Expansion sleeping lens", new Color(.23f,.36f,.35f));
        BuildStations();
        RefreshAll();
    }

    void Update()
    {
        AetherGame game = AetherGame.I;
        if (game == null) return;
        if (!game.ReducedMotion)
        {
            for (int i=0; i<moving.Count; i++)
            {
                moving[i].position = homes[i] + Vector3.up * Mathf.Sin(Time.time * 1.1f + i) * .06f;
                moving[i].Rotate(0, 11 * Time.deltaTime, 0, Space.World);
            }
        }
        if (game.Phase != GamePhase.Playing)
        {
            NearbyPrompt = ""; nearest = -1;
            if (IsPreviewing) StopPreview();
            return;
        }
        if (!IsOpen) { FindNearest(); return; }
        NearbyPrompt = "";
        int max = ActivePuzzle == 0 ? 9 : ActivePuzzle == 1 || ActivePuzzle == 4 ? 5 : ActivePuzzle == 3 ? 3 : 4;
        for (int i=0; i<max; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))
                DoAction(i, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
        if (Input.GetKeyDown(KeyCode.H)) game.Notify(GetHint(),12);
        if (Input.GetKeyDown(KeyCode.R) && ActivePuzzle == 2) Replay();
    }

    void FindNearest()
    {
        nearest = -1; NearbyPrompt = "";
        var game = AetherGame.I;
        if (game == null || game.Player == null || game.ActivePuzzle >= 0 || game.HasOpenSidePuzzle || game.JournalOpen || game.MapOpen || game.PhotoMode) return;
        float best = 3.8f;
        for (int i=0;i<Positions.Length;i++)
        {
            Vector3 target = Positions[i]+Vector3.up*1.2f;
            float distance = Vector3.Distance(game.Player.transform.position+Vector3.up,target);
            float facing = Vector3.Dot(game.Player.View.transform.forward,(target-game.Player.View.transform.position).normalized);
            if (distance < best && facing > -.05f) { nearest=i; best=distance; }
        }
        if (nearest >= 0) NearbyPrompt = Titles[nearest] + (Solved[nearest] ? " · 복구 완료" : " 살펴보기");
    }

    public void TryInteract()
    {
        var game = AetherGame.I;
        if (game == null || game.Phase != GamePhase.Playing || game.ActivePuzzle >= 0 || game.HasOpenSidePuzzle || game.JournalOpen || game.MapOpen || game.PhotoMode) return;
        FindNearest();
        if (nearest < 0) return;
        if (Solved[nearest]) { game.Notify("이 성소의 빛은 정원과 다시 이어졌습니다."); return; }
        ActivePuzzle = nearest;
        SelectedIndex = -1;
        Progress = EchoProgress(boards[2]);
        NearbyPrompt = "";
        game.RefreshCursor();
        AetherAudio.I?.Click();
        if (ActivePuzzle == 2 && Progress == 0) Replay();
    }

    public void Close()
    {
        StopPreview();
        ActivePuzzle = -1; SelectedIndex = -1; NearbyPrompt = "";
        AetherGame.I?.RefreshCursor();
        Save();
    }

    public void DoAction(int index, int delta = 1)
    {
        var game = AetherGame.I;
        if (game == null || game.Phase != GamePhase.Playing || !IsOpen || Solved[ActivePuzzle] || IsPreviewing) return;
        int puzzle = ActivePuzzle;
        int[] values = boards[puzzle];
        bool changed = false, solved = false;
        switch (puzzle)
        {
            case 0:
                changed = Slide(values,index);
                if (!changed) { game.Notify("빈칸과 상하좌우로 맞닿은 조각을 눌러 주세요.",2); return; }
                solved = SlidingSolved(values);
                break;
            case 1:
                if (index<0 || index>=5) return;
                int mask = 0; for (int i=0;i<5;i++) mask |= values[i]<<i;
                mask = CircuitToggle5(mask,index);
                for (int i=0;i<5;i++) values[i] = (mask>>i)&1;
                changed=true; solved=mask==31;
                break;
            case 2:
                if (index<0 || index>3) return;
                AetherAudio.I?.Chime(index);
                if (Progress>=EchoTarget.Length) return;
                if (index != EchoTarget[Progress])
                {
                    ClearSlots(values); Progress=0; Highlight=-1;
                    game.Notify("울림이 흩어졌습니다. 첫 음부터 다시 시작하거나 R로 다시 들어 보세요.",4);
                    Save(); return;
                }
                values[Progress++]=index; changed=true;
                solved=Progress==EchoTarget.Length;
                break;
            case 3:
                if (index<0 || index>=3) return;
                values[index]=Modulo(values[index]+delta,7);
                changed=true; solved=BalanceSolved(values);
                break;
            case 4:
                if (index<0 || index>=5) return;
                if (SelectedIndex<0) { SelectedIndex=index; AetherAudio.I?.Click(); return; }
                if (SelectedIndex==index) { SelectedIndex=-1; return; }
                int old=values[index]; values[index]=values[SelectedIndex]; values[SelectedIndex]=old;
                SelectedIndex=-1; changed=true; solved=RunesSolved(values);
                break;
            case 5:
                if (index<0 || index>=4) return;
                values[index]=Modulo(values[index]+delta,4);
                changed=true; solved=MirrorsSolved(values);
                break;
        }
        if (!changed) return;
        if (puzzle!=2) AetherAudio.I?.Chime(index%4);
        RefreshStation(puzzle);
        if (solved)
        {
            Solved[puzzle]=true;
            RefreshStation(puzzle);
            Close();
            game.Notify(Titles[puzzle]+" · 빛 복구 완료",7);
            AetherAudio.I?.Solve();
            game.OnExpansionSolved();
        }
        else Save();
    }

    public void Replay()
    {
        if (AetherGame.I == null || AetherGame.I.Phase != GamePhase.Playing || ActivePuzzle!=2 || Solved[2]) return;
        StopPreview(); ClearSlots(boards[2]); Progress=0;
        Save(); preview=StartCoroutine(PlayEcho());
    }

    IEnumerator PlayEcho()
    {
        IsPreviewing=true;
        AetherGame.I.Notify("긴 메아리에 귀를 기울이세요…",2);
        yield return new WaitForSeconds(.5f);
        for (int i=0;i<EchoTarget.Length;i++)
        {
            Highlight=EchoTarget[i];
            AetherAudio.I?.Chime(Highlight);
            AetherGame.I.Notify("울림 "+(i+1)+" / 6   ·   "+new[]{"I","II","III","IV"}[Highlight],.8f);
            yield return new WaitForSeconds(.62f);
            Highlight=-1;
            yield return new WaitForSeconds(.20f);
        }
        Highlight=-1; IsPreviewing=false; preview=null;
        AetherGame.I.Notify("당신의 차례입니다. 여섯 개의 음으로 답해 주세요.",4);
    }

    void StopPreview()
    {
        if (preview!=null) StopCoroutine(preview);
        preview=null; IsPreviewing=false; Highlight=-1;
    }

    public string GetHint()
    {
        switch (ActivePuzzle)
        {
            case 0: return "완성 모습은 1 2 3 / 4 5 6 / 7 8 빈칸. 윗줄부터 차근차근 맞추세요. 모든 시작 조각은 실제 이동으로 섞어 해결할 수 있습니다.";
            case 1:
                int mask=0; for(int i=0;i<5;i++) mask|=boards[1][i]<<i;
                for(int choice=1;choice<32;choice++)
                {
                    int trial=mask; var switches=new List<string>();
                    for(int i=0;i<5;i++) if((choice&(1<<i))!=0) { trial=CircuitToggle5(trial,i); switches.Add((i+1).ToString()); }
                    if(trial==31) return "현재 상태에서는 스위치 "+string.Join(" → ",switches)+"을 각각 한 번 누르면 모든 빛이 깨어납니다.";
                }
                return "다섯 빛을 모두 켜세요. 마지막 스위치는 V와 I을 함께 바꿉니다.";
            case 2: return "긴 메아리: I → IV → II → III → I → III. 숫자 1 → 4 → 2 → 3 → 1 → 3으로도 응답할 수 있습니다.";
            case 3: return "A+B=4와 A+C=7을 더하고 B+C=5를 빼면 2A=6. A=3, B=1, C=4입니다.";
            case 4: return "기록의 순서는 별 → 물결 → 태양 → 씨앗 → 산, 즉 V → II → IV → I → III. 두 칸을 차례로 눌러 기록을 교환하세요.";
            case 5: return "거울 I=동쪽(E), II=서쪽(W), III=남쪽(S), IV=북쪽(N). 숫자 표시로는 1 · 3 · 2 · 0입니다.";
            default: return "각 성소의 기록을 읽고 장치를 조작해 보세요.";
        }
    }

    public void ResetJourney(bool resume)
    {
        StopPreview(); ActivePuzzle=-1; SelectedIndex=-1; nearest=-1; NearbyPrompt="";
        InitializeBoards();
        SaveData saved=resume?ReadSave():null;
        if(saved!=null)
        {
            Solved=(bool[])saved.solved.Clone();
            for(int i=0;i<6;i++) boards[i]=(int[])saved.boards[i].values.Clone();
        }
        Progress=EchoProgress(boards[2]);
        RefreshAll();
        Save();
    }

    void InitializeBoards()
    {
        Solved=new bool[6];
        boards=new[] {
            CreateSlidingStart(),new[]{1,0,0,0,0},new[]{-1,-1,-1,-1,-1,-1},
            new[]{0,0,0},new[]{2,4,0,3,1},new[]{0,0,0,0}
        };
        Progress=0;
    }

    public void Save()
    {
        if (boards==null || AetherGame.I==null) return;
        var data=new SaveData { solved=(bool[])Solved.Clone(), boards=new Board[6] };
        for(int i=0;i<6;i++) data.boards[i]=new Board { values=(int[])boards[i].Clone() };
        AetherSaveStore.SetString(SaveKey,JsonUtility.ToJson(data));
    }

    SaveData ReadSave()
    {
        if(!AetherSaveStore.HasKey(SaveKey)) return null;
        try
        {
            var data=JsonUtility.FromJson<SaveData>(AetherSaveStore.GetString(SaveKey));
            if(data==null || data.version!=1 || data.solved==null || data.solved.Length!=6 || data.boards==null || data.boards.Length!=6) return null;
            for(int i=0;i<6;i++) if(data.boards[i]==null || data.boards[i].values==null || data.boards[i].values.Length!=Lengths[i]) return null;
            if(!Permutation(data.boards[0].values,9) || !SlidingReachable(data.boards[0].values)) return null;
            if(!Permutation(data.boards[4].values,5)) return null;
            int bits=0;
            foreach(int value in data.boards[1].values) { if(value<0 || value>1) return null; bits+=value; }
            if(bits%2!=1) return null;
            foreach(int value in data.boards[3].values) if(value<0 || value>6) return null;
            foreach(int value in data.boards[5].values) if(value<0 || value>3) return null;
            bool gap=false;
            for(int i=0;i<6;i++)
            {
                int value=data.boards[2].values[i];
                if(value==-1) gap=true;
                else if(gap || value!=EchoTarget[i]) return null;
            }
            bool[] achieved={SlidingSolved(data.boards[0].values),bits==5,EchoProgress(data.boards[2].values)==6,BalanceSolved(data.boards[3].values),RunesSolved(data.boards[4].values),MirrorsSolved(data.boards[5].values)};
            for(int i=0;i<6;i++) if(data.solved[i]!=achieved[i]) return null;
            return data;
        }
        catch { return null; }
    }

    [Serializable] class SaveData { public int version=1; public bool[] solved; public Board[] boards; }
    [Serializable] class Board { public int[] values; }

    public static bool SlidingSolved(int[] values)
    {
        if(values==null || values.Length!=9) return false;
        for(int i=0;i<8;i++) if(values[i]!=i+1) return false;
        return values[8]==0;
    }
    public static bool CanSlide(int[] values,int index)
    {
        if(values==null || values.Length!=9 || index<0 || index>=9 || values[index]==0) return false;
        int blank=Array.IndexOf(values,0);
        return blank>=0 && Math.Abs(blank/3-index/3)+Math.Abs(blank%3-index%3)==1;
    }
    public static bool Slide(int[] values,int index)
    {
        if(!CanSlide(values,index)) return false;
        int blank=Array.IndexOf(values,0); values[blank]=values[index]; values[index]=0; return true;
    }
    public static int[] CreateSlidingStart()
    {
        int[] values={1,2,3,4,5,6,7,8,0};
        var random=new System.Random(2631); int last=-1;
        for(int step=0;step<18;step++)
        {
            var choices=new List<int>();
            for(int i=0;i<9;i++) if(CanSlide(values,i) && i!=last) choices.Add(i);
            int blank=Array.IndexOf(values,0); Slide(values,choices[random.Next(choices.Count)]); last=blank;
        }
        if(SlidingSolved(values)) Slide(values,7);
        return values;
    }
    public static int CircuitToggle5(int mask,int index)
    {
        if(index<0 || index>=5) throw new ArgumentOutOfRangeException(nameof(index));
        return (mask^(1<<index)^(1<<((index+1)%5)))&31;
    }
    public static bool BalanceSolved(int[] values) => values!=null && values.Length==3 && values[0]==3 && values[1]==1 && values[2]==4;
    public static bool RunesSolved(int[] values) => Equal(values,RuneTarget);
    public static bool MirrorsSolved(int[] values) => Equal(values,MirrorTarget);
    static bool Equal(int[] a,int[] b) { if(a==null || a.Length!=b.Length) return false; for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) return false; return true; }
    static void ClearSlots(int[] values) { for(int i=0;i<values.Length;i++) values[i]=-1; }
    static int Modulo(int value,int modulus) => (value%modulus+modulus)%modulus;
    static int EchoProgress(int[] values) { int count=0; while(count<values.Length && values[count]>=0) count++; return count; }
    static bool Permutation(int[] values,int count)
    {
        if(values==null || values.Length!=count) return false;
        int mask=0;
        foreach(int value in values) { if(value<0 || value>=count || (mask&(1<<value))!=0) return false; mask|=1<<value; }
        return true;
    }
    static bool SlidingReachable(int[] values)
    {
        int inversions=0;
        for(int i=0;i<9;i++) for(int j=i+1;j<9;j++) if(values[i]!=0 && values[j]!=0 && values[i]>values[j]) inversions++;
        return inversions%2==0;
    }

    public static List<string> RunSelfTests()
    {
        var results=new List<string>();
        Action<bool,string> check=(ok,label)=>results.Add((ok?"PASS ":"FAIL ")+label);
        var start=CreateSlidingStart();
        check(!SlidingSolved(start)&&Permutation(start,9)&&SlidingReachable(start),"sliding scramble is valid and solvable");
        var goal=new[]{1,2,3,4,5,6,7,8,0};
        check(SlidingSolved(goal)&&!SlidingSolved(null),"sliding goal and null input");
        check(CanSlide(goal,7)&&CanSlide(goal,5)&&!CanSlide(goal,6)&&!CanSlide(goal,8)&&!CanSlide(goal,-1),"sliding orthogonal adjacency");
        check(Slide(goal,7)&&Slide(goal,8)&&SlidingSolved(goal),"sliding move can be reversed");
        bool transitions=true;
        for(int mask=0;mask<32;mask++) for(int i=0;i<5;i++)
            transitions&=CircuitToggle5(CircuitToggle5(mask,i),i)==mask;
        check(transitions,"five-light switch reversibility over all states");
        check(CircuitToggle5(CircuitToggle5(1,1),3)==31,"five lights reachable from the initial state");
        check(BalanceSolved(new[]{3,1,4})&&!BalanceSolved(new[]{3,2,4})&&!BalanceSolved(null),"balance target and wrong values");
        check(RunesSolved(new[]{4,1,3,0,2})&&!RunesSolved(new[]{2,4,0,3,1})&&!RunesSolved(null),"rune ordering");
        check(MirrorsSolved(new[]{1,3,2,0})&&!MirrorsSolved(new[]{0,0,0,0})&&!MirrorsSolved(null),"mirror directions");
        return results;
    }

    void BuildStations()
    {
        for(int i=0;i<6;i++)
        {
            clues[i]=new List<Renderer>();
            Vector3 p=Positions[i];
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[i]+" · stepped footing",p+Vector3.up*.16f,new Vector3(3.0f,.16f,3.0f),basalt);
            AetherWorld.Shape(PrimitiveType.Cylinder,Titles[i]+" · carved console",p+Vector3.up*.62f,new Vector3(2.15f,.36f,2.15f),stone);
            var core=AetherWorld.Shape(PrimitiveType.Sphere,"Sanctum "+(i+4)+" · sleeping heart",p+Vector3.up*2.1f,Vector3.one*.35f,dormant,null,false);
            cores[i]=core.GetComponent<Renderer>(); beacons[i]=core.AddComponent<Light>();
            beacons[i].color=new Color(.37f,1,.88f); beacons[i].range=5; beacons[i].intensity=.35f;
            moving.Add(core.transform); homes.Add(core.transform.position);
            Label("0"+(i+4),p+new Vector3(0,.67f,-1.11f),.13f);
            rays[i]=MakeRay("Sanctum "+(i+4)+" · restored ray",new[]{p+Vector3.up*2.1f,p+Vector3.up*12},cyan,.055f);
            rays[i].gameObject.SetActive(false);
            if(i==0)
            {
                for(int n=0;n<9;n++)
                {
                    Vector3 cell=p+new Vector3((n%3-1)*.39f,1.13f,(n/3-1)*.39f);
                    var tile=AetherWorld.Shape(PrimitiveType.Cube,"Star tile "+n,cell,new Vector3(.35f,.15f,.35f),gold,null,false);
                    clues[i].Add(tile.GetComponent<Renderer>());
                }
                Label("1 2 3 / 4 5 6 / 7 8 _",p+new Vector3(0,.91f,-1.15f),.065f);
            }
            else if(i==1)
            {
                for(int n=0;n<5;n++)
                {
                    float angle=n*Mathf.PI*2/5;
                    var lens=AetherWorld.Shape(PrimitiveType.Sphere,"Five-light lens "+n,p+new Vector3(Mathf.Sin(angle)*.68f,1.18f,Mathf.Cos(angle)*.68f),Vector3.one*.24f,dormant,null,false);
                    clues[i].Add(lens.GetComponent<Renderer>());
                }
            }
            else if(i==2)
            {
                for(int n=0;n<4;n++)
                {
                    var chime=AetherWorld.Shape(PrimitiveType.Cube,"Echo prism "+n,p+new Vector3((n-1.5f)*.37f,1.37f,0),new Vector3(.18f,.44f+n*.13f,.18f),cyan,null,false);
                    chime.transform.rotation=Quaternion.Euler(0,45,12);
                    clues[i].Add(chime.GetComponent<Renderer>());
                }
                Label("I  IV  II  III  I  III",p+new Vector3(0,.91f,-1.15f),.06f);
            }
            else if(i==3)
            {
                for(int n=0;n<3;n++)
                {
                    var weight=AetherWorld.Shape(PrimitiveType.Cylinder,"Memory weight "+n,p+new Vector3((n-1)*.57f,1.20f,0),new Vector3(.34f,.16f,.34f),gold,null,false);
                    clues[i].Add(weight.GetComponent<Renderer>());
                }
                Label("A+B=4 / B+C=5 / A+C=7",p+new Vector3(0,.91f,-1.15f),.06f);
            }
            else if(i==4)
            {
                for(int n=0;n<5;n++)
                {
                    var page=AetherWorld.Shape(PrimitiveType.Cube,"Archive tablet "+n,p+new Vector3((n-2)*.33f,1.23f,0),new Vector3(.25f,.43f,.13f),gold,null,false);
                    clues[i].Add(page.GetComponent<Renderer>());
                }
                Label("V  >  II  >  IV  >  I  >  III",p+new Vector3(0,.91f,-1.15f),.065f);
            }
            else
            {
                for(int n=0;n<4;n++)
                {
                    Vector3 point=p+new Vector3((n%2-.5f)*.83f,1.24f,(n/2-.5f)*.67f);
                    var mirror=AetherWorld.Shape(PrimitiveType.Cube,"Directional mirror "+n,point,new Vector3(.35f,.39f,.055f),cyan,null,false);
                    clues[i].Add(mirror.GetComponent<Renderer>());
                }
                MakeRay("Mirror pair · first",new[]{p+new Vector3(-.42f,1.17f,-.34f),p+new Vector3(.42f,1.17f,-.34f)},gold,.018f);
                MakeRay("Mirror pair · second",new[]{p+new Vector3(-.42f,1.17f,.34f),p+new Vector3(.42f,1.17f,.34f)},gold,.018f);
                Label("E  >  W  >  S  >  N",p+new Vector3(0,.91f,-1.15f),.07f);
            }
        }
    }

    void RefreshAll() { for(int i=0;i<6;i++) RefreshStation(i); }
    void RefreshStation(int puzzle)
    {
        if(cores[puzzle]==null) return;
        cores[puzzle].sharedMaterial=Solved[puzzle]?cyan:dormant;
        beacons[puzzle].intensity=Solved[puzzle]?2.2f:.35f;
        rays[puzzle].gameObject.SetActive(Solved[puzzle]);
        int beaconIndex=puzzle+3;
        if(AetherWorld.BeaconTops!=null && AetherWorld.BeaconTops.Length>beaconIndex && AetherWorld.BeaconTops[beaconIndex]!=null)
        {
            var districtBeacon=AetherWorld.BeaconTops[beaconIndex].GetComponent<Renderer>();
            if(districtBeacon!=null) districtBeacon.sharedMaterial=Solved[puzzle]?cyan:dormant;
        }
        for(int n=0;n<clues[puzzle].Count;n++)
        {
            Renderer clue=clues[puzzle][n];
            if(puzzle==0) clue.enabled=boards[0][n]!=0;
            else if(puzzle==1) clue.sharedMaterial=boards[1][n]==1?gold:dormant;
            else if(puzzle==3) clue.transform.localScale=new Vector3(.34f,.10f+boards[3][n]*.055f,.34f);
            else if(puzzle==4) clue.sharedMaterial=boards[4][n]==RuneTarget[n]?cyan:gold;
            else if(puzzle==5) clue.transform.rotation=Quaternion.Euler(0,boards[5][n]*90,0);
        }
    }
    static LineRenderer MakeRay(string name,Vector3[] points,Material material,float width)
    {
        var line=new GameObject(name).AddComponent<LineRenderer>(); line.sharedMaterial=material;
        line.generateLightingData=true;
        line.positionCount=points.Length; line.SetPositions(points); line.widthMultiplier=width;
        line.numCapVertices=3; line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; return line;
    }
    static void Label(string text,Vector3 point,float size)
    {
        var go=new GameObject("Inscription · "+text); go.transform.position=point;
        var label=go.AddComponent<TextMesh>(); label.text=text; label.anchor=TextAnchor.MiddleCenter;
        label.alignment=TextAlignment.Center; label.fontSize=48; label.characterSize=size;
        label.color=new Color(.23f,.34f,.31f);
        var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if(font!=null) { label.font=font; go.GetComponent<MeshRenderer>().sharedMaterial=font.material; }
    }
    void OnDestroy() { StopPreview(); if(I==this) I=null; }
}




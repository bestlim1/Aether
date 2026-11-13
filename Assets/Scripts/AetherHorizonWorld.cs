using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Three quiet destinations beyond the southern horizon. All ground and bridges are at y=0.</summary>
public static class AetherHorizonWorld
{
    static Transform region;
    static readonly System.Random random=new System.Random(927031);
    static Material stone,basalt,gold,pearl,ice,violet;
    public static readonly Vector3[] Centres={new Vector3(-43,0,-99),new Vector3(0,0,-99),new Vector3(43,0,-99)};
    static readonly Vector2[][] trials={
        new[]{new Vector2(-50,-105),new Vector2(-36,-99),new Vector2(-49,-89)},
        new[]{new Vector2(-8,-105),new Vector2(8,-98),new Vector2(0,-88)},
        new[]{new Vector2(36,-105),new Vector2(50,-99),new Vector2(40,-88)}
    };
    static readonly Vector2[][] memories={
        new[]{new Vector2(-55,-109),new Vector2(-31,-109),new Vector2(-54,-97),new Vector2(-31,-88),new Vector2(-55,-86)},
        new[]{new Vector2(-12,-110),new Vector2(12,-110),new Vector2(-12,-95),new Vector2(12,-86),new Vector2(-9,-86)},
        new[]{new Vector2(31,-110),new Vector2(55,-109),new Vector2(55,-94),new Vector2(31,-87),new Vector2(55,-86)}
    };

    public static void Build()
    {
        var root=new GameObject("BEYOND THE HORIZON · 수평선 너머의 정원").transform;
        stone=M("Horizon · limestone",new Color(.69f,.73f,.71f));
        basalt=M("Horizon · deep basalt",new Color(.09f,.15f,.23f));
        gold=M("Horizon · Pale Gold",new Color(.86f,.65f,.27f));
        pearl=M("Horizon · pearl",new Color(.85f,.90f,.86f));
        ice=M("Horizon · living ice",new Color(.38f,.78f,.97f),.65f);
        violet=M("Horizon · violet starlight",new Color(.58f,.38f,.91f),.80f);
        string[] names={"FROST CONSERVATORY · 서리의 온실","STARLIGHT GROTTO · 별빛의 동굴","DAWN SANCTUARY · 새벽의 성소"};
        for(int i=0;i<3;i++)
        {
            region=new GameObject(names[i]).transform;region.SetParent(root,false);
            BuildIsland(i);BuildBridge(i);BuildRoutes(i);
            if(i==0)FrostConservatory();else if(i==1)StarlightGrotto();else DawnSanctuary();
            GroundCover(i);OuterBoundary(i);Batch(region);
        }
    }

    static Material M(string name,Color color,float emission=0){return AetherWorld.Mat(name,color,emission);}
    static Vector3 P(float x,float z,float y=0){return new Vector3(x,y,z);}
    static float R(float min,float max){return min+(float)random.NextDouble()*(max-min);}
    static GameObject S(PrimitiveType type,string name,Vector3 p,Vector3 scale,Material mat,bool collision=false,Transform parent=null)
    {return AetherWorld.Shape(type,name,p,scale,mat,parent!=null?parent:region,collision);}

    static void BuildIsland(int index)
    {
        float x=Centres[index].x;
        var surface=M("Horizon · ground "+index,index==0?new Color(.64f,.74f,.79f):index==1?new Color(.20f,.20f,.32f):new Color(.43f,.46f,.31f));
        var edge=M("Horizon · cliff "+index,index==0?new Color(.35f,.48f,.60f):index==1?new Color(.18f,.16f,.30f):new Color(.41f,.36f,.26f));
        Vector2[] polygon={new Vector2(-13,-16),new Vector2(13,-16),new Vector2(15,-14),new Vector2(15,14),new Vector2(13,16),new Vector2(-13,16),new Vector2(-15,14),new Vector2(-15,-14)};
        var vertices=new List<Vector3>{P(x,-99)};var triangles=new List<int>();
        for(int i=0;i<polygon.Length;i++)vertices.Add(P(x+polygon[i].x,-99+polygon[i].y));
        for(int i=0;i<polygon.Length;i++){triangles.Add(0);triangles.Add((i+1)%polygon.Length+1);triangles.Add(i+1);}
        MeshObject("Horizon island · seamless walkable surface",vertices,triangles,surface,true);
        float[] levels={0,-.7f,-6.4f,-15};float[] scales={1,1.035f,.87f,.23f};
        for(int band=0;band<3;band++)for(int i=0;i<polygon.Length;i++)
        {
            int next=(i+1)%polygon.Length;
            var v=new List<Vector3>{P(x+polygon[i].x*scales[band],-99+polygon[i].y*scales[band],levels[band]),P(x+polygon[next].x*scales[band],-99+polygon[next].y*scales[band],levels[band]),P(x+polygon[next].x*scales[band+1],-99+polygon[next].y*scales[band+1],levels[band+1]),P(x+polygon[i].x*scales[band+1],-99+polygon[i].y*scales[band+1],levels[band+1])};
            MeshObject("Horizon island · layered cliff",v,new List<int>{0,1,2,0,2,3},band==0?edge:basalt,false);
        }
        for(int i=0;i<18;i++)
        {
            float a=i*Mathf.PI*2/18;
            Rock("Horizon island · fractured hanging stone",new Vector3(x+Mathf.Cos(a)*14.1f,R(-8,-3),-99+Mathf.Sin(a)*15),new Vector3(R(1.8f,3.5f),R(4,9),R(1.8f,3.5f)),edge,100+index*20+i);
        }
        if(index==0)
        {
            for(int i=0;i<13;i++)Rock("Frost shore · pendant icicle",P(x-11+i*1.8f,-114.8f,-1.7f),new Vector3(.30f,R(1.8f,4.6f),.43f),ice,150+i);
        }
    }

    static void BuildBridge(int index)
    {
        float x=Centres[index].x;const float north=-63,south=-83,length=20;
        var deck=M("Horizon · bridge paving "+index,index==0?new Color(.66f,.77f,.79f):index==1?new Color(.45f,.46f,.63f):new Color(.74f,.66f,.45f));
        Material accent=index==0?ice:index==1?violet:gold;
        S(PrimitiveType.Cube,"Horizon bridge · uninterrupted floor",P(x,-73,-.28f),new Vector3(4,.56f,length+.36f),deck,true);
        for(int i=0;i<14;i++)
        {
            float z=Mathf.Lerp(south,north,(i+.5f)/14);
            S(PrimitiveType.Cube,"Horizon bridge · paver",P(x,z,.022f),new Vector3(3.78f,.044f,length/14-.07f),i%4==0?pearl:deck);
            S(PrimitiveType.Cube,"Horizon bridge · luminous waymark",P(x,z,.048f),new Vector3(.05f,.012f,.36f),accent);
        }
        for(int side=-1;side<=1;side+=2)
        {
            S(PrimitiveType.Cube,"Horizon bridge · safety handrail",P(x+side*2.1f,-73,.91f),new Vector3(.15f,.13f,20.15f),deck,true);
            S(PrimitiveType.Cube,"Horizon bridge · low rail",P(x+side*2.1f,-73,.30f),new Vector3(.13f,.12f,20.15f),deck,true);
            S(PrimitiveType.Cube,"Horizon bridge · structural beam",P(x+side*2.1f,-73,-.64f),new Vector3(.40f,.35f,20.4f),basalt);
            for(int i=0;i<=8;i++)
            {
                Vector3 p=P(x+side*2.1f,south+i*2.5f);
                S(PrimitiveType.Cube,"Horizon bridge · carved baluster",p+Vector3.up*.52f,new Vector3(.28f,1.04f,.28f),deck,true);
                S(PrimitiveType.Cube,"Horizon bridge · baluster cap",p+Vector3.up*1.1f,new Vector3(.41f,.12f,.41f),accent);
            }
            Beam("Horizon bridge · flying brace",P(x+side*2.1f,-64,-4),P(x+side*2.1f,-72,-.55f),.30f,basalt);
            Beam("Horizon bridge · flying brace",P(x+side*2.1f,-82,-4),P(x+side*2.1f,-74,-.55f),.30f,basalt);
        }
    }

    static void BuildRoutes(int index)
    {
        float x=Centres[index].x;
        var paving=M("Horizon · pilgrim tiles "+index,index==0?new Color(.71f,.82f,.84f):index==1?new Color(.42f,.41f,.58f):new Color(.80f,.72f,.50f));
        Vector3 a=P(trials[index][0].x,trials[index][0].y),b=P(trials[index][1].x,trials[index][1].y),c=P(trials[index][2].x,trials[index][2].y);
        Path(P(x,-83),c,2.9f,paving);Path(c,a,2.65f,paving);Path(c,b,2.65f,paving);Path(a,b,2.5f,paving);
        Path(a,P(x,-108),2.45f,paving);Path(b,P(x,-108),2.45f,paving);
        Vector3[] walk={P(x-12,-112),P(x+12,-112),P(x+12,-86),P(x-12,-86)};
        for(int i=0;i<4;i++)Path(walk[i],walk[(i+1)%4],1.45f,paving);
        Path(P(x,-83),walk[2],1.65f,paving);Path(P(x,-83),walk[3],1.65f,paving);
        // Every trial and waystone reserves an unobstructed three metre radius at ground level.
        Material trim=index==0?ice:index==1?violet:gold;
        for(int i=0;i<3;i++)
        {
            Vector3 p=P(trials[index][i].x,trials[index][i].y,.076f);
            Ring("Horizon trial · outer floor inlay",p,3.23f,.048f,trim);
            Ring("Horizon trial · inner floor inlay",p,2.99f,.018f,paving);
        }
        Ring("Horizon waystone · arrival compass",P(x,-108,.070f),3.08f,.045f,trim);
    }

    static void FrostConservatory()
    {
        var snow=M("Frost · fresh snow",new Color(.85f,.91f,.93f));
        var bark=M("Frost · silver birch bark",new Color(.64f,.73f,.76f));
        var leaf=M("Frost · frozen canopy",new Color(.66f,.82f,.85f));
        var frame=M("Frost · silver conservatory frame",new Color(.51f,.66f,.75f));
        var glass=Glass("Frost · translucent ice panes",new Color(.45f,.77f,.90f,.14f));
        const float x=-43,z=-111;
        // Two pointed vaults and slender glazing distinguish this conservatory from the rain garden.
        for(int side=-1;side<=1;side+=2)
        {
            for(int j=0;j<3;j++)
            {
                Vector3 p=P(x+side*4.7f,z-2.2f+j*2.2f);
                Column(p,3.7f,.19f,frame,pearl);
            }
            S(PrimitiveType.Cube,"Frost conservatory · side glazing",P(x+side*4.7f,z,1.9f),new Vector3(.015f,3.5f,4.15f),glass);
            S(PrimitiveType.Cube,"Frost conservatory · side sill",P(x+side*4.7f,z,.21f),new Vector3(.25f,.42f,4.65f),frame,true);
            S(PrimitiveType.Cube,"Frost conservatory · growing bench",P(x+side*4.0f,z,.39f),new Vector3(.68f,.78f,3.2f),frame,true);
            for(int j=0;j<5;j++)Rock("Frost conservatory · cultivated ice flower",P(x+side*4.0f,z-1.15f+j*.57f,1.11f),new Vector3(.22f,.61f,.25f),ice,200+j);
        }
        for(int depth=0;depth<3;depth++)
        {
            float zz=z-2.2f+depth*2.2f;
            Vector3 left=P(x-4.7f,zz,3.7f),right=P(x+4.7f,zz,3.7f),peak=P(x,zz,7.4f);
            Beam("Frost conservatory · pointed silver rib",left,peak,.095f,frame);
            Beam("Frost conservatory · pointed silver rib",right,peak,.095f,frame);
        }
        for(int side=-1;side<=1;side+=2)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Quad(v,t,P(x+side*4.7f,z-2.15f,3.7f),P(x+side*4.7f,z+2.15f,3.7f),P(x,z+2.15f,7.4f),P(x,z-2.15f,7.4f),true);
            MeshObject("Frost conservatory · ice roof",v,t,glass,false);
            Beam("Frost conservatory · snow-covered roof bar",P(x+side*2.35f,z-2.3f,5.55f),P(x+side*2.35f,z+2.3f,5.55f),.15f,snow);
        }
        Beam("Frost conservatory · ridge",P(x,z-2.35f,7.45f),P(x,z+2.35f,7.45f),.13f,pearl);
        // The front stays entirely open, including the reserved travel-stone approach.
        Vector3[] trees={P(-54,-103),P(-53.3f,-92.6f),P(-32.5f,-93),P(-40,-87.8f),P(-43,-96),P(-39,-103),P(-51,-111),P(-34,-111)};
        for(int i=0;i<trees.Length;i++)WhiteTree(trees[i],i%3==0?1.08f:.87f,bark,leaf,snow,i);
        Vector3[] crystalBeds={P(-33.4f,-103.5f),P(-45,-92.4f),P(-53.4f,-100.0f),P(-38.8f,-111.5f)};
        for(int i=0;i<crystalBeds.Length;i++)
        {
            Ring("Frost garden · crystalline bed",crystalBeds[i]+Vector3.up*.03f,.92f,.10f,frame);
            for(int j=0;j<4;j++)
            {
                float a=j*Mathf.PI*.5f;
                Rock("Frost garden · faceted ice spire",crystalBeds[i]+new Vector3(Mathf.Cos(a)*.39f,.56f,Mathf.Sin(a)*.39f),new Vector3(.33f,1.23f+j*.21f,.35f),ice,250+i*4+j);
            }
        }
        for(int i=0;i<33;i++)
        {
            Vector3 p=P(R(-54.3f,-31.7f),R(-112,-86));
            if(Reserved(p,0,1.1f)||OnRoute(p,0,1.7f))continue;
            Rock("Frost garden · soft snow drift",p+Vector3.up*.04f,new Vector3(R(.8f,2),R(.10f,.24f),R(.7f,1.65f)),snow,300+i);
        }
        Snow(P(-43,-99,8.5f),new Vector3(25,.5f,27));
        Lantern(P(-43,-85.7f),ice);Lantern(P(-40.2f,-99.7f),ice);Lantern(P(-52.0f,-95.5f),ice);
        RegionalLight(P(-43,-102,5),new Color(.45f,.69f,1),.45f,22);
    }

    static void StarlightGrotto()
    {
        var rock=M("Grotto · violet slate",new Color(.23f,.20f,.34f));
        var litRock=M("Grotto · crystal-veined rock",new Color(.31f,.28f,.45f));
        var cyan=M("Grotto · luminous aquamarine",new Color(.28f,.75f,.94f),1.1f);
        var amethyst=M("Grotto · amethyst",new Color(.68f,.34f,.90f),.95f);
        var deepPool=M("Grotto · still midnight pool",new Color(.07f,.14f,.29f),.16f);
        // A low open grotto, not a roof over the whole island: approach, side paths and skyline stay open.
        Rock("Grotto · west vault support",P(-5.6f,-111,2.0f),new Vector3(2.2f,4.0f,3.4f),rock,400,true);
        Rock("Grotto · east vault support",P(5.6f,-111,2.0f),new Vector3(2.2f,4.0f,3.4f),rock,401,true);
        Rock("Grotto · low broad vault",P(0,-111.6f,5.9f),new Vector3(13.8f,3.0f,5.2f),litRock,402);
        // The hollow is at least 4.3m high, and its ten metre central opening includes the waystone.
        for(int i=0;i<7;i++)
        {
            float x=-4.7f+i*1.58f;
            Rock("Grotto · ceiling crystal",P(x,-111.25f,4.50f),new Vector3(.18f,R(.45f,.95f),.24f),i%2==0?amethyst:cyan,420+i);
        }
        Vector3[] ridge={P(-9.8f,-100.6f,1.4f),P(-8.3f,-111.8f,1.8f),P(8.5f,-111.9f,1.9f),P(10.0f,-103.5f,1.3f)};
        for(int i=0;i<ridge.Length;i++)Rock("Grotto · low outer ridge",ridge[i],new Vector3(1.8f,3.2f,2.35f),i%2==0?rock:litRock,440+i,true);
        Vector3[] beds={P(-10,-91),P(10.2f,-90.8f),P(-3.8f,-98.7f),P(4.0f,-102.4f),P(-9.0f,-110.2f),P(9.2f,-108.1f)};
        for(int i=0;i<beds.Length;i++)
        {
            for(int j=0;j<5;j++)
            {
                float a=j*Mathf.PI*2/5;
                float h=.7f+(j%3)*.43f;
                var crystal=Rock("Grotto · prismatic star garden",beds[i]+new Vector3(Mathf.Cos(a)*.40f,h*.45f,Mathf.Sin(a)*.4f),new Vector3(.34f,h,.38f),j%2==0?amethyst:cyan,470+i*5+j);
                crystal.transform.rotation=Quaternion.Euler(0,0,0);
            }
        }
        // A dark reflecting disk and a slow suspended astrolabe create an observatory within the cavern.
        S(PrimitiveType.Cylinder,"Grotto · reflecting star pool",P(0,-102,.019f),new Vector3(3.0f,.016f,3.0f),deepPool);
        Ring("Grotto · astronomical pool inlay",P(0,-102,.051f),1.60f,.055f,gold);
        var astronomy=new GameObject("Grotto · suspended celestial model").transform;astronomy.SetParent(region,false);astronomy.position=P(0,-102,3.15f);
        var equator=Arc("Grotto · celestial equator",astronomy.position,1.05f,.028f,gold);equator.transform.rotation=Quaternion.Euler(24,0,17);equator.transform.SetParent(astronomy,true);
        var meridian=Arc("Grotto · celestial meridian",astronomy.position,1.21f,.028f,violet);meridian.transform.rotation=Quaternion.Euler(78,30,0);meridian.transform.SetParent(astronomy,true);
        S(PrimitiveType.Sphere,"Grotto · central star",astronomy.position,Vector3.one*.21f,cyan,false,astronomy);
        Batch(astronomy);astronomy.gameObject.AddComponent<AetherHorizonMotion>().Configure(new Vector3(0,5,0),.08f,.5f);
        Vector3[] stars={P(-4.1f,-110.9f,4.95f),P(-2.5f,-110.9f,5.45f),P(-.5f,-110.9f,5.15f),P(1.3f,-110.9f,5.7f),P(3.6f,-110.9f,5.12f)};
        for(int i=0;i<stars.Length;i++)
        {
            S(PrimitiveType.Sphere,"Grotto · ceiling star",stars[i],Vector3.one*.10f,cyan);
            if(i<stars.Length-1)Beam("Grotto · constellation filament",stars[i],stars[i+1],.016f,cyan);
        }
        GlowMotes(P(0,-99,2.5f),new Vector3(12,2,14),new Color(.64f,.44f,.97f,.65f));
        Lantern(P(-2.8f,-85.5f),cyan);Lantern(P(2.8f,-85.5f),cyan);Lantern(P(-5.7f,-96.0f),amethyst);
        RegionalLight(P(0,-107,3.8f),new Color(.55f,.32f,1),.85f,18);
    }

    static void DawnSanctuary()
    {
        var marble=M("Dawn · pale rose marble",new Color(.84f,.78f,.67f));
        var rose=M("Dawn · flowering canopy",new Color(.84f,.51f,.58f));
        var roseLight=M("Dawn · pale blossom",new Color(.97f,.76f,.74f));
        var trunk=M("Dawn · fruitwood",new Color(.40f,.36f,.27f));
        var sunrise=M("Dawn · first light",new Color(1,.76f,.32f),1.4f);
        // Three slender arches form a processional perspective, with a clear central walking space.
        for(int j=0;j<3;j++)
        {
            float z=-103-j*5;const float x=45,r=5.8f,spring=1.8f;
            for(int side=-1;side<=1;side+=2)
            {
                Column(P(x+side*r,z),spring,.55f,marble,gold);
                S(PrimitiveType.Sphere,"Dawn arcade · pearl capital",P(x+side*r,z,spring+.18f),Vector3.one*.22f,sunrise);
            }
            var arc=Arc("Dawn arcade · great golden arch",P(x,z,spring),r,.115f,gold,0,180);arc.transform.rotation=Quaternion.Euler(-90,0,0);
            var inner=Arc("Dawn arcade · fine luminous arch",P(x,z,spring),r-.22f,.024f,sunrise,0,180);inner.transform.rotation=Quaternion.Euler(-90,0,0);
            for(int i=1;i<6;i++)
            {
                float a=i*Mathf.PI/6;
                Vector3 p=P(x+Mathf.Cos(a)*r,z,spring+Mathf.Sin(a)*r);
                Rock("Dawn arcade · suspended golden leaf",p,new Vector3(.17f,.38f,.12f),gold,550+j*6+i);
            }
        }
        Vector3 altar=P(45,-113.1f);
        S(PrimitiveType.Cylinder,"Dawn sanctuary · altar step",altar+Vector3.up*.11f,new Vector3(3.8f,.11f,2.2f),marble,true);
        S(PrimitiveType.Cube,"Dawn sanctuary · altar",altar+Vector3.up*.60f,new Vector3(2.6f,.95f,.92f),marble,true);
        S(PrimitiveType.Cube,"Dawn sanctuary · altar inlay",altar+Vector3.up*1.10f,new Vector3(2.75f,.10f,1.02f),gold);
        var halo=Arc("Dawn sanctuary · sun halo",altar+Vector3.up*3.1f,1.15f,.05f,gold);halo.transform.rotation=Quaternion.Euler(90,0,0);
        var sun=Rock("Dawn sanctuary · floating seed",altar+Vector3.up*3.1f,new Vector3(.50f,.92f,.50f),sunrise,581);
        // Mesh vertices are localised before animation so the seed rotates about itself.
        LocaliseMesh(sun,altar+Vector3.up*3.1f);
        sun.AddComponent<AetherHorizonMotion>().Configure(new Vector3(0,14,0),.10f,.7f);
        Vector3[] trees={P(33,-92),P(54,-104),P(47,-88),P(33.5f,-112)};
        for(int i=0;i<trees.Length;i++)BlossomTree(trees[i],i%2==0?.90f:1.04f,trunk,i%2==0?rose:roseLight,i);
        // Raised-looking flower beds remain low and non-colliding, with routes kept completely open.
        Vector3[] beds={P(34.1f,-98.4f),P(44.2f,-95.0f),P(52.8f,-89.6f),P(35.9f,-110.5f),P(49.1f,-110.8f)};
        for(int i=0;i<beds.Length;i++)
        {
            Ring("Dawn garden · flower-bed edging",beds[i]+Vector3.up*.035f,1.20f,.11f,marble);
            for(int j=0;j<17;j++)
            {
                float a=j*2.39996f,d=Mathf.Sqrt((j+.5f)/17)*1.04f;
                Flower(beds[i]+new Vector3(Mathf.Cos(a)*d,0,Mathf.Sin(a)*d),R(.18f,.33f),j%3==0?roseLight:rose,sunrise);
            }
        }
        for(int i=0;i<65;i++)
        {
            Vector3 p=P(R(32.5f,53.5f),R(-111.8f,-86));
            if(!Reserved(p,2,.40f)&&!OnRoute(p,2,1.5f))Flower(p,R(.13f,.26f),i%2==0?rose:roseLight,gold);
        }
        GlowMotes(P(43,-99,2.4f),new Vector3(12,2,14),new Color(1,.75f,.46f,.60f));
        Lantern(P(44.3f,-85.4f),sunrise);Lantern(P(42.6f,-100.1f),sunrise);Lantern(P(36.7f,-93.1f),sunrise);
        RegionalLight(P(45,-108,4.8f),new Color(1,.74f,.45f),.42f,18);
    }

    static void WhiteTree(Vector3 p,float scale,Material bark,Material leaves,Material snow,int seed)
    {
        S(PrimitiveType.Cylinder,"Frost birch · trunk",p+Vector3.up*2.05f*scale,new Vector3(.33f*scale,2.05f*scale,.33f*scale),bark,true);
        for(int i=0;i<3;i++)
        {
            float a=(i*120+seed*19)*Mathf.Deg2Rad;
            Vector3 end=p+new Vector3(Mathf.Cos(a)*1.05f,3.25f+i*.42f,Mathf.Sin(a)*1.05f)*scale;
            Beam("Frost birch · silver branch",p+Vector3.up*(2.3f*scale),end,.12f*scale,bark);
            Rock("Frost birch · frozen crown",end+Vector3.up*.37f*scale,new Vector3(2.3f,1.06f,1.95f)*scale,leaves,600+seed*4+i);
            Rock("Frost birch · snow cap",end+Vector3.up*.75f*scale,new Vector3(1.85f,.33f,1.6f)*scale,snow,660+seed*4+i);
        }
    }

    static void BlossomTree(Vector3 p,float scale,Material trunk,Material leaves,int seed)
    {
        S(PrimitiveType.Cylinder,"Dawn orchard · trunk",p+Vector3.up*1.9f*scale,new Vector3(.32f*scale,1.9f*scale,.32f*scale),trunk,true);
        for(int i=0;i<3;i++)
        {
            float a=(i*120+seed*27)*Mathf.Deg2Rad;
            Vector3 end=p+new Vector3(Mathf.Cos(a)*.85f,3.1f+i*.25f,Mathf.Sin(a)*.85f)*scale;
            Beam("Dawn orchard · branch",p+Vector3.up*(2.1f*scale),end,.12f*scale,trunk);
            Rock("Dawn orchard · flowering crown",end+Vector3.up*.30f*scale,new Vector3(2.05f,1.16f,1.94f)*scale,leaves,720+seed*4+i);
        }
    }

    static void Flower(Vector3 p,float height,Material petal,Material centre)
    {
        var stem=M("Dawn · flower stems",new Color(.27f,.43f,.29f));
        Beam("Dawn meadow · flower stalk",p,p+Vector3.up*height,.026f,stem);
        var v=new List<Vector3>();var t=new List<int>();Vector3 top=p+Vector3.up*height;
        for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2/5;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),side=new Vector3(-dir.z,0,dir.x);
            Quad(v,t,top,top+dir*.065f-side*.036f,top+dir*.13f+Vector3.up*.024f,top+dir*.065f+side*.036f,true);
        }
        MeshObject("Dawn meadow · five-petalled blossom",v,t,petal,false);
        S(PrimitiveType.Sphere,"Dawn meadow · flower heart",top+Vector3.up*.014f,Vector3.one*.037f,centre);
    }

    static void GroundCover(int index)
    {
        float cx=Centres[index].x;
        Material mat=M("Horizon · meadow "+index,index==0?new Color(.49f,.66f,.69f):index==1?new Color(.36f,.31f,.51f):new Color(.42f,.53f,.31f));
        var v=new List<Vector3>();var t=new List<int>();
        int amount=index==0?440:index==1?650:1200;
        for(int i=0;i<amount;i++)
        {
            Vector3 p=P(cx+R(-10.7f,10.7f),R(-111.8f,-86),.015f);
            if(Reserved(p,index,.35f)||OnRoute(p,index,1.5f))continue;
            if(index==0&&p.z<-108&&Mathf.Abs(p.x-cx)<5)continue;
            if(index==1&&p.z<-109&&Mathf.Abs(p.x)<7.5f)continue;
            float a=R(0,Mathf.PI*2);Vector3 across=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R(.035f,.065f),tip=p+new Vector3(.035f,R(.11f,.30f),0);
            int n=v.Count;v.Add(p-across);v.Add(p+across);v.Add(tip);v.Add(p+across);v.Add(p-across);v.Add(tip);
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n+3);t.Add(n+4);t.Add(n+5);
        }
        MeshObject("Horizon island · meadow",v,t,mat,false);
    }

    static bool Reserved(Vector3 p,int index,float extra)
    {
        Vector2 q=new Vector2(p.x,p.z);
        for(int i=0;i<trials[index].Length;i++)if(Vector2.Distance(q,trials[index][i])<3.05f+extra)return true;
        for(int i=0;i<memories[index].Length;i++)if(Vector2.Distance(q,memories[index][i])<1.05f+extra)return true;
        return Vector2.Distance(q,new Vector2(Centres[index].x,-108))<3.05f+extra;
    }

    static bool OnRoute(Vector3 p,int index,float distance)
    {
        Vector2 q=new Vector2(p.x,p.z);Vector2[] points=trials[index];
        for(int i=0;i<3;i++)if(NearSegment(q,points[i],points[(i+1)%3],distance))return true;
        Vector2 stonePos=new Vector2(Centres[index].x,-108);
        return NearSegment(q,points[0],stonePos,distance)||NearSegment(q,points[1],stonePos,distance);
    }

    static bool NearSegment(Vector2 p,Vector2 a,Vector2 b,float distance)
    {Vector2 d=b-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);return Vector2.Distance(p,a+d*f)<distance;}

    static void OuterBoundary(int index)
    {
        float x=Centres[index].x;Material edge=index==0?pearl:index==1?M("Grotto · boundary",new Color(.34f,.31f,.47f)):stone;
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<10;i++)S(PrimitiveType.Cube,"Horizon island · low boundary",P(x+side*14.55f,-111.4f+i*2.7f,.11f),new Vector3(.36f,.22f,2.32f),edge,true);
            for(int i=0;i<9;i++)
            {
                float dx=-11.6f+i*2.9f;if(side>0&&Mathf.Abs(dx)<2.6f)continue;
                S(PrimitiveType.Cube,"Horizon island · low boundary",P(x+dx,side>0?-83.45f:-114.55f,.11f),new Vector3(2.52f,.22f,.36f),edge,true);
            }
        }
    }

    static void Path(Vector3 from,Vector3 to,float width,Material mat)
    {
        Vector3 delta=to-from;int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/1.4f));
        for(int i=0;i<count;i++)
        {
            var tile=S(PrimitiveType.Cube,"Horizon · walking-route tile",Vector3.Lerp(from,to,(i+.5f)/count)+Vector3.up*.019f,new Vector3(width,.038f,delta.magnitude/count-.065f),mat);
            tile.transform.rotation=Quaternion.LookRotation(delta.normalized);
        }
    }

    static void Column(Vector3 p,float height,float width,Material body,Material accent)
    {
        S(PrimitiveType.Cube,"Horizon column · base",p+Vector3.up*.12f,new Vector3(width*1.5f,.24f,width*1.5f),body,true);
        S(PrimitiveType.Cube,"Horizon column · shaft",p+Vector3.up*height*.5f,new Vector3(width,height,width),body,true);
        S(PrimitiveType.Cube,"Horizon column · capital",p+Vector3.up*height,new Vector3(width*1.4f,.17f,width*1.4f),accent);
    }

    static void Lantern(Vector3 p,Material glow)
    {
        S(PrimitiveType.Cube,"Horizon lantern · base",p+Vector3.up*.10f,new Vector3(.36f,.2f,.36f),stone);
        S(PrimitiveType.Cube,"Horizon lantern · stem",p+Vector3.up*.43f,new Vector3(.16f,.56f,.16f),stone);
        S(PrimitiveType.Cube,"Horizon lantern · cap",p+Vector3.up*.74f,new Vector3(.32f,.09f,.32f),gold);
        S(PrimitiveType.Sphere,"Horizon lantern · light",p+Vector3.up*.91f,new Vector3(.12f,.20f,.12f),glow);
    }

    static void RegionalLight(Vector3 p,Color color,float strength,float range)
    {
        var go=new GameObject("Horizon · local atmospheric light");go.transform.SetParent(region,false);go.transform.position=p;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=strength;light.range=range;light.shadows=LightShadows.None;
    }

    static void Snow(Vector3 centre,Vector3 volume)
    {
        var ps=Particles("Frost · gently falling snow",centre,volume,new Color(.82f,.93f,1,.63f));
        var main=ps.main;main.startLifetime=9;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.060f);main.maxParticles=210;
        var emission=ps.emission;emission.rateOverTime=23;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=-.85f;velocity.x=.12f;
        var noise=ps.noise;noise.enabled=true;noise.strength=.15f;noise.frequency=.23f;noise.scrollSpeed=.07f;ps.Play();
    }

    static void GlowMotes(Vector3 centre,Vector3 volume,Color color)
    {
        var ps=Particles("Horizon · drifting light motes",centre,volume*2,color);
        var main=ps.main;main.startLifetime=14;main.startSpeed=new ParticleSystem.MinMaxCurve(.01f,.06f);main.startSize=new ParticleSystem.MinMaxCurve(.018f,.048f);main.maxParticles=65;
        var emission=ps.emission;emission.rateOverTime=4;
        var noise=ps.noise;noise.enabled=true;noise.strength=.11f;noise.frequency=.20f;noise.scrollSpeed=.05f;ps.Play();
    }

    static ParticleSystem Particles(string name,Vector3 centre,Vector3 volume,Color color)
    {
        var go=new GameObject(name);go.transform.SetParent(region,false);go.transform.position=centre;
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.duration=14;main.prewarm=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startColor=color;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=volume;
        var life=ps.colorOverLifetime;life.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.15f),new GradientAlphaKey(.7f,.8f),new GradientAlphaKey(0,1)});life.color=gradient;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
        var material=new Material(Shader.Find("Legacy Shaders/Particles/Additive"));material.name="Horizon · soft particle";
        var texture=new Texture2D(24,24,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<24;y++)for(int x=0;x<24;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(11.5f,11.5f))/11.5f;texture.SetPixel(x,y,new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-d),2.2f)));}
        texture.Apply();material.mainTexture=texture;renderer.sharedMaterial=material;go.AddComponent<AetherHorizonWeather>();return ps;
    }

    static Material Glass(string name,Color color)
    {
        var mat=new Material(Shader.Find("Standard"));mat.name=name;mat.color=color;mat.SetFloat("_Mode",2);mat.SetFloat("_Glossiness",.66f);
        mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);mat.EnableKeyword("_ALPHABLEND_ON");mat.renderQueue=3000;return mat;
    }

    static GameObject Beam(string name,Vector3 a,Vector3 b,float width,Material mat)
    {var go=S(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),mat);go.transform.up=(b-a).normalized;return go;}

    static GameObject Arc(string name,Vector3 centre,float radius,float tube,Material mat,float from=0,float to=360)
    {
        var v=new List<Vector3>();var t=new List<int>();const int segments=64,sides=6;
        for(int i=0;i<=segments;i++)for(int j=0;j<sides;j++)
        {float a=Mathf.Lerp(from,to,i/(float)segments)*Mathf.Deg2Rad,b=j*Mathf.PI*2/sides,r=radius+Mathf.Cos(b)*tube;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*tube,Mathf.Sin(a)*r));}
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
        {int a=i*sides+j,b=i*sides+(j+1)%sides,c=(i+1)*sides+j,d=(i+1)*sides+(j+1)%sides;t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);}
        var go=MeshObject(name,v,t,mat,false);go.transform.position=centre;return go;
    }

    static void Ring(string name,Vector3 centre,float radius,float width,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();const int count=64;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            Quad(v,t,centre+new Vector3(Mathf.Cos(a)*(radius-width),0,Mathf.Sin(a)*(radius-width)),centre+new Vector3(Mathf.Cos(b)*(radius-width),0,Mathf.Sin(b)*(radius-width)),centre+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius),centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),false);
        }
        MeshObject(name,v,t,mat,false);
    }

    static GameObject Rock(string name,Vector3 p,Vector3 size,Material mat,int seed,bool collision=false)
    {
        var rng=new System.Random(seed);var v=new List<Vector3>();var t=new List<int>();const int sides=7;var ring=new Vector3[sides];
        for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,r=.83f+(float)rng.NextDouble()*.17f;ring[i]=new Vector3(Mathf.Cos(a)*size.x*.5f*r,((float)rng.NextDouble()-.5f)*size.y*.17f,Mathf.Sin(a)*size.z*.5f*r);}
        for(int i=0;i<sides;i++)
        {
            int n=v.Count;v.Add(p+Vector3.up*size.y*.5f);v.Add(p+ring[(i+1)%sides]);v.Add(p+ring[i]);t.Add(n);t.Add(n+1);t.Add(n+2);
            n=v.Count;v.Add(p-Vector3.up*size.y*.45f);v.Add(p+ring[i]);v.Add(p+ring[(i+1)%sides]);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        return MeshObject(name,v,t,mat,collision);
    }

    static void LocaliseMesh(GameObject go,Vector3 centre)
    {
        var mesh=go.GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]-=centre;mesh.vertices=v;mesh.RecalculateBounds();go.transform.position=centre;
    }

    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool doubleSided)
    {
        int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);
        if(doubleSided){n=v.Count;v.Add(d);v.Add(c);v.Add(b);v.Add(a);t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);}
    }

    static GameObject MeshObject(string name,List<Vector3> v,List<int> t,Material mat,bool collision)
    {
        var mesh=new Mesh();mesh.name=name;if(v.Count>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var normals=mesh.normals;for(int i=0;i<normals.Length;i++){float s=normals[i].sqrMagnitude;if(s<.000001f||float.IsNaN(s)||float.IsInfinity(s))normals[i]=Vector3.up;}mesh.normals=normals;
        var go=new GameObject(name);go.transform.SetParent(region,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }

    static void Batch(Transform root)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();var renderers=new List<MeshRenderer>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||renderer.sharedMaterial==null||renderer.sharedMaterial.renderQueue>2500||filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
            bool moving=false;for(Transform p=filter.transform;p!=null&&p!=root;p=p.parent)if(p.GetComponent<AetherHorizonMotion>()!=null){moving=true;break;}if(moving)continue;
            List<CombineInstance> list;if(!groups.TryGetValue(renderer.sharedMaterial,out list)){list=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,list);}
            list.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderers.Add(renderer);
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh();mesh.name="Batched horizon · "+pair.Key.name;mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            var go=new GameObject(mesh.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
        }
        foreach(var renderer in renderers)renderer.enabled=false;
    }
}

public sealed class AetherHorizonMotion : MonoBehaviour
{
    Vector3 origin,rotation;float time,amplitude,speed;
    public void Configure(Vector3 degreesPerSecond,float bob=0,float frequency=1){origin=transform.position;rotation=degreesPerSecond;amplitude=bob;speed=frequency;}
    void Update(){if(AetherGame.I!=null&&AetherGame.I.ReducedMotion)return;time+=Time.deltaTime;transform.position=origin+Vector3.up*(Mathf.Sin(time*speed)*amplitude);transform.Rotate(rotation*Time.deltaTime,Space.Self);}
}

public sealed class AetherHorizonWeather : MonoBehaviour
{
    ParticleSystem particles;bool stopped;
    void Awake(){particles=GetComponent<ParticleSystem>();}
    void Update(){bool reduced=AetherGame.I!=null&&AetherGame.I.ReducedMotion;if(reduced==stopped)return;stopped=reduced;if(stopped)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);else particles.Play();}
}

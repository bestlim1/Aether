using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>The three southern islands: authored routes, original scenery and safe walkable bridges.</summary>
public static class AetherFrontierWorld
{
    static Transform region;
    static readonly System.Random random = new System.Random(63018);
    static Material limestone, basalt, brass, water, paleGlow;
    static readonly Vector2[][] puzzles = {
        new [] { new Vector2(-50,-53), new Vector2(-36,-47), new Vector2(-49,-37) },
        new [] { new Vector2(-8,-53), new Vector2(8,-46), new Vector2(0,-36) },
        new [] { new Vector2(36,-53), new Vector2(50,-47), new Vector2(40,-36) }
    };
    static readonly Vector2[][] records = {
        new [] { new Vector2(-55,-57), new Vector2(-31,-57), new Vector2(-54,-45), new Vector2(-31,-36), new Vector2(-55,-34) },
        new [] { new Vector2(-12,-58), new Vector2(12,-58), new Vector2(-12,-43), new Vector2(12,-34), new Vector2(-9,-34) },
        new [] { new Vector2(31,-58), new Vector2(55,-57), new Vector2(55,-42), new Vector2(31,-35), new Vector2(55,-34) }
    };

    public static void Build()
    {
        var archipelago = new GameObject("THE SOUTHERN REACHES · 남쪽의 세 정원").transform;
        limestone = M("Southern limestone",new Color(.69f,.72f,.63f));
        basalt = M("Southern basalt",new Color(.11f,.23f,.24f));
        brass = M("Southern Antique Gold",new Color(.76f,.54f,.22f));
        water = M("Southern water",new Color(.08f,.49f,.59f),.32f);
        paleGlow = M("Southern pearl light",new Color(.58f,.95f,.85f),1.25f);
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*43;
            region=new GameObject(i==0?"RAIN ARBORETUM · 비의 수목원":i==1?"TIDAL CLOISTER · 조수의 회랑":"WIND ATELIER · 바람의 공방").transform;
            region.SetParent(archipelago,false);
            Material floor=M("Frontier soil "+i,i==0?new Color(.16f,.31f,.27f):i==1?new Color(.26f,.38f,.42f):new Color(.43f,.30f,.23f));
            Material edge=M("Frontier cut stone "+i,i==0?new Color(.20f,.35f,.31f):i==1?new Color(.27f,.40f,.46f):new Color(.41f,.26f,.22f));
            Island(x,floor,edge);
            Bridge(x,i==1?-16:-11,i);
            Paths(x,i);
            if(i==0)Arboretum();else if(i==1)TidalCloister();else WindAtelier();
            Meadow(x,i);
            Perimeter(x,edge);
            BatchStatic(region);
        }
    }

    static Material M(string name,Color color,float emission=0){return AetherWorld.Mat(name,color,emission);}
    static GameObject S(PrimitiveType type,string name,Vector3 p,Vector3 scale,Material mat,bool collision=false,Transform parent=null)
    {return AetherWorld.Shape(type,name,p,scale,mat,parent!=null?parent:region,collision);}
    static Vector3 P(float x,float z,float y=0){return new Vector3(x,y,z);}
    static float R(float min,float max){return min+(float)random.NextDouble()*(max-min);}

    static void Island(float x,Material floor,Material edge)
    {
        Vector2[] outline={new Vector2(-13,-16),new Vector2(13,-16),new Vector2(15,-14),new Vector2(15,14),new Vector2(13,16),new Vector2(-13,16),new Vector2(-15,14),new Vector2(-15,-14)};
        var v=new List<Vector3>{new Vector3(x,0,-47)};var t=new List<int>();
        for(int i=0;i<outline.Length;i++)v.Add(new Vector3(x+outline[i].x,0,-47+outline[i].y));
        for(int i=0;i<outline.Length;i++){t.Add(0);t.Add((i+1)%outline.Length+1);t.Add(i+1);}
        MeshObject("Southern island · continuous level floor",v,t,floor,true);
        float[] levels={0,-.65f,-7.2f,-16.5f};float[] scales={1,1.035f,.86f,.20f};
        for(int band=0;band<3;band++)for(int i=0;i<outline.Length;i++)
        {
            int next=(i+1)%outline.Length;
            var points=new List<Vector3>{
                new Vector3(x+outline[i].x*scales[band],levels[band],-47+outline[i].y*scales[band]),
                new Vector3(x+outline[next].x*scales[band],levels[band],-47+outline[next].y*scales[band]),
                new Vector3(x+outline[next].x*scales[band+1],levels[band+1],-47+outline[next].y*scales[band+1]),
                new Vector3(x+outline[i].x*scales[band+1],levels[band+1],-47+outline[i].y*scales[band+1])};
            MeshObject("Southern island · fractured cliff",points,new List<int>{0,1,2,0,2,3},band==0?edge:basalt,false);
        }
        for(int i=0;i<18;i++)
        {
            float a=i*Mathf.PI*2/18;
            Rock("Suspended cliff shard",new Vector3(x+Mathf.Cos(a)*14.1f,R(-8,-3),-47+Mathf.Sin(a)*15.0f),new Vector3(R(2,4),R(4,9),R(2,4)),edge,i+30);
        }
    }

    static void Bridge(float x,float north,int theme)
    {
        float south=-31;float length=north-south;
        Material deck=M("Southern bridge stone "+theme,theme==2?new Color(.67f,.50f,.35f):new Color(.57f,.65f,.62f));
        // The uninterrupted deck overlaps each island by 18cm and its top is exactly y=0.
        S(PrimitiveType.Cube,"Southern bridge · solid floor",P(x,(north+south)*.5f,-.28f),new Vector3(4,.56f,length+.36f),deck,true);
        int steps=Mathf.CeilToInt(length/1.5f);
        for(int i=0;i<steps;i++)
        {
            float z=Mathf.Lerp(south,north,(i+.5f)/steps);
            S(PrimitiveType.Cube,"Bridge · dressed paver",P(x,z,.022f),new Vector3(3.78f,.044f,length/steps-.07f),i%4==0?limestone:deck);
            S(PrimitiveType.Cube,"Bridge · path inlay",P(x,z,.047f),new Vector3(.045f,.008f,.41f),theme==2?brass:paleGlow);
        }
        for(int side=-1;side<=1;side+=2)
        {
            S(PrimitiveType.Cube,"Bridge · continuous handrail",P(x+side*2.10f,(north+south)*.5f,.92f),new Vector3(.15f,.13f,length+.2f),deck,true);
            S(PrimitiveType.Cube,"Bridge · lower beam",P(x+side*2.10f,(north+south)*.5f,-.62f),new Vector3(.4f,.34f,length+.5f),basalt);
            int posts=Mathf.CeilToInt(length/2.6f);
            for(int i=0;i<=posts;i++)
            {
                Vector3 p=P(x+side*2.1f,Mathf.Lerp(south,north,i/(float)posts));
                S(PrimitiveType.Cube,"Bridge · baluster",p+Vector3.up*.5f,new Vector3(.27f,1,.27f),deck,true);
                S(PrimitiveType.Cube,"Bridge · brass cap",p+Vector3.up*1.06f,new Vector3(.39f,.12f,.39f),brass);
                S(PrimitiveType.Sphere,"Bridge · pearl guide",p+Vector3.up*1.23f,Vector3.one*.12f,paleGlow);
            }
            Beam("Bridge · diagonal brace",P(x+side*2.1f,north-1,-4),P(x+side*2.1f,north-length*.45f,-.55f),.28f,basalt);
            Beam("Bridge · diagonal brace",P(x+side*2.1f,south+1,-4),P(x+side*2.1f,south+length*.45f,-.55f),.28f,basalt);
        }
    }

    static void Paths(float x,int theme)
    {
        Material paving=M("Frontier paving "+theme,theme==0?new Color(.48f,.62f,.53f):theme==1?new Color(.58f,.70f,.73f):new Color(.73f,.54f,.36f));
        Vector3 entry=P(x,-31),north=P(puzzles[theme][2].x,puzzles[theme][2].y),left=P(puzzles[theme][0].x,puzzles[theme][0].y),right=P(puzzles[theme][1].x,puzzles[theme][1].y);
        Path(entry,north,2.9f,paving);
        Path(north,left,2.65f,paving);Path(north,right,2.65f,paving);
        if(theme==1){Path(left,P(0,-56),2.4f,paving);Path(P(0,-56),right,2.4f,paving);}
        else Path(left,right,2.6f,paving);
        // An unbroken peripheral walking circuit provides safe access to all five memory locations.
        Vector3[] corners={P(x-12,-60),P(x+12,-60),P(x+12,-34),P(x-12,-34)};
        for(int i=0;i<4;i++)Path(corners[i],corners[(i+1)%4],1.4f,paving);
        Path(entry,P(x+12,-34),1.65f,paving);Path(entry,P(x-12,-34),1.65f,paving);
        for(int i=0;i<3;i++)
        {
            Vector3 c=P(puzzles[theme][i].x,puzzles[theme][i].y);
            Ring("Reserved trial · outer floor inlay",c+Vector3.up*.079f,3.22f,.05f,theme==2?brass:paleGlow);
            Ring("Reserved trial · second floor inlay",c+Vector3.up*.077f,2.97f,.018f,paving);
            for(int mark=0;mark<8;mark++)
            {
                float a=mark*Mathf.PI/4;
                var tick=S(PrimitiveType.Cube,"Reserved trial · floor marker",c+new Vector3(Mathf.Sin(a)*3.12f,.085f,Mathf.Cos(a)*3.12f),new Vector3(.052f,.01f,.17f),brass);
                tick.transform.rotation=Quaternion.Euler(0,mark*45,0);
            }
        }
    }

    static void Path(Vector3 from,Vector3 to,float width,Material mat)
    {
        Vector3 delta=to-from;int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/1.4f));
        for(int i=0;i<count;i++)
        {
            var slab=S(PrimitiveType.Cube,"Southern walking route",Vector3.Lerp(from,to,(i+.5f)/count)+Vector3.up*.019f,new Vector3(width,.038f,delta.magnitude/count-.07f),mat);
            slab.transform.rotation=Quaternion.LookRotation(delta.normalized);
        }
    }

    static void Arboretum()
    {
        var frame=M("Arboretum · patinated greenhouse frame",new Color(.22f,.48f,.43f));
        var jade=M("Arboretum · deep emerald leaves",new Color(.09f,.40f,.32f));
        var mint=M("Arboretum · rainlit leaves",new Color(.26f,.65f,.50f));
        var trunk=M("Arboretum · wet cedar bark",new Color(.15f,.26f,.23f));
        var crystal=M("Arboretum · rain crystal",new Color(.27f,.90f,.81f),.7f);
        var glass=Glass("Arboretum · greenhouse glazing",new Color(.18f,.62f,.55f,.10f));
        // This open greenhouse has a 3m front entrance and a seven metre vaulted silhouette.
        const float cx=-43,cz=-58;
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<3;i++)
            {
                Vector3 p=P(cx+side*4.5f,cz-2.6f+i*2.6f);
                S(PrimitiveType.Cube,"Greenhouse · upright",p+Vector3.up*1.7f,new Vector3(.16f,3.4f,.16f),frame,true);
            }
            S(PrimitiveType.Cube,"Greenhouse · horizontal cornice",P(cx+side*4.5f,cz,3.4f),new Vector3(.19f,.18f,5.4f),brass);
            S(PrimitiveType.Cube,"Greenhouse · glass wall",P(cx+side*4.5f,cz,1.75f),new Vector3(.012f,3.2f,5.05f),glass);
            S(PrimitiveType.Cube,"Greenhouse · planted stone bed",P(cx+side*3.1f,cz,.21f),new Vector3(1.2f,.42f,3.1f),frame,true);
            for(int i=0;i<4;i++)Fern(P(cx+side*3.1f,cz-1.2f+i*.8f,.42f),.78f,mint);
        }
        for(int depth=0;depth<3;depth++)
        {
            float z=cz-2.6f+depth*2.6f;
            var rib=Arc("Greenhouse · vaulted brass rib",P(cx,z,3.4f),4.5f,.065f,frame,0,180);
            rib.transform.rotation=Quaternion.Euler(-90,0,0);
        }
        for(int j=0;j<7;j++)
        {
            float angle=j*Mathf.PI/6;
            Vector3 p=P(cx+Mathf.Cos(angle)*4.5f,cz,3.4f+Mathf.Sin(angle)*4.5f);
            S(PrimitiveType.Cube,"Greenhouse · roof purlin",p,new Vector3(.07f,.07f,5.3f),frame);
            if(j<6)
            {
                float a=angle,b=(j+1)*Mathf.PI/6;
                var verts=new List<Vector3>{P(cx+Mathf.Cos(a)*4.48f,cz-2.5f,3.4f+Mathf.Sin(a)*4.48f),P(cx+Mathf.Cos(a)*4.48f,cz+2.5f,3.4f+Mathf.Sin(a)*4.48f),P(cx+Mathf.Cos(b)*4.48f,cz+2.5f,3.4f+Mathf.Sin(b)*4.48f),P(cx+Mathf.Cos(b)*4.48f,cz-2.5f,3.4f+Mathf.Sin(b)*4.48f)};
                var paneVertices=new List<Vector3>();var paneTriangles=new List<int>();
                Quad(paneVertices,paneTriangles,verts[0],verts[1],verts[2],verts[3],true);
                MeshObject("Greenhouse · glazed roof pane",paneVertices,paneTriangles,glass,false);
            }
        }
        S(PrimitiveType.Cube,"Greenhouse · doorway lintel",P(cx,cz+2.65f,3.1f),new Vector3(9.1f,.16f,.20f),frame);
        Lantern(P(cx-1.8f,cz+2.9f),crystal);Lantern(P(cx+1.8f,cz+2.9f),crystal);
        Vector3[] trees={P(-54,-51),P(-53.3f,-40.6f),P(-32.5f,-41),P(-40,-35.8f),P(-43,-44),P(-39,-51),P(-51,-59),P(-34,-59)};
        for(int i=0;i<trees.Length;i++)Tree(trees[i],i%3==0?1.22f:.93f,i%2==0?jade:mint,trunk,i);
        Vector3[] beds={P(-33.6f,-51.9f),P(-45,-39.8f),P(-53.4f,-47.7f),P(-39,-59.5f)};
        for(int i=0;i<beds.Length;i++)
        {
            Ring("Arboretum · rain garden bed",beds[i]+Vector3.up*.04f,1.1f,.11f,frame);
            for(int j=0;j<4;j++)
            {
                float a=j*Mathf.PI*.5f;
                Rock("Arboretum · water-grown quartz",beds[i]+new Vector3(Mathf.Cos(a)*.4f,.62f,Mathf.Sin(a)*.4f),new Vector3(.36f,1.25f+j*.13f,.37f),crystal,170+i*4+j);
            }
        }
        // Water spills over the southern cliff; nothing removes the walkable island surface.
        Waterfall(P(-40.5f,-62.75f),2.6f,12.5f,new Color(.19f,.74f,.76f,.52f));
        Waterfall(P(-45,-62.75f),1.25f,10.5f,new Color(.30f,.86f,.83f,.46f));
        Rain(P(-43,-47,10.4f),new Vector3(24,.2f,26));
        for(int i=0;i<16;i++)
        {
            Vector3 p=P(R(-54,-32),R(-59,-35));
            if(!Reserved(p,0,1.0f))Fern(p,R(.4f,.74f),i%2==0?jade:mint);
        }
        Lantern(P(-43,-33.9f),crystal);Lantern(P(-40.5f,-47.8f),crystal);Lantern(P(-51.9f,-43),crystal);
    }

    static void TidalCloister()
    {
        var blueStone=M("Tidal cloister · blue limestone",new Color(.54f,.67f,.73f));
        var blue=M("Tidal cloister · dark mosaic",new Color(.12f,.31f,.45f));
        var pearl=M("Tidal cloister · pearl ceramic",new Color(.75f,.85f,.82f));
        var gleam=M("Tidal cloister · waterlight",new Color(.22f,.77f,.94f),.8f);
        // Four supports leave the central bay open directly onto the southern bridge.
        for(int i=0;i<4;i++)
        {
            float x=-9+i*6;
            Column(P(x,-59.4f),5.6f,.65f,blueStone,pearl);
            if(i<3)
            {
                var arch=Arc("Cloister · flowing stone arch",P(x+3,-59.4f,2.85f),3,.19f,blueStone,0,180);
                arch.transform.rotation=Quaternion.Euler(-90,0,0);
                S(PrimitiveType.Cube,"Cloister · raised aqueduct",P(x+3,-59.4f,6.0f),new Vector3(6.1f,.34f,.86f),blueStone);
                S(PrimitiveType.Cube,"Cloister · high water thread",P(x+3,-59.4f,6.19f),new Vector3(6,.018f,.30f),water);
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<3;i++)
            {
                Vector3 p=P(side*10.1f,-48+i*5.7f);
                if(Reserved(p,1,1))continue;
                Column(p,3.7f,.45f,blueStone,pearl);
            }
            Channel(P(side*4.2f,-47),.60f,25,blue,water);
            // Six small footbridges cross the shallow rills at the level of a normal step.
            foreach(float z in new[]{-39f,-47f,-56f})
                S(PrimitiveType.Cube,"Cloister · channel stepping bridge",P(side*4.2f,z,.075f),new Vector3(1.3f,.15f,1.25f),pearl,true);
        }
        Vector3 basin=P(0,-49);
        S(PrimitiveType.Cylinder,"Cloister · fountain basin",basin+Vector3.up*.09f,new Vector3(5.5f,.09f,5.5f),blue,true);
        Ring("Cloister · low fountain lip",basin+Vector3.up*.19f,2.75f,.23f,pearl);
        S(PrimitiveType.Cylinder,"Cloister · water mirror",basin+Vector3.up*.188f,new Vector3(4.95f,.012f,4.95f),water);
        S(PrimitiveType.Cylinder,"Cloister · fountain pedestal",basin+Vector3.up*.55f,new Vector3(.72f,.55f,.72f),blueStone,true);
        S(PrimitiveType.Sphere,"Cloister · pearl fountain source",basin+Vector3.up*1.32f,Vector3.one*.34f,gleam);
        for(int i=0;i<8;i++)
        {
            float angle=i*Mathf.PI/4;
            Vector3 from=basin+Vector3.up*1.35f,to=basin+new Vector3(Mathf.Cos(angle)*1.7f,.23f,Mathf.Sin(angle)*1.7f);
            WaterArc("Cloister · falling fountain jet",from,to,.66f,gleam);
        }
        Ring("Cloister · tide compass",P(0,-43.6f,.075f),2.15f,.07f,brass);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            var tile=S(PrimitiveType.Cube,"Cloister · compass shell",P(Mathf.Sin(a)*1.75f,-43.6f+Mathf.Cos(a)*1.75f,.079f),new Vector3(.15f,.018f,.25f),pearl);
            tile.transform.rotation=Quaternion.Euler(0,i*45,0);
        }
        Waterfall(P(2.5f,-62.75f),2.4f,13.5f,new Color(.18f,.64f,.89f,.5f));
        Waterfall(P(-4.2f,-62.75f),.85f,11,new Color(.25f,.79f,.95f,.5f));
        for(int side=-1;side<=1;side+=2)
        {
            Lantern(P(side*2.75f,-33.2f),gleam);Lantern(P(side*6.4f,-58),gleam);
            for(int i=0;i<5;i++)
            {
                var p=P(side*(6.3f+i*.7f),-40.4f);
                if(Reserved(p,1,.5f))continue;
                Rock("Cloister · bleached riverstone",p+Vector3.up*.15f,new Vector3(.47f,.34f,.7f),pearl,210+i);
            }
        }
    }

    static void WindAtelier()
    {
        var terra=M("Atelier · red sandstone",new Color(.57f,.30f,.21f));
        var clay=M("Atelier · sun-warmed clay",new Color(.72f,.41f,.25f));
        var canvas=M("Atelier · linen sails",new Color(.87f,.78f,.52f));
        var copper=M("Atelier · deep bronze",new Color(.30f,.33f,.25f));
        var amber=M("Atelier · amber aether",new Color(.97f,.67f,.21f),1.1f);
        // The principal windmill can be seen from both the original observatory and the new bridge.
        Windmill(P(46,-58),8.4f,4.25f,terra,brass,canvas,0);
        Windmill(P(53,-51.5f),4.8f,2.05f,clay,brass,canvas,1);
        Windmill(P(32.8f,-45.5f),4.3f,1.55f,terra,brass,canvas,2);
        // The open workshop sits between trials and remains traversable on all four sides.
        Vector3 bench=P(43,-43.5f);
        S(PrimitiveType.Cube,"Atelier · workbench top",bench+Vector3.up*1.0f,new Vector3(3.4f,.18f,1.4f),clay,true);
        for(int side=-1;side<=1;side+=2)
        {
            S(PrimitiveType.Cube,"Atelier · bench trestle",bench+new Vector3(side*1.25f,.45f,0),new Vector3(.22f,.9f,1.1f),copper,true);
            Column(bench+new Vector3(side*2.1f,0,-.7f),3.3f,.33f,terra,brass);
        }
        S(PrimitiveType.Cube,"Atelier · canopy beam",bench+new Vector3(0,3.4f,-.7f),new Vector3(4.6f,.20f,.36f),terra);
        Flag(bench+new Vector3(-2.0f,3.2f,-.65f),2.3f,.65f,canvas);
        for(int i=0;i<4;i++)
        {
            var gear=Gear("Atelier · loose brass mechanism",bench+new Vector3(-1.05f+i*.68f,1.17f,0),.25f+i*.03f,brass,copper);
            gear.rotation=Quaternion.Euler(90,0,i*23);
        }
        Gear("Atelier · suspended clockwork",P(45.3f,-50.6f,2.3f),1.0f,brass,copper).gameObject.AddComponent<AetherFrontierMotion>().Configure(new Vector3(0,0,5));
        Column(P(45.3f,-50.6f),1.1f,.47f,terra,brass);
        for(int i=0;i<5;i++)
        {
            var p=P(34.1f+i*4.0f,-61.0f);
            if(Reserved(p,2,.55f))continue;
            Column(p,2.6f+(i%2)*.5f,.28f,terra,brass);
            Flag(p+Vector3.up*(2.85f+(i%2)*.5f),1.55f,.52f,i%2==0?canvas:clay);
        }
        Vector3[] pots={P(45.1f,-41.3f),P(39.8f,-47.8f),P(51.1f,-39.1f),P(34.8f,-37.4f)};
        for(int i=0;i<pots.Length;i++)
        {
            if(Reserved(pots[i],2,.6f))continue;
            S(PrimitiveType.Cylinder,"Atelier · clay vessel",pots[i]+Vector3.up*.39f,new Vector3(.68f,.39f,.68f),clay,true);
            Ring("Atelier · vessel lip",pots[i]+Vector3.up*.80f,.36f,.085f,brass);
            for(int j=0;j<3;j++)Rock("Atelier · copper reed",pots[i]+new Vector3((j-1)*.17f,1.2f,j*.10f),new Vector3(.17f,1.0f,.18f),copper,250+i*3+j);
        }
        Lantern(P(44.6f,-33.4f),amber);Lantern(P(46.5f,-37.7f),amber);Lantern(P(41.3f,-54.9f),amber);
        for(int i=0;i<8;i++)
        {
            Vector3 p=P(R(33,53),R(-60,-35));
            if(Reserved(p,2,.9f))continue;
            Rock("Atelier · fractured terracotta",p+Vector3.up*.18f,new Vector3(.85f,.39f,.65f),i%2==0?terra:clay,280+i);
        }
    }

    static void Tree(Vector3 p,float scale,Material leaves,Material trunk,int seed)
    {
        S(PrimitiveType.Cylinder,"Arboretum · cedar trunk",p+Vector3.up*(2.15f*scale),new Vector3(.43f*scale,2.15f*scale,.43f*scale),trunk,true);
        for(int i=0;i<3;i++)
        {
            float a=(i*120+seed*17)*Mathf.Deg2Rad;
            Vector3 end=p+new Vector3(Mathf.Cos(a)*1.3f,3.5f+i*.33f,Mathf.Sin(a)*1.3f)*scale;
            Beam("Arboretum · spreading branch",p+Vector3.up*(2.6f*scale),end,.15f*scale,trunk);
            Rock("Arboretum · rain-heavy crown",end+Vector3.up*.38f*scale,new Vector3(2.5f,1.32f,2.2f)*scale,leaves,300+seed*4+i);
        }
        Rock("Arboretum · highest crown",p+Vector3.up*4.9f*scale,new Vector3(2.8f,1.5f,2.6f)*scale,leaves,370+seed);
    }

    static void Fern(Vector3 p,float scale,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<7;i++)
        {
            float a=i*Mathf.PI*2/7;Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Vector3 across=new Vector3(-dir.z,0,dir.x)*.15f*scale;
            Vector3 mid=p+(dir*.35f+Vector3.up*.50f)*scale,end=p+(dir*.70f+Vector3.up*.20f)*scale;
            Quad(v,t,p,mid-across,end,mid+across,true);
        }
        MeshObject("Arboretum · fern fronds",v,t,mat,false);
    }

    static void Column(Vector3 p,float height,float width,Material mat,Material accent)
    {
        S(PrimitiveType.Cube,"Frontier column · plinth",p+Vector3.up*.12f,new Vector3(width*1.5f,.24f,width*1.5f),mat,true);
        S(PrimitiveType.Cube,"Frontier column · shaft",p+Vector3.up*(height*.5f),new Vector3(width,height,width),mat,true);
        S(PrimitiveType.Cube,"Frontier column · capital",p+Vector3.up*height,new Vector3(width*1.4f,.20f,width*1.4f),accent);
    }

    static void Lantern(Vector3 p,Material glow)
    {
        S(PrimitiveType.Cube,"Southern lantern · base",p+Vector3.up*.10f,new Vector3(.39f,.20f,.39f),limestone);
        S(PrimitiveType.Cube,"Southern lantern · stem",p+Vector3.up*.44f,new Vector3(.18f,.57f,.18f),limestone);
        S(PrimitiveType.Cube,"Southern lantern · crown",p+Vector3.up*.76f,new Vector3(.35f,.10f,.35f),brass);
        S(PrimitiveType.Sphere,"Southern lantern · pearl",p+Vector3.up*.95f,new Vector3(.13f,.22f,.13f),glow);
    }

    static void Channel(Vector3 c,float width,float length,Material edge,Material surface)
    {
        S(PrimitiveType.Cube,"Cloister · level water channel",c+Vector3.up*.015f,new Vector3(width,.022f,length),surface);
        for(int side=-1;side<=1;side+=2)S(PrimitiveType.Cube,"Cloister · channel coping",c+new Vector3(side*(width*.5f+.055f),.06f,0),new Vector3(.11f,.12f,length),edge);
    }

    static void WaterArc(string name,Vector3 from,Vector3 to,float rise,Material mat)
    {
        var go=new GameObject(name);go.transform.SetParent(region,false);var line=go.AddComponent<LineRenderer>();
        line.useWorldSpace=true;line.positionCount=15;line.widthMultiplier=.028f;line.sharedMaterial=mat;line.numCapVertices=2;
        line.generateLightingData=true;line.shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<15;i++){float u=i/14f;line.SetPosition(i,Vector3.Lerp(from,to,u)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*rise));}
    }

    static void Windmill(Vector3 p,float height,float radius,Material tower,Material metal,Material sail,int seed)
    {
        S(PrimitiveType.Cylinder,"Windmill · broad foundation",p+Vector3.up*.18f,new Vector3(3.3f,.18f,3.3f),tower,true);
        S(PrimitiveType.Cylinder,"Windmill · tower",p+Vector3.up*(height*.48f),new Vector3(1.65f,height*.48f,1.65f),tower,true);
        for(int i=0;i<4;i++)Ring("Windmill · tower brass collar",p+Vector3.up*(.9f+i*height*.21f),.87f,.09f,metal);
        Vector3 centre=p+new Vector3(0,height,1.04f);
        var rotor=new GameObject("Windmill · wind-driven sails").transform;rotor.SetParent(region,false);rotor.position=centre;
        var hub=S(PrimitiveType.Cylinder,"Windmill · axle",centre,new Vector3(.58f,.32f,.58f),metal,false,rotor);hub.transform.rotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<4;i++)
        {
            float a=(i*90+30)*Mathf.Deg2Rad;Vector3 direction=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0),side=new Vector3(direction.y,-direction.x,0);
            Beam("Windmill · sail spar",centre+direction*.3f,centre+direction*radius,.10f,metal,rotor);
            var v=new List<Vector3>();var t=new List<int>();
            Quad(v,t,centre+direction*(radius*.28f)-side*.07f,centre+direction*radius-side*.10f,centre+direction*(radius*.93f)+side*(radius*.25f),centre+direction*(radius*.32f)+side*(radius*.075f),true);
            var panel=MeshObject("Windmill · linen sail",v,t,sail,false);panel.transform.SetParent(rotor,true);
        }
        BatchStatic(rotor);rotor.gameObject.AddComponent<AetherFrontierMotion>().Configure(new Vector3(0,0,seed%2==0?-7:9));
    }

    static Transform Gear(string name,Vector3 centre,float radius,Material teeth,Material body)
    {
        var root=new GameObject(name).transform;root.SetParent(region,false);root.position=centre;
        var ring=Arc(name+" · rim",centre,radius,.09f,teeth);ring.transform.rotation=Quaternion.Euler(90,0,0);ring.transform.SetParent(root,true);
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;Vector3 p=centre+new Vector3(Mathf.Sin(a)*radius,Mathf.Cos(a)*radius,0);
            var tooth=S(PrimitiveType.Cube,name+" · tooth",p,new Vector3(radius*.20f,radius*.28f,.18f),teeth,false,root);tooth.transform.rotation=Quaternion.Euler(0,0,-i*30);
            if(i%3==0)Beam(name+" · spoke",centre,p,.065f,body,root);
        }
        S(PrimitiveType.Sphere,name+" · hub",centre,Vector3.one*radius*.30f,body,false,root);
        BatchStatic(root);return root;
    }

    static void Flag(Vector3 p,float width,float height,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<5;i++)
        {
            float a=i/5f,b=(i+1)/5f;
            Vector3 from=p+new Vector3(a*width,-a*.13f,Mathf.Sin(a*Mathf.PI)*.2f),to=p+new Vector3(b*width,-b*.13f,Mathf.Sin(b*Mathf.PI)*.2f);
            Quad(v,t,from,to,to-Vector3.up*height,from-Vector3.up*height,true);
        }
        var cloth=MeshObject("Atelier · pennant",v,t,mat,false);
        // Geometry is authored in world coordinates; a small translation provides a calm breeze.
        cloth.AddComponent<AetherFrontierMotion>().Configure(Vector3.zero,.045f,.65f);
    }

    static void Rain(Vector3 centre,Vector3 volume)
    {
        var go=new GameObject("Arboretum · gentle rainfall");go.transform.SetParent(region,false);go.transform.position=centre;
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.duration=4;main.prewarm=true;main.startLifetime=1.18f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.025f,.055f);main.maxParticles=170;main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.startColor=new Color(.40f,.75f,.78f,.35f);
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=volume;
        var emission=ps.emission;emission.rateOverTime=125;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.y=-8.7f;velocity.x=.34f;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.038f;renderer.lengthScale=2.4f;renderer.shadowCastingMode=ShadowCastingMode.Off;
        var mat=new Material(Shader.Find("Legacy Shaders/Particles/Additive"));mat.name="Arboretum · soft rain streak";mat.mainTexture=StreakTexture(false);renderer.sharedMaterial=mat;
        go.AddComponent<AetherFrontierRain>();ps.Play();
    }

    static void Waterfall(Vector3 lip,float width,float depth,Color color)
    {
        var mat=new Material(Shader.Find("Legacy Shaders/Particles/Additive"));mat.name="Southern waterfall · luminous veil";mat.mainTexture=StreakTexture(true);mat.SetColor("_TintColor",color);
        var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<=12;i++)
        {
            float f=i/12f;float spread=width*(1+f*.17f);Vector3 c=lip+new Vector3(Mathf.Sin(f*5)*.10f,-depth*f,-.16f-f*.75f);
            v.Add(c+Vector3.left*spread*.5f);v.Add(c+Vector3.right*spread*.5f);uv.Add(new Vector2(0,f*3));uv.Add(new Vector2(1,f*3));
            if(i<12){int n=i*2;t.Add(n);t.Add(n+2);t.Add(n+1);t.Add(n+1);t.Add(n+2);t.Add(n+3);}
        }
        var go=MeshObject("Southern waterfall · flowing ribbon",v,t,mat,false);go.GetComponent<MeshFilter>().sharedMesh.SetUVs(0,uv);
        // The particle shader multiplies by vertex colour, so the ribbon supplies explicit white vertices.
        var mesh=go.GetComponent<MeshFilter>().sharedMesh;var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;mesh.colors=colors;
        go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        go.AddComponent<AetherFrontierFlow>().Configure(mat);
    }

    static Texture2D StreakTexture(bool fallingWater)
    {
        int w=fallingWater?32:8,h=32;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.wrapMode=fallingWater?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float u=(x+.5f)/w,v=(y+.5f)/h;
            float alpha=fallingWater?(Mathf.Sin(u*Mathf.PI)*(.22f+.48f*Mathf.Pow(Mathf.Sin(u*31+v*12)*.5f+.5f,3))):Mathf.Sin(u*Mathf.PI)*Mathf.Sin(v*Mathf.PI);
            texture.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        texture.Apply();return texture;
    }

    static Material Glass(string name,Color color)
    {
        var mat=new Material(Shader.Find("Standard"));mat.name=name;mat.color=color;mat.SetFloat("_Mode",2);mat.SetFloat("_Glossiness",.7f);
        mat.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);mat.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);mat.SetInt("_ZWrite",0);
        mat.DisableKeyword("_ALPHATEST_ON");mat.EnableKeyword("_ALPHABLEND_ON");mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");mat.renderQueue=3000;return mat;
    }

    static void Meadow(float x,int theme)
    {
        Material mat=M("Southern meadow "+theme,theme==0?new Color(.19f,.48f,.35f):theme==1?new Color(.27f,.45f,.49f):new Color(.55f,.40f,.22f));
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<1050;i++)
        {
            Vector3 p=P(x+R(-10.6f,10.6f),R(-59,-35),.018f);
            if(Reserved(p,theme,.25f)||OnRoute(p,theme))continue;
            if(theme==0&&p.z<-55&&p.x>-48&&p.x<-38)continue;
            if(theme==1&&(Mathf.Abs(Mathf.Abs(p.x)-4.2f)<.7f||Vector2.Distance(new Vector2(p.x,p.z),new Vector2(0,-49))<3))continue;
            float a=R(0,Mathf.PI*2);Vector3 across=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R(.035f,.07f),tip=p+new Vector3(.035f,R(.13f,.38f),0);
            int n=v.Count;v.Add(p-across);v.Add(p+across);v.Add(tip);v.Add(p+across);v.Add(p-across);v.Add(tip);
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n+3);t.Add(n+4);t.Add(n+5);
        }
        MeshObject("Southern island · meadow",v,t,mat,false);
    }

    static bool Reserved(Vector3 p,int theme,float extra)
    {
        Vector2 point=new Vector2(p.x,p.z);
        for(int i=0;i<puzzles[theme].Length;i++)if(Vector2.Distance(point,puzzles[theme][i])<3.05f+extra)return true;
        for(int i=0;i<records[theme].Length;i++)if(Vector2.Distance(point,records[theme][i])<1.05f+extra)return true;
        return false;
    }

    static bool OnRoute(Vector3 p,int theme)
    {
        Vector2 point=new Vector2(p.x,p.z);Vector2[] q=puzzles[theme];
        for(int i=0;i<3;i++)
        {
            Vector2 a=q[i],b=q[(i+1)%3],d=b-a;float f=Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude);
            if(Vector2.Distance(point,a+d*f)<1.45f)return true;
        }
        return false;
    }

    static void Perimeter(float x,Material mat)
    {
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<10;i++)S(PrimitiveType.Cube,"Southern island · low outer kerb",P(x+side*14.55f,-59.4f+i*2.7f,.11f),new Vector3(.36f,.22f,2.32f),mat,true);
            for(int i=0;i<9;i++)
            {
                float offset=-11.6f+i*2.9f;
                if(Mathf.Abs(offset)<2.6f)continue;
                S(PrimitiveType.Cube,"Southern island · low outer kerb",P(x+offset,side>0?-31.45f:-62.55f,.11f),new Vector3(2.52f,.22f,.36f),mat,true);
            }
        }
    }

    static GameObject Beam(string name,Vector3 a,Vector3 b,float width,Material mat,Transform parent=null)
    {
        var go=S(PrimitiveType.Cylinder,name,(a+b)*.5f,new Vector3(width,(b-a).magnitude*.5f,width),mat,false,parent);go.transform.up=(b-a).normalized;return go;
    }

    static GameObject Arc(string name,Vector3 centre,float radius,float tube,Material mat,float from=0,float to=360)
    {
        var v=new List<Vector3>();var t=new List<int>();const int segments=56,sides=6;
        for(int i=0;i<=segments;i++)for(int j=0;j<sides;j++)
        {
            float a=Mathf.Lerp(from,to,i/(float)segments)*Mathf.Deg2Rad,b=j*Mathf.PI*2/sides,r=radius+Mathf.Cos(b)*tube;
            v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*tube,Mathf.Sin(a)*r));
        }
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
        {int a=i*sides+j,b=i*sides+(j+1)%sides,c=(i+1)*sides+j,d=(i+1)*sides+(j+1)%sides;t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);}
        var go=MeshObject(name,v,t,mat,false);go.transform.position=centre;return go;
    }

    static void Ring(string name,Vector3 c,float radius,float width,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();const int count=56;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            Quad(v,t,c+new Vector3(Mathf.Cos(a)*(radius-width),0,Mathf.Sin(a)*(radius-width)),c+new Vector3(Mathf.Cos(b)*(radius-width),0,Mathf.Sin(b)*(radius-width)),c+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius),c+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),false);
        }
        MeshObject(name,v,t,mat,false);
    }

    static void Rock(string name,Vector3 p,Vector3 size,Material mat,int seed)
    {
        var rng=new System.Random(seed);var v=new List<Vector3>();var t=new List<int>();const int count=7;var ring=new Vector3[count];
        for(int i=0;i<count;i++){float a=i*Mathf.PI*2/count,r=.8f+(float)rng.NextDouble()*.2f;ring[i]=new Vector3(Mathf.Cos(a)*size.x*.5f*r,((float)rng.NextDouble()-.5f)*size.y*.17f,Mathf.Sin(a)*size.z*.5f*r);}
        for(int i=0;i<count;i++)
        {
            int n=v.Count;v.Add(p+Vector3.up*size.y*.5f);v.Add(p+ring[(i+1)%count]);v.Add(p+ring[i]);t.Add(n);t.Add(n+1);t.Add(n+2);
            n=v.Count;v.Add(p-Vector3.up*size.y*.45f);v.Add(p+ring[i]);v.Add(p+ring[(i+1)%count]);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        MeshObject(name,v,t,mat,false);
    }

    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool doubleSided)
    {
        int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);
        if(doubleSided){n=v.Count;v.Add(d);v.Add(c);v.Add(b);v.Add(a);t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n);t.Add(n+2);t.Add(n+3);}
    }

    static GameObject MeshObject(string name,List<Vector3> v,List<int> t,Material mat,bool collision)
    {
        var mesh=new Mesh();mesh.name=name;if(v.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var normals=mesh.normals;for(int i=0;i<normals.Length;i++){float s=normals[i].sqrMagnitude;if(s<.000001f||float.IsNaN(s)||float.IsInfinity(s))normals[i]=Vector3.up;}mesh.normals=normals;
        var go=new GameObject(name);go.transform.SetParent(region,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }

    static void BatchStatic(Transform root)
    {
        var groups=new Dictionary<Material,List<CombineInstance>>();var renderers=new List<MeshRenderer>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null||!renderer.enabled||renderer.sharedMaterial==null||renderer.sharedMaterial.renderQueue>2500||filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
            bool moving=false;for(Transform p=filter.transform;p!=null&&p!=root;p=p.parent)if(p.GetComponent<AetherFrontierMotion>()!=null||p.GetComponent<AetherFrontierFlow>()!=null){moving=true;break;}
            if(moving)continue;
            List<CombineInstance> group;if(!groups.TryGetValue(renderer.sharedMaterial,out group)){group=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,group);}
            group.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});renderers.Add(renderer);
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh();mesh.name="Batched frontier · "+pair.Key.name;mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            var go=new GameObject(mesh.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
        }
        foreach(var renderer in renderers)renderer.enabled=false;
    }
}

public sealed class AetherFrontierMotion : MonoBehaviour
{
    Vector3 rotation,origin;float amplitude,speed,time;
    public void Configure(Vector3 degreesPerSecond,float bob=0,float frequency=1){rotation=degreesPerSecond;origin=transform.position;amplitude=bob;speed=frequency;}
    void Update()
    {
        if(AetherGame.I!=null&&AetherGame.I.ReducedMotion)return;
        time+=Time.deltaTime;transform.position=origin+Vector3.up*(Mathf.Sin(time*speed)*amplitude);transform.Rotate(rotation*Time.deltaTime,Space.Self);
    }
}

public sealed class AetherFrontierFlow : MonoBehaviour
{
    Material material;float scroll;
    public void Configure(Material mat){material=mat;}
    void Update(){if(material==null||(AetherGame.I!=null&&AetherGame.I.ReducedMotion))return;scroll=Mathf.Repeat(scroll+Time.deltaTime*.48f,1);material.mainTextureOffset=new Vector2(0,scroll);}
}

public sealed class AetherFrontierRain : MonoBehaviour
{
    ParticleSystem rain;bool stopped;
    void Awake(){rain=GetComponent<ParticleSystem>();}
    void Update()
    {
        bool reduced=AetherGame.I!=null&&AetherGame.I.ReducedMotion;
        if(reduced==stopped)return;stopped=reduced;
        if(stopped)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);else rain.Play();
    }
}

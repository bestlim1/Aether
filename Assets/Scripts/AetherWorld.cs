using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Hand-composed sanctuary built from original procedural geometry.</summary>
public static class AetherWorld
{
    public static Transform Portal;
    public static Transform[] BeaconTops = new Transform[9];
    static Transform world;
    static Material stone, paleStone, shadowStone, ground, darkRock, gold, teal, grass, foliage, bark, whiteGlow;
    static readonly System.Random rng = new System.Random(1709);
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public static Material Mat(string name, Color color, float emission = 0)
    {
        Material value;
        if (materials.TryGetValue(name, out value) && value != null) return value;
        value = new Material(Shader.Find("Standard"));
        value.name = name;
        value.color = color;
        value.SetFloat("_Glossiness", name.Contains("Gold") ? .56f : .16f);
        value.SetFloat("_Metallic", name.Contains("Gold") ? .72f : 0);
        if (emission > 0)
        {
            value.EnableKeyword("_EMISSION");
            value.SetColor("_EmissionColor", color * emission);
        }
        materials[name] = value;
        return value;
    }

    public static GameObject Shape(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent != null ? parent : world, false);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) { c.enabled = false; Object.Destroy(c); }
        }
        return go;
    }

    public static void Build()
    {
        world = new GameObject("AETHER · The Forgotten Garden").transform;
        stone = Mat("Limestone · warm", new Color(.66f, .64f, .49f));
        paleStone = Mat("Limestone · sunlit", new Color(.86f, .82f, .65f));
        shadowStone = Mat("Limestone · aged", new Color(.39f, .49f, .44f));
        ground = Mat("Garden moss", new Color(.24f, .36f, .32f));
        darkRock = Mat("Basalt · midnight", new Color(.075f, .17f, .20f));
        gold = Mat("Antique Gold", new Color(.81f, .56f, .20f));
        teal = Mat("Living turquoise", new Color(.12f, .90f, .77f), 1.5f);
        whiteGlow = Mat("Warm starlight", new Color(1f, .78f, .38f), 2.6f);
        grass = Mat("Grass · jade", new Color(.29f, .53f, .39f));
        foliage = Mat("Leaves · sea glass", new Color(.28f, .61f, .49f));
        bark = Mat("Cypress bark", new Color(.21f, .30f, .26f));
        ConfigureAtmosphere();
        BuildMainIsland();
        BuildWalkways();
        BuildArchitecture();
        BuildGardens();
        BuildDistantIslands();
        BuildFinalIsland();
        Fireflies(new Vector3(0, 2.2f, 5), new Vector3(15, 2, 17), 100, 4.5f);
        BuildArchipelago();
    }

    static void ConfigureAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.48f, .62f, .62f);
        RenderSettings.ambientEquatorColor = new Color(.30f, .41f, .37f);
        RenderSettings.ambientGroundColor = new Color(.11f, .17f, .18f);
        RenderSettings.ambientIntensity = 1;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = .0065f;
        RenderSettings.fogColor = new Color(.30f, .51f, .53f);
        RenderSettings.reflectionIntensity = .35f;
        var skyShader = Shader.Find("Aether/Atmosphere");
        if (skyShader != null)
        {
            var sky = new Material(skyShader);
            sky.SetColor("_Zenith", new Color(.035f, .15f, .21f));
            sky.SetColor("_Horizon", new Color(.47f, .65f, .62f));
            sky.SetColor("_Nadir", new Color(.16f, .33f, .38f));
            RenderSettings.skybox = sky;
        }
        var sun = new GameObject("Evening sun").AddComponent<Light>();
        sun.transform.SetParent(world);
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(37, -33, 0);
        sun.color = new Color(1, .85f, .61f);
        sun.intensity = 1.65f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .82f;
        sun.shadowBias = .025f;
        RenderSettings.sun = sun;
        QualitySettings.shadowDistance = 85;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowCascades = 2;
        QualitySettings.antiAliasing = 4;
        var sea = Mat("The endless cloud sea", new Color(.13f, .34f, .39f));
        Shape(PrimitiveType.Cube, "Cloud sea", new Vector3(0, -27, 70), new Vector3(1100, .1f, 1100), sea, world, false);
        for (int i = 0; i < 16; i++)
        {
            var cloudColor = new Color(.32f + i % 3 * .018f, .51f + i % 3 * .014f, .52f + i % 3 * .018f);
            var cloud = Mat("Cloud bank " + i % 3, cloudColor);
            var c = Shape(PrimitiveType.Sphere, "Low drifting cloud bank", new Vector3(Range(-180, 180), Range(-24, -16), Range(25, 200)), new Vector3(Range(35, 110), Range(1.3f, 3), Range(8, 20)), cloud, world, false);
            c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void BuildMainIsland()
    {
        Vector2[] outline = {
            new Vector2(-14,-16), new Vector2(14,-16), new Vector2(18,-12),
            new Vector2(18,18), new Vector2(14,22), new Vector2(-14,22),
            new Vector2(-18,18), new Vector2(-18,-12)
        };
        Island("Sanctuary", outline, Vector3.zero, 12, true);
        // Rim markers create a clear, walkable silhouette without a fence around the whole vista.
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 9; i++)
            {
                float z = -11 + i * 3.45f;
                var rim = Shape(PrimitiveType.Cube, "Broken garden edging", new Vector3(side * 17.25f, .16f, z), new Vector3(.58f, .32f, 2.75f), i % 3 == 0 ? paleStone : shadowStone);
                rim.transform.rotation = Quaternion.Euler(0, Range(-3,3), 0);
                if (i % 2 == 0) StonePost(new Vector3(side * 16.9f, 0, z), .85f, .46f, false);
            }
            for (int i = 0; i < 5; i++)
                Shape(PrimitiveType.Cube, "Garden rim", new Vector3(side * (4.9f + i * 2.3f), .14f, 21.55f), new Vector3(1.9f, .28f, .55f), shadowStone);
        }
        for (int i = 0; i < 24; i++)
        {
            float angle = i * Mathf.PI * 2 / 24;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 17, Range(-7,-2), 3 + Mathf.Sin(angle) * 18);
            CrystalRock("Fractured cliff", p, new Vector3(Range(2,5), Range(4,10), Range(2,5)), darkRock, i + 1, false);
        }
        // Three pendant crystals make the island read as floating, even when seen from the approach.
        for (int i = -1; i <= 1; i++)
        {
            Vector3 p = new Vector3(i * 5, -10 - Mathf.Abs(i), -10);
            CrystalRock("Suspended aetherite", p, new Vector3(.9f, 2.5f, .9f), Mat("Aetherite", new Color(.15f,.64f,.58f), .65f), i + 14, false);
        }
    }

    static void Island(string name, Vector2[] outline, Vector3 center, float depth, bool collision)
    {
        int n = outline.Length;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        vertices.Add(center);
        for (int i=0; i<n; i++) vertices.Add(center + new Vector3(outline[i].x, 0, outline[i].y));
        for (int i=0; i<n; i++) { triangles.Add(0); triangles.Add((i+1)%n + 1); triangles.Add(i+1); }
        CreateMesh(name + " · level garden floor", vertices, triangles, ground, collision);
        // Separate faceted cliff bands make light catch the stone's broad planes.
        Material[] facets = {
            darkRock, Mat("Cliff teal", new Color(.12f,.25f,.27f)),
            Mat("Cliff slate", new Color(.15f,.28f,.28f)),
            Mat("Cliff lit", new Color(.22f,.34f,.31f)), shadowStone
        };
        float[] levels = { 0, -.60f, -depth * .46f, -depth };
        float[] scales = { 1, 1.035f, .85f, .20f };
        for (int band=0; band<3; band++)
        {
            for (int i=0; i<n; i++)
            {
                int next=(i+1)%n;
                var v = new List<Vector3> {
                    center + new Vector3(outline[i].x*scales[band],levels[band],outline[i].y*scales[band]),
                    center + new Vector3(outline[next].x*scales[band],levels[band],outline[next].y*scales[band]),
                    center + new Vector3(outline[next].x*scales[band+1],levels[band+1],outline[next].y*scales[band+1]),
                    center + new Vector3(outline[i].x*scales[band+1],levels[band+1],outline[i].y*scales[band+1])
                };
                CreateMesh(name + " · cut cliff", v, new List<int>{0,1,2,0,2,3}, band==0 ? shadowStone : facets[(i+band)%4], false);
            }
        }
    }

    static void BuildWalkways()
    {
        for (int i=0;i<22;i++)
        {
            float z = -12.4f + i * 1.55f;
            for (int side=-1;side<=1;side+=2)
            {
                var p = Shape(PrimitiveType.Cube,"The pilgrim's way",new Vector3(side * .88f,.035f,z),new Vector3(1.69f,.07f,1.43f),i%5==0?paleStone:stone,world,false);
                p.transform.rotation = Quaternion.Euler(0,Range(-1.4f,1.4f),0);
            }
        }
        // Branch paths lead toward the two side sanctums.
        for (int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<6;i++)
            {
                float t=(i+1)/6f;
                var p=Shape(PrimitiveType.Cube,"Branch walkway",new Vector3(side*(2+t*6.7f),.045f,side<0?1:3),new Vector3(1.12f,.09f,2.0f),stone,world,false);
                p.transform.rotation = Quaternion.Euler(0,Range(-3,3),0);
            }
        }
        var medallion = new Vector3(0,.095f,7.2f);
        Ring("Pilgrim compass · outer",medallion,3.3f,.14f,.025f,gold);
        Ring("Pilgrim compass · inner",medallion,2.95f,.035f,.025f,paleStone);
        Ring("Pilgrim compass · centre",medallion,.6f,.055f,.025f,gold);
        for (int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            var tick=Shape(PrimitiveType.Cube,"Compass inlay",medallion+new Vector3(Mathf.Sin(a)*3.06f,0,Mathf.Cos(a)*3.06f),new Vector3(.055f,.018f,i%3==0?.38f:.19f),gold,world,false);
            tick.transform.rotation=Quaternion.Euler(0,i*30,0);
        }
        for(int side=-1;side<=1;side+=2)
        {
            foreach(float z in new float[]{-8.5f,-3,8.5f,13,19.5f}) Lantern(new Vector3(side*2.45f,0,z));
            for (int i=0;i<7;i++)
            {
                float z=-8+i*3.1f;
                if(z>0 && z<5)continue;
                var mat=Mat("Water in the rills",new Color(.065f,.38f,.36f),.22f);
                Shape(PrimitiveType.Cube,"Shallow reflecting rill",new Vector3(side*3.25f,.022f,z),new Vector3(.38f,.026f,2.95f),mat,world,false);
                Shape(PrimitiveType.Cube,"Rill border",new Vector3(side*3.53f,.075f,z),new Vector3(.12f,.15f,3),shadowStone,world,false);
            }
        }
        // Arrival terrace and its four engraved concentric arcs.
        Ring("Arrival terrace",new Vector3(0,.016f,-11),4.9f,.55f,.08f,shadowStone);
        Ring("Arrival terrace · gold inlay",new Vector3(0,.12f,-11),4.53f,.027f,.014f,gold);
    }

    static void BuildArchitecture()
    {
        // A broken colonnade frames the composition without blocking the long sightline.
        for(int i=0;i<4;i++)
        {
            float z=-7+i*5.9f;
            StonePost(new Vector3(-14.35f,0,z),i==1?3.2f:6.4f,.90f,true);
            if(i<3)
            {
                var lintel=Shape(PrimitiveType.Cube,"West arcade · lintel",new Vector3(-14.35f,6.43f,z+2.85f),new Vector3(1.16f,.62f,5.85f),paleStone);
                if(i==1) { lintel.transform.localScale=new Vector3(1.16f,.62f,3.15f); lintel.transform.position+=Vector3.forward*1.3f; }
            }
        }
        StonePost(new Vector3(14.25f,0,-5.5f),5.2f,1.1f,true);
        StonePost(new Vector3(14.25f,0,.5f),5.2f,1.1f,true);
        Shape(PrimitiveType.Cube,"East arcade · entablature",new Vector3(14.25f,5.28f,-2.5f),new Vector3(1.6f,.65f,7.35f),paleStone);
        Shape(PrimitiveType.Cube,"East arcade · crown",new Vector3(14.25f,5.73f,-2.5f),new Vector3(1.85f,.22f,7.6f),stone);
        // A freestanding celestial instrument on the eastern lawn.
        var orrery = new Vector3(12,.28f,12.8f);
        Shape(PrimitiveType.Cylinder,"Celestial instrument · foot",orrery,new Vector3(3,.24f,3),stone);
        Shape(PrimitiveType.Cylinder,"Celestial instrument · pedestal",orrery+Vector3.up*.55f,new Vector3(.68f,.5f,.68f),paleStone);
        var centre = orrery + Vector3.up*2.1f;
        var ring=Tube("Celestial instrument · meridian",centre,1.64f,.065f,gold);
        ring.transform.rotation=Quaternion.Euler(66,20,30);
        var ring2=Tube("Celestial instrument · equator",centre,1.32f,.06f,gold);
        ring2.transform.rotation=Quaternion.Euler(28,0,12);
        var orb=Shape(PrimitiveType.Sphere,"Celestial instrument · suspended heart",centre,Vector3.one*.34f,whiteGlow,world,false);
        orb.AddComponent<AetherWorldMotion>().Set(.12f,.65f,new Vector3(0,16,0));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            Shape(PrimitiveType.Cube,"Instrument engraved marks",orrery+new Vector3(Mathf.Sin(a)*1.2f,.25f,Mathf.Cos(a)*1.2f),new Vector3(.08f,.025f,.22f),gold,world,false).transform.rotation=Quaternion.Euler(0,i*45,0);
        }
        Vector3[] stations={new Vector3(-10,0,1),new Vector3(10,0,3),new Vector3(0,0,16)};
        for(int i=0;i<stations.Length;i++) BuildSanctum(stations[i],i);
        // Small rubble clusters establish age and scale.
        for(int i=0;i<18;i++)
        {
            float x=i%2==0?Range(-15.8f,-12.5f):Range(12.8f,16.1f);
            float z=Range(-11,18);
            if(Vector3.Distance(new Vector3(x,0,z),new Vector3(12,0,12.8f))<3)continue;
            var rock=Shape(PrimitiveType.Cube,"Fallen dressed stone",new Vector3(x,.18f,z),new Vector3(Range(.35f,1.25f),Range(.25f,.65f),Range(.4f,1.2f)),i%2==0?stone:shadowStone);
            rock.transform.rotation=Quaternion.Euler(Range(-8,8),Range(0,180),Range(-8,8));
        }
    }

    static void BuildSanctum(Vector3 c,int index)
    {
        Shape(PrimitiveType.Cylinder,"Sanctum " +(index+1)+" · terrace",c+Vector3.up*.055f,new Vector3(6,.055f,6),shadowStone,world,false);
        Ring("Sanctum · perimeter brass",c+Vector3.up*.125f,2.76f,.05f,.02f,gold);
        for(int side=-1;side<=1;side+=2)
        {
            StonePost(c+new Vector3(side*2.30f,0,1.85f),3.9f,.64f,true);
            Shape(PrimitiveType.Cube,"Sanctum · crown",c+new Vector3(side*2.30f,4.04f,1.85f),new Vector3(.95f,.25f,1.04f),paleStone);
        }
        var arch=Tube("Sanctum · celestial arch",c+new Vector3(0,3.45f,2.25f),2.42f,.08f,gold,180,360);
        arch.transform.rotation=Quaternion.Euler(90,0,0);
        var behind=c+new Vector3(0,0,3);
        Shape(PrimitiveType.Cube,"Beacon · lower plinth",behind+Vector3.up*.22f,new Vector3(1.25f,.44f,1.05f),stone);
        Shape(PrimitiveType.Cube,"Beacon · obelisk",behind+Vector3.up*2,new Vector3(.44f,3.15f,.44f),paleStone);
        for(int j=0;j<3;j++) Shape(PrimitiveType.Cube,"Beacon · brass collar",behind+Vector3.up*(1.0f+j*1.1f),new Vector3(.50f,.11f,.50f),gold,world,false);
        BeaconTops[index]=Shape(PrimitiveType.Sphere,"Beacon " +(index+1)+" · dormant star",behind+Vector3.up*4.1f,Vector3.one*.40f,Mat("Dormant beacon",new Color(.35f,.66f,.61f),.30f),world,false).transform;
        var halo=Tube("Beacon · halo",behind+Vector3.up*4.1f,.55f,.035f,gold);
        halo.transform.rotation=Quaternion.Euler(90,0,0);
        BeaconTops[index].gameObject.AddComponent<AetherWorldMotion>().Set(.10f,.9f,new Vector3(0,15,0));
        for(int side=-1;side<=1;side+=2) Lantern(c+new Vector3(side*2.65f,0,-1.3f));
    }

    static void BuildGardens()
    {
        // Asymmetric clusters leave intentional breathing room around the three puzzles.
        Tree(new Vector3(-11.5f,0,-8.8f),1.28f,0);
        Tree(new Vector3(11.3f,0,-9.2f),1.0f,1);
        Tree(new Vector3(-11.7f,0,13.6f),1.17f,2);
        Tree(new Vector3(15.4f,0,17.8f),.86f,3);
        Tree(new Vector3(-6.4f,0,19.3f),.74f,4);
        Tree(new Vector3(7.2f,0,19.6f),.7f,5);
        var mint=Mat("Leaves · mint highlights",new Color(.50f,.69f,.47f));
        for(int i=0;i<54;i++)
        {
            float x=Range(-16,16),z=Range(-13,20.5f);
            if(Mathf.Abs(x)<4.3f || NearStation(x,z,3.35f))continue;
            if((x<0&&Mathf.Abs(z-1)<1.4f)||(x>0&&Mathf.Abs(z-3)<1.4f))continue;
            var p=new Vector3(x,0,z);
            CrystalRock("Mossy garden stone",p+Vector3.up*.13f,new Vector3(Range(.45f,1.1f),Range(.25f,.7f),Range(.45f,1.2f)),i%4==0?stone:shadowStone,30+i,true);
            if(i%3==0)LowBush(p+new Vector3(.5f,.18f,.3f),Range(.6f,1.3f),mint);
        }
        // A single mesh contains every meadow blade, avoiding hundreds of renderers.
        var verts=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<2000;i++)
        {
            float x=Range(-16.6f,16.6f),z=Range(-14.5f,21);
            if(Mathf.Abs(x)<4.05f || NearStation(x,z,3.5f))continue;
            if((x<0&&Mathf.Abs(z-1)<1.6f)||(x>0&&Mathf.Abs(z-3)<1.6f))continue;
            float h=Range(.12f,.47f),width=Range(.045f,.095f),a=Range(0,Mathf.PI*2);
            Vector3 basePos=new Vector3(x,.02f,z),w=new Vector3(Mathf.Cos(a)*width,0,Mathf.Sin(a)*width);
            int n=verts.Count;
            Vector3 tip=basePos+Vector3.up*h+new Vector3(.10f,0,.02f);
            // Each side needs its own vertices; shared opposing faces cancel their normals to zero.
            verts.Add(basePos-w);verts.Add(basePos+w);verts.Add(tip);
            verts.Add(basePos+w);verts.Add(basePos-w);verts.Add(tip);
            tris.Add(n);tris.Add(n+1);tris.Add(n+2);tris.Add(n+3);tris.Add(n+4);tris.Add(n+5);
        }
        CreateMesh("The jade meadow",verts,tris,grass,false);
        var bloomMat=Mat("Wildflowers · pale gold",new Color(.95f,.77f,.38f),.2f);
        for(int i=0;i<65;i++)
        {
            var p=new Vector3(Range(-14.5f,14.5f),.25f,Range(-12,18));
            if(Mathf.Abs(p.x)<4.2f||NearStation(p.x,p.z,3.6f))continue;
            Shape(PrimitiveType.Sphere,"Tiny wildflower",p,new Vector3(.09f,.07f,.09f),bloomMat,world,false);
        }
    }

    static bool NearStation(float x,float z,float distance)
    {
        return Vector2.Distance(new Vector2(x,z),new Vector2(-10,1))<distance || Vector2.Distance(new Vector2(x,z),new Vector2(10,3))<distance || Vector2.Distance(new Vector2(x,z),new Vector2(0,16))<distance;
    }

    static void Tree(Vector3 p,float scale,int index)
    {
        var trunk=Shape(PrimitiveType.Cylinder,"Garden cypress · trunk",p+Vector3.up*(2.35f*scale),new Vector3(.43f*scale,2.35f*scale,.43f*scale),bark);
        trunk.transform.rotation=Quaternion.Euler(0,0,index%2==0?-5:4);
        for(int i=0;i<3;i++)
        {
            var start=p+Vector3.up*((2.3f+i*.6f)*scale);
            var end=p+new Vector3((i%2==0?1:-1)*1.2f*scale,(3.3f+i*.45f)*scale,(i-1)*.55f*scale);
            Beam("Garden cypress · branch",start,end,.17f*scale,bark,false);
        }
        var light=Mat("Leaves · sun edge",new Color(.44f,.66f,.44f));
        // Flattened low-poly crowns provide the airy silhouette of a Mediterranean garden.
        CrystalRock("Garden cypress · crown",p+new Vector3(-.2f,4.7f,.2f)*scale,new Vector3(3.1f,1.3f,2.25f)*scale,foliage,index+91,false);
        CrystalRock("Garden cypress · crown",p+new Vector3(1,4.0f,.35f)*scale,new Vector3(2.1f,.9f,1.8f)*scale,light,index+37,false);
        CrystalRock("Garden cypress · crown",p+new Vector3(-1.2f,3.8f,-.5f)*scale,new Vector3(1.7f,1,1.7f)*scale,foliage,index+71,false);
        Ring("Tree garden curb",p+Vector3.up*.035f,2.35f*scale,.13f,.08f,shadowStone);
    }

    static void LowBush(Vector3 p,float scale,Material material)
    {
        CrystalRock("Wild sage",p,new Vector3(.85f,.48f,.7f)*scale,material,43,false);
        CrystalRock("Wild sage",p+new Vector3(.4f,.08f,.12f)*scale,new Vector3(.6f,.55f,.6f)*scale,foliage,49,false);
    }

    static void BuildDistantIslands()
    {
        Vector3[] places={new Vector3(-51,-6,50),new Vector3(58,-10,62),new Vector3(-100,-10,115),new Vector3(100,-14,130),new Vector3(-39,-3,-27),new Vector3(47,-8,-20),new Vector3(12,-12,146),new Vector3(-155,-5,185)};
        for(int i=0;i<places.Length;i++)
        {
            float r=i<2?13:Range(12,23);
            Vector2[] poly=new Vector2[9];
            for(int j=0;j<poly.Length;j++) {float a=j*Mathf.PI*2/poly.Length;poly[j]=new Vector2(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.68f);}
            Island("Drifting isle "+i,poly,places[i],r*1.4f,false);
            if(i<4)
            {
                for(int side=-1;side<=1;side+=2) StonePost(places[i]+new Vector3(side*3.7f,0,0),9.5f,1.1f,false);
                Shape(PrimitiveType.Cube,"Far temple lintel",places[i]+new Vector3(0,9.6f,0),new Vector3(9.8f,.9f,1.6f),paleStone,world,false);
                Tree(places[i]+new Vector3(-6,0,2),1.15f,i+9);
            }
            else
            {
                for(int j=0;j<3;j++) StonePost(places[i]+new Vector3(-4+j*3,0,0),Range(6,13),.8f,false);
            }
        }
        // Tapered silhouettes at the horizon anchor the sky without visual clutter.
        for(int i=0;i<8;i++)
        {
            Vector3 p=new Vector3(-240+i*65,-18,260+Range(-30,30));
            CrystalRock("Horizon spire",p,new Vector3(Range(15,29),Range(35,70),Range(12,23)),Mat("Distant mountain teal",new Color(.22f,.42f,.44f)),i+202,false);
        }
    }

    static void BuildFinalIsland()
    {
        Vector2[] outline=new Vector2[12];
        for(int i=0;i<outline.Length;i++) {float a=i*Mathf.PI*2/outline.Length;outline[i]=new Vector2(Mathf.Cos(a)*8,Mathf.Sin(a)*7);}
        var c=new Vector3(0,0,35);
        Island("The threshold",outline,c,14,true);
        for(int i=0;i<8;i++) Shape(PrimitiveType.Cube,"Threshold processional stone",new Vector3(0,.045f,28.8f+i*1.35f),new Vector3(3.9f,.09f,1.23f),stone,world,false);
        Ring("Threshold ceremonial circle",c+Vector3.up*.11f,5.5f,.16f,.03f,gold);
        var portalRoot=new GameObject("THE AETHER GATE").transform;
        portalRoot.SetParent(world,false);
        portalRoot.position=new Vector3(0,3.5f,34);
        Portal=portalRoot;
        Vector3 gate=new Vector3(0,3.5f,34);
        var outer=Tube("Gate · carved limestone annulus",gate,3.5f,.42f,paleStone);
        outer.transform.rotation=Quaternion.Euler(90,0,0);
        var inner=Tube("Gate · gold inner band",gate,3.06f,.095f,gold);
        inner.transform.rotation=Quaternion.Euler(90,0,0);
        var accent=Tube("Gate · turquoise filament",gate,2.89f,.028f,teal);
        accent.transform.rotation=Quaternion.Euler(90,0,0);
        accent.transform.SetParent(portalRoot,true);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            var spark=Shape(PrimitiveType.Cube,"Gate · orbiting celestial seal",gate+new Vector3(Mathf.Sin(a)*2.89f,Mathf.Cos(a)*2.89f,-.035f),new Vector3(.14f,.30f,.09f),whiteGlow,portalRoot,false);
            spark.transform.rotation=Quaternion.Euler(0,0,45-i*120);
        }
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI*2/16;
            Vector3 p=gate+new Vector3(Mathf.Sin(a)*3.52f,Mathf.Cos(a)*3.52f,-.42f);
            var tick=Shape(PrimitiveType.Cube,"Gate · engraved hour",p,new Vector3(i%4==0?.15f:.085f,i%4==0?.32f:.20f,.03f),gold,world,false);
            tick.transform.rotation=Quaternion.Euler(0,0,-i*22.5f);
        }
        for(int side=-1;side<=1;side+=2)
        {
            StonePost(new Vector3(side*4.75f,0,34),7.7f,.74f,true);
            var fin=CrystalRock("Threshold crown crystal",new Vector3(side*4.75f,8.45f,34),new Vector3(.43f,1.1f,.43f),gold,side+300,false);
            Lantern(new Vector3(side*3.1f,0,29.2f));
            Tree(new Vector3(side*5.4f,0,38.4f),.68f,side+6);
        }
        Shape(PrimitiveType.Cube,"Gate · foot",new Vector3(0,.12f,34),new Vector3(6.2f,.24f,1.6f),paleStone);
        Fireflies(gate,new Vector3(2.3f,3,1),45,2.2f);
    }

    static void BuildArchipelago()
    {
        // Open the two existing low garden curbs exactly where the new bridges meet them.
        // The rest of the original sanctuary remains untouched.
        var sanctuary = world;
        foreach (var renderer in sanctuary.GetComponentsInChildren<MeshRenderer>())
        {
            Vector3 p = renderer.transform.position;
            if (renderer.name == "Broken garden edging" && Mathf.Abs(Mathf.Abs(p.x)-17.25f)<.1f && p.z>-6 && p.z<1)
            {
                renderer.enabled=false;
                var collider=renderer.GetComponent<Collider>();if(collider!=null)collider.enabled=false;
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            var district=new GameObject(side<0?"MOONLIT ARCHIVE · 달빛 기록보관소":"SOLAR OBSERVATORY · 태양 관측소").transform;
            district.SetParent(sanctuary,false);world=district;
            BuildBranchIsland(side);
            BuildPermanentBridge(side);
            if(side<0)BuildMoonlitArchive();else BuildSolarObservatory();
            BatchDistrict(district);
        }
        world=sanctuary;
        world.gameObject.AddComponent<AetherWorldAtmosphere>();
    }

    static void BuildBranchIsland(int side)
    {
        var savedGround=ground;var savedRock=darkRock;var savedEdge=shadowStone;
        ground=Mat(side<0?"Archive · blue slate soil":"Observatory · ochre garden",side<0?new Color(.21f,.25f,.34f):new Color(.37f,.37f,.23f));
        darkRock=Mat(side<0?"Archive · indigo basalt":"Observatory · sunset basalt",side<0?new Color(.07f,.13f,.23f):new Color(.18f,.21f,.18f));
        shadowStone=Mat(side<0?"Archive · weathered rim":"Observatory · weathered rim",side<0?new Color(.31f,.40f,.53f):new Color(.48f,.44f,.29f));
        Vector2[] outline={new Vector2(-13,-16),new Vector2(13,-16),new Vector2(15,-14),new Vector2(15,14),new Vector2(13,16),new Vector2(-13,16),new Vector2(-15,14),new Vector2(-15,-14)};
        Island(side<0?"Island of remembered stars":"Island of the returning sun",outline,new Vector3(side*43,0,5),16,true);
        for(int i=0;i<24;i++)
        {
            float a=i*Mathf.PI*2/24;
            CrystalRock("Island · hanging basalt buttress",new Vector3(side*43+Mathf.Cos(a)*14.3f,Range(-8,-3),5+Mathf.Sin(a)*15.1f),new Vector3(Range(2.1f,4.4f),Range(6,12),Range(2,3.8f)),darkRock,i+400+side,false);
        }
        ground=savedGround;darkRock=savedRock;shadowStone=savedEdge;
    }

    static void BuildPermanentBridge(int side)
    {
        var accent=Mat(side<0?"Archive · silver inlay":"Observatory · golden inlay",side<0?new Color(.58f,.71f,.88f):new Color(.92f,.64f,.22f),.18f);
        var deck=Mat(side<0?"Archive · causeway limestone":"Observatory · causeway limestone",side<0?new Color(.57f,.63f,.66f):new Color(.69f,.61f,.43f));
        // This single box provides an uninterrupted collision surface at exactly y=0.
        Shape(PrimitiveType.Cube,"Permanent bridge · continuous floor",new Vector3(side*23,-.30f,-3),new Vector3(10.35f,.60f,4),deck);
        for(int i=0;i<11;i++)
        {
            float x=side*(18+i);
            Shape(PrimitiveType.Cube,"Causeway · inset paving",new Vector3(x,.025f,-3),new Vector3(.92f,.045f,3.65f),i%3==0?paleStone:deck,world,false);
            Shape(PrimitiveType.Cube,"Causeway · guiding thread",new Vector3(x,.054f,-3),new Vector3(.84f,.012f,.045f),accent,world,false);
            if(i%2==0)
            {
                for(int edge=-1;edge<=1;edge+=2)
                {
                    var p=new Vector3(x,0,-3+edge*2.13f);
                    Shape(PrimitiveType.Cube,"Causeway · carved baluster",p+Vector3.up*.52f,new Vector3(.32f,1.04f,.32f),deck);
                    Shape(PrimitiveType.Cube,"Causeway · baluster crown",p+Vector3.up*1.10f,new Vector3(.45f,.12f,.45f),accent,world,false);
                    Shape(PrimitiveType.Sphere,"Causeway · star cap",p+Vector3.up*1.24f,Vector3.one*.12f,side<0?teal:whiteGlow,world,false);
                }
            }
        }
        for(int edge=-1;edge<=1;edge+=2)
        {
            float z=-3+edge*2.13f;
            Shape(PrimitiveType.Cube,"Causeway · safety rail",new Vector3(side*23,.84f,z),new Vector3(10.5f,.13f,.15f),deck);
            Shape(PrimitiveType.Cube,"Causeway · lower structural beam",new Vector3(side*23,-.62f,z),new Vector3(10.75f,.35f,.45f),shadowStone,world,false);
            Beam("Causeway · flying brace",new Vector3(side*18,-2.8f,z),new Vector3(side*23,-.55f,z),.34f,shadowStone,false);
            Beam("Causeway · flying brace",new Vector3(side*28,-2.8f,z),new Vector3(side*23,-.55f,z),.34f,shadowStone,false);
        }
        // Flush approach paving crosses the original garden, keeping the new routes obvious.
        BranchPath(new Vector3(side*4.2f,0,-3),new Vector3(side*17.5f,0,-3),2.6f,deck,accent);
        for(int edge=-1;edge<=1;edge+=2)
        {
            var p=new Vector3(side*29.7f,0,-3+edge*2.9f);
            DistrictColumn(p,5.0f,.65f,deck,accent);
        }
        Shape(PrimitiveType.Cube,"Island gateway · floating lintel",new Vector3(side*29.7f,5.1f,-3),new Vector3(.9f,.45f,7.2f),deck);
        Shape(PrimitiveType.Cube,"Island gateway · luminous frieze",new Vector3(side*29.18f,5.1f,-3),new Vector3(.02f,.055f,6.9f),accent,world,false);
    }

    static void BuildMoonlitArchive()
    {
        var moonStone=Mat("Archive · silver blue marble",new Color(.57f,.65f,.76f));
        var deepStone=Mat("Archive · midnight shelves",new Color(.18f,.23f,.34f));
        var silver=Mat("Archive · lunar silver",new Color(.66f,.76f,.91f),.1f);
        var violet=Mat("Archive · violet crystal",new Color(.54f,.36f,.88f),.8f);
        var ice=Mat("Archive · ice crystal",new Color(.34f,.76f,.92f),.8f);
        var ink=Mat("Archive · sleeping ink",new Color(.14f,.27f,.48f),.25f);
        Vector3[] stations={new Vector3(-46,0,-4),new Vector3(-36,0,5),new Vector3(-47,0,14)};
        BranchPath(new Vector3(-29,0,-3),stations[0],2.7f,moonStone,silver);
        BranchPath(new Vector3(-36,0,-3),stations[1],2.7f,moonStone,silver);
        BranchPath(stations[0],new Vector3(-51,0,4),2.55f,moonStone,silver);
        BranchPath(new Vector3(-51,0,4),stations[2],2.55f,moonStone,silver);
        BranchPath(stations[1],stations[2],2.55f,moonStone,silver);
        BranchPath(stations[1],new Vector3(-31,0,14),1.7f,moonStone,silver);
        for(int i=0;i<3;i++)DistrictSanctum(stations[i],i+3,moonStone,silver,ice,false);
        // The open library's facade is visible from the sanctuary and frames the moon dial.
        for(int i=0;i<6;i++)
        {
            float x=-55+i*4.65f;
            DistrictColumn(new Vector3(x,0,19.55f),i==1?5.8f:8.8f,.76f,moonStone,silver);
            if(i<5)
            {
                Shape(PrimitiveType.Cube,"Archive · entablature",new Vector3(x+2.32f,8.85f,19.55f),new Vector3(4.75f,.56f,1.15f),moonStone);
                Shape(PrimitiveType.Cube,"Archive · cornice",new Vector3(x+2.32f,9.25f,19.55f),new Vector3(4.82f,.18f,1.37f),silver,world,false);
            }
            if(i!=1)Bookcase(new Vector3(x+1.5f,0,19.85f),2.25f,5.8f,deepStone,moonStone,i);
        }
        for(int i=0;i<4;i++)
        {
            float z=-6+i*7.2f;
            DistrictColumn(new Vector3(-56.2f,0,z),i==2?3.1f:6.2f,.72f,moonStone,silver);
            if(i<3)Shape(PrimitiveType.Cube,"Archive · western gallery beam",new Vector3(-56.2f,6.3f,z+3.6f),new Vector3(1.08f,.50f,7.3f),moonStone);
        }
        // A crescent-shaped broken dome establishes the district's memorable skyline.
        Vector3 moon=new Vector3(-44,8.0f,19.45f);
        var lunarArc=Tube("Archive · great lunar crescent",moon,5.7f,.18f,silver,35,322);
        lunarArc.transform.rotation=Quaternion.Euler(90,0,-18);
        var innerArc=Tube("Archive · inner lunar engraving",moon,5.24f,.048f,violet,45,310);
        innerArc.transform.rotation=Quaternion.Euler(90,0,-18);
        var star=CrystalRock("Archive · suspended pole star",moon,new Vector3(.6f,1.45f,.6f),ice,621,false);
        star.AddComponent<AetherWorldMotion>().Set(.17f,.6f,new Vector3(0,12,0));
        for(int i=0;i<7;i++)
        {
            float a=(-60+i*20)*Mathf.Deg2Rad;
            Shape(PrimitiveType.Sphere,"Archive · constellation stone",moon+new Vector3(Mathf.Sin(a)*4.4f,Mathf.Cos(a)*4.4f,-.1f),Vector3.one*(i%2==0?.16f:.10f),ice,world,false);
        }
        // Reflection pools are flush with the floor and can be crossed without a physics pit.
        ReflectionPool(new Vector3(-43,0,6.4f),new Vector2(3.5f,5.2f),deepStone,ink,silver);
        ReflectionPool(new Vector3(-33,0,-7.9f),new Vector2(4.2f,2.1f),deepStone,ink,silver);
        Vector3[] crystalBeds={new Vector3(-54,0,-.7f),new Vector3(-55,0,9.7f),new Vector3(-31.7f,0,-7.4f),new Vector3(-32.8f,0,18.2f),new Vector3(-42.2f,0,18.4f),new Vector3(-42,0,-8.5f)};
        for(int i=0;i<crystalBeds.Length;i++)
        {
            var c=crystalBeds[i];Ring("Archive · crystal bed",c+Vector3.up*.03f,1.25f,.12f,.05f,deepStone);
            for(int j=0;j<5;j++)
            {
                float a=j*Mathf.PI*2/5;
                Vector3 p=c+new Vector3(Mathf.Cos(a)*.53f,.6f+((j+i)%3)*.28f,Mathf.Sin(a)*.53f);
                var crystal=CrystalRock("Archive · growing prism",p,new Vector3(.46f,1.4f+((j+i)%3)*.65f,.48f),j%2==0?violet:ice,640+i*5+j,false);
                crystal.transform.rotation=Quaternion.Euler(Range(-12,12),j*63,Range(-12,12));
            }
        }
        // Scattered books and desks offer human-scale detail among the monumental ruins.
        for(int i=0;i<4;i++)
        {
            Vector3 p=new Vector3(-54.1f,0,1.5f+i*4.2f);
            Shape(PrimitiveType.Cube,"Archive · reader's desk",p+Vector3.up*.85f,new Vector3(1.35f,.17f,.78f),moonStone);
            for(int side=-1;side<=1;side+=2)Shape(PrimitiveType.Cube,"Archive · desk support",p+new Vector3(side*.46f,.41f,0),new Vector3(.18f,.82f,.48f),deepStone);
            for(int j=0;j<3;j++)Book(p+new Vector3(-.4f+j*.34f,.99f,Range(-.16f,.16f)),new Vector3(.26f,.10f,.38f),j+i);
        }
        for(int i=0;i<4;i++)
        {
            var book=Book(new Vector3(-43.5f+i*.55f,2.7f+i*.3f,7.3f),new Vector3(.32f,.10f,.48f),i+4);
            book.transform.rotation=Quaternion.Euler(15,i*34,7);
            book.AddComponent<AetherWorldMotion>().Set(.16f,.6f,new Vector3(0,3,0));
        }
        Meadow(-1,Mat("Archive · lavender grass",new Color(.37f,.40f,.63f)),Mat("Archive · moonflowers",new Color(.67f,.72f,.94f),.25f));
        var motes=Fireflies(new Vector3(-43,3.4f,6),new Vector3(13,3,14),110,5);
        var particles=motes.main;particles.startColor=new ParticleSystem.MinMaxGradient(new Color(.39f,.63f,1,.4f),new Color(.79f,.58f,1,.75f));
        ColoredLantern(new Vector3(-31,0,2),moonStone,ice);
        ColoredLantern(new Vector3(-40,0,-6),moonStone,ice);
        ColoredLantern(new Vector3(-51,0,8),moonStone,ice);
        ColoredLantern(new Vector3(-35,0,13),moonStone,ice);
        BuildDistrictPerimeter(-1,moonStone,silver);
    }

    static void BuildSolarObservatory()
    {
        var sunStone=Mat("Observatory · warm marble",new Color(.79f,.69f,.48f));
        var sunGold=Mat("Observatory · Solar Gold",new Color(.92f,.59f,.16f),.10f);
        var copper=Mat("Observatory · oxidised copper",new Color(.30f,.39f,.30f));
        var amber=Mat("Observatory · living amber",new Color(1,.58f,.14f),1.1f);
        var redLeaves=Mat("Observatory · copper leaves",new Color(.74f,.32f,.15f));
        var goldLeaves=Mat("Observatory · golden leaves",new Color(.83f,.56f,.18f));
        Vector3[] stations={new Vector3(36,0,-4),new Vector3(47,0,5),new Vector3(39,0,15)};
        BranchPath(new Vector3(29,0,-3),stations[0],2.7f,sunStone,sunGold);
        BranchPath(stations[0],stations[1],2.8f,sunStone,sunGold);
        BranchPath(stations[0],stations[2],2.7f,sunStone,sunGold);
        BranchPath(stations[1],stations[2],2.7f,sunStone,sunGold);
        BranchPath(stations[1],new Vector3(51,0,14.7f),2.5f,sunStone,sunGold);
        BranchPath(stations[2],new Vector3(31,0,15),1.7f,sunStone,sunGold);
        for(int i=0;i<3;i++)DistrictSanctum(stations[i],i+6,sunStone,sunGold,amber,true);
        // A great armillary sphere dominates the eastern skyline. Its rings move independently.
        Vector3 c=new Vector3(50.6f,0,14.9f);
        Shape(PrimitiveType.Cylinder,"Observatory · armillary foundation",c+Vector3.up*.13f,new Vector3(8,.13f,8),copper,world,false);
        Ring("Observatory · armillary brass dais",c+Vector3.up*.28f,3.65f,.20f,.025f,sunGold);
        Shape(PrimitiveType.Cylinder,"Observatory · armillary plinth",c+Vector3.up*.75f,new Vector3(2.1f,.75f,2.1f),sunStone);
        Shape(PrimitiveType.Cylinder,"Observatory · armillary axle",c+Vector3.up*2.1f,new Vector3(.62f,1.30f,.62f),sunGold);
        Vector3 heart=c+Vector3.up*5.25f;
        for(int i=0;i<3;i++)
        {
            var axis=Tube("Observatory · rotating celestial sphere "+i,heart,3.25f+i*.28f,i==0?.105f:.078f,i==1?copper:sunGold);
            axis.transform.rotation=Quaternion.Euler(i==0?74:32+i*40,i*58,23+i*19);
            axis.AddComponent<AetherWorldMotion>().Set(0,0,new Vector3(0,i%2==0?3:-2,0));
        }
        Shape(PrimitiveType.Sphere,"Observatory · suspended sun",heart,Vector3.one*.73f,amber,world,false).AddComponent<AetherWorldMotion>().Set(.12f,.5f,new Vector3(0,9,0));
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            var ray=Shape(PrimitiveType.Cube,"Observatory · dial hour",c+new Vector3(Mathf.Sin(a)*3.2f,.305f,Mathf.Cos(a)*3.2f),new Vector3(.08f,.022f,i%3==0?.47f:.23f),sunGold,world,false);
            ray.transform.rotation=Quaternion.Euler(0,i*30,0);
        }
        // Slender pylons and a broken solar colonnade frame the far edge without boxing it in.
        for(int i=0;i<5;i++)
        {
            Vector3 p=new Vector3(32+i*5.2f,0,20);
            DistrictColumn(p,i==2?4.3f:7.2f,.7f,sunStone,sunGold);
            if(i<4&&i!=2)Shape(PrimitiveType.Cube,"Observatory · northern beam",p+new Vector3(2.6f,7.3f,0),new Vector3(5.4f,.48f,1.12f),sunStone);
        }
        for(int i=0;i<3;i++)
        {
            Vector3 p=new Vector3(55.8f,0,-4+i*5.2f);
            DistrictColumn(p,5.7f,.64f,sunStone,sunGold);
            var symbol=Tube("Observatory · sun standard",p+Vector3.up*6.4f,.66f,.065f,sunGold);
            symbol.transform.rotation=Quaternion.Euler(90,0,0);
            Shape(PrimitiveType.Sphere,"Observatory · standard heart",p+Vector3.up*6.4f,Vector3.one*.15f,amber,world,false);
        }
        // Geometric beds recall terraced orchards, with distinct copper and amber canopies.
        Vector3[] trees={new Vector3(32,0,-8.1f),new Vector3(44,0,-8.5f),new Vector3(54.3f,0,-8.6f),new Vector3(32,0,7.5f),new Vector3(54.5f,0,1.1f),new Vector3(44.3f,0,18.9f)};
        for(int i=0;i<trees.Length;i++)AmberTree(trees[i],i%2==0?.88f:1.05f,i%2==0?goldLeaves:redLeaves,sunStone,i);
        ReflectionPool(new Vector3(45.5f,0,-2.8f),new Vector2(4.5f,1.6f),copper,Mat("Observatory · amber reflecting pool",new Color(.35f,.34f,.12f),.2f),sunGold);
        ReflectionPool(new Vector3(34.2f,0,11.5f),new Vector2(2.2f,2.7f),copper,Mat("Observatory · water",new Color(.20f,.41f,.32f),.15f),sunGold);
        // A bronze telescope invites a quiet view over the golden cloud sea.
        Vector3 telescope=new Vector3(54.2f,0,10.2f);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2/3;
            Beam("Observatory · telescope tripod",telescope+new Vector3(Mathf.Sin(a)*.8f,.05f,Mathf.Cos(a)*.8f),telescope+Vector3.up*1.45f,.13f,sunGold,true);
        }
        Beam("Observatory · long-range telescope",telescope+new Vector3(-.55f,1.55f,-.45f),telescope+new Vector3(1.05f,2.2f,.90f),.48f,copper,false);
        Meadow(1,Mat("Observatory · sun grasses",new Color(.58f,.53f,.24f)),Mat("Observatory · golden flowers",new Color(.98f,.71f,.27f),.22f));
        var motes=Fireflies(new Vector3(43,3,5),new Vector3(13,2.8f,14),100,4.8f);
        var particles=motes.main;particles.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.61f,.19f,.4f),new Color(1,.85f,.45f,.8f));
        ColoredLantern(new Vector3(32,0,.4f),sunStone,amber);
        ColoredLantern(new Vector3(41,0,3.3f),sunStone,amber);
        ColoredLantern(new Vector3(43,0,11.8f),sunStone,amber);
        ColoredLantern(new Vector3(49,0,-6.2f),sunStone,amber);
        BuildDistrictPerimeter(1,sunStone,sunGold);
    }

    static void BranchPath(Vector3 from,Vector3 to,float width,Material surface,Material accent)
    {
        Vector3 delta=to-from;delta.y=0;int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/1.4f));
        for(int i=0;i<count;i++)
        {
            Vector3 p=Vector3.Lerp(from,to,(i+.5f)/count);
            var slab=Shape(PrimitiveType.Cube,"Island route · limestone paver",p+Vector3.up*.025f,new Vector3(width,.05f,delta.magnitude/count-.075f),surface,world,false);
            slab.transform.rotation=Quaternion.LookRotation(delta.normalized);
            if(i%3==0)
            {
                var line=Shape(PrimitiveType.Cube,"Island route · luminous dash",p+Vector3.up*.054f,new Vector3(.04f,.01f,.35f),accent,world,false);
                line.transform.rotation=slab.transform.rotation;
            }
        }
    }

    static void DistrictSanctum(Vector3 c,int index,Material marble,Material metal,Material glow,bool solar)
    {
        Shape(PrimitiveType.Cylinder,"Sanctum "+(index+1)+" · engraved terrace",c+Vector3.up*.06f,new Vector3(6.2f,.06f,6.2f),marble,world,false);
        Ring("Sanctum · outer instrument ring",c+Vector3.up*.13f,3.02f,.065f,.025f,metal);
        Ring("Sanctum · inner instrument ring",c+Vector3.up*.135f,2.68f,.025f,.025f,metal);
        for(int side=-1;side<=1;side+=2)
        {
            Vector3 p=c+new Vector3(side*2.78f,0,1.4f);
            DistrictColumn(p,3.4f,.42f,marble,metal);
            ColoredLantern(c+new Vector3(side*2.85f,0,-1.25f),marble,glow);
        }
        Vector3 back=c+new Vector3(0,0,3.12f);
        Shape(PrimitiveType.Cube,"Sanctum · beacon plinth",back+Vector3.up*.18f,new Vector3(.75f,.36f,.65f),marble);
        Shape(PrimitiveType.Cube,"Sanctum · beacon spine",back+Vector3.up*1.8f,new Vector3(.26f,3.25f,.26f),marble);
        for(int i=0;i<3;i++)Shape(PrimitiveType.Cube,"Sanctum · beacon collar",back+Vector3.up*(.7f+i*1.1f),new Vector3(.34f,.08f,.34f),metal,world,false);
        var sphere=Shape(PrimitiveType.Sphere,"Beacon "+(index+1)+" · dormant star",back+Vector3.up*3.75f,Vector3.one*.32f,Mat("Dormant beacon",new Color(.35f,.66f,.61f),.30f),world,false);
        BeaconTops[index]=sphere.transform;sphere.AddComponent<AetherWorldMotion>().Set(.075f,.75f,new Vector3(0,9,0));
        var halo=Tube("Sanctum · lunar or solar seal",back+Vector3.up*3.75f,solar?.64f:.59f,.045f,metal,solar?0:35,solar?360:325);
        halo.transform.rotation=Quaternion.Euler(90,0,solar?0:-24);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;
            var marker=Shape(PrimitiveType.Cube,"Sanctum · radial inscription",c+new Vector3(Mathf.Sin(a)*2.84f,.17f,Mathf.Cos(a)*2.84f),new Vector3(.065f,.02f,.14f),metal,world,false);
            marker.transform.rotation=Quaternion.Euler(0,i*45,0);
        }
    }

    static void DistrictColumn(Vector3 p,float height,float width,Material marble,Material metal)
    {
        Shape(PrimitiveType.Cube,"District column · plinth",p+Vector3.up*.15f,new Vector3(width*1.6f,.30f,width*1.6f),marble);
        Shape(PrimitiveType.Cube,"District column · shaft",p+Vector3.up*(height*.5f),new Vector3(width,height,width),marble);
        Shape(PrimitiveType.Cube,"District column · capital",p+Vector3.up*height,new Vector3(width*1.5f,.23f,width*1.5f),marble);
        Shape(PrimitiveType.Cube,"District column · collar",p+Vector3.up*(height-.28f),new Vector3(width*1.04f,.09f,width*1.04f),metal,world,false);
        Shape(PrimitiveType.Cube,"District column · front inlay",p+new Vector3(0,height*.52f,-width*.506f),new Vector3(.038f,height*.65f,.012f),metal,world,false);
    }

    static void Bookcase(Vector3 p,float width,float height,Material frame,Material trim,int seed)
    {
        Shape(PrimitiveType.Cube,"Archive · bookcase back",p+Vector3.up*(height*.5f),new Vector3(width,height,.33f),frame);
        for(int side=-1;side<=1;side+=2)Shape(PrimitiveType.Cube,"Archive · bookcase jamb",p+new Vector3(side*width*.5f,height*.5f,-.25f),new Vector3(.16f,height,.58f),trim);
        for(int row=0;row<5;row++)
        {
            float y=.25f+row*1.10f;
            Shape(PrimitiveType.Cube,"Archive · shelf",p+new Vector3(0,y,-.28f),new Vector3(width,.13f,.72f),trim,world,false);
            for(int i=0;i<7;i++)
            {
                if((i+row+seed)%6==0)continue;
                float h=.43f+(i+seed+row)%3*.11f;
                Book(p+new Vector3(-width*.4f+i*width*.13f,y+.09f+h*.5f,-.35f),new Vector3(width*.085f,h,.30f),i+row+seed);
            }
        }
    }

    static GameObject Book(Vector3 p,Vector3 size,int index)
    {
        Color[] colors={new Color(.22f,.33f,.52f),new Color(.40f,.26f,.50f),new Color(.27f,.43f,.48f),new Color(.55f,.42f,.25f),new Color(.35f,.38f,.57f)};
        var cover=Mat("Archive · book binding "+index%5,colors[index%5]);
        var book=Shape(PrimitiveType.Cube,"Archive · lost volume",p,size,cover,world,false);
        var spine=Shape(PrimitiveType.Cube,"Archive · gilded book spine",p+new Vector3(0,0,-size.z*.505f),new Vector3(size.x*.72f,size.y*.065f,.008f),gold,world,false);
        spine.transform.SetParent(book.transform,true);
        return book;
    }

    static void ReflectionPool(Vector3 c,Vector2 size,Material curb,Material water,Material trim)
    {
        Shape(PrimitiveType.Cube,"Garden · mirror water",c+Vector3.up*.016f,new Vector3(size.x,.027f,size.y),water,world,false);
        for(int side=-1;side<=1;side+=2)
        {
            Shape(PrimitiveType.Cube,"Garden · reflecting pool edge",c+new Vector3(side*(size.x*.5f+.06f),.045f,0),new Vector3(.12f,.09f,size.y+.24f),curb,world,false);
            Shape(PrimitiveType.Cube,"Garden · reflecting pool edge",c+new Vector3(0,.045f,side*(size.y*.5f+.06f)),new Vector3(size.x+.24f,.09f,.12f),curb,world,false);
        }
        Ring("Garden · reflection ripple",c+Vector3.up*.035f,Mathf.Min(size.x,size.y)*.31f,.019f,.005f,trim);
    }

    static void AmberTree(Vector3 p,float scale,Material leaves,Material curb,int index)
    {
        Shape(PrimitiveType.Cylinder,"Solar orchard · trunk",p+Vector3.up*(2.1f*scale),new Vector3(.35f*scale,2.1f*scale,.35f*scale),bark);
        Beam("Solar orchard · branch",p+Vector3.up*(2.2f*scale),p+new Vector3(1,3.1f,.3f)*scale,.17f*scale,bark,false);
        Beam("Solar orchard · branch",p+Vector3.up*(2.6f*scale),p+new Vector3(-.9f,3.5f,-.3f)*scale,.17f*scale,bark,false);
        CrystalRock("Solar orchard · crown",p+new Vector3(.05f,4.1f,0)*scale,new Vector3(3.05f,1.65f,2.55f)*scale,leaves,770+index,false);
        CrystalRock("Solar orchard · crown",p+new Vector3(1,3.4f,.35f)*scale,new Vector3(1.65f,1.12f,1.6f)*scale,leaves,780+index,false);
        Ring("Solar orchard · tree curb",p+Vector3.up*.025f,1.92f*scale,.13f,.055f,curb);
    }

    static void ColoredLantern(Vector3 p,Material body,Material glow)
    {
        Shape(PrimitiveType.Cube,"District waylight · base",p+Vector3.up*.10f,new Vector3(.38f,.20f,.38f),body,world,false);
        Shape(PrimitiveType.Cube,"District waylight · stem",p+Vector3.up*.45f,new Vector3(.18f,.65f,.18f),body,world,false);
        Shape(PrimitiveType.Cube,"District waylight · cap",p+Vector3.up*.82f,new Vector3(.34f,.12f,.34f),gold,world,false);
        Shape(PrimitiveType.Sphere,"District waylight · heart",p+Vector3.up*1.03f,new Vector3(.13f,.25f,.13f),glow,world,false);
    }

    static bool DistrictReserved(Vector3 p,int side)
    {
        Vector2 q=new Vector2(p.x,p.z);
        Vector2[] points=side<0?new[]{new Vector2(-46,-4),new Vector2(-36,5),new Vector2(-47,14),new Vector2(-54,-6),new Vector2(-31,14),new Vector2(-53,17)}:new[]{new Vector2(36,-4),new Vector2(47,5),new Vector2(39,15),new Vector2(53,-7),new Vector2(31,15),new Vector2(54,18)};
        for(int i=0;i<points.Length;i++)if(Vector2.Distance(q,points[i])<(i<3?3.55f:1.3f))return true;
        return false;
    }

    static void Meadow(int side,Material blades,Material flowers)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<1950;i++)
        {
            Vector3 p=new Vector3(side*43+Range(-13.7f,13.7f),.015f,Range(-9.5f,19.7f));
            if(DistrictReserved(p,side)||Mathf.Abs(p.z+3)<1.8f||DistrictOnPath(p,side))continue;
            float a=Range(0,Mathf.PI*2),w=Range(.035f,.075f),h=Range(.12f,.39f);
            Vector3 cross=new Vector3(Mathf.Cos(a)*w,0,Mathf.Sin(a)*w);int n=v.Count;
            Vector3 tip=p+Vector3.up*h+Vector3.right*.06f;
            v.Add(p-cross);v.Add(p+cross);v.Add(tip);
            v.Add(p+cross);v.Add(p-cross);v.Add(tip);
            t.Add(n);t.Add(n+1);t.Add(n+2);t.Add(n+3);t.Add(n+4);t.Add(n+5);
            if(i%28==0)Shape(PrimitiveType.Sphere,"District meadow · luminous flower",p+Vector3.up*(h+.025f),new Vector3(.09f,.05f,.09f),flowers,world,false);
        }
        CreateMesh("District · wind-combed meadow",v,t,blades,false);
    }

    static bool DistrictOnPath(Vector3 p,int side)
    {
        Vector2[] nodes=side<0?new[]{new Vector2(-46,-4),new Vector2(-51,4),new Vector2(-47,14),new Vector2(-36,5),new Vector2(-36,-3)}:new[]{new Vector2(36,-4),new Vector2(47,5),new Vector2(39,15),new Vector2(36,-4)};
        Vector2 q=new Vector2(p.x,p.z);
        for(int i=0;i<nodes.Length-1;i++)
        {
            Vector2 d=nodes[i+1]-nodes[i];float u=Mathf.Clamp01(Vector2.Dot(q-nodes[i],d)/d.sqrMagnitude);
            if(Vector2.Distance(q,nodes[i]+u*d)<1.6f)return true;
        }
        return false;
    }

    static void BuildDistrictPerimeter(int side,Material edge,Material inlay)
    {
        float c=side*43;
        for(int i=0;i<9;i++)
        {
            float x=c-12+i*3;
            Shape(PrimitiveType.Cube,"District boundary · low edging",new Vector3(x,.13f,-10.5f),new Vector3(2.55f,.26f,.44f),edge);
            if(i%2==0)Shape(PrimitiveType.Cube,"District boundary · engraved cap",new Vector3(x,.27f,-10.5f),new Vector3(.7f,.025f,.3f),inlay,world,false);
        }
        for(int edgeSide=-1;edgeSide<=1;edgeSide+=2)
        {
            float x=c+edgeSide*14.45f;
            for(int i=0;i<9;i++)
            {
                float z=-8+i*3.15f;
                // Leave a generous 5.5m opening at the bridge's landing.
                if(edgeSide==-side&&z>-6&&z<0)continue;
                Shape(PrimitiveType.Cube,"District boundary · low edging",new Vector3(x,.13f,z),new Vector3(.44f,.26f,2.6f),edge);
            }
        }
    }

    static void BatchDistrict(Transform root)
    {
        // Combine static scenery per material, retaining individual collision shapes and animated props.
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var originals=new List<MeshRenderer>();
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null||!renderer.enabled||renderer.sharedMaterial==null||filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
            bool moving=false;
            for(Transform p=filter.transform;p!=null&&p!=root;p=p.parent)if(p.GetComponent<AetherWorldMotion>()!=null){moving=true;break;}
            if(moving)continue;
            List<CombineInstance> group;
            if(!groups.TryGetValue(renderer.sharedMaterial,out group)){group=new List<CombineInstance>();groups.Add(renderer.sharedMaterial,group);}
            group.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix});
            originals.Add(renderer);
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh();mesh.name="District scenery · "+pair.Key.name;mesh.indexFormat=IndexFormat.UInt32;
            mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            var go=new GameObject(mesh.name);go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
        }
        foreach(var renderer in originals)renderer.enabled=false;
    }

    static void StonePost(Vector3 p,float height,float width,bool collision)
    {
        Shape(PrimitiveType.Cube,"Column · lower plinth",p+Vector3.up*.14f,new Vector3(width*1.65f,.28f,width*1.65f),shadowStone,world,collision);
        Shape(PrimitiveType.Cube,"Column · upper plinth",p+Vector3.up*.36f,new Vector3(width*1.36f,.16f,width*1.36f),paleStone,world,collision);
        Shape(PrimitiveType.Cube,"Column · shaft",p+Vector3.up*(height*.5f+.15f),new Vector3(width,height-.3f,width),paleStone,world,collision);
        Shape(PrimitiveType.Cube,"Column · capital",p+Vector3.up*(height-.06f),new Vector3(width*1.5f,.25f,width*1.5f),stone,world,collision);
        Shape(PrimitiveType.Cube,"Column · gold collar",p+Vector3.up*(height-.32f),new Vector3(width*1.04f,.10f,width*1.04f),gold,world,false);
        // Thin front grooves make the rectangular columns feel dressed and authored.
        for(int i=-1;i<=1;i++) Shape(PrimitiveType.Cube,"Column · fluting",p+new Vector3(i*width*.25f,height*.52f,-width*.505f),new Vector3(.028f,height*.62f,.015f),shadowStone,world,false);
    }

    static void Lantern(Vector3 p)
    {
        Shape(PrimitiveType.Cube,"Waylight · base",p+Vector3.up*.12f,new Vector3(.48f,.24f,.48f),shadowStone,world,false);
        Shape(PrimitiveType.Cube,"Waylight · stem",p+Vector3.up*.43f,new Vector3(.24f,.48f,.24f),paleStone,world,false);
        Shape(PrimitiveType.Cube,"Waylight · cap",p+Vector3.up*.72f,new Vector3(.42f,.11f,.42f),gold,world,false);
        var orb=Shape(PrimitiveType.Sphere,"Waylight · flame",p+Vector3.up*.90f,new Vector3(.14f,.23f,.14f),whiteGlow,world,false);
        var light=orb.AddComponent<Light>();
        light.type=LightType.Point;light.color=new Color(1,.71f,.30f);light.intensity=.55f;light.range=3.5f;light.shadows=LightShadows.None;
    }

    static GameObject Beam(string name,Vector3 start,Vector3 end,float width,Material material,bool collision)
    {
        var delta=end-start;
        var go=Shape(PrimitiveType.Cylinder,name,(start+end)*.5f,new Vector3(width,delta.magnitude*.5f,width),material,world,collision);
        go.transform.up=delta.normalized;return go;
    }

    static GameObject Ring(string name,Vector3 p,float radius,float width,float height,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>(); const int count=80;
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            int n=v.Count;
            v.Add(p+new Vector3(Mathf.Cos(a)*(radius-width),height,Mathf.Sin(a)*(radius-width)));
            v.Add(p+new Vector3(Mathf.Cos(a)*radius,height,Mathf.Sin(a)*radius));
            v.Add(p+new Vector3(Mathf.Cos(b)*radius,height,Mathf.Sin(b)*radius));
            v.Add(p+new Vector3(Mathf.Cos(b)*(radius-width),height,Mathf.Sin(b)*(radius-width)));
            t.Add(n);t.Add(n+2);t.Add(n+1);t.Add(n);t.Add(n+3);t.Add(n+2);
        }
        return CreateMesh(name,v,t,mat,false);
    }

    static GameObject Tube(string name,Vector3 p,float radius,float tube,Material mat,float from=0,float to=360)
    {
        var v=new List<Vector3>();var t=new List<int>();const int segments=80,sides=8;
        for(int i=0;i<=segments;i++)
        {
            float a=Mathf.Lerp(from,to,i/(float)segments)*Mathf.Deg2Rad;
            for(int j=0;j<sides;j++)
            {
                float b=j*Mathf.PI*2/sides,r=radius+Mathf.Cos(b)*tube;
                v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*tube,Mathf.Sin(a)*r));
            }
        }
        for(int i=0;i<segments;i++)for(int j=0;j<sides;j++)
        {
            int a=i*sides+j,b=i*sides+(j+1)%sides,c=(i+1)*sides+j,d=(i+1)*sides+(j+1)%sides;
            t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);
        }
        var go=CreateMesh(name,v,t,mat,false);go.transform.position=p;return go;
    }

    static GameObject CrystalRock(string name,Vector3 p,Vector3 size,Material mat,int seed,bool collision)
    {
        var random=new System.Random(seed);const int sides=7;
        var ring=new Vector3[sides];
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            float r=.72f+(float)random.NextDouble()*.28f;
            ring[i]=new Vector3(Mathf.Cos(a)*size.x*.5f*r,((float)random.NextDouble()-.5f)*size.y*.3f,Mathf.Sin(a)*size.z*.5f*r);
        }
        var v=new List<Vector3>();var t=new List<int>();
        Vector3 top=new Vector3(size.x*.08f,size.y*.5f,size.z*.03f),bottom=new Vector3(-size.x*.1f,-size.y*.45f,-size.z*.06f);
        for(int i=0;i<sides;i++)
        {
            int n=v.Count;v.Add(top);v.Add(ring[(i+1)%sides]);v.Add(ring[i]);t.Add(n);t.Add(n+1);t.Add(n+2);
            n=v.Count;v.Add(bottom);v.Add(ring[i]);v.Add(ring[(i+1)%sides]);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        var go=CreateMesh(name,v,t,mat,collision);go.transform.position=p;return go;
    }

    static GameObject CreateMesh(string name,List<Vector3> vertices,List<int> triangles,Material mat,bool collision)
    {
        var go=new GameObject(name);go.transform.SetParent(world,false);
        var mesh=new Mesh();mesh.name=name;
        if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        var normals=mesh.normals;
        for(int i=0;i<normals.Length;i++)
        {
            float length=normals[i].sqrMagnitude;
            if(length<.000001f||float.IsNaN(length)||float.IsInfinity(length))normals[i]=Vector3.up;
        }
        mesh.normals=normals;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;
        return go;
    }

    static ParticleSystem Fireflies(Vector3 center,Vector3 volume,int count,float rate)
    {
        var go=new GameObject("Drifting motes of aether");go.transform.SetParent(world,false);go.transform.position=center;
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.prewarm=true;main.duration=18;main.startLifetime=new ParticleSystem.MinMaxCurve(10,18);main.startSpeed=new ParticleSystem.MinMaxCurve(.015f,.075f);main.startSize=new ParticleSystem.MinMaxCurve(.018f,.060f);main.maxParticles=count;
        main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.88f,.47f,.3f),new Color(.62f,1,.83f,.85f));main.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=rate;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=volume*2;
        var noise=ps.noise;noise.enabled=true;noise.strength=.16f;noise.frequency=.21f;noise.scrollSpeed=.07f;
        var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.25f),new GradientAlphaKey(.6f,.75f),new GradientAlphaKey(0,1)});colors.color=gradient;
        var renderer=go.GetComponent<ParticleSystemRenderer>();
        Shader shader=Shader.Find("Legacy Shaders/Particles/Additive");if(shader==null)shader=Shader.Find("Particles/Standard Unlit");
        var material=new Material(shader);material.name="Soft firefly points";
        var texture=new Texture2D(32,32,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float d=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/15.5f;float alpha=Mathf.Pow(Mathf.Clamp01(1-d),2.4f);
            texture.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        texture.Apply();material.mainTexture=texture;renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;ps.Play();return ps;
    }

    static float Range(float min,float max){return min+(float)rng.NextDouble()*(max-min);}
}

public sealed class AetherWorldMotion : MonoBehaviour
{
    Vector3 origin,rotation;float amplitude,speed,phase;
    public void Set(float a,float s,Vector3 r){origin=transform.position;amplitude=a;speed=s;rotation=r;phase=origin.x*.73f+origin.z*.31f;}
    void Update()
    {
        if(AetherGame.I!=null&&AetherGame.I.ReducedMotion)return;
        transform.position=origin+Vector3.up*(Mathf.Sin(Time.time*speed+phase)*amplitude);
        transform.Rotate(rotation*Time.deltaTime,Space.Self);
    }
}

/// <summary>Applies the same restrained visual finish to the title and traveller cameras.</summary>
public sealed class AetherWorldAtmosphere : MonoBehaviour
{
    float nextCameraCheck;
    void LateUpdate()
    {
        if(Time.unscaledTime<nextCameraCheck)return;
        nextCameraCheck=Time.unscaledTime+1;
        foreach(var camera in Camera.allCameras)
        {
            var finish=camera.GetComponent<AetherAtmosphericFinish>();
            if(finish==null)
            {
                camera.allowHDR=true;
                finish=camera.gameObject.AddComponent<AetherAtmosphericFinish>();
            }
            finish.enabled=AetherGame.I==null||AetherGame.I.QualityLevel>0;
        }
    }
}

[RequireComponent(typeof(Camera))]
public sealed class AetherAtmosphericFinish : MonoBehaviour
{
    Material finish;
    void OnEnable()
    {
        var shader=Shader.Find("Hidden/Aether/AtmosphericFinish");
        if(shader!=null&&shader.isSupported)finish=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
    }
    void OnDisable(){if(finish!=null)Destroy(finish);}
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(finish==null){Graphics.Blit(source,destination);return;}
        int width=Mathf.Max(1,source.width/2),height=Mathf.Max(1,source.height/2);
        var format=SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32;
        var a=RenderTexture.GetTemporary(width,height,0,format);
        var b=RenderTexture.GetTemporary(width,height,0,format);
        a.filterMode=FilterMode.Bilinear;b.filterMode=FilterMode.Bilinear;
        try
        {
            finish.SetFloat("_Threshold",1.08f);
            Graphics.Blit(source,a,finish,0);
            for(int i=0;i<2;i++)
            {
                finish.SetVector("_BlurDirection",new Vector4((1.3f+i)/width,0,0,0));Graphics.Blit(a,b,finish,1);
                finish.SetVector("_BlurDirection",new Vector4(0,(1.3f+i)/height,0,0));Graphics.Blit(b,a,finish,1);
            }
            finish.SetTexture("_BloomTex",a);finish.SetFloat("_BloomStrength",.13f);
            Graphics.Blit(source,destination,finish,2);
        }
        finally {RenderTexture.ReleaseTemporary(a);RenderTexture.ReleaseTemporary(b);}
    }
}

Shader "Aether/Atmosphere"
{
    Properties
    {
        _Zenith ("High sky", Color) = (0.035,0.15,0.21,1)
        _Horizon ("Horizon haze", Color) = (0.47,0.65,0.62,1)
        _Nadir ("Cloud sea", Color) = (0.16,0.33,0.38,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Zenith, _Horizon, _Nadir;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 direction : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.direction=v.vertex.xyz; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d=normalize(i.direction);
                float skyHeight=saturate(d.y);
                float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(skyHeight,0.62));
                color=lerp(color,_Nadir.rgb,saturate(-d.y*3.0));
                float3 sunDir=normalize(float3(-.52,.45,.71));
                float distanceToSun=distance(d,sunDir);
                float glow=exp(-distanceToSun*distanceToSun*6.0);
                color+=float3(.20,.12,.028)*glow;
                float disc=1-smoothstep(.047,.050,distanceToSun);
                color=lerp(color,float3(1.0,.91,.64),disc*.95);
                // Quiet horizontal wisps: broad enough to remain calm while moving through the scene.
                float wisp=sin(d.x*13+d.z*9+sin(d.x*5)*2.3)*.5+.5;
                wisp=pow(wisp,12)*exp(-pow((d.y-.13)*15,2));
                color+=wisp*float3(.065,.078,.068);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
    Fallback Off
}

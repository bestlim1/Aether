Shader "Hidden/Aether/AtmosphericFinish"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        sampler2D _BloomTex;
        float4 _BlurDirection;
        float _Threshold;
        float _BloomStrength;
        float3 FiniteColor(float3 c)
        {
            // A single invalid lighting sample must never spread through a blur kernel.
            c.r=(c.r>=0 && c.r<65500)?min(c.r,32.0):0;
            c.g=(c.g>=0 && c.g<65500)?min(c.g,32.0):0;
            c.b=(c.b>=0 && c.b<65500)?min(c.b,32.0):0;
            return c;
        }
        float4 Extract(v2f_img i) : SV_Target
        {
            float3 c=FiniteColor(tex2D(_MainTex,i.uv).rgb);
            float brightness=max(c.r,max(c.g,c.b));
            float contribution=max(0.0,brightness-_Threshold)/max(brightness,.001);
            return float4(c*saturate(contribution),1);
        }
        float4 Blur(v2f_img i) : SV_Target
        {
            float2 d=_BlurDirection.xy;
            float3 c=FiniteColor(tex2D(_MainTex,i.uv).rgb)*.227027;
            c+=FiniteColor(tex2D(_MainTex,i.uv+d*1.384615).rgb)*.316216;
            c+=FiniteColor(tex2D(_MainTex,i.uv-d*1.384615).rgb)*.316216;
            c+=FiniteColor(tex2D(_MainTex,i.uv+d*3.230769).rgb)*.070270;
            c+=FiniteColor(tex2D(_MainTex,i.uv-d*3.230769).rgb)*.070270;
            return float4(c,1);
        }
        float4 Finish(v2f_img i) : SV_Target
        {
            float3 c=FiniteColor(tex2D(_MainTex,i.uv).rgb);
            c+=FiniteColor(tex2D(_BloomTex,i.uv).rgb)*_BloomStrength;
            // Retain the scene's authored palette; only soften very bright HDR highlights.
            float luminance=dot(c,float3(.2126,.7152,.0722));
            c/=1+max(0,luminance-1)*.13;
            c=lerp(c,c*1.055-.012,.35);
            float2 edge=(i.uv-.5)*2;
            c*=1-.075*smoothstep(.35,1.7,dot(edge,edge));
            return float4(max(c,0),1);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Extract
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment Finish
            ENDCG
        }
    }
    Fallback Off
}

Shader "JFA_Analysis"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            // https://www.shadertoy.com/view/4djSRW
            float3 hash31(float p)
            {
               float3 p3 = frac(float3(1,1,1)*p * float3(.1031, .1030, .0973));
               p3 += dot(p3, p3.yzx+33.33);
               return frac((p3.xxy+p3.yzz)*p3.zyx); 
            }

            float3 frag (v2f i) : SV_Target
            {
              float3 sampleData= tex2Dlod(_MainTex,float4(i.uv,0,0)).xyz;
              return  sampleData.xyz*pow(1-distance(sampleData.xy,i.uv),2);
            }
            ENDCG
        }
    }
}

Shader "JFA"
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

            int _pass;
            int _maxPasses;
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

            float4 frag (v2f i) : SV_Target
            {
                _pass = clamp(_pass, 0.0, _maxPasses);
                float2 step = floor(exp2(_maxPasses - _pass)) *_MainTex_TexelSize.xy;
                float3 data = tex2Dlod(_MainTex, float4(i.uv,0,0)).xyz;
                float2 pos = data.xy;
                float id= data.z;
                float minDist = 99999999.9;
                float2 bestpos = pos;
                float dist =0;
                float bestId=0;
                
                for (float y = -1; y <= 1; y+=1) {
                     for (float x = -1; x <= 1; x+=1) {
                        float2 sampleCoord = i.uv + float2(x,y)*step ;
                        float3 sampleData = tex2Dlod(_MainTex,float4(sampleCoord,0,0)).xyz;
                        dist = distance(sampleData.xy,i.uv);
                        if((sampleData.x!=0 || sampleData.y!=0) && dist<minDist ){
                            minDist = dist;
                            bestpos = sampleData.xy;
                            bestId = sampleData.z;
                        }
                    }
                }
                return float4(bestpos,bestId,0);
            }
            ENDCG
        }
    }
}

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
            
            uniform float4 translate;
            uniform bool showDistance;
            uniform bool filterDistance;
            uniform bool boxFilterDistance;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            float3 hash31(float p)
            {
               float3 p3 = frac(float3(1,1,1)*p * float3(.1031, .1030, .0973));
               p3 += dot(p3, p3.yzx+33.33);
               return frac((p3.xxy+p3.yzz)*p3.zyx); 
            }
                       
            float almostIdentity( float x, float m, float n )
            {
                if( x>m ) return x;
                 float a = 2.0*n - m;
                 float b = 2.0*m - 3.0*n;
                 float t = x/m;
                return (a*t + b)*t*t + n;
            }

            float3 BoxFilter(float2 uv,float2 seedcoords){
              float3 acc =0;
                for(int i = -5; i <= 5; i++){
                     for(int j = -5; j <= 5; j++){
                       float2 uvtemp = uv + float2(i, j )*_MainTex_TexelSize;
     	             	acc += distance(seedcoords,uvtemp);
                     }
                }
                return acc/(11*11);
            }
 
            float4 frag (v2f i) : SV_Target
            {
              float2 uvFocus = ((i.uv*2-1))*translate.z+(translate.xy);
              uvFocus =  (uvFocus*0.5+0.5);
              float3 sampleData= tex2D(_MainTex,uvFocus).xyz;
              if(showDistance){
               float d= distance(sampleData.xy,uvFocus);
                 d =(almostIdentity(d,0.5,0));
                 return ( d);
              }
			  if(filterDistance){
               float d= distance(sampleData.xy,uvFocus);
                 d =(almostIdentity(d,0.5,0));
                 return (1- d);
              }
              if(boxFilterDistance){
                 float d= BoxFilter(uvFocus,sampleData.xy);
                  d =(almostIdentity(d,0.5,0));
                 return float4(float3(1,1,1)*exp (-d*10),1);
              }
              return  float4(hash31((sampleData.z)*1013) ,1);
            }
            ENDCG
        }
    }
}

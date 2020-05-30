Shader "Shaders/ToonBound"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Main Tex", 2D) = "white" {}
        _Ramp ("Ramp Texture", 2D) = "white" {}                  //控制漫反射色调的渐变纹理
        _Outline ("Outline", Range(0, 1)) = 0.1                  //控制轮廓线宽度
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1) //轮廓线颜色
        _Specular ("Specular", Color) = (1, 1, 1, 1)          //高光反色颜色
        _SpecularScale ("Specular Scale", Range(0, 0.1)) = 0.01 //高光反射系数阈值
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            //命名Pass块，以便复用
            NAME "OUTLINE"
            //剔除正面
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            float _Outline;
            fixed4 _OutlineColor;

            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            }; 
            
            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (a2v v) {
                v2f o;
                //让描边在观察空间达到最好的效果
                float4 pos = mul(UNITY_MATRIX_MV, v.vertex); 
                float3 normal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);  
                normal.z = -0.5;
                pos = pos + float4(normalize(normal), 0) * _Outline;
                //将顶点从视角空间变换到裁剪空间
                o.pos = mul(UNITY_MATRIX_P, pos);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target { 
                //轮廓线颜色渲染整个背面
                return float4(_OutlineColor.rgb, 1);               
            }

            ENDCG
        }
        Pass 
        {
            Tags { "LightMode"="ForwardBase" }
            //渲染正面
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
        
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "UnityShaderVariables.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Ramp;
            fixed4 _Specular;
            fixed _SpecularScale;
        
            struct a2v {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;
            }; 
        
            struct v2f {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
            };

            v2f vert (a2v v) {
                v2f o;
                
                o.pos = UnityObjectToClipPos( v.vertex);
                o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
                o.worldNormal  = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                TRANSFER_SHADOW(o);
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target { 
                fixed3 worldNormal = normalize(i.worldNormal);
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
                fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                fixed3 worldHalfDir = normalize(worldLightDir + worldViewDir);
                
                fixed4 c = tex2D (_MainTex, i.uv);
                fixed3 albedo = c.rgb * _Color.rgb; //计算材质反射率
                
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo; //计算环境光
                
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos); //计算当前世界坐标下的阴影值
                
                fixed diff =  dot(worldNormal, worldLightDir);
                //计算半兰伯特漫反射系数
                diff = (diff * 0.5 + 0.5) * atten;
                //对渐变纹理_Ramp进行采样
                fixed3 diffuse = _LightColor0.rgb * albedo * tex2D(_Ramp, float2(diff, diff)).rgb;
                
                fixed spec = dot(worldNormal, worldHalfDir);
                fixed w = fwidth(spec) * 2.0; //抗锯齿处理
                fixed3 specular = _Specular.rgb * lerp(0, 1, smoothstep(-w, w, spec + _SpecularScale - 1)) * step(0.0001, _SpecularScale);
                
                return fixed4(ambient + diffuse + specular, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
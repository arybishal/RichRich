Shader "RichRich/StylizedLit"
{
    Properties
    {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _Emission ("Emission Color", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 150

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _Emission;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0.0, dot(normal, lightDir));

                // Stylized ambient tint + direct light
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb + fixed3(0.20, 0.22, 0.28);
                fixed3 direct = _LightColor0.rgb * (ndotl * 0.70 + 0.30);
                fixed3 diffuse = _Color.rgb * (ambient + direct);
                fixed3 finalRgb = diffuse + _Emission.rgb;

                return fixed4(finalRgb, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Mobile/Diffuse"
}

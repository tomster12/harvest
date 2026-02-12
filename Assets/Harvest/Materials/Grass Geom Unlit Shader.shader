Shader "Geometry/Grass Geometry Unlit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SecondaryColor("Secondary Color", Color) = (1,1,1,1)
        _BladeGradientMap("Blade Gradient Map", 2D) = "white" {}
        
        _WindTexture("Wind Texture", 2D) = "white" {}
        _WindStrength("Wind Strength", float) = 0
        _WindScale("Wind Scale", Range(0.0, 0.2)) = 1
        _WindSpeed("Wind Speed", float) = 0
        _WindColor("Wind Color", Color) = (1,1,1,1)
 
        _HeightNoiseTexture("Height Noise Texture", 2D) = "white" {} 
        _HeightNoiseScale("Height Noise Scale", Range(0.0, 1.0)) = 1
        _HeightNoiseStrength("Height Noise Strength", Range(0.0, 1.0)) = 0.5
        _BladeHeight("Blade Height", float) = 0
        _BladeWidth("Blade Width", Range(0.0, 1.0)) = 1.0
        _BladePositionRandomness("Blade Position Randomness", float) = 0
        _BladeCount("Grass Blades / triangle", Range(0, 30)) = 1
    }
    SubShader
    {
        CGINCLUDE
         
            #include "UnityCG.cginc"
 
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
 
            struct v2g
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
 
            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };
 
            fixed4 _BaseColor;
            fixed4 _SecondaryColor;
            sampler2D _BladeGradientMap;
 
            sampler2D _HeightNoiseTexture;
            float4 _HeightNoiseTexture_ST;
            float _HeightNoiseScale;
            float _HeightNoiseStrength;
            sampler2D _WindTexture;
            float4 _WindTexture_ST;
            float _WindStrength;
            float _WindScale;
            float _WindSpeed;
            fixed4 _WindColor;
 
            float _BladeHeight;
            float _BladeWidth;
            float _BladePositionRandomness;
            float _BladeCount;
 
            float random(float2 st) {
                return frac(sin(dot(st.xy,
                                    float2(12.9898,78.233)))*
                    43758.5453123);
            }
 
            g2f GetVertex(float4 pos, float2 uv, fixed4 color) {
                g2f o;
                o.vertex = UnityObjectToClipPos(pos);
                o.color = color;
                o.uv = uv;
                return o;
            }
 
            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.color = v.color;
                return o;
            }
 
            [maxvertexcount(93)] // 3 + 3 * 40 = 93
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                float3 normal = normalize(cross(input[1].vertex - input[0].vertex, input[2].vertex - input[0].vertex));

                float grassAmount = (input[0].color.a + input[1].color.a + input[2].color.a) / 3.0;
                int bladeCount = _BladeCount * grassAmount;

                for (uint i = 0; i < bladeCount; i++)
                {
                    float r1 = random(mul(unity_ObjectToWorld, input[0].vertex).xz * (i + 1));
                    float r2 = random(mul(unity_ObjectToWorld, input[1].vertex).xz * (i + 1));
 
                    //Random barycentric coordinates from https://stackoverflow.com/a/19654424
                    float4 midpoint = (1 - sqrt(r1)) * input[0].vertex + (sqrt(r1) * (1 - r2)) * input[1].vertex + (sqrt(r1) * r2) * input[2].vertex;
 
                    r1 = r1 * 2.0 - 1.0;
                    r2 = r2 * 2.0 - 1.0;
 
                    float4 pointA = midpoint + _BladeWidth * normalize(input[i % 3].vertex - midpoint);
                    float4 pointB = midpoint - _BladeWidth * normalize(input[i % 3].vertex - midpoint);
 
                    float4 worldPos = mul(unity_ObjectToWorld, midpoint);
 
                    float2 windSample = tex2Dlod(_WindTexture, float4(worldPos.xz * _WindTexture_ST.xy * _WindScale + _Time.y * _WindSpeed, 0.0, 0.0)).xy;
                    float2 wind = 2.0f * (windSample - 0.25f) * _WindStrength;
 
                    float noiseSample = tex2Dlod(_HeightNoiseTexture, float4(worldPos.xz * _HeightNoiseTexture_ST.xy * _HeightNoiseScale, 0.0, 0.0)).x;
                    float finalHeight = (1.0 - noiseSample * _HeightNoiseStrength) * _BladeHeight * grassAmount;                   
                    float4 newVertexPoint = midpoint + float4(normal, 0.0) * finalHeight + float4(r1, 0.0, r2, 0.0) * _BladePositionRandomness + float4(wind.x, 0.0, wind.y, 0.0);
                    
                    triStream.Append(GetVertex(pointA, float2(0,0), fixed4(0.01, 0, 0, grassAmount)));
                    triStream.Append(GetVertex(newVertexPoint, float2(0.5, 1), fixed4(1, length(windSample), 0, grassAmount)));
                    triStream.Append(GetVertex(pointB, float2(1,0), fixed4(0.01, 0, 0, grassAmount)));
 
                    triStream.RestartStrip();
                }

                for (int i = 0; i < 3; i++)
                {
                    triStream.Append(GetVertex(input[i].vertex, float2(0,0), fixed4(0.01, 0, 0, input[i].color.a)));
                }
 
                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 gradientComp = tex2D(_BladeGradientMap, float2(i.color.r, 0.0));
                fixed4 windComp = _WindColor * i.color.g;
                fixed4 color = (gradientComp + windComp) * _BaseColor;
                color = color * i.color.a + _SecondaryColor * (1 - i.color.a);
                return color;
            }

        ENDCG
 
        Pass
        {
            Tags { "RenderType"="Opaque"}
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
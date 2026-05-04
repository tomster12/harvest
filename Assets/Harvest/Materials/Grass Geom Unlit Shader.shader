Shader "Geometry/Grass Geometry Unlit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.6, 0.8, 0.6, 1)
        _SecondaryColor("Secondary Color", Color) = (0.5, 0.65, 0.45, 1)
        _ColorNoiseScale("Color Noise Scale", Range(0.0, 2.0)) = 1.0
        _ColorNoiseStrength("Color Noise Strength", Range(0, 1.5)) = 0.5
        _BladeGradientMap("Blade Gradient Map", 2D) = "white" {}

        _WindStrength("Wind Strength", float) = 0.05
        _WindScale("Wind Scale", Range(0.0, 2.0)) = 0.3
        _WindSpeed("Wind Speed", float) = 0.8
        _WindColor("Wind Color", Color) = (1, 1, 1, 1)
        _WindColorStrength("Wind Color Strength", float) = 3.1

        _BladeHeight("Blade Height", float) = 1.0
        _HeightNoiseScale("Height Noise Scale", Range(0.0, 2.0)) = 0.85
        _HeightNoiseStrength("Height Noise Strength", Range(0.0, 1.5)) = 1.0

        _BladeCount("Grass Blades / triangle", Range(0, 30)) = 6
        _DensityNoiseScale("Density Noise Scale", Range(0.0, 2.0)) = 0.4
        _DensityNoiseStrength("Density Noise Strength", Range(0.0, 1.5)) = 0.6
        _BladeWidth("Blade Width", Range(0.0, 0.1)) = 0.1
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        fixed4 _BaseColor;
        fixed4 _SecondaryColor;
        float _ColorNoiseScale;
        float _ColorNoiseStrength;
        sampler2D _BladeGradientMap;

        float _WindStrength;
        float _WindScale;
        float _WindSpeed;
        fixed4 _WindColor;
        float _WindColorStrength;

        float _BladeHeight;
        float _HeightNoiseScale;
        float _HeightNoiseStrength;

        float _BladeCount;
        float _DensityNoiseScale;
        float _DensityNoiseStrength;
        float _BladeWidth;

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

        float2 perlinGrad(int2 cell)
        {

            int n = cell.x * 127 + cell.y * 311;
            n = (n << 13) ^ n;
            n = n * (n * n * 15731 + 789221) + 1376312589;
            float angle = (n & 0x7fffffff) * (6.28318530718 / 2147483648.0);
            return float2(cos(angle), sin(angle));
        }

        float perlinFade(float t)
        {
            return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
        }

        float perlinNoise(float2 p)
        {
            int2 cell = int2(floor(p));
            float2 f = frac(p);

            float2 u = float2(perlinFade(f.x), perlinFade(f.y));

            float n00 = dot(perlinGrad(cell + int2(0, 0)), f - float2(0, 0));
            float n10 = dot(perlinGrad(cell + int2(1, 0)), f - float2(1, 0));
            float n01 = dot(perlinGrad(cell + int2(0, 1)), f - float2(0, 1));
            float n11 = dot(perlinGrad(cell + int2(1, 1)), f - float2(1, 1));

            return lerp(lerp(n00, n10, u.x),
            lerp(n01, n11, u.x),
            u.y);
        }

        float perlin01(float2 p)
        {
            return perlinNoise(p) * 0.5 + 0.5;
        }

        float random(float2 st)
        {
            return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
        }

        g2f GetVertex(float4 pos, float2 uv, fixed4 color)
        {
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

        [maxvertexcount(93)]
        void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
        {
            float triangleDensity = (
                    input[0].color.a +
                    input[1].color.a +
                    input[2].color.a
                ) / 3.0;
            
            float2 triangleWorldPos = (
                    mul(unity_ObjectToWorld, input[0].vertex).xz +
                    mul(unity_ObjectToWorld, input[1].vertex).xz +
                    mul(unity_ObjectToWorld, input[2].vertex).xz
                ) / 3;

            float densitySample = perlin01(triangleWorldPos * _DensityNoiseScale);
            densitySample = clamp(lerp(1.0, densitySample, _DensityNoiseStrength), 0, 1);

            int bladeCount = _BladeCount * triangleDensity * densitySample;

            for (uint i = 0; i < bladeCount; i++)
            {
                //Calculate random point (barycentric) in triangle for the blade
                float r1 = random(mul(unity_ObjectToWorld, input[0].vertex).xz * (i + 1));
                float r2 = random(mul(unity_ObjectToWorld, input[1].vertex).xz * (i + 1));
                float4 midpoint = (1 - sqrt(r1)) * input[0].vertex
                    + (sqrt(r1) * (1 - r2)) * input[1].vertex
                    + (sqrt(r1) * r2) * input[2].vertex;

                float2 worldPos = mul(unity_ObjectToWorld, midpoint).xz;

                // Wind Noise (two independent Perlin samples per axis)
                float2 windCoord = worldPos * _WindScale + _Time.y * _WindSpeed;
                float windX = perlinNoise(windCoord);
                float windY = perlinNoise(windCoord + float2(31.7, 17.3));
                float2 windVector = float2(windX, windY) * _WindStrength;
                float windMagnitude = length(windVector) * _WindColorStrength;
                windMagnitude = windMagnitude * windMagnitude;

                // Height noise
                float heightSample = perlin01(worldPos * _HeightNoiseScale);

                // Colour noise
                float baseColorSample = perlin01(worldPos * _ColorNoiseScale);

                // Calculate blade tip with midpoint and samples
                float tipX = midpoint.x + windVector.x;
                float tipY = midpoint.y + windVector.y;
                float tipZ = midpoint.z + (_BladeHeight * triangleDensity) * (1.0 - heightSample * _HeightNoiseStrength);
                float4 tipPos = float4(tipX, tipY, tipZ, 0.0);

                // Construct blade triangle
                float4 pointA = midpoint + _BladeWidth * normalize(input[i % 3].vertex - midpoint);
                float4 pointB = midpoint - _BladeWidth * normalize(input[i % 3].vertex - midpoint);

                triStream.Append(GetVertex(
                    pointA, float2(0, 0),
                    fixed4(0.01, 0, baseColorSample, 0)));

                triStream.Append(GetVertex(
                    tipPos, float2(0.5, 1),
                    fixed4(1, windMagnitude, baseColorSample, 0)));

                triStream.Append(GetVertex(
                    pointB, float2(1, 0),
                    fixed4(0.01, 0, baseColorSample, 0)));

                triStream.RestartStrip();
            }

            // Add in the ground also
            for (int j = 0; j < 3; j++)
            {
                float2 vertWorldPos = mul(unity_ObjectToWorld, input[j].vertex).xz;
                float baseColorSample = perlin01(vertWorldPos * _ColorNoiseScale);

                triStream.Append(GetVertex(
                    input[j].vertex, float2(0, 0),
                    fixed4(0.01, 0, baseColorSample, input[j].color.a)));
            }

            triStream.RestartStrip();
        }

        fixed4 frag(g2f i) : SV_Target
        {
            float bladeVerticalPosition = i.color.r;
            fixed4 bladeGradientComp = tex2D(_BladeGradientMap, float2(bladeVerticalPosition, 0.0));

            float windMagnitude = i.color.g;
            fixed4 windComp = _WindColor * windMagnitude;

            float baseColorSample = i.color.b;
            baseColorSample = lerp(0.5, baseColorSample, _ColorNoiseStrength);
            fixed4 baseColor = lerp(_BaseColor, _SecondaryColor, baseColorSample);

            return (bladeGradientComp + windComp) * baseColor;
        }

        ENDCG

        Pass
        {
            Tags { "RenderType" = "Opaque" }
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDCG
        }
    }
}
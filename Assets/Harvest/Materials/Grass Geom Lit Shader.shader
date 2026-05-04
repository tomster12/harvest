Shader "Geometry/Grass Geometry Lit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.6, 0.8, 0.6, 1)
        _SecondaryColor("Secondary Color", Color) = (0.5, 0.65, 0.45, 1)
        _BladeGradientMap("Blade Gradient Map", 2D) = "white" {}

        _ColorNoiseScale("Color Noise Scale", Range(0.0, 2.0)) = 1.0
        _ColorNoiseStrength("Color Noise Strength", Range(0, 1)) = 0.5

        _WindStrength("Wind Strength", float) = 0.05
        _WindColorStrength("Wind Color Strength", float) = 3.1
        _WindScale("Wind Scale", Range(0.0, 2.0)) = 0.3
        _WindSpeed("Wind Speed", float) = 0.8
        _WindColor("Wind Color", Color) = (1, 1, 1, 1)

        _HeightNoiseScale("Height Noise Scale", Range(0.0, 2.0)) = 0.85
        _HeightNoiseStrength("Height Noise Strength", Range(0.0, 1.0)) = 1.0
        _BladeHeight("Blade Height", float) = 1.0
        _BladeWidth("Blade Width", Range(0.0, 0.1)) = 0.1
        _BladeCount("Grass Blades / triangle", Range(0, 30)) = 6

        _DensityNoiseScale("Density Noise Scale", Range(0.0, 2.0)) = 0.4
        _DensityNoiseStrength("Density Noise Strength", Range(0.0, 1.0)) = 0.6

        _BladeBend("Blade Bend", Range(0.0, 0.5)) = 0.15
        _ShadowStrength("Shadow Strength", Range(0.0, 1.0)) = 0.7
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"

            fixed4 _BaseColor;
            fixed4 _SecondaryColor;
            sampler2D _BladeGradientMap;

            float _ColorNoiseScale;
            float _ColorNoiseStrength;
            float _HeightNoiseScale;
            float _HeightNoiseStrength;
            float _WindStrength;
            float _WindColorStrength;
            float _WindScale;
            float _WindSpeed;
            fixed4 _WindColor;
            float _BladeHeight;
            float _BladeWidth;
            float _BladeCount;
            float _DensityNoiseScale;
            float _DensityNoiseStrength;
            float _BladeBend;
            float _ShadowStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
            };

            struct g2f
            {
                float4 pos      : SV_POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                SHADOW_COORDS(2)
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
                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y);
            }

            float perlin01(float2 p)
            {
                return perlinNoise(p) * 0.5 + 0.5;
            }

            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            g2f MakeVert(float4 objPos, float2 uv, fixed4 col)
            {
                g2f o;
                o.pos      = UnityObjectToClipPos(objPos);
                o.uv       = uv;
                o.color    = col;
                o.worldPos = mul(unity_ObjectToWorld, objPos);
                o._ShadowCoord = ComputeScreenPos(o.pos);
                return o;
            }

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.color  = v.color;
                return o;
            }

            [maxvertexcount(153)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                float overallMultiplier = (input[0].color.a
                                        + input[1].color.a
                                        + input[2].color.a) / 3.0;

                float2 wp0 = mul(unity_ObjectToWorld, input[0].vertex).xz;
                float2 wp1 = mul(unity_ObjectToWorld, input[1].vertex).xz;
                float2 wp2 = mul(unity_ObjectToWorld, input[2].vertex).xz;
                float2 centroid = (wp0 + wp1 + wp2) / 3.0;

                float densitySample     = perlin01(centroid * _DensityNoiseScale);
                float densityMultiplier = lerp(1.0, densitySample, _DensityNoiseStrength);

                int bladeCount = (int)(_BladeCount * overallMultiplier * densityMultiplier);

                for (uint i = 0; i < (uint)bladeCount; i++)
                {
                    float r1 = random(wp0 * (i + 1));
                    float r2 = random(wp1 * (i + 1));
                    float4 root = (1 - sqrt(r1)) * input[0].vertex
                                + (sqrt(r1) * (1 - r2)) * input[1].vertex
                                + (sqrt(r1) * r2) * input[2].vertex;

                    float2 worldPos = mul(unity_ObjectToWorld, root).xz;

                    float2 windCoord = worldPos * _WindScale + _Time.y * _WindSpeed;
                    float  windX     = perlinNoise(windCoord);
                    float  windZ     = perlinNoise(windCoord + float2(31.7, 17.3));
                    float2 windVec   = float2(windX, windZ) * _WindStrength;
                    float  windMag   = length(windVec) * _WindColorStrength;
                    windMag = windMag * windMag;

                    float  randAngle  = random(worldPos + float2(3.7, 9.1)) * 6.28318;
                    float2 bladeRight = float2(cos(randAngle), sin(randAngle));
                    float2 leanDir    = float2(-bladeRight.y, bladeRight.x);
                    float  lean       = (random(worldPos + float2(1.3, 5.7)) - 0.5) * _BladeBend * 2.0;

                    float heightSample    = perlin01(worldPos * _HeightNoiseScale);
                    float baseColorSample = perlin01(worldPos * _ColorNoiseScale);
                    float bladeH = _BladeHeight * overallMultiplier
                                 * (1.0 - heightSample * _HeightNoiseStrength);

                    float halfW  = _BladeWidth * 0.5;
                    float halfWm = halfW * 0.4;

                    float4 baseL = root + float4( bladeRight.x * halfW,  0,  bladeRight.y * halfW,  0);
                    float4 baseR = root + float4(-bladeRight.x * halfW,  0, -bladeRight.y * halfW,  0);

                    float4 midOff = float4(leanDir.x * lean, 0, leanDir.y * lean, 0);
                    float4 midL = root + float4(0, bladeH * 0.5, 0, 0) + midOff
                                + float4( bladeRight.x * halfWm, 0,  bladeRight.y * halfWm, 0);
                    float4 midR = root + float4(0, bladeH * 0.5, 0, 0) + midOff
                                + float4(-bladeRight.x * halfWm, 0, -bladeRight.y * halfWm, 0);

                    float4 tip = root + float4(
                        leanDir.x * lean * 1.5 + windVec.x,
                        bladeH,
                        leanDir.y * lean * 1.5 + windVec.y,
                        0);

                    fixed4 cBase = fixed4(0.0, windMag, baseColorSample, 0);
                    fixed4 cMid  = fixed4(0.5, windMag, baseColorSample, 0);
                    fixed4 cTip  = fixed4(1.0, windMag, baseColorSample, 0);

                    triStream.Append(MakeVert(baseL, float2(0,   0  ), cBase));
                    triStream.Append(MakeVert(baseR, float2(1,   0  ), cBase));
                    triStream.Append(MakeVert(midL,  float2(0,   0.5), cMid ));
                    triStream.Append(MakeVert(midR,  float2(1,   0.5), cMid ));
                    triStream.Append(MakeVert(tip,   float2(0.5, 1  ), cTip ));
                    triStream.RestartStrip();
                }

                for (int j = 0; j < 3; j++)
                {
                    float2 vwp = mul(unity_ObjectToWorld, input[j].vertex).xz;
                    float  bc  = perlin01(vwp * _ColorNoiseScale);
                    triStream.Append(MakeVert(input[j].vertex, float2(0, 0),
                        fixed4(0.0, 0, bc, input[j].color.a)));
                }
                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                float bladeVertPos  = i.color.r;
                fixed4 gradientComp = tex2D(_BladeGradientMap, float2(bladeVertPos, 0.0));

                float  windMag  = i.color.g;
                fixed4 windComp = _WindColor * windMag;

                float  baseColorSample = lerp(0.5, i.color.b, _ColorNoiseStrength);
                fixed4 baseColor       = lerp(_BaseColor, _SecondaryColor, baseColorSample);

                fixed4 litColor = (gradientComp + windComp) * baseColor;

                fixed shadow       = SHADOW_ATTENUATION(i);
                fixed shadowFactor = lerp(1.0 - _ShadowStrength, 1.0, shadow);

                return litColor * shadowFactor;
            }

            ENDCG
        }
    }

    Fallback "Diffuse"
}

Shader "Custom/Super Simple Bark Cut Lit"
{
    Properties
    {
        [Header(Wood Options)]
        _WoodColor ("Wood Color", Color) = (0.72, 0.62, 0.50, 1)
        _BarkColor ("Bark Color", Color) = (0.42, 0.37, 0.31, 1)
        _BarkEdgeColor ("Bark Edge Color", Color) = (0.49, 0.42, 0.35, 1)
        _BarkWidth ("Bark Width", Range(0, 0.2)) = 0.02
        _BarkEdgeWidth ("Bark Edge Width", Range(0, 0.1)) = 0.04
        _InnerDarkenAmount ("Dark Amount", Range(0, 1)) = 0.35
        _InnerDarkenStart ("Darken Start", Range(0, 1)) = 0.35

        [Header(Surface Options)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.4
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _WoodColor, _BarkColor, _BarkEdgeColor;
            float _BarkWidth, _BarkEdgeWidth, _InnerDarkenAmount, _InnerDarkenStart;
            float _Smoothness, _Metallic;
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
            };

            v2f vert (appdata v)
            {
                v2f o;

                VertexPositionInputs posInput = GetVertexPositionInputs(v.vertex.xyz);
                VertexNormalInputs nrmInput = GetVertexNormalInputs(v.normal);

                o.pos = posInput.positionCS;
                o.posWS = posInput.positionWS;
                o.normal = nrmInput.normalWS;
                o.uv = v.uv;

                o.shadowCoord = GetShadowCoord(posInput);
                OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUV);
                OUTPUT_SH(o.normal, o.vertexSH);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Calculate colour based on the depth from the UV
                float depth = i.uv.x;
                float barkFullWidth = _BarkWidth + _BarkEdgeWidth;
                half3 albedo;
                if (depth < _BarkWidth) albedo = _BarkColor.rgb;
                else if (depth < barkFullWidth) albedo = _BarkEdgeColor.rgb;
                else
                {
                    float t = saturate((depth - barkFullWidth) / (_InnerDarkenStart - barkFullWidth + 0.0001));
                    albedo = _WoodColor.rgb * lerp(1.0, 1.0 - _InnerDarkenAmount, t);
                }

                // Standard URP PBR lighting setup
                InputData inputData = (InputData)0;
                inputData.positionWS = i.posWS;
                inputData.normalWS = NormalizeNormalPerPixel(i.normal);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(i.posWS));
                inputData.shadowCoord = i.shadowCoord;
                inputData.fogCoord = ComputeFogFactor(i.pos.z);
                inputData.bakedGI = SAMPLE_GI(i.lightmapUV, i.vertexSH, inputData.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = 0;
                surfaceData.occlusion = 1;
                surfaceData.alpha = 1;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }

    FallBack "Universal Render Pipeline/Lit"
}

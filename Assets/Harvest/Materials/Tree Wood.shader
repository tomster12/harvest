Shader "Custom/Tree Wood"
{
    Properties
    {
        [Header(Wood Options)]
        _BarkColor ("Bark Color", Color) = (0.42, 0.37, 0.31, 1)
        _InnerColor ("Inner Color", Color) = (0.72, 0.62, 0.50, 1)
        _CoreColor ("Core Color", Color) = (0.38, 0.33, 0.26, 1)
        _BarkWidth ("Bark Width", Range(0, 0.2)) = 0.02
        _BarkEdgeWidth ("Bark Edge Width", Range(0, 0.1)) = 0.04
        _CoreStart ("Core Start", Range(0, 1)) = 0.35

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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BarkColor, _InnerColor, _CoreColor;
            float _BarkWidth, _BarkEdgeWidth, _CoreStart;
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
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
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
                else if (depth < barkFullWidth) albedo = lerp(_BarkColor.rgb, _InnerColor.rgb, 0.35);
                else
                {
                    float t = saturate((depth - barkFullWidth) / (_CoreStart - barkFullWidth + 0.0001));
                    albedo = lerp(_InnerColor.rgb, _CoreColor.rgb, t);
                }

                // Standard URP PBR lighting setup
                InputData inputData = (InputData)0;
                inputData.positionWS = i.posWS;
                inputData.normalWS = NormalizeNormalPerPixel(i.normal);
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(i.posWS));
                inputData.positionCS = i.pos;
                #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(i.posWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                inputData.shadowMask = half4(1, 1, 1, 1);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.pos);
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

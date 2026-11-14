Shader "DevilsHarvest/CoastalWater"
{
    Properties
    {
        _ShallowColor ("Shallows", Color) = (0.12,0.52,0.49,0.82)
        _DeepColor ("Deep Water", Color) = (0.025,0.14,0.22,0.96)
        _FoamColor ("Shore Foam", Color) = (0.73,0.85,0.79,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _FoamColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; half fog : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                world.y += 0.09 * sin(world.x * 0.13 + _Time.y * 0.8) + 0.06 * sin(world.z * 0.19 - _Time.y * 0.6);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float depth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surfaceDepth = -TransformWorldToView(input.world).z;
                float thickness = max(0, depth - surfaceDepth);
                half4 color = lerp(_ShallowColor, _DeepColor, saturate(thickness / 9));
                float wave = sin(input.world.x * 0.38 + input.world.z * 0.26 + _Time.y * 1.4);
                float foam = (1 - smoothstep(0.1, 1.3, thickness)) * smoothstep(-0.25, 0.5, wave);
                color.rgb = lerp(color.rgb, _FoamColor.rgb, foam * 0.65);
                float3 normal = normalize(float3(-0.035 * cos(input.world.x * 0.13 + _Time.y), 1, -0.028 * cos(input.world.z * 0.19 - _Time.y)));
                Light sun = GetMainLight();
                float3 halfway = normalize(normalize(GetWorldSpaceViewDir(input.world)) + sun.direction);
                color.rgb += sun.color * pow(saturate(dot(normal, halfway)), 160) * 0.45;
                color.rgb = MixFog(color.rgb, input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}

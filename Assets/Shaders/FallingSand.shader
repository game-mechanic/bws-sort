Shader "Custom/FallingSandUI"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sand Simulation", 2D) = "black" {}

        _Color ("Sand Color", Color) =
            (0.82, 0.58, 0.25, 1)

        _GrainVariation ("Grain Variation", Range(0, 1)) = 0.20

        _GrainContrast ("Grain Contrast", Range(0, 1)) = 0.30

        _GrainDetail ("Grain Detail", Range(0, 1)) = 0.35

        _GrainEdge ("Grain Edge", Range(0, 1)) = 0.20
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha

        Cull Off
        ZWrite Off

        Pass
        {
            Name "SandGrains"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            // =====================================================
            // TEXTURE
            // =====================================================

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;


            // =====================================================
            // PROPERTIES
            // =====================================================

            float4 _Color;

            float _GrainVariation;
            float _GrainContrast;
            float _GrainDetail;
            float _GrainEdge;


            // =====================================================
            // STRUCTURES
            // =====================================================

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            // =====================================================
            // RANDOM
            // =====================================================

            float Hash21(float2 p)
            {
                p = frac(
                    p * float2(
                        127.1,
                        311.7
                    )
                );

                p += dot(
                    p,
                    p + 34.5
                );

                return frac(
                    p.x * p.y
                );
            }


            // =====================================================
            // SMOOTH RANDOM
            // =====================================================

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }


            // =====================================================
            // SAMPLE SAND
            // =====================================================

            float SampleSand(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                ).a;
            }


            // =====================================================
            // VERTEX
            // =====================================================

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(
                        input.positionOS.xyz
                    );

                output.uv = input.uv;

                output.color =
                    input.color * _Color;

                return output;
            }


            // =====================================================
            // FRAGMENT
            // =====================================================

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;


                // =================================================
                // CURRENT SAND
                // =================================================

                float4 sand =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        uv
                    );


                float alpha = sand.a;


                // Empty pixel
                if (alpha < 0.01)
                {
                    return half4(0, 0, 0, 0);
                }


                // =================================================
                // SIMULATION PIXEL
                // =================================================

                float2 pixel =
                    floor(
                        uv /
                        _MainTex_TexelSize.xy
                    );


                // =================================================
                // RANDOM GRAIN
                // =================================================

                float random =
                    Hash21(pixel);


                // =================================================
                // SECONDARY DETAIL
                // =================================================

                float detail =
                    Noise(
                        pixel * 0.45
                    );


                // =================================================
                // NATURAL BRIGHTNESS VARIATION
                // =================================================

                float grainBrightness =
                    lerp(
                        0.82,
                        1.18,
                        random
                    );


                float detailBrightness =
                    lerp(
                        0.92,
                        1.08,
                        detail
                    );


                float brightness =
                    grainBrightness *
                    detailBrightness;


                brightness =
                    lerp(
                        1.0,
                        brightness,
                        _GrainContrast
                    );


                // =================================================
                // NEIGHBOUR SAMPLES
                // =================================================

                float2 texel =
                    _MainTex_TexelSize.xy;


                float left =
                    SampleSand(
                        uv +
                        float2(-texel.x, 0)
                    );


                float right =
                    SampleSand(
                        uv +
                        float2(texel.x, 0)
                    );


                float up =
                    SampleSand(
                        uv +
                        float2(0, texel.y)
                    );


                float down =
                    SampleSand(
                        uv +
                        float2(0, -texel.y)
                    );


                float upLeft =
                    SampleSand(
                        uv +
                        float2(
                            -texel.x,
                            texel.y
                        )
                    );


                float upRight =
                    SampleSand(
                        uv +
                        float2(
                            texel.x,
                            texel.y
                        )
                    );


                float downLeft =
                    SampleSand(
                        uv +
                        float2(
                            -texel.x,
                            -texel.y
                        )
                    );


                float downRight =
                    SampleSand(
                        uv +
                        float2(
                            texel.x,
                            -texel.y
                        )
                    );


                // =================================================
                // EDGE DETECTION
                // =================================================

                float horizontal =
                    abs(left - right);


                float vertical =
                    abs(up - down);


                float diagonal =
                    abs(
                        upLeft +
                        downRight -
                        upRight -
                        downLeft
                    );


                float edge =
                    saturate(
                        horizontal +
                        vertical +
                        diagonal
                    );


                // =================================================
                // SMALL GRAIN DARKENING
                // =================================================

                float grainShade =
                    lerp(
                        1.0,
                        0.90,
                        edge * _GrainEdge
                    );


                // =================================================
                // MICRO DETAIL
                // =================================================

                float microNoise =
                    Noise(
                        pixel * 1.7
                    );


                float microVariation =
                    lerp(
                        0.94,
                        1.06,
                        microNoise
                    );


                microVariation =
                    lerp(
                        1.0,
                        microVariation,
                        _GrainDetail
                    );


                // =================================================
                // FINAL COLOR
                // =================================================

                float3 finalColor =
                    _Color.rgb *
                    brightness *
                    grainShade *
                    microVariation;


                finalColor =
                    saturate(
                        finalColor
                    );


                // =================================================
                // FINAL ALPHA
                // =================================================

                float finalAlpha =
                    alpha *
                    _Color.a;


                return half4(
                    finalColor,
                    finalAlpha
                );
            }

            ENDHLSL
        }
    }

    FallBack Off
}
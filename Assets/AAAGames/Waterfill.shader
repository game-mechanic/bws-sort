Shader "Custom/WaterLiquidFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shape", 2D) = "white" {}

        _WaterColor ("Water Color", Color) = (0.02, 0.45, 1.0, 1)
        _DeepColor ("Deep Water Color", Color) = (0.01, 0.15, 0.45, 1)
        _SurfaceColor ("Surface Highlight", Color) = (0.35, 0.85, 1.0, 1)

        _Fill ("Fill Amount", Range(0,1)) = 0

        // Surface wave
        _WaveHeight ("Surface Wave Height", Range(0,0.05)) = 0.012
        _WaveSpeed ("Surface Wave Speed", Range(0,5)) = 1.5
        _WaveFrequency ("Surface Wave Frequency", Range(0,30)) = 8

        // Internal liquid movement
        _LiquidWaveHeight ("Liquid Wave Height", Range(0,0.05)) = 0.008
        _LiquidWaveSpeed ("Liquid Wave Speed", Range(0,5)) = 1
        _LiquidWaveFrequency ("Liquid Wave Frequency", Range(0,30)) = 5

        // Surface
        _SurfaceThickness ("Surface Thickness", Range(0,0.1)) = 0.025
        _SurfaceBrightness ("Surface Brightness", Range(0,3)) = 1.2

        // Transparency
        _WaterAlpha ("Water Alpha", Range(0,1)) = 0.9

        // Sprite shape
        _AlphaCutoff ("Shape Cutoff", Range(0,1)) = 0.1

        // Edge
        _EdgeSoftness ("Liquid Edge Softness", Range(0,0.05)) = 0.005

        // Bubbles
        _BubbleAmount ("Bubble Amount", Range(0,30)) = 8
        _BubbleSize ("Bubble Size", Range(0.001,0.05)) = 0.012
        _BubbleSpeed ("Bubble Speed", Range(0,2)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;

            float4 _WaterColor;
            float4 _DeepColor;
            float4 _SurfaceColor;

            float _Fill;

            float _WaveHeight;
            float _WaveSpeed;
            float _WaveFrequency;

            float _LiquidWaveHeight;
            float _LiquidWaveSpeed;
            float _LiquidWaveFrequency;

            float _SurfaceThickness;
            float _SurfaceBrightness;

            float _WaterAlpha;

            float _AlphaCutoff;
            float _EdgeSoftness;

            float _BubbleAmount;
            float _BubbleSize;
            float _BubbleSpeed;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            // --------------------------------------------------
            // Simple pseudo random
            // --------------------------------------------------

            float randomValue(float2 value)
            {
                return frac(
                    sin(
                        dot(
                            value,
                            float2(12.9898, 78.233)
                        )
                    ) * 43758.5453
                );
            }


            // --------------------------------------------------
            // Bubble function
            // --------------------------------------------------

            float bubblePattern(float2 uv)
            {
                float bubbles = 0.0;

                float2 gridUV =
                    uv * _BubbleAmount;

                float2 cell =
                    floor(gridUV);

                float2 local =
                    frac(gridUV) - 0.5;

                float random =
                    randomValue(cell);

                // Only some cells contain bubbles
                if (random > 0.35)
                {
                    float bubbleSize =
                        _BubbleSize *
                        (0.5 + random);

                    float2 bubbleOffset =
                        float2(
                            randomValue(cell + 3.1),
                            randomValue(cell + 7.4)
                        ) - 0.5;

                    float2 bubblePosition =
                        local - bubbleOffset;

                    float distanceFromBubble =
                        length(bubblePosition);

                    bubbles =
                        1.0 -
                        smoothstep(
                            bubbleSize * 0.5,
                            bubbleSize,
                            distanceFromBubble
                        );
                }

                return bubbles;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                // ==================================================
                // 1. ZERO FILL = COMPLETELY INVISIBLE
                // ==================================================

                if (_Fill <= 0.001)
                {
                    discard;
                }


                // ==================================================
                // 2. GET ORIGINAL SPRITE SHAPE
                // ==================================================

                fixed4 shape =
                    tex2D(
                        _MainTex,
                        i.uv
                    );

                // Only use alpha from original image.
                if (shape.a < _AlphaCutoff)
                {
                    discard;
                }


                // ==================================================
                // 3. WATER SURFACE WAVE
                // ==================================================

                float surfaceWave =
                    sin(
                        i.uv.x * _WaveFrequency
                        + _Time.y * _WaveSpeed
                    )
                    * _WaveHeight;


                // Second wave gives less mechanical movement.

                float surfaceWave2 =
                    sin(
                        i.uv.x * (_WaveFrequency * 0.55)
                        - _Time.y * (_WaveSpeed * 0.7)
                    )
                    * (_WaveHeight * 0.45);


                float totalSurfaceWave =
                    surfaceWave +
                    surfaceWave2;


                // Actual water surface.

                float waterLevel =
                    _Fill +
                    totalSurfaceWave;


                // ==================================================
                // 4. WATER BODY MASK
                // ==================================================

                float liquidMask =
                    1.0 -
                    smoothstep(
                        waterLevel - _EdgeSoftness,
                        waterLevel + _EdgeSoftness,
                        i.uv.y
                    );


                if (liquidMask <= 0.001)
                {
                    discard;
                }


                // ==================================================
                // 5. WATER DEPTH COLOR
                // ==================================================

                // Bottom = darker
                // Top = lighter

                float depth =
                    saturate(i.uv.y / max(_Fill, 0.001));

                float depthGradient =
                    smoothstep(
                        0.0,
                        1.0,
                        depth
                    );

                fixed4 waterColor =
                    lerp(
                        _DeepColor,
                        _WaterColor,
                        depthGradient
                    );


                // ==================================================
                // 6. INTERNAL LIQUID MOVEMENT
                // ==================================================

                float internalWave =
                    sin(
                        i.uv.x * _LiquidWaveFrequency
                        + i.uv.y * 10.0
                        + _Time.y * _LiquidWaveSpeed
                    );

                internalWave =
                    internalWave * 0.5 + 0.5;


                // Very subtle color variation.

                waterColor.rgb +=
                    internalWave *
                    _LiquidWaveHeight;


                // ==================================================
                // 7. WATER SURFACE HIGHLIGHT
                // ==================================================

                float distanceToSurface =
                    abs(
                        i.uv.y -
                        waterLevel
                    );

                float surfaceMask =
                    1.0 -
                    smoothstep(
                        0.0,
                        _SurfaceThickness,
                        distanceToSurface
                    );


                waterColor.rgb =
                    lerp(
                        waterColor.rgb,
                        _SurfaceColor.rgb,
                        surfaceMask *
                        _SurfaceBrightness
                    );


                // ==================================================
                // 8. BUBBLES
                // ==================================================

                float2 bubbleUV = i.uv;

                bubbleUV.y +=
                    _Time.y *
                    _BubbleSpeed;

                float bubbles =
                    bubblePattern(
                        bubbleUV
                    );

                // Bubbles only inside water.

                bubbles *= liquidMask;

                waterColor.rgb =
                    lerp(
                        waterColor.rgb,
                        _SurfaceColor.rgb,
                        bubbles * 0.35
                    );


                // ==================================================
                // 9. FINAL COLOR
                // ==================================================

                waterColor.a =
                    _WaterAlpha;

                // SpriteRenderer color
                waterColor.rgb *=
                    i.color.rgb;

                return waterColor;
            }

            ENDCG
        }
    }
}
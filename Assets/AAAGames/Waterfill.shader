Shader "Custom/WaterLiquidFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shape", 2D) = "white" {}

        _WaterColor ("Water Color", Color) = (0.05, 0.55, 1.0, 1)

        _Fill ("Fill Amount", Range(0,1)) = 1

        _WaveHeight ("Wave Height", Range(0,0.1)) = 0.02
        _WaveSpeed ("Wave Speed", Range(0,10)) = 2
        _WaveFrequency ("Wave Frequency", Range(0,30)) = 8

        _AlphaCutoff ("Shape Cutoff", Range(0,1)) = 0.1
        _EdgeSoftness ("Liquid Edge Softness", Range(0,0.05)) = 0.005
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

            float _Fill;

            float _WaveHeight;
            float _WaveSpeed;
            float _WaveFrequency;

            float _AlphaCutoff;
            float _EdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // -----------------------------------------
                // 1. Completely hide water at zero
                // -----------------------------------------

                if (_Fill <= 0.001)
                {
                    discard;
                }


                // -----------------------------------------
                // 2. Get sprite shape
                // -----------------------------------------

                fixed4 shape = tex2D(_MainTex, i.uv);

                if (shape.a < _AlphaCutoff)
                {
                    discard;
                }


                // -----------------------------------------
                // 3. Water wave
                // -----------------------------------------

                float wave =
                    sin(
                        i.uv.x * _WaveFrequency
                        + _Time.y * _WaveSpeed
                    )
                    * _WaveHeight;


                float waterLevel = _Fill + wave;


                // -----------------------------------------
                // 4. Bottom -> Top fill
                // -----------------------------------------

                float liquidMask = 1.0 - smoothstep(
                    waterLevel - _EdgeSoftness,
                    waterLevel + _EdgeSoftness,
                    i.uv.y
                );


                if (liquidMask <= 0.001)
                {
                    discard;
                }


                // -----------------------------------------
                // 5. Solid water
                // -----------------------------------------

                fixed4 water = _WaterColor;

                water.a = 1.0;

                water.rgb *= i.color.rgb;

                return water;
            }

            ENDCG
        }
    }
}
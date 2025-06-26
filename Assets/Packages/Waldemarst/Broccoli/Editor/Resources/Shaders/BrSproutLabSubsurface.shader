Shader "Hidden/Broccoli/SproutLabSubsurface"
{
    Properties {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5

        _MinSproutTint ("Min Sprout Tint", Float) = 1
        _MaxSproutTint ("Max Sprout Tint", Float) = 1
        _TintColor ("Tint Color", Color) = (0.5,1,1,1)
        [Enum(Random,0,Hierarchy,1,Branch,2)] _SproutTintMode("Mode", Int) = 0
        _InvertSproutTintMode ("Sprout Tint Invert Mode", Float) = 0
        _SproutTintVariance ("Sprout Tint Variance", Float) = 0

        _MinSproutSat ("Min Sprout Saturation", Float) = 1
        _MaxSproutSat ("Max Sprout Saturation", Float) = 1
        [Enum(Random,0,Hierarchy,1,Branch,2)] _SproutSatMode("Mode", Int) = 0
        _InvertSproutSatMode ("Sprout Sat Invert Mode", Float) = 0
        _SproutSatVariance ("Sprout Sat Variance", Float) = 0
        _SproutSubsurface ("Sprout Brightness", Float) = 1

        _BranchSat ("Branch Saturation", Float) = 1
        _BranchSub ("Branch Subsurface", Float) = 1

        _IsLinearColorSpace ("Is Linear Color Space", Float) = 0
    }
    SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}

        LOD 200

        Lighting Off

        Cull Off

        Pass {
            ZWrite Off
            Name "White"
            CGPROGRAM 
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                
                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed _Cutoff;
                float4 _SubsurfaceColor;
                float _BranchSat;
                float _SproutSat;
                float _SproutSubsurface;
                

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    //UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.color = v.color;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.uv3 = v.uv3;
                    o.uv5 = v.uv5;
                    o.uv6 = v.uv6;
                    return o;
                }
                
                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    //clip(col.a * i.color.a - _Cutoff);
                    clip(col.a * i.color.a - 0.3);
                    col.rgb = 1;
                    return  col;
                }
                
            ENDCG
        }
        
        

        Pass {
            Blend SrcColor Zero
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
                #include "BlendModes.hlsl"

                static const float4 BLACK_COLOR = float4(0.0, 0.0, 0.0, 1.0);

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed _Cutoff;

                float4 _SubsurfaceColor;
                float _SproutSubsurface;

                float4 _TintColor;
                float _MinSproutTint;
                float _MaxSproutTint;
                uniform int _SproutTintMode;
                uniform int _InvertSproutTintMode;
                float _SproutTintVariance;
                float _RandSproutTint;
                float _PosSproutTint;
                float _SproutTint;

                float _BranchSat;
                float _BranchSub;

                float _MinSproutSat;
                float _MaxSproutSat;
                uniform int _SproutSatMode;
                uniform int _InvertSproutSatMode;
                float _SproutSatVariance;
                float _RandSproutSat;
                float _PosSproutSat;
                float _SproutSat;
                float _ApplyExtraSat;

                float _IsLinearColorSpace;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    o.color = v.color;
                    o.uv3 = v.uv3;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.uv3 = v.uv3;
                    o.uv5 = v.uv5;
                    o.uv6 = v.uv6;
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }
                float3 hsv_to_rgb(float3 HSV)
                {
                    float3 RGB = HSV.z;
        
                    float var_h = HSV.x * 6;
                    float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
                    float var_1 = HSV.z * (1.0 - HSV.y);
                    float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                    float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                    if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                    else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                    else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                    else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                    else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                    else                 { RGB = float3(HSV.z, var_1, var_2); }            
                    return (RGB);
                }
                
                fixed4 frag (v2f i) : SV_Target
                {
                    if (i.uv3.z == 0) {
                        fixed4 colBlack = fixed4(0, 0, 0, 1);
                        return colBlack;
                    }
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    clip(col.a * i.color.a - 0.3);

                    if (_IsLinearColorSpace) {
                        col.rgb = pow (col.rgb, 0.4545);
                        _TintColor.rgb = pow (_TintColor.rgb, 0.4545);
                    }
                    float4 texCol = col;

                    col.a *= 0.5;
                    if (abs(i.uv5.w - 1.0) < 0.01) { // Geometry is sprout, uv5.w == 1.
                        // TINT
                        _RandSproutTint = lerp (_MinSproutTint, _MaxSproutTint, i.color.g);
                        if (_SproutTintMode > 0) {
                            float sproutPos = (_SproutTintMode == 1?i.uv6.y:i.uv6.x);
                            if (_InvertSproutTintMode ==  0) _PosSproutTint = lerp (_MinSproutTint, _MaxSproutTint, sproutPos);
                            else _PosSproutTint = lerp (_MaxSproutTint, _MinSproutTint, sproutPos);
                            _SproutTint = lerp (_PosSproutTint, _RandSproutTint, _SproutTintVariance);
                        } else {
                            _SproutTint = _RandSproutTint;
                        }
                        float3 shiftedColor = BlendColor (col, lerp(col, _TintColor.rgb, _SproutTint));

                        // VIBRANCE PRESERVATION
                        half maxBase = max(col.r, max(col.g, col.b));
                        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
                        maxBase /= newMaxBase;
                        maxBase = maxBase * 0.5f + 0.5f;
                        shiftedColor.rgb *= maxBase;

                        // SATURATION
                        _RandSproutSat = lerp (_MinSproutSat, _MaxSproutSat, i.color.b);
                        if (_SproutSatMode > 0) {
                            float sproutPos = (_SproutSatMode == 1?i.uv6.y:i.uv6.x);
                            if (_InvertSproutSatMode ==  0) _PosSproutSat = lerp (_MinSproutSat, _MaxSproutSat, sproutPos);
                            else _PosSproutSat = lerp (_MaxSproutSat, _MinSproutSat, sproutPos);
                            _SproutSat = lerp (_PosSproutSat, _RandSproutSat, _SproutSatVariance);
                        } else {
                            _SproutSat = _RandSproutSat;
                        }
                        if (_ApplyExtraSat == 0) {
                            col.rgb = ContrastSaturationBrightness (shiftedColor, 1.0, _SproutSat, 1.0);
                        } else {
                            col.rgb = ContrastSaturationBrightness (shiftedColor, 1.1, _SproutSat, 1.0);
                        }

                        float _subsurfaceVal = lerp(1.2, 2.2, _SproutSubsurface);
                        _subsurfaceVal = _subsurfaceVal * i.color.r;
                        col.rgb = ContrastSaturationBrightness (saturate (shiftedColor), _subsurfaceVal, _SproutSat * 0.65, 1.0);
                    } else {
                        col = tex2D(_MainTex, i.texcoord);
                        col.rgb = ContrastSaturationBrightness (col.rgb, 1, _BranchSat, 1);
                    }

                    if (_IsLinearColorSpace)
                        col.rgb = pow (col.rgb, 2.2);
                        
                    return  col;
                }
                
            ENDCG
        }
        Pass {
            Blend DstAlpha OneMinusSrcAlpha
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_fog
                
                #include "UnityCG.cginc"
                #include "BlendModes.hlsl"

                static const float4 BLACK_COLOR = float4(0.0, 0.0, 0.0, 1.0);

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float4 uv3: TEXCOORD3;
                    float4 uv5: TEXCOORD4;
                    float4 uv6: TEXCOORD5;
                    fixed4 color : COLOR;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed _Cutoff;

                float4 _SubsurfaceColor;
                float _SproutSubsurface;

                float4 _TintColor;
                float _MinSproutTint;
                float _MaxSproutTint;
                uniform int _SproutTintMode;
                uniform int _InvertSproutTintMode;
                float _SproutTintVariance;
                float _RandSproutTint;
                float _PosSproutTint;
                float _SproutTint;

                float _BranchSat;
                float _BranchSub;

                float _MinSproutSat;
                float _MaxSproutSat;
                uniform int _SproutSatMode;
                uniform int _InvertSproutSatMode;
                float _SproutSatVariance;
                float _RandSproutSat;
                float _PosSproutSat;
                float _SproutSat;
                float _ApplyExtraSat;
                
                float _IsLinearColorSpace;

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    o.color = v.color;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.uv3 = v.uv3;
                    o.uv5 = v.uv5;
                    o.uv6 = v.uv6;
                    UNITY_TRANSFER_FOG(o,o.vertex);
                    return o;
                }
                float3 hsv_to_rgb(float3 HSV)
                {
                    float3 RGB = HSV.z;
        
                    float var_h = HSV.x * 6;
                    float var_i = floor(var_h);
                    float var_1 = HSV.z * (1.0 - HSV.y);
                    float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                    float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                    if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                    else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                    else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                    else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                    else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                    else                 { RGB = float3(HSV.z, var_1, var_2); }            
                    return (RGB);
                }
                
                
                fixed4 frag (v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.texcoord);
                    if (_IsLinearColorSpace) {
                        col.rgb = pow (col.rgb, 0.4545);
                        _TintColor.rgb = pow (_TintColor.rgb, 0.4545);
                    }
                    fixed4 vcol = i.color;
                    clip(col.a * vcol.a - 0.3);
                    col = lerp (BLACK_COLOR, col, saturate(_SproutSubsurface));
                    col.rgb *= 1 - ((1 - i.color.r) / 2);
                    col.a *= 0;
                    if (i.color.g == 0) {
                        half3 shiftedColor = lerp(col, _TintColor.rgb, i.color.b);
                        // preserve vibrance
                        half maxBase = max(col.r, max(col.g, col.b));
                        half newMaxBase = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));
                        maxBase /= newMaxBase;
                        maxBase = maxBase * 0.5f + 0.5f;
                        shiftedColor.rgb *= maxBase;
                        col.rgb = ContrastSaturationBrightness (saturate (shiftedColor), 1.5, 1, 1);
                    } else if (abs(i.uv5.w - 1.0) > 0.01) { // Geometry is branch, uv5.w == 1.
                            col.rgb = ContrastSaturationBrightness (col.rgb, 1, _BranchSat, 1);
                            col = lerp (BLACK_COLOR, col, saturate(_BranchSub));
                    }
                    if (_IsLinearColorSpace)
                        col.rgb = pow (col.rgb, 2.2);

                    return  col;
                }
                
            ENDCG
        }
    }
}
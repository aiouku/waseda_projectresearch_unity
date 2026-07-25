#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// メインライトの方向・色・影を取得する
void MainLight_float(float3 WorldPos, out float3 Direction, out float3 Color, out float ShadowAtten)
{
#ifdef SHADERGRAPH_PREVIEW
    // グラフのプレビュー用のダミー値
    Direction = normalize(float3(0.5, 0.5, -0.5));
    Color = float3(1, 1, 1);
    ShadowAtten = 1;
#else
    #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #else
        float4 shadowCoord = float4(0, 0, 0, 0);
    #endif

    Light mainLight = GetMainLight(shadowCoord);
    Direction   = mainLight.direction;
    Color       = mainLight.color;
    ShadowAtten = mainLight.shadowAttenuation;
#endif
}

#endif
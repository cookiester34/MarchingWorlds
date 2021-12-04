Shader "Custom/WorldShader"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;
        
        float minHeight;
        float maxHeight;
        
        int layerCount;
        
        float3 BaseColours[maxLayerCount];
        float BaseStartHeights[maxLayerCount];
        float BaseBlends[maxLayerCount];
        float BaseColourStrength[maxLayerCount];
		float BaseTextureScales[maxLayerCount];

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
			float3 worldNormal;
        };

        float InverseLerp(float a, float b, float value)
        {
            return saturate((value-a)/(b-a));
        }

        float3 Triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercent = InverseLerp(minHeight,maxHeight, IN.worldPos.y);
			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			for (int i = 0; i < layerCount; i ++) {
				float drawStrength = InverseLerp(-BaseBlends[i]/2 - epsilon, BaseBlends[i]/2, heightPercent - BaseStartHeights[i]);

				float3 baseColour = BaseColours[i] * BaseColourStrength[i];
				float3 textureColour = Triplanar(IN.worldPos, BaseTextureScales[i], blendAxes, i) * (1-BaseColourStrength[i]);

				o.Albedo = o.Albedo * (1-drawStrength) + (baseColour+textureColour) * drawStrength;
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}

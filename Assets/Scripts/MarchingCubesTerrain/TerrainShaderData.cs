using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainShaderData : ScriptableObject
{
     private const int TextureSize = 216;
     private const TextureFormat TextureFormat = UnityEngine.TextureFormat.RGB565;
     [Tooltip("When adding more textures and you get the error, array size changed, just save the project and it should work again")]
     public TerrainType[] terrainTypes;
     [System.Serializable]
     public struct TerrainType
     {
          public Texture2D texture;
          public float textureScale;
          [Range(0, 1)] public float startHeight;
          [Range(0, 1)] public float blendStrength;
          public Color tint;
          [Range(0,1)]
          public float tintStrength;
     }
     
     
     private static readonly int MINHeight = Shader.PropertyToID("minHeight");
     private static readonly int MAXHeight = Shader.PropertyToID("maxHeight");
     private static readonly int BaseColours = Shader.PropertyToID("BaseColours");
     private static readonly int BaseBlends = Shader.PropertyToID("BaseBlends");
     private static readonly int BaseStartHeights = Shader.PropertyToID("BaseStartHeights");
     private static readonly int LayerCount = Shader.PropertyToID("layerCount");
     private static readonly int BaseColourStrength = Shader.PropertyToID("BaseColourStrength");
     private static readonly int BaseTextureScales = Shader.PropertyToID("BaseTextureScales");
     private static readonly int BaseTextures = Shader.PropertyToID("baseTextures");

     public void UpdateTerrainShader(Material terrainMaterial, float minHeight, float maxHeight)
     {
          terrainMaterial.SetColorArray(BaseColours, terrainTypes.Select(x => x.tint).ToArray());
          terrainMaterial.SetFloatArray(BaseBlends, terrainTypes.Select(x => x.blendStrength).ToArray());
          terrainMaterial.SetFloatArray(BaseStartHeights, terrainTypes.Select(x => x.startHeight).ToArray());
          terrainMaterial.SetInt(LayerCount, terrainTypes.Length);
          terrainMaterial.SetFloatArray(BaseColourStrength, terrainTypes.Select(x => x.tintStrength).ToArray());
          terrainMaterial.SetFloatArray(BaseTextureScales, terrainTypes.Select(x => x.textureScale).ToArray());
          var texturesArray = GenerateTextureArray (terrainTypes.Select (x => x.texture).ToArray());
          terrainMaterial.SetTexture (BaseTextures, texturesArray);
          terrainMaterial.SetFloat(MINHeight, minHeight);
          terrainMaterial.SetFloat(MAXHeight, maxHeight);
     }
     
     Texture2DArray GenerateTextureArray(Texture2D[] textures) {
          var textureArray = new Texture2DArray (TextureSize, TextureSize, textures.Length, TextureFormat, true);
          for (var i = 0; i < textures.Length; i++) {
               textureArray.SetPixels (textures [i].GetPixels (), i);
          }
          textureArray.Apply ();
          return textureArray;
     }
}

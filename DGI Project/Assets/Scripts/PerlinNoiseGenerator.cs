using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinNoiseGenerator
{

    public static float GetHeightValue(Vector3 terrainPoint, Vector2Int terrainDimensions, Vector2 offset, int numberofOctaves, float persistance) {
        Vector2 perlinPoint = new Vector2((terrainPoint.x + offset.x) / (float)terrainDimensions.x, (terrainPoint.z + offset.y) / (float)terrainDimensions.y);
        float heightValue = 0f;  // Accumulate the total height from the different octaves
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < numberofOctaves; ++i) {
            heightValue += Mathf.PerlinNoise(perlinPoint.x * frequency, perlinPoint.y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistance;
            frequency *= 2;
        }

        //if (heightValue/maxValue > 1f) {
            //Debug.Log("Heightvalue: " + heightValue/maxValue);
        //}

        return heightValue/maxValue;
    }

}

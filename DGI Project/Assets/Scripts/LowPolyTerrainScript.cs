﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet;
using TriangleNet.Geometry;

/*  This script generates a low polygon terrain Mesh by performing the following steps:
 *  1. Generate random points within the public Height and Width values (using Sebastian Lagues poisson disc sampling implementation): https://www.youtube.com/watch?v=7WcmyxyFO7o
 *  2. Triangulate the points using Delauney triangulation (implementation API: https://github.com/mapbox/delaunator)
 *  3. Generate a heightmap by using several Perlin Noises using different octaves with different impact on the terrain: https://adrianb.io/2014/08/09/perlinnoise.html
 *  4. Color the triangles in the mesh by sampling colors from a gradient depending on the height of the triangle
 */

public class LowPolyTerrainScript : MonoBehaviour
{
    [Header("Terrain settings")]
    public Vector2Int terrainDimensions = new Vector2Int(10, 10);
    public float heightScale = 10f;
    [Header("Poisson sampling settings")]
    public int sampleTries = 10;
    public float radiusError = 2f;
    [Header("Perlin noise settings")]
    public Vector2 perlinOffset = new Vector2(0,0);
    public int octaves = 1;
    [Range(0f, 1f)]
    public float persistance = 1f;
    public Gradient gradient;
    [Header("Click checkbox to generate mesh")]
    public bool generateMesh = false;

    MeshFilter mf;
    GameObject lowPolyTerrain;

    List<Vector2> poissonPoints;

    // Start is called before the first frame update
    void Start()
    {
        lowPolyTerrain = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0.05f, 0, Space.Self);
    }

    private void OnValidate()
    {
        // Check the editor variables so we don't get disallowed entries that break the implementation
        CheckEditorVariables();

        if (generateMesh == true) {
            GenerateMesh();
            generateMesh = false;
        }
    }

    // Randomizes the Perlin X and Y, used for UI generate mesh button
    public void GenerateRandomMesh() {
        perlinOffset = new Vector2(Random.Range(0,1000), Random.Range(0,1000));
        GenerateMesh();
    }


    // Generates a Low Poly Mesh
    void GenerateMesh() {

        // Access the meshFilter and initialize a new Mesh object if we don't have one
        mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) {
            mf.sharedMesh = new UnityEngine.Mesh();
            mf.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // Placeholder used for dividing program to show steps
        int step = 3;
        
        // Get a list of Vector2:s from the poisson disc sampling, these are our vertices
        poissonPoints = PoissonDiscSampling.GeneratePoints(radiusError, terrainDimensions, sampleTries);

        // Add them to a Polygon object so it speaks with the Triangle.net classes
        Polygon polygon = new Polygon();
        foreach (Vector2 point in poissonPoints)
        {
            polygon.Add(new Vertex(point.x, point.y));
        }

        // Perform Delauney triangulation, netMesh is the resulting Triangle.net Mesh which we make into a Unity variant later
        TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };
        TriangleNet.Mesh netMesh = (TriangleNet.Mesh)polygon.Triangulate(options);

        int netMeshVerticeCount = netMesh.Triangles.Count * 3;

        // Set the Unity Mesh data by taking it from the triangle.net mesh, adding one triangle at a time with duplicate vertices
        Vector3[] meshPoints = new Vector3[netMeshVerticeCount];
        int[] meshTriangles = new int[netMeshVerticeCount];
        Vector3[] normals = new Vector3[netMeshVerticeCount];
        Vector2[] uvs = new Vector2[netMeshVerticeCount];
        Color[] colors = new Color[netMeshVerticeCount];

        int tris = 0;
        foreach (ITriangle tri in netMesh.Triangles)
        {
            int unity_v0 = tris;
            int unity_v1 = tris + 1;
            int unity_v2 = tris + 2;

            int triangleNet_v0 = tri.GetVertexID(2);
            int triangleNet_v1 = tri.GetVertexID(1);
            int triangleNet_v2 = tri.GetVertexID(0);

            // We want duplicate vertices for shading purposes, so we add them for every triangle
            meshPoints[unity_v0] = new Vector3((float)tri.GetVertex(2).x, 0, (float)tri.GetVertex(2).y);
            meshPoints[unity_v1] = new Vector3((float)tri.GetVertex(1).x, 0, (float)tri.GetVertex(1).y);
            meshPoints[unity_v2] = new Vector3((float)tri.GetVertex(0).x, 0, (float)tri.GetVertex(0).y);

            // Sample Perlin noise for height values
            meshPoints[unity_v0].y = PerlinNoiseGenerator.GetHeightValue(meshPoints[unity_v0], terrainDimensions, perlinOffset, octaves, persistance);
            meshPoints[unity_v1].y = PerlinNoiseGenerator.GetHeightValue(meshPoints[unity_v1], terrainDimensions, perlinOffset, octaves, persistance);
            meshPoints[unity_v2].y = PerlinNoiseGenerator.GetHeightValue(meshPoints[unity_v2], terrainDimensions, perlinOffset, octaves, persistance);


            Vector3 triangleNormal = Vector3.Cross(meshPoints[unity_v1] - meshPoints[unity_v0], meshPoints[unity_v2] - meshPoints[unity_v0]);   // Cross product gives us the triangle normal
            normals[unity_v0] = triangleNormal;
            normals[unity_v1] = triangleNormal;
            normals[unity_v2] = triangleNormal;

            colors[unity_v0] = Color.grey;
            colors[unity_v1] = Color.grey;
            colors[unity_v2] = Color.grey;

            //uvs[unity_v0] = Vector3.zero;
            //uvs[unity_v1] = Vector3.zero;
            //uvs[unity_v2] = Vector3.zero;

            meshTriangles[unity_v0] = unity_v0;
            meshTriangles[unity_v1] = unity_v1;
            meshTriangles[unity_v2] = unity_v2;
            tris += 3;
        }


        // When sampling our perlin noises we might have ended up in a range of say 0.25 to 0.80, we normalize it back to 0.0 to 1.0.
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        foreach (Vector3 vertex in meshPoints) {
            if (vertex.y > maxValue) { maxValue = vertex.y; }
            if (vertex.y < minValue) { minValue = vertex.y; }
        }
        for(int i = 0; i < meshPoints.Length; ++i) {
            meshPoints[i].y = (Mathf.InverseLerp(minValue, maxValue, meshPoints[i].y) * 2 - 0.7f);
        }

        // Color the meshes according to their height
        for (int i = 0; i < meshPoints.Length - 2; i += 3)
        {
            float triangleCentreHeight = (meshPoints[i].y + meshPoints[i + 1].y + meshPoints[i + 2].y) / 3f;
            float gradientSampleValue = Mathf.InverseLerp(-0.7f, 1.3f, triangleCentreHeight);
            Color triangleColor = gradient.Evaluate(gradientSampleValue);
            colors[i] = triangleColor;
            colors[i + 1] = triangleColor;
            colors[i + 2] = triangleColor;
        }


        // Scale the mesh points according to the height scale and move them so that the origin is in the center
        for (int i = 0; i < meshPoints.Length; ++i)
        {
            if (meshPoints[i].y < 0f)
            {
                meshPoints[i].y /= 100f;
            }
            else
            {
                meshPoints[i].y *= heightScale;
            }
            meshPoints[i].x -= terrainDimensions.x / 2;
            meshPoints[i].z -= terrainDimensions.y / 2;
        }


        // Clear the mesh to avoid errors, set all the used parts of the Unity mesh
        mf.sharedMesh.Clear();
        mf.sharedMesh.vertices = meshPoints;
        mf.sharedMesh.triangles = meshTriangles;
        mf.sharedMesh.uv = uvs;
        mf.sharedMesh.normals = normals;
        mf.sharedMesh.colors = colors;
    }

    void CheckEditorVariables() {
        heightScale = Mathf.Max(heightScale, 1);
        //persistance = Mathf.Max(persistance, 1);
        octaves = Mathf.Max(octaves, 1);
        terrainDimensions.x = Mathf.Max(terrainDimensions.x, 1);
        terrainDimensions.y = Mathf.Max(terrainDimensions.y, 1);
        sampleTries = Mathf.Max(sampleTries, 1);
        radiusError = Mathf.Max(radiusError, 1);
    }

}

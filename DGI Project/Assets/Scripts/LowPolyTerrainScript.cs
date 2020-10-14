using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet;
using TriangleNet.Geometry;

/*  This script generates a low polygon terrain Mesh by performing the following steps:
 *  1. Generate random points within the public Height and Width values (using Sebastian Lagues poisson disc sampling implementation)
 *  2. Triangulate the points using Delauney triangulation (implementation API: https://github.com/mapbox/delaunator)
 *  3. 
 */

public class LowPolyTerrainScript : MonoBehaviour
{
    public Vector2 terrainDimensions = new Vector2(10f, 10f);
    //public int terrainPoints = 10;
    public int sampleTries = 10;
    public float radiusError = 2f;
    public bool generateMesh = false;

    UnityEngine.Mesh terrainMesh;
    MeshFilter mf;

    List<Vector2> poissonPoints;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnValidate()
    {
        CheckEditorVariables();

        if (generateMesh == true) {
            GenerateMesh();
            generateMesh = false;
        }
    }


    // Generates a Low Poly Mesh
    void GenerateMesh() {

        // Access the meshFilter and initialize a new Mesh object if we don't have one
        mf = GetComponent<MeshFilter>();
        if (mf.sharedMesh == null) {
            mf.sharedMesh = new UnityEngine.Mesh();
        }
        
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

            Vector3 triangleNormal = Vector3.Cross(meshPoints[unity_v1] - meshPoints[unity_v0], meshPoints[unity_v2] - meshPoints[unity_v0]);   // Cross product gives us the triangle normal
            normals[unity_v0] = triangleNormal;
            normals[unity_v1] = triangleNormal;
            normals[unity_v2] = triangleNormal;

            Color randomColor = new Color((tris/3) % 3, ((tris / 3)+1) % 3, ((tris / 3)+2) % 3);

            colors[unity_v0] = randomColor;
            colors[unity_v1] = randomColor;
            colors[unity_v2] = randomColor;

            uvs[unity_v0] = Vector3.zero;
            uvs[unity_v1] = Vector3.zero;
            uvs[unity_v2] = Vector3.zero;

            meshTriangles[unity_v0] = unity_v0;
            meshTriangles[unity_v1] = unity_v1;
            meshTriangles[unity_v2] = unity_v2;
            tris += 3;
        }

        // Set the vertices and triangles as our Unity mesh
        mf.sharedMesh.Clear();
        mf.sharedMesh.vertices = meshPoints;
        mf.sharedMesh.triangles = meshTriangles;
        mf.sharedMesh.uv = uvs;
        mf.sharedMesh.normals = normals;
        mf.sharedMesh.colors = colors;
    }

    void CheckEditorVariables() {   
        terrainDimensions.x = Mathf.Max(terrainDimensions.x, 1);
        terrainDimensions.y = Mathf.Max(terrainDimensions.y, 1);
        sampleTries = Mathf.Max(sampleTries, 1);
        radiusError = Mathf.Max(radiusError, 1);
    }

}

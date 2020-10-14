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

    UnityEngine.Mesh terrainMesh;
    MeshFilter mf;
    Vector3[] meshPoints;
    int[] meshTriangles;

    List<Vector2> poissonPoints;

    // Start is called before the first frame update
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mf.sharedMesh = new UnityEngine.Mesh();
        poissonPoints = PoissonDiscSampling.GeneratePoints(radiusError, terrainDimensions, sampleTries);
        Polygon polygon = new Polygon();
        foreach (Vector2 point in poissonPoints) {
            polygon.Add(new Vertex(point.x, point.y));
        }
        TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };
        TriangleNet.Mesh netMesh = (TriangleNet.Mesh)polygon.Triangulate(options);
        meshPoints = new Vector3[netMesh.Vertices.Count];
        terrainMesh = mf.sharedMesh;
        int verts = 0;
        foreach (Vertex vert in netMesh.Vertices) {
            meshPoints[verts] = new Vector3((float) vert.x, 0, (float) vert.y);
            Debug.Log("We have a vert numbered: " + verts);
            verts++;
        }

        meshTriangles = new int[netMesh.Triangles.Count * 3];

        Debug.Log("Allocated this many triangle ints: " + meshTriangles.Length);


        int tris = 0;
        foreach (ITriangle tri in netMesh.Triangles) {
            Debug.Log("Checking triangle with index: " + tris);
            meshTriangles[tris] = tri.GetVertexID(0);
            meshTriangles[tris+1] = tri.GetVertexID(2);
            meshTriangles[tris+2] = tri.GetVertexID(1);
            Debug.Log("Triangle ID:s are: " + tri.GetVertexID(0) + ", " + tri.GetVertexID(1) + ", " + tri.GetVertexID(2));
            tris += 3;
        }

        terrainMesh.vertices = meshPoints;
        terrainMesh.triangles = meshTriangles;

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

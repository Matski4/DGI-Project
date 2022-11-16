![Low Polygon Terrain](DGI%20Project/Images/LowPolyTerrain.PNG)

# DGI-Project

This was my project for the course DH2323 - Computer Graphics and Interaction at KTH Royal Institute of Technology.
The project was heavily inspired by a video presentation by Kristin Lague (last name was Stock at the time of the project) named [Procedurally Generated Low-Poly Terrains in Unity](https://www.youtube.com/watch?v=sRn8TL3EKDU).

An interactive demo is available as a WebGL build on [Unity Play](https://play.unity.com/mg/other/webgl-builds-268600).

In the project a low polygon terrain is generated by doing the following:
1. Generate vertices randomly, but with a minimum distance between each
other using poisson disc sampling.
2. Triangulate the the vertices using Delauney triangulation, which results
in triangles with maximized minimum angles.
3. Generate a heightmap using Perlin noise, using the heightmap data to
decide the height for each vertex.
4. Color the triangles by taking the average height of the triangle vertices
and sampling a gradient to decide the color of the triangle.

The main script used for generating is [LowPolyTerrain.cs](Assets/Scripts/LowPolyTerrainScript.cs)

For the poisson disc sampling an existing implementation of Poisson Disc Sampling created by Sebastian Lague was used.
An existing library for Delauney triangulation called Delaunator was used for the triangulation.
The heightmap was generated using Unitys built in Perlin noise.
The terrain coloring was implemented using a gradient in Unitys editor and sampling from it.

For more information and full credits to the sources used, read the [project report](Project%20Report/DGI19_Project_Report.pdf).

/*IMPORTANT: READ !!!!!!
@Autor: Gabriel Santos
@Description Class that Combine the Meshes and create SubMeshes for Meshes which uses different material.
@IMPORTANT: This script is used by CombineSkinnedMeshes script!
This script was based on the MeshCombineUtility provided by Unity, I have just modified in order to create new submeshes for meshes which uses different materials
PS: It was tested with FBX files exported from 3D MAX*/
 
using UnityEngine;
using System.Collections;
 
public class SkinMeshCombineUtility {
 
	public struct MeshInstance
	{
		public Mesh      mesh;
		public int       subMeshIndex;            
		public Matrix4x4 transform;
	}
 
	public static Mesh Combine (MeshInstance[] combines)
	{
		int vertexCount = 0;
		int triangleCount = 0;
 		
		Mesh pervMesh = null;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
			{
				vertexCount += combine.mesh.vertexCount;						
			}
			pervMesh = combine.mesh;
		}
		pervMesh = null;
		
 		if(vertexCount > 65000)
			Debug.LogWarning("vertexCount > 65000  = " + vertexCount);
		
		// Precomputed how many triangles we need instead
 
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
			{
				triangleCount += combine.mesh.GetTriangles(combine.subMeshIndex).Length;
			}
			pervMesh = combine.mesh;
		}
		pervMesh = null;
 
		Vector3[] vertices = new Vector3[vertexCount] ;
		Vector3[] normals = new Vector3[vertexCount] ;
		Vector4[] tangents = new Vector4[vertexCount] ;
		Vector2[] uv = new Vector2[vertexCount];
		Vector2[] uv1 = new Vector2[vertexCount];		
 
		int offset;
 
		offset=0;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
				Copy(combine.mesh.vertexCount, combine.mesh.vertices, vertices, ref offset, combine.transform);			
			
			pervMesh = combine.mesh;
		}		
 		pervMesh = null;
		
		offset=0;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
			{
				Matrix4x4 invTranspose = combine.transform;
				invTranspose = invTranspose.inverse.transpose;
				CopyNormal(combine.mesh.vertexCount, combine.mesh.normals, normals, ref offset, invTranspose);
			}
 			pervMesh = combine.mesh;
		}
		pervMesh = null;
		
		offset=0;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
			{
				Matrix4x4 invTranspose = combine.transform;
				invTranspose = invTranspose.inverse.transpose;
				CopyTangents(combine.mesh.vertexCount, combine.mesh.tangents, tangents, ref offset, invTranspose);
			}
 			pervMesh = combine.mesh;
		}
		pervMesh = null;
		
		offset=0;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
				Copy(combine.mesh.vertexCount, combine.mesh.uv, uv, ref offset);
			pervMesh = combine.mesh;
		}
 		pervMesh = null;
		
		offset=0;
		foreach( MeshInstance combine in combines )
		{
			if (combine.mesh!=null && combine.mesh!=pervMesh)
				Copy(combine.mesh.vertexCount, combine.mesh.uv2, uv1, ref offset);
			pervMesh = combine.mesh;
		}
 		pervMesh = null;
		
		int triangleOffset=0;
		int vertexOffset=0;
 
		int j=0;
 
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.normals = normals;
		mesh.uv = uv;
		mesh.uv2 = uv1;
		mesh.tangents = tangents;
 
		//Setting SubMeshes
		mesh.subMeshCount = combines.Length;
 
		
//		vertexOffset = -combines[0].mesh.vertexCount;
		
		foreach( MeshInstance combine in combines )
		{		
			if(combine.mesh!=null)
			{
				if ( combine.mesh!=pervMesh)
				{
					if(pervMesh!=null)vertexOffset += pervMesh.vertexCount;	
				}
				Debug.Log("combine skin name = " + combine.mesh.name + "  subMeshIndex = " + combine.subMeshIndex + " vertexOffset = " + vertexOffset );
				int[] inputtriangles = combine.mesh.GetTriangles(combine.subMeshIndex);
				int[] trianglesx = new int[inputtriangles.Length];
				for (int i=0;i<inputtriangles.Length;i++)
				{
					//triangles[i+triangleOffset] = inputtriangles[i] + vertexOffset;
					trianglesx[i] = inputtriangles[i] + vertexOffset;						
				}
				triangleOffset += inputtriangles.Length;
				mesh.SetTriangles(trianglesx,j++);
//				vertexOffset += combine.mesh.vertexCount;
					
				
			}
			pervMesh = combine.mesh;
			
		}
 		pervMesh = null;
		
		mesh.name = "Combined Mesh";
 
		return mesh;
	}
 
	static void Copy (int vertexcount, Vector3[] src, Vector3[] dst, ref int offset, Matrix4x4 transform)
	{
		for (int i=0;i<src.Length;i++)
		{
			dst[i+offset] = transform.MultiplyPoint(src[i]);
		}
		offset += vertexcount;
	}
 
	static void CopyBoneWei (int vertexcount, BoneWeight[] src, BoneWeight[] dst, ref int offset, Matrix4x4 transform)
	{
		for (int i=0;i<src.Length;i++)
			dst[i+offset] =src[i];
		offset += vertexcount;
	}
 
	static void CopyNormal (int vertexcount, Vector3[] src, Vector3[] dst, ref int offset, Matrix4x4 transform)
	{
		for (int i=0;i<src.Length;i++)
			dst[i+offset] = transform.MultiplyVector(src[i]).normalized;
		offset += vertexcount;
	}
 
	static void Copy (int vertexcount, Vector2[] src, Vector2[] dst, ref int offset)
	{
		for (int i=0;i<src.Length;i++)
			dst[i+offset] = src[i];
		offset += vertexcount;
	}
 
	static void CopyTangents (int vertexcount, Vector4[] src, Vector4[] dst, ref int offset, Matrix4x4 transform)
	{
		for (int i=0;i<src.Length;i++)
		{
			Vector4 p4 = src[i];
			Vector3 p = new Vector3(p4.x, p4.y, p4.z);
			p = transform.MultiplyVector(p).normalized;
			dst[i+offset] = new Vector4(p.x, p.y, p.z, p4.w);
		}
 
		offset += vertexcount;
	}
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;

public class CombineMeshInEditorWindow : EditorWindow
{
	private GameObject m_parentObject;
	private bool m_saveCombinedMeshAssets;
	private static CombineMeshInEditorWindow m_window;
	private TextureAtlasInfo m_textureAtlasInfo;
	private int m_texPropArraySize;
	private bool m_showShaderProperties;
	private bool[] m_shaderFoldoutBools;
	private bool m_createPrefab;
	static private MeshImportSettings m_modelImportSettings;
	static private TextureImportSettings m_textureImportSettings;
	private bool m_showModelSettings = false;
	private bool m_showTextureSettings = false;
	private Vector2 m_scrollPosition;
	static private string m_pathToAssets;
	private bool exportAssets = false;
	private string m_folderPath = "";
	
	private bool gCollider = false;
	
	[MenuItem("Purdyjo/Combine In Editor Window")]
	static void Init()
	{
		m_window = EditorWindow.GetWindow<CombineMeshInEditorWindow>("Draw Call Min");	
		m_modelImportSettings = new MeshImportSettings();
		m_textureImportSettings = new TextureImportSettings();
		m_pathToAssets = Application.dataPath + "/";
	}
	
	void OnGUI()
	{
		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
		Rect verticalLayoutRect = EditorGUILayout.BeginVertical();
		EditorGUILayout.LabelField("Select the parent object that is to be combined.");
		
		EditorGUI.BeginChangeCheck();
		m_parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object Of Meshes To Be Combined", m_parentObject, typeof(GameObject), true);
		if(EditorGUI.EndChangeCheck())
		{
			m_textureAtlasInfo = new TextureAtlasInfo();
			m_texPropArraySize = m_textureAtlasInfo.shaderPropertiesToLookFor.Length;
			m_shaderFoldoutBools = new bool[m_texPropArraySize];
		}
		
		if(m_parentObject != null)
		{			
			m_textureAtlasInfo.compressTexturesInMemory = false;//EditorGUILayout.Toggle("Compress Texture", m_textureAtlasInfo.compressTexturesInMemory);
			
			EditorGUI.BeginChangeCheck();
			m_texPropArraySize = EditorGUILayout.IntSlider("# Of Shader Properties", m_texPropArraySize, 0, 20);
			if(EditorGUI.EndChangeCheck())
			{
				if(m_texPropArraySize > m_textureAtlasInfo.shaderPropertiesToLookFor.Length)
				{
					ShaderProperties[] temp = new ShaderProperties[m_texPropArraySize];
					
					for(int i = 0; i < m_texPropArraySize; i++)
					{
						if(i < m_textureAtlasInfo.shaderPropertiesToLookFor.Length)
						{
							temp[i] = m_textureAtlasInfo.shaderPropertiesToLookFor[i];
						}
						else
						{
							temp[i] = new ShaderProperties(false, "");
						}
					}
					
					m_textureAtlasInfo.shaderPropertiesToLookFor = temp;					
				}
				else if(m_texPropArraySize < m_textureAtlasInfo.shaderPropertiesToLookFor.Length)
				{
					ShaderProperties[] temp = new ShaderProperties[m_texPropArraySize];
					
					for(int i = 0; i < m_texPropArraySize; i++)
					{
						temp[i] = m_textureAtlasInfo.shaderPropertiesToLookFor[i];
					}
				
					m_textureAtlasInfo.shaderPropertiesToLookFor = temp;
				}
				
				m_shaderFoldoutBools = new bool[m_texPropArraySize];
			}
			
			m_showShaderProperties = EditorGUILayout.Foldout(m_showShaderProperties, "Shader Properties To Watch For");
			if(m_showShaderProperties)
			{
				for(int i = 0; i < m_texPropArraySize; i++)
				{
					m_shaderFoldoutBools[i] = EditorGUILayout.Foldout(m_shaderFoldoutBools[i], "Shader Properties Element " + i);
					
					if(m_shaderFoldoutBools[i] == true)
					{
						m_textureAtlasInfo.shaderPropertiesToLookFor[i].markAsNormal = EditorGUILayout.Toggle("Mark As Normal Map", m_textureAtlasInfo.shaderPropertiesToLookFor[i].markAsNormal);
						m_textureAtlasInfo.shaderPropertiesToLookFor[i].propertyName = EditorGUILayout.TextField("Shader Property Name", m_textureAtlasInfo.shaderPropertiesToLookFor[i].propertyName);
					}
				}
			}
			
			GUILayout.Space(m_window.position.height * 0.05f);
			
			EditorGUI.BeginChangeCheck();
			m_folderPath = EditorGUILayout.TextField("Combined Asset Path", m_folderPath);
			if(EditorGUI.EndChangeCheck())
			{
				m_pathToAssets = Application.dataPath + "/" + m_folderPath + "/";
			}
			
			GUILayout.Space(m_window.position.height * 0.05f);
			
			m_showModelSettings = EditorGUILayout.Foldout(m_showModelSettings, "Model Settings");
			if(m_showModelSettings == true)
			{
				GUILayout.Label("Meshes", "BoldLabel");
				EditorGUILayout.LabelField("Meshes");
				m_modelImportSettings.globalScale = EditorGUILayout.FloatField("Global Scale", m_modelImportSettings.globalScale);
				m_modelImportSettings.meshCompression = (ModelImporterMeshCompression)EditorGUILayout.EnumPopup("Mesh Compression", m_modelImportSettings.meshCompression);
				m_modelImportSettings.optimizeMesh = EditorGUILayout.Toggle("Optimize Mesh", m_modelImportSettings.optimizeMesh);
				m_modelImportSettings.addCollider = EditorGUILayout.Toggle("Generate Colliders", m_modelImportSettings.addCollider);
				m_modelImportSettings.swapUVChannels = EditorGUILayout.Toggle("Swap UVs", m_modelImportSettings.swapUVChannels);
				m_modelImportSettings.generateSecondaryUV = EditorGUILayout.Toggle("Generate Lightmap UVs", m_modelImportSettings.generateSecondaryUV);
				
				GUILayout.Label("Normals & Tangents", "BoldLabel");

				m_modelImportSettings.normalImportMode = (ModelImporterTangentSpaceMode)EditorGUILayout.EnumPopup("Normals", m_modelImportSettings.normalImportMode);
				m_modelImportSettings.tangentImportMode = (ModelImporterTangentSpaceMode)EditorGUILayout.EnumPopup("Tangents", m_modelImportSettings.tangentImportMode);
				
				if((m_modelImportSettings.normalImportMode == ModelImporterTangentSpaceMode.Calculate) && !(m_modelImportSettings.tangentImportMode == ModelImporterTangentSpaceMode.None))
				{
					m_modelImportSettings.tangentImportMode = ModelImporterTangentSpaceMode.Calculate;
				}
				
				EditorGUI.BeginDisabledGroup(!(m_modelImportSettings.normalImportMode == ModelImporterTangentSpaceMode.Calculate));
				m_modelImportSettings.normalSmoothingAngle = EditorGUILayout.IntSlider("Normal Smoothing Angle", (int)m_modelImportSettings.normalSmoothingAngle, 0, 180);
				EditorGUI.EndDisabledGroup();
				
				EditorGUI.BeginDisabledGroup(!(m_modelImportSettings.tangentImportMode == ModelImporterTangentSpaceMode.Calculate));
				m_modelImportSettings.splitTangentsAcrossSeams = EditorGUILayout.Toggle("Split Tangents", m_modelImportSettings.splitTangentsAcrossSeams);
				EditorGUI.EndDisabledGroup();				
			}
			
			m_showTextureSettings = EditorGUILayout.Foldout(m_showTextureSettings, "Texture Settings");
			if(m_showTextureSettings == true)
			{
				m_textureImportSettings.textureType = (TextureImporterType)EditorGUILayout.EnumPopup("", m_textureImportSettings.textureType);
			
				switch(m_textureImportSettings.textureType)
				{
				case TextureImporterType.Bump:					
					//m_textureImportSettings.convertToNormalmap = EditorGUILayout.Toggle("", m_textureImportSettings.convertToNormalmap);
					m_textureImportSettings.heightmapScale = EditorGUILayout.Slider(m_textureImportSettings.heightmapScale, 0.0f, 0.3f);
					m_textureImportSettings.normalmapFilter = (TextureImporterNormalFilter)EditorGUILayout.EnumPopup("Normal Map Filter", m_textureImportSettings.normalmapFilter);
					
					m_textureImportSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Texture Wrap Mode", m_textureImportSettings.wrapMode);
					m_textureImportSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Texture Filter Mode", m_textureImportSettings.filterMode);
					m_textureImportSettings.anisoLevel = EditorGUILayout.IntSlider("Aniso Level", m_textureImportSettings.anisoLevel, 0, 10);
					break;
				case TextureImporterType.Lightmap:
					m_textureImportSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Texture Filter Mode", m_textureImportSettings.filterMode);
					m_textureImportSettings.anisoLevel = EditorGUILayout.IntSlider("Aniso Level", m_textureImportSettings.anisoLevel, 0, 10);					
					break;
				case TextureImporterType.Reflection:
					m_textureImportSettings.grayscaleToAlpha = EditorGUILayout.Toggle("Alpha From Grayscale", m_textureImportSettings.grayscaleToAlpha);
					m_textureImportSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Texture Filter Mode", m_textureImportSettings.filterMode);
					m_textureImportSettings.anisoLevel = EditorGUILayout.IntSlider("Aniso Level", m_textureImportSettings.anisoLevel, 0, 10);
					break;
				case TextureImporterType.Image:
				default:
					m_textureImportSettings.textureType = TextureImporterType.Image;
					m_textureImportSettings.grayscaleToAlpha = EditorGUILayout.Toggle("Alpha From Grayscale", m_textureImportSettings.grayscaleToAlpha);
					m_textureImportSettings.wrapMode = (TextureWrapMode)EditorGUILayout.EnumPopup("Texture Wrap Mode", m_textureImportSettings.wrapMode);
					m_textureImportSettings.filterMode = (FilterMode)EditorGUILayout.EnumPopup("Texture Filter Mode", m_textureImportSettings.filterMode);
					m_textureImportSettings.anisoLevel = EditorGUILayout.IntSlider("Aniso Level", m_textureImportSettings.anisoLevel, 0, 10);
					break;
				}
				
				m_textureImportSettings.maxTextureSize = (int)(object)EditorGUILayout.EnumPopup((TextureSize)m_textureImportSettings.maxTextureSize);
				m_textureImportSettings.textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup(m_textureImportSettings.textureFormat);
				
				
				/*mip map stuff*/
			}
			exportAssets = EditorGUILayout.Toggle("Export Assets ", exportAssets); 
			gCollider = EditorGUILayout.Toggle("Collider ", gCollider); 
			GUILayout.Space(m_window.position.height * 0.05f);
			
			
			EditorGUILayout.EndVertical();	
			
			EditorGUILayout.BeginHorizontal();

			if(GUILayout.Button("Combine Mesh", GUILayout.Height(m_window.position.height * 0.1f)))
			{
				combineMesh(exportAssets,gCollider);
			}	
			
			
			
			EditorGUILayout.EndHorizontal();
		}
		
		EditorGUILayout.EndScrollView();
	}
	
	void displayTextureImportProperties()
	{
	}
	
	void displayMeshImportProperties()
	{
	}
	
	void checkAndCreateFolder()
	{
		if(!File.Exists(m_pathToAssets))
		{
			Directory.CreateDirectory(m_pathToAssets);
			AssetDatabase.Refresh();
		}
	}
	void checkAndCreateFolder(string path)
	{
		if(!File.Exists(path))
		{
			Directory.CreateDirectory(path);
			AssetDatabase.Refresh();
		}
	} 
	
	
	GameObject combineMesh(bool exportAssets,bool genCollider=true,bool doSetParent=true,bool doSetLayer=true ,bool doSetTag=true)
	{
//		GameObject returnObject = new GameObject(m_parentObject.name);
//		returnObject.transform.position = m_parentObject.transform.position;
//		returnObject.transform.rotation = m_parentObject.transform.rotation;

		
		MeshFilter[] filters = m_parentObject.GetComponentsInChildren<MeshFilter>();
		Matrix4x4 myTransform = m_parentObject.transform.worldToLocalMatrix;
		
		this.checkAndCreateFolder();
		
		Dictionary<string, Dictionary<Material, List<MeshCombineUtility.MeshInstance>>> allMeshesAndMaterials = new Dictionary<string, Dictionary<Material, List<MeshCombineUtility.MeshInstance>>>();
		for(int i = 0; i < filters.Length; i++)
		{
			
			Renderer curRenderer = filters[i].GetComponent<Renderer>();
			MeshCombineUtility.MeshInstance instance = new MeshCombineUtility.MeshInstance();
			
			instance.mesh = filters[i].sharedMesh;
			
			if(curRenderer != null && curRenderer.enabled && instance.mesh != null)
			{
				instance.transform = myTransform * filters[i].transform.localToWorldMatrix;
				
				Material[] materials = curRenderer.sharedMaterials;
				for(int m = 0; m < materials.Length; m++)
				{
					instance.subMeshIndex = System.Math.Min(m, instance.mesh.subMeshCount - 1);
					
					if(!allMeshesAndMaterials.ContainsKey(materials[m].shader.ToString()))
					{
						allMeshesAndMaterials.Add(materials[m].shader.ToString(), new Dictionary<Material, List<MeshCombineUtility.MeshInstance>>());
					}

					if(!allMeshesAndMaterials[materials[m].shader.ToString()].ContainsKey(materials[m]))
					{
						allMeshesAndMaterials[materials[m].shader.ToString()].Add(materials[m], new List<MeshCombineUtility.MeshInstance>());
					}
					
					allMeshesAndMaterials[materials[m].shader.ToString()][materials[m]].Add(instance);
				}
			}
		}
		
		foreach(KeyValuePair<string, Dictionary<Material, List<MeshCombineUtility.MeshInstance>>>  firstPass in allMeshesAndMaterials)
		{
			Material[] allMaterialTextures = new Material[firstPass.Value.Keys.Count];
			int index = 0;
								
			foreach(KeyValuePair<Material, List<MeshCombineUtility.MeshInstance>> kv in firstPass.Value)
			{
				allMaterialTextures[index] = kv.Key;
				index++;
			}
			
			TextureCombineUtility.TexturePosition[] textureUVPositions;
			Material combined = TextureCombineUtility.combine(allMaterialTextures, out textureUVPositions, m_textureAtlasInfo);
			List<MeshCombineUtility.MeshInstance> meshes = new List<MeshCombineUtility.MeshInstance>();
			
			foreach(KeyValuePair<Material, List<MeshCombineUtility.MeshInstance>> kv in firstPass.Value)
			{
				List<MeshCombineUtility.MeshInstance> meshIntermediates = new List<MeshCombineUtility.MeshInstance>();
				Mesh[] firstCombineStep = MeshCombineUtility.Combine(kv.Value.ToArray());
				
				for(int i = 0; i < firstCombineStep.Length; i++)
				{
					MeshCombineUtility.MeshInstance instance = new MeshCombineUtility.MeshInstance();
					instance.mesh = firstCombineStep[i];
					instance.subMeshIndex = 0;
					instance.transform = Matrix4x4.identity;
					meshIntermediates.Add(instance);
				}
				if(textureUVPositions!=null)
				{
					TextureCombineUtility.TexturePosition refTexture = textureUVPositions[0];
						
					for(int j = 0; j < textureUVPositions.Length; j++)
					{
						if(kv.Key.mainTexture.name == textureUVPositions[j].textures[0].name)
						{
							refTexture = textureUVPositions[j];										
							break;
						}
					}	
					
					
					for(int j = 0; j < meshIntermediates.Count; j++)
					{			
						Vector2[] uvCopy = meshIntermediates[j].mesh.uv;
						for(int k = 0; k < uvCopy.Length; k++)
						{
							uvCopy[k].x = refTexture.position.x + uvCopy[k].x * refTexture.position.width;
							uvCopy[k].y = refTexture.position.y + uvCopy[k].y * refTexture.position.height;
						}
						
						meshIntermediates[j].mesh.uv = uvCopy;				
						
						uvCopy = meshIntermediates[j].mesh.uv2;
						for(int k = 0; k < uvCopy.Length; k++)
						{
							uvCopy[k].x = refTexture.position.x + uvCopy[k].x * refTexture.position.width;
							uvCopy[k].y = refTexture.position.y + uvCopy[k].y * refTexture.position.height;
						}					
					
						meshIntermediates[j].mesh.uv2 = uvCopy;
	
						uvCopy = meshIntermediates[j].mesh.uv2;
						for(int k = 0; k < uvCopy.Length; k++)
						{
							uvCopy[k].x = refTexture.position.x + uvCopy[k].x * refTexture.position.width;
							uvCopy[k].y = refTexture.position.y + uvCopy[k].y * refTexture.position.height;
						}					
						
						meshIntermediates[j].mesh.uv2 = uvCopy;
						
						meshes.Add(meshIntermediates[j]);
					}
				}
			}
			
			Material mat = combined;
			
			if(exportAssets) //combined exportMaterial and combined Textures
			{
				checkAndCreateFolder();
					
					Debug.Log((mat.mainTexture as Texture2D).format);
					if((mat.mainTexture as Texture2D).format != TextureFormat.ARGB32 && 
						(mat.mainTexture as Texture2D).format != TextureFormat.RGB24 &&
						(mat.mainTexture as Texture2D).format != TextureFormat.RGBA32)
					{
						Debug.LogError("Textures assigned to objects must be either RGBA32 or RGB 24 to be exported");
						return null;
					}
			
				byte[] textureByte = (mat.mainTexture as Texture2D).EncodeToPNG();
				string texturefolder =  m_pathToAssets.Substring(m_pathToAssets.LastIndexOf("Assets/")) +"Textures/";
				checkAndCreateFolder(texturefolder);
				System.IO.File.WriteAllBytes(texturefolder + m_parentObject.name + ".png", textureByte);
				Material outMat = new Material(mat);
				string materialfolder = m_pathToAssets.Substring(m_pathToAssets.LastIndexOf("Assets/"))+"Materials/";
				checkAndCreateFolder(materialfolder);
				string assetPathFile = materialfolder + m_parentObject.name + ".mat";
				AssetDatabase.CreateAsset(outMat,assetPathFile );
				outMat.CopyPropertiesFromMaterial(mat);
				outMat.mainTexture = AssetDatabase.LoadAssetAtPath(texturefolder + m_parentObject.name + ".png",typeof(Texture)) as Texture; 
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				mat = outMat;
			}
			
			Mesh[] combinedMeshes = MeshCombineUtility.Combine(meshes.ToArray());
			
			GameObject parent = new GameObject(m_parentObject.name + "_Combined "  + firstPass.Key + " Mesh Parent");
			parent.transform.position = m_parentObject.transform.position;
			parent.transform.rotation = m_parentObject.transform.rotation;
			parent.transform.parent = m_parentObject.transform;
	
			if(doSetLayer)
				parent.layer = m_parentObject.layer;
					
			if(doSetTag)
				parent.tag = m_parentObject.tag;
			
			if(doSetParent)
				parent.transform.parent = m_parentObject.transform.parent;
			
			parent.isStatic = m_parentObject.isStatic;
			
			for(int i = 0; i < combinedMeshes.Length; i++)
			{
				GameObject go = new GameObject( m_parentObject.name + "_Combined_Meshs");
				go.transform.parent = parent.transform;
				go.tag = m_parentObject.tag;
				go.layer = m_parentObject.layer;
				go.transform.localScale = Vector3.one;
				go.transform.localRotation = Quaternion.identity;
				go.transform.localPosition = Vector3.zero;
				MeshFilter filter = go.AddComponent<MeshFilter>();
				go.AddComponent<MeshRenderer>();
				go.GetComponent<Renderer>().sharedMaterial = mat;
				
				filter.mesh = combinedMeshes[i];
				
				if(exportAssets == true)
				{
					exportMeshFilter(filter,m_parentObject.name + i);
//					exportMesh(combinedMeshes[i], m_parentObject.name + i,mat);
				}
				if(genCollider)
					if(go.GetComponent<MeshCollider>()==null)go.gameObject.AddComponent<MeshCollider>();
				
				if(doSetLayer)
					go.layer = m_parentObject.layer;
				
				if(doSetTag)
					go.tag = m_parentObject.tag;
				
				go.isStatic = m_parentObject.isStatic;
			}
			
		}
		
		//if(developmentBake == true)
		//{
		foreach(Renderer r in m_parentObject.GetComponentsInChildren<Renderer>())
		{
			r.enabled = false;
		}
		//}
		
		return m_parentObject;
	}
	
	void exportMeshFilter(MeshFilter mf, string assetName)
	{
		EditorObjExporter.MeshToFile(mf,m_pathToAssets,assetName);
		
//		AssetDatabase.ImportAsset(m_pathToAssets + assetName + ".obj",ImportAssetOptions.ForceUpdate);
//		AssetDatabase.GenerateUniqueAssetPath(m_pathToAssets + assetName + ".obj");
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		string filename = m_pathToAssets.Substring(m_pathToAssets.LastIndexOf("Assets/")) + assetName + ".obj";
		Mesh outputMesh = AssetDatabase.LoadAssetAtPath(filename,typeof(Mesh)) as Mesh;
		Debug.Log(" ### load path = " +m_pathToAssets + assetName + ".obj"+" success = " +  (outputMesh != null));
		mf.mesh = outputMesh;
	}
	
	void exportMesh(Mesh currentMesh, string assetName,Material mat)
	{       
		
		StringBuilder sb = new StringBuilder();
	
		sb.Append("g ").Append(assetName).Append("\n");
			
		foreach(Vector3 v in currentMesh.vertices)
		{
			sb.Append(string.Format("v {0} {1} {2}\n", -v.x, v.y,v.z));
		}
		sb.Append("\n");
		foreach(Vector3 v in currentMesh.normals)
		{
			sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
		}
		sb.Append("\n");
		foreach(Vector2 v in currentMesh.uv)
		{
			sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}
		sb.Append("\n");
		foreach(Vector2 v in currentMesh.uv2)
		{
			sb.Append(string.Format("vt1 {0} {1}\n", v.x, v.y));
		}
		sb.Append("\n");
		foreach(Vector2 v in currentMesh.uv2)
		{
			sb.Append(string.Format("vt2 {0} {1}\n", v.x, v.y));
		}
		sb.Append("\n");
        sb.Append("usemtl ").Append(mat.name).Append("\n");
        sb.Append("usemap ").Append(mat.name).Append("\n");
		sb.Append("\n");
		foreach(Color c in currentMesh.colors)
		{
			sb.Append(string.Format("vc {0} {1} {2} {3}\n", c.r, c.g, c.b, c.a));
		}
		
 
		for(int j = 0; j < currentMesh.triangles.Length; j += 3)
		{
			sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", 
                    currentMesh.triangles[j] + 1, currentMesh.triangles[j + 1] + 1, currentMesh.triangles[j + 2] + 1));
		}
		string outputFileName = m_pathToAssets + assetName + ".obj";
		try
		{
			string matprop = "mtllib ./"+mat.name+".mtl";
			Debug.Log("Exporting mesh to " + outputFileName + " Material " + matprop);
			
			checkAndCreateFolder();
			using(StreamWriter sw = new StreamWriter(outputFileName))
			{
				sw.WriteLine(matprop);
				sw.WriteLine(sb.ToString());
			}
			
		}
		catch(System.Exception)
		{
		}
		AssetDatabase.Refresh();
		
	}
	
	
}

public class MeshImportSettings
{
	public float globalScale;
	public bool addCollider;
	public float normalSmoothingAngle;
	public bool splitTangentsAcrossSeams;
	public bool swapUVChannels;
	public bool generateSecondaryUV;
	public bool optimizeMesh;
	public ModelImporterTangentSpaceMode normalImportMode;
	public ModelImporterTangentSpaceMode tangentImportMode;
	public ModelImporterMeshCompression meshCompression;
	
	public MeshImportSettings()
	{
		globalScale = 1.0f;
		addCollider = false;
		normalSmoothingAngle = 60.0f;
		splitTangentsAcrossSeams = true;
		swapUVChannels = false;
		generateSecondaryUV = false;
		optimizeMesh = false;
		normalImportMode = ModelImporterTangentSpaceMode.Import;
		tangentImportMode = ModelImporterTangentSpaceMode.Calculate;
		meshCompression = ModelImporterMeshCompression.Off;
	}
}

public class TextureImportSettings
{
	public TextureImporterFormat textureFormat;
	public int maxTextureSize;
	public bool grayscaleToAlpha;
	public TextureImporterGenerateCubemap generateCubemap;
	public bool isReadable;
	public bool mipmapEnabled;
	public bool borderMipmap;
	public TextureImporterMipFilter mipmapFilter;
	public bool fadeout;
	public int mipmapFadeDistanceStart;
	public int mipmapFadeDistanceEnd;
	public bool generateMipsInLinearSpace;
	public TextureImporterNormalFilter normalmapFilter;
	public float heightmapScale;
	public int anisoLevel;
	public FilterMode filterMode;
	public TextureWrapMode wrapMode;
	public TextureImporterType textureType;
	
	public TextureImportSettings()
	{
		textureFormat = TextureImporterFormat.AutomaticTruecolor;
		maxTextureSize = (int)TextureSize.Unlimited;
		grayscaleToAlpha = false;
		generateCubemap = TextureImporterGenerateCubemap.None;
		isReadable = false;
		mipmapEnabled = true;
		borderMipmap = false;
		mipmapFilter = TextureImporterMipFilter.BoxFilter;
		fadeout = false;
		mipmapFadeDistanceStart = 1;
		mipmapFadeDistanceEnd = 1;
		generateMipsInLinearSpace = false;
		normalmapFilter = TextureImporterNormalFilter.Standard;
		heightmapScale = 0.25f;
		anisoLevel = 1;
		filterMode = FilterMode.Bilinear;
		wrapMode = TextureWrapMode.Repeat;
		textureType = TextureImporterType.Image;
	}
}

public enum TextureSize
{
	_32 = 1 << 5,
	_64 = 1 << 6,
	_128 = 1 << 7,
	_256 = 1 << 8,
	_512 = 1 << 9,
	_1024 = 1 << 10,
	_2048 = 1 << 11,
	_4096 = 1 << 12,
	Unlimited
}
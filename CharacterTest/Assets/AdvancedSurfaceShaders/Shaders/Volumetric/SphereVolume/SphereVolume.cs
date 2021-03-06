using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SphereVolume : MonoBehaviour
{
    public Material volumetricMaterial = null;

    public float radius = 3.0f;
    public float visibility = 6.0f;
    public Color volumeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    public Texture2D texture = null;
    public float textureScale = 1f;
    public Vector3 textureMovement = new Vector3(0f, -0.1f, 0f);

    private Mesh meshInstance = null;
    private Material materialInstance = null;

    private Transform thisTransform = null;

    private float previousRadius = 1.0f;
    private float previousVisibility = 1.0f;
    private Color previousVolumeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private Vector3 forcedLocalScale = Vector3.one;
    private Texture2D previousTexture = null;
    private float previousTextureScale = 10f;
    private Vector3 previousTextureMovement = new Vector3(0f, 0.1f, 0f);

    private Vector3[] unitVerts = new Vector3[8];

#if UNITY_EDITOR
    private bool setSceneCameraDepthMode = false;

    [MenuItem("GameObject/Create Other/Volumetric Objects/Sphere Volume")]
    static public void CreateSphereVolume()
    {
        GameObject newObject = new GameObject("Sphere Volume");
        SphereVolume sphereVolume = (SphereVolume)newObject.AddComponent<SphereVolume>();
        sphereVolume.enabled = false;
        sphereVolume.enabled = true;
    }
#endif

    void Awake()
    {
        if (volumetricMaterial == null)
        {
            this.enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        SetupUnitVerts();

        thisTransform = transform;

        if (meshInstance != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(meshInstance);
#else
            Destroy(meshInstance);
#endif
        }
        meshInstance = CreateCube();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.sharedMesh = meshInstance;

        if (materialInstance != null)
        {
#if UNITY_EDITOR
            //DestroyImmediate(materialInstance);
#else
            Destroy(materialInstance);
#endif
        }

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = (Material)Instantiate(volumetricMaterial);
        materialInstance = mr.sharedMaterial;

        if (Camera.current)
        {
            Camera.current.depthTextureMode |= DepthTextureMode.Depth;
        }
        if (Camera.main)
        {
            Camera.main.depthTextureMode |= DepthTextureMode.Depth;
        }
#if UNITY_EDITOR
        setSceneCameraDepthMode = true;
#endif

        UpdateVolume();
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        if (setSceneCameraDepthMode)
        {
            setSceneCameraDepthMode = false;
            SetSceneCameraDepthMode();
        }
#endif

        if (radius != previousRadius ||
            visibility != previousVisibility ||
            volumeColor != previousVolumeColor ||
            thisTransform.localScale != forcedLocalScale ||
            texture != previousTexture ||
            textureScale != previousTextureScale ||
            textureMovement != previousTextureMovement)
        {
            previousRadius = radius;
            previousVisibility = visibility;
            previousVolumeColor = volumeColor;
            thisTransform.localScale = forcedLocalScale;
            previousTexture = texture;
            previousTextureScale = textureScale;
            previousTextureMovement = textureMovement;
            UpdateVolume();
        }
    }

#if UNITY_EDITOR
    void SetSceneCameraDepthMode()
    {
        Camera[] sceneCameras = SceneView.GetAllSceneCameras();
        for (int i = 0; i < sceneCameras.Length; i++)
        {
            sceneCameras[i].depthTextureMode |= DepthTextureMode.Depth;
        }
    }
#endif

    public void UpdateVolume()
    {
        Vector3 halfBoxSize = Vector3.one * radius;

        if (meshInstance)
        {
            ScaleMesh(meshInstance, halfBoxSize);

            // Set bounding volume so modified vertices don't get culled
            Bounds bounds = new Bounds();
            bounds.SetMinMax(-halfBoxSize, halfBoxSize);
            meshInstance.bounds = bounds;
        }

        if (materialInstance)
        {
            materialInstance.SetVector("_TextureData", new Vector4(-textureMovement.x, -textureMovement.y, -textureMovement.z, (1f / textureScale)));
            materialInstance.SetFloat("_RadiusSqr", radius * radius);
            materialInstance.SetFloat("_Visibility", visibility);
            materialInstance.SetColor("_Color", volumeColor);
            materialInstance.SetTexture("_MainTex", texture);
        }
    }

    public void SetupUnitVerts()
    {
        // Vert order
        // -x -y -z
        // +x -y -z
        // +x +y -z
        // +x -y +z
        // +x +y +z
        // -x +y -z
        // -x +y +z
        // -x -y +z

        float s = 1.0f;

        unitVerts[0].x = -s; unitVerts[0].y = -s; unitVerts[0].z = -s;
        unitVerts[1].x = +s; unitVerts[1].y = -s; unitVerts[1].z = -s;
        unitVerts[2].x = +s; unitVerts[2].y = +s; unitVerts[2].z = -s;
        unitVerts[3].x = +s; unitVerts[3].y = -s; unitVerts[3].z = +s;
        unitVerts[4].x = +s; unitVerts[4].y = +s; unitVerts[4].z = +s;
        unitVerts[5].x = -s; unitVerts[5].y = +s; unitVerts[5].z = -s;
        unitVerts[6].x = -s; unitVerts[6].y = +s; unitVerts[6].z = +s;
        unitVerts[7].x = -s; unitVerts[7].y = -s; unitVerts[7].z = +s;
    }

    public Mesh CreateCube()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] verts = new Vector3[unitVerts.Length];
        unitVerts.CopyTo(verts, 0);
        mesh.vertices = verts;

        int[] indices = new int[36];
        int i = 0;
        indices[i] = 0; i++; indices[i] = 2; i++; indices[i] = 1; i++;
        indices[i] = 0; i++; indices[i] = 5; i++; indices[i] = 2; i++;
        indices[i] = 3; i++; indices[i] = 6; i++; indices[i] = 7; i++;
        indices[i] = 3; i++; indices[i] = 4; i++; indices[i] = 6; i++;
        indices[i] = 1; i++; indices[i] = 4; i++; indices[i] = 3; i++;
        indices[i] = 1; i++; indices[i] = 2; i++; indices[i] = 4; i++;
        indices[i] = 7; i++; indices[i] = 5; i++; indices[i] = 0; i++;
        indices[i] = 7; i++; indices[i] = 6; i++; indices[i] = 5; i++;
        indices[i] = 7; i++; indices[i] = 1; i++; indices[i] = 3; i++;
        indices[i] = 7; i++; indices[i] = 0; i++; indices[i] = 1; i++;
        indices[i] = 5; i++; indices[i] = 4; i++; indices[i] = 2; i++;
        indices[i] = 5; i++; indices[i] = 6; i++; indices[i] = 4; i++;

        mesh.triangles = indices;

        mesh.RecalculateBounds();

        return mesh;
    }

    public void ScaleMesh(Mesh mesh, Vector3 scaleFactor)
    {
        Vector3[] scaledVertices = new Vector3[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; i++)
		{
            scaledVertices[i] = ScaleVector(unitVerts[i], scaleFactor);
		}
        mesh.vertices = scaledVertices;
    }

    Vector3 ScaleVector(Vector3 vector, Vector3 scale)
    {
        return new Vector3(vector.x * scale.x, vector.y * scale.y, vector.z * scale.z);
    }

    public Mesh CopyMesh(Mesh original)
    {
        Mesh mesh = new Mesh();
        // Copy Verts
        Vector3[] verts = new Vector3[original.vertices.Length];
        original.vertices.CopyTo(verts, 0);
        mesh.vertices = verts;

        // Copy UV
        Vector2[] uv = new Vector2[original.uv.Length];
        original.uv.CopyTo(uv, 0);
        mesh.uv = uv;

        // Copy UV1
        Vector2[] uv1 = new Vector2[original.uv1.Length];
        original.uv1.CopyTo(uv1, 0);
        mesh.uv1 = uv1;

        // Copy UV2
        Vector2[] uv2 = new Vector2[original.uv2.Length];
        original.uv2.CopyTo(uv2, 0);
        mesh.uv2 = uv2;

        // Copy Normals
        Vector3[] norms = new Vector3[original.normals.Length];
        original.normals.CopyTo(norms, 0);
        mesh.normals = norms;

        // Copy Tangents
        Vector4[] tans = new Vector4[original.tangents.Length];
        original.tangents.CopyTo(tans, 0);
        mesh.tangents = tans;

        // Copy Colors
        Color[] cols = new Color[original.colors.Length];
        original.colors.CopyTo(cols, 0);
        mesh.colors = cols;

        // Triangles (sub meshes)
        mesh.subMeshCount = original.subMeshCount;
        for (int i = 0; i < original.subMeshCount; i++)
        {
            int[] subTris = original.GetTriangles(i);
            int[] triangles = new int[subTris.Length];
            subTris.CopyTo(triangles, 0);
            mesh.SetTriangles(subTris, i);
        }

        mesh.RecalculateBounds();

        return mesh;
    }
}
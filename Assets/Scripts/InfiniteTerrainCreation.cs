using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class InfiniteTerrainCreation : MonoBehaviour
{
    public const float maxViewDistance = 450f;
    public Transform viewer;
    public Material mapMaterial;
    
    public static Vector2 viewerPosition;
    static MapGenerator mapGenerator;
    private int chunkSize;
    private int chunkVisibleInViewDistance;
    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new();
    List<TerrainChunk> alreadySeenChunksLastUpdate = new();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunkVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {

        for (int i = 0; i < alreadySeenChunksLastUpdate.Count; i++)
        {
            alreadySeenChunksLastUpdate[i].SetVisible(false);
        }
        alreadySeenChunksLastUpdate.Clear();
        
        int currentChunkCoordX = Mathf.RoundToInt(viewer.position.x / chunkSize);
        int currentChunkCoordZ = Mathf.RoundToInt(viewer.position.z / chunkSize);

        for (int zOffset = -chunkVisibleInViewDistance; zOffset <= chunkVisibleInViewDistance; zOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDistance; xOffset <= chunkVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordZ + zOffset);
                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDict[viewedChunkCoord].isVisible())
                    {
                        alreadySeenChunksLastUpdate.Add(terrainChunkDict[viewedChunkCoord]);
                    } 
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        private Bounds bounds;
        
        MeshFilter meshFilter;
        MeshRenderer meshRenderer;

        public TerrainChunk(Vector2 coordinates, int size, Material material)
        {
            position = coordinates * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            
            meshObject = new GameObject("Terrain Chunk");
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            
            meshObject.transform.position = positionV3;
            SetVisible(false);
            
            mapGenerator.RequestMapData(OnMapDataReceived);
        }
        
        // public TerrainChunk(Vector2 coordinates, int size, Transform parent, Material material)
        // {
        //     position = coordinates * size;
        //     bounds = new Bounds(position, Vector2.one * size);
        //     Vector3 positionV3 = new Vector3(position.x, 0, position.y);
        //     
        //     meshObject = new GameObject("Terrain Chunk");
        //     meshFilter = meshObject.AddComponent<MeshFilter>();
        //     meshRenderer = meshObject.AddComponent<MeshRenderer>();
        //     meshRenderer.material = material;
        //     
        //     meshObject.transform.position = positionV3;
        //     meshObject.transform.parent = parent;
        //     SetVisible(false);
        //     
        //     mapGenerator.RequestMapData(OnMapDataReceived);
        // }

        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }
        
        void OnMeshDataReceived(MeshData meshData)
        {
            Debug.Log("Mesh data received and applied.");
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateTerrainChunk()
        {
            float viewDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewDistanceFromNearestEdge <= maxViewDistance;
            SetVisible(visible);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }
    }
}

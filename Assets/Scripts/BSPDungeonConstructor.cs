using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Sabresaurus.SabreCSG;
using ProceduralGeneration.BSPDungeonGenerator;

public class BSPDungeonConstructor : MonoBehaviour {

    public Material roomMaterial;
    public Material partitionMaterial;
    public Material connectionMaterial;

    public bool drawPartitions;
    public bool drawRooms;
    public bool drawConnections;

    public BSPSettings binarySpacePartitionSettings;
    public RoomSettings roomSettings;
    public ConnectionSettings connectionSettings;

    CSGModelBase csgModel;
    GameObject level;

    private void Start() {
        CreateRoom();
    }

    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            CreateRoom();
        }
    }

    void CreateRoom() {
        if (level != null) Destroy(level);
        level = new GameObject("Level");
        level.transform.parent = gameObject.transform;

        BSPDungeon bspDungeon = new BSPDungeon(binarySpacePartitionSettings, roomSettings, connectionSettings);
        bspDungeon.GenerateDungeon();
        List<Partition> finalPartitions = bspDungeon.GenerateFinalPartitionList();
        List<Partition> allPartitions = bspDungeon.GeneratePartitionList();

        Vector3 position;
        Vector3 size;

        csgModel = level.AddComponent<CSGModelBase>();

        //Debug.Log("drawing rooms and partitions");
        foreach (Partition partition in finalPartitions) {

            if (drawPartitions) {
                position = (Vector2)(partition.start + partition.end) / 2;
                size = (Vector2)(partition.end - partition.start);
                position.z += 5;
                size.x -= 0.5f;
                size.y -= 0.5f;
                size.z = 1;
                //Debug.Log("Position: " + position + ", Size: " + size);
                csgModel.CreateBrush(PrimitiveBrushType.Cube, position, size, Quaternion.identity, partitionMaterial);
            }

            if (drawRooms) {
                if (partition.room != null) {
                    foreach (Shape shape in partition.room) {
                        if (shape.GetType() == typeof(Rectangle)) {
                            Rectangle rectangle = (Rectangle)shape;
                            position = (Vector2)(rectangle.start + rectangle.end) / 2;
                            position.z -= 0;
                            size = (Vector2)(rectangle.end - rectangle.start);
                            size.z = 1;
                            //Debug.Log("Position: " + position + ", Size: " + size);
                            csgModel.CreateBrush(PrimitiveBrushType.Cube, position, size, Quaternion.identity, roomMaterial);
                        }
                    }
                }
            }
        }
        
        foreach (Partition partition in allPartitions) {
            if (drawConnections) {
                foreach (Shape shape in partition.connection) {
                    if (shape.GetType() == typeof(Rectangle)) {
                        Rectangle rectangle = (Rectangle)shape;
                        position = (Vector2)(rectangle.start + rectangle.end) / 2;
                        position.z -= 5;
                        size = (Vector2)(rectangle.end - rectangle.start);
                        size.z = 1;
                        //Debug.Log("Position: " + position + ", Size: " + size);
                        csgModel.CreateBrush(PrimitiveBrushType.Cube, position, size, Quaternion.identity, connectionMaterial);
                    }
                }
            }
        }
        Debug.Log(finalPartitions.Count);

        csgModel.Build(true, false);
    }
}

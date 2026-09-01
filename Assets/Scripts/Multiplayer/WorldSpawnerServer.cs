using System;
using FishNet;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldSpawnerServer : NetworkBehaviour
{
    public GameObject worldParentTransform;
    public Transform worldParentTransformPosition;
    private EventBinding<RoomInfo> roomInfoEventBinding;

    private void Start()
    {
        roomInfoEventBinding = new EventBinding<RoomInfo>(OnRoomInitialized);
        EventBus<RoomInfo>.Register(roomInfoEventBinding);
    }

    private void OnDestroy()
    {
        EventBus<RoomInfo>.Deregister(roomInfoEventBinding);
    }

    private void OnRoomInitialized(RoomInfo obj)
    {
        // SpawnWorld(obj.gameScene);
    }

    public void SpawnWorldLocal(Scene scene)
    {
        SpawnWorld(scene);
    }

    [Server]
    private void SpawnWorld(Scene scene)
    {
        GameObject world = Instantiate(
            worldParentTransform
        );

        world.transform.SetPositionAndRotation(
            worldParentTransformPosition.position,
            worldParentTransformPosition.rotation
        );
        DisableAllMeshRenderers(world);

        InstanceFinder.ServerManager.Spawn(world,scene:scene);
    }

    [Server]
    private void DisableAllMeshRenderers(GameObject world)
    {
        MeshRenderer[] renderers =
            world.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }
}

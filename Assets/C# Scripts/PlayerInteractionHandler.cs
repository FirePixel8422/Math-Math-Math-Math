using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionHandler : MonoBehaviour
{
    public float interactionRange;

    public Transform blockBreakAnimationObj;
    public LayerMask blockLayer;

    public int3 selectedChunkGridPos;
    public int3 selectedBlockGridPos;


    public bool leftClickHeld;

    private Camera mainCam;


    private void Start()
    {
        mainCam = Camera.main;
    }


    public void OnLeftClick(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            leftClickHeld = true;
        }
        else if (ctx.canceled)
        {
            leftClickHeld = false;
        }
    }


    private void Update()
    {
        if (leftClickHeld)
        {
            // Create a ray from the mouse position
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);


            // Perform the raycast
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, blockLayer))
            {
                if (hit.transform.TryGetComponent(out Chunk chunk))
                {
                    Vector3 pos = hit.point - hit.normal * 0.5f;
                    Vector3 posInt = new Vector3((int)pos.x, (int)pos.y, (int)pos.z);
                    Vector3 blockPos = posInt - hit.normal;

                    blockBreakAnimationObj.position = blockPos;
                }

                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction, Color.red);
            }
        }
    }
}

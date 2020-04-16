using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{   
    public static CameraManager Instance { get; private set; }


    [Header("Follow Player")]
    [SerializeField] private float FollowSpeed = 2f;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        
    }

    private void LateUpdate()
    {
        SmoothFollow();
    }

    void SmoothFollow()
    {
        Vector3 newPosition = PlayerController.Instance.transform.position;
        newPosition.z = -10;
        transform.position = Vector3.Slerp(transform.position, newPosition, FollowSpeed * Time.deltaTime);
    }
}

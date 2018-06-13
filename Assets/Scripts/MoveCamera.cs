using UnityEngine;
using System.Collections;


public class MoveCamera : MonoBehaviour
{
    public Transform target;
    public float smothing = 5f;
    public Brain brain;
    Vector3 offset;
    void Start()
    {
        if (brain.brainType == BrainType.External)
            this.enabled = false;
        offset = transform.position - target.position;
    }

    void Update()
    {
        Quaternion rotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, smothing * Time.deltaTime);

        Vector3 targetCampos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetCampos, smothing * Time.deltaTime);
    }
}
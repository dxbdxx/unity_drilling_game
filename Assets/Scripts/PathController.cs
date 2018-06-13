using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class PathController : MonoBehaviour
{
    // private NGUI ui;
    // ui.
    // private A = LLLL;
    [SerializeField]
    LineRenderer line;

    public Brain brain;
    void Start()
    {
        if (brain.brainType != BrainType.Show)
            this.enabled = false;
        else
        {
            List<Vector3> path = new List<Vector3>();
            path = XlsxReader.ReadExcelPath("path");
            Vector3[] path_array = path.ToArray();

            lastPos = transform.position;
            lastRotZ = 0;

            DrawLine(path_array);
            transform.DOPath(path_array, 10f);  // Start animation
        }
    }
    private void Update()
    {
        updateDirection();
    }

    Vector3 lastPos;
    Vector3 direction;
    float lastRotZ;
    private void updateDirection()
    {
        direction = transform.position - lastPos;
        if (direction != Vector3.zero)
        {
            this.transform.forward = direction.normalized;
            transform.RotateAround(transform.position, transform.forward, 500 * Time.deltaTime + lastRotZ);
        }
        else
            transform.RotateAround(transform.position, transform.forward, 500 * Time.deltaTime);

        lastPos = transform.position;
        lastRotZ = transform.rotation.eulerAngles.z;
    }
    
    private void DrawLine(Vector3[] path)
    {
        line.positionCount = path.Length;
        line.SetPositions(path);
    }
}
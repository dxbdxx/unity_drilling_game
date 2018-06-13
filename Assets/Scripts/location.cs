using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class location : MonoBehaviour {

    public Transform Soil;

    void Start () {
        transform.position = new Vector3(Soil.position.x, 0, Soil.position.z);
	}
	
	void Update () {
		
	}
}

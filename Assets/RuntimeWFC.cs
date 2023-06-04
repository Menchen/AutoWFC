using System.Collections;
using System.Collections.Generic;
using AutoWfc;
using UnityEngine;

public class RuntimeWFC : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var wfc = GetComponent<WfcHelper>().GenerateWfc(new BoundsInt(Vector3Int.zero, new Vector3Int(20,20,1)),new string[20*20]);
            GetComponent<WfcHelper>().ApplyWfc(wfc,new BoundsInt(Vector3Int.zero, new Vector3Int(20,20,1)));
            
        }
    }
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReportMenuManager : MonoBehaviour
{
    public GameObject menu;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Close()
    {
        Destroy(menu);
    }
}

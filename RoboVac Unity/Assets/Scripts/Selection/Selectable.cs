﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour
{

    public bool isSelected = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!Object.FindObjectOfType<Simulation>().IsStopped())
        {
            if(Selection.selected == this.gameObject)
            {
                Selection.selected = null;
            }
            isSelected = false;
        }

        if(Selection.selected != this.gameObject)
        {
            isSelected = false;
        }
    }

    private void OnMouseUpAsButton()
    {
        Select();
    }

    public void Select()
    {
        Selection.selected = this.gameObject;
    }
}

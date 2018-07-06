using UnityEngine;
using System.Collections;

public class GUISample : MonoBehaviour
{
    void OnGUI()
    {
        GUI.TextField(new Rect(10, 10, 250, 200),
            "No anykey to Idle\n" +
            "Press W to walk\n" +
            "Press W & left shift to run\n" +
            "Press space bar to full jump\n" +
            "Press space bar & shift to half jump\n" +
            "Press left click to dodge\n" +
            "Press right click to damage\n" +
            "Press middle click to death");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{

	[SerializeField] private float intensity;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public float getIntensity()
	{
		return intensity;
	}

	public void setActive()
	{
		GetComponent<Renderer>().material = Resources.Load<Material>("Materials/enemyActive");
	}

	public void setInactive()
	{
		GetComponent<Renderer>().material = Resources.Load<Material>("Materials/enemyInactive");
	}

	public override string ToString()
	{
		return "I am an enemy!";
	}
}

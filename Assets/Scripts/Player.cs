using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	public string name;
	public string id;
	public Color color = Color.white;
	public int mass = 5;
	public bool isMine;

	// Use this for initialization
	void Start () {
		transform.localScale = Vector3.one * mass;
		GetComponent<SpriteRenderer>().color = color;
	}

	// Update is called once per frame
	void Update () 
	{
		if(isMine)
		{
			transform.Translate(new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0) * Time.deltaTime * 3f);
		}
	}

	public void UpdateStates(Vector2 position, int mass)
	{
		
		transform.localScale = Vector3.one * mass;

		if(!isMine)
		transform.localPosition = position;
	}

}

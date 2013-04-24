using UnityEngine;
using System.Collections;

public class Gun : MonoBehaviour {

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0))
		{
			Vector3 position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
			Quaternion rotation = new Quaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
			Instantiate (Resources.Load ("Bullet"), position, rotation);
		}	
		
		GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
		foreach(GameObject bullet in bullets)
		{
			bullet.transform.Translate(new Vector3(0,0,.1f));
		}		
		this.transform.Translate(new Vector3(Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y"), 0));
	}
}

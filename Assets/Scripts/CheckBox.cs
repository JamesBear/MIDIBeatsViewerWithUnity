using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CheckBox : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnHitCheckBox(Toggle toggle)
    {
        GameObject.Find("Game").GetComponent<Test>().CheckBoxClicked(name, toggle.isOn);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoReloader : MonoBehaviour
{
    private bool loading = false;

	// Update is called once per frame
	void Update ()
    {
		if (Input.GetKey(KeyCode.R))
            ReloadDemo();
	}


    public void ReloadDemo()
    {
        if (loading)
            return;

        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        loading = true;
    }
}

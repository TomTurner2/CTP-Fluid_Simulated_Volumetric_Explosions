using UnityEngine;
using UnityEngine.SceneManagement;


public class DemoReloader : MonoBehaviour
{
    private bool loading = false;


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

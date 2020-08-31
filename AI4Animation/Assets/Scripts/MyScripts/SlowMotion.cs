using UnityEngine;

public class SlowMotion : MonoBehaviour
{
    public float slowdownFactor = 0.05f;
    public float slowdownLength = 2.0f;


    void Update()
    {
        Time.timeScale += (1.0f / slowdownLength) * Time.unscaledDeltaTime;
        Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
    }
    public void DoSlowMotion()
    {
        Time.timeScale = slowdownFactor;
        Time.fixedDeltaTime = Time.timeScale * 0.02f;

    }
    
}

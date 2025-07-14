using UnityEngine;

public class InputTest : MonoBehaviour
{
    void Update()
    {
        // Test basic input detection
        if (Input.GetKeyDown(KeyCode.W))
            Debug.Log("W key pressed!");
        if (Input.GetKeyDown(KeyCode.A))
            Debug.Log("A key pressed!");
        if (Input.GetKeyDown(KeyCode.S))
            Debug.Log("S key pressed!");
        if (Input.GetKeyDown(KeyCode.D))
            Debug.Log("D key pressed!");
            
        // Test axis input
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        
        if (h != 0 || v != 0)
        {
            Debug.Log($"Horizontal: {h}, Vertical: {v}");
        }
    }
}

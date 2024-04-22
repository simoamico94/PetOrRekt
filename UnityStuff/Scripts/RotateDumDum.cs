using UnityEngine;

public class RotateDumDum : MonoBehaviour
{
	public float rotationSpeed = 100f; // Speed of rotation

	public bool canRotate;

	private float previousMouseXPosition;

	void Update()
	{
		if(canRotate)
		{
			// Check for mouse input
			if (Input.GetMouseButtonDown(0))
			{
				previousMouseXPosition = Input.mousePosition.x;
			}
			else if (Input.GetMouseButton(0))
			{
				float mouseXDelta = Input.mousePosition.x - previousMouseXPosition;
				float rotationAmount = mouseXDelta * rotationSpeed * Time.deltaTime;
				transform.Rotate(0, rotationAmount, 0, Space.World);
				previousMouseXPosition = Input.mousePosition.x;
			}

			// Check for touch input
			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Moved)
				{
					float touchXDelta = touch.deltaPosition.x;
					float rotationAmount = touchXDelta * rotationSpeed * Time.deltaTime;
					transform.Rotate(0, rotationAmount, 0, Space.World);
				}
			}
		}
	}
}

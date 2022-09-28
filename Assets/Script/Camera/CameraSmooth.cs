using UnityEngine;
[RequireComponent(typeof(Camera))]
public class CameraSmooth : MonoBehaviour {

	public Transform target;
	public float smoothSpeed = 0.125f;
	public Vector3 PositionOffset;
	public int FPS = 60;
	private void Start()
	{
		QualitySettings.vSyncCount = 0;

		// destroys frame timing on quest 2
		// Application.targetFrameRate = FPS;
	}
	void Update ()
	{
		if (target == null) return;
		transform.position = Vector3.Lerp(transform.position, target.position + PositionOffset, smoothSpeed);
		transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, smoothSpeed);
	}
}

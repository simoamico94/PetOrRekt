using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DumDumManager : MonoBehaviour
{
	[SerializeField] private Animator dumdum;
	[SerializeField] private Animator dumdumOutline;

	[SerializeField] private ParticleSystem particles;

	[SerializeField] private AudioSource audioSource;

	[SerializeField] private AudioClip happySong;
	[SerializeField] private AudioClip angrySong;

	[SerializeField] private GameObject gun;
	[SerializeField] private MeshRenderer hat;
	[SerializeField] private MeshRenderer glasses;
    [SerializeField] private SkinnedMeshRenderer coat;
    [SerializeField] private MeshRenderer balloon;
    [SerializeField] private List<Material> hatMats;
	[SerializeField] private List<Material> glassesMats;
    [SerializeField] private List<Material> coatMats;
	[SerializeField] private List<Material> balloonMats;

	public bool HasPet { get => hasPet; set { if (hasPet != value || firstSetup) { hasPet = value; OnPetValueChanged(); } } }
	private bool hasPet = false;

	public float particlesTime = 8f;

	public float rotationSpeed = 50f;
	public bool canRotate = true;

	private float previousMouseXPosition;
	private Coroutine particlesCoroutine;

	private bool firstSetup = true;

	private void Awake()
	{

	}

	void Update()
	{
		if (canRotate)
		{
			if (Input.GetMouseButtonDown(0))
			{
				previousMouseXPosition = Input.mousePosition.x;
			}
			else if (Input.GetMouseButton(0))
			{
				float mouseXDelta = Input.mousePosition.x - previousMouseXPosition;
				float rotationAmount = - mouseXDelta * rotationSpeed * Time.deltaTime;
				transform.Rotate(0, rotationAmount, 0, Space.World);
				previousMouseXPosition = Input.mousePosition.x;
			}

			if (Input.touchCount > 0)
			{
				Touch touch = Input.GetTouch(0);
				if (touch.phase == TouchPhase.Moved)
				{
					float touchXDelta = touch.deltaPosition.x;
					float rotationAmount = - touchXDelta * rotationSpeed * Time.deltaTime;
					transform.Rotate(0, rotationAmount, 0, Space.World);
				}
			}
		}
	}

	public void OnPetValueChanged()
    {
		firstSetup = false;

		if(hasPet)
		{
			dumdum.SetTrigger("Happy");
			dumdumOutline.SetTrigger("Happy");
			if(audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			audioSource.PlayOneShot(happySong);
			particlesCoroutine = StartCoroutine(Particles());
		}
		else
		{
			dumdum.SetTrigger("Angry");
			dumdumOutline.SetTrigger("Angry");
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			audioSource.PlayOneShot(angrySong);

			particles.Stop();
			if(particlesCoroutine != null)
			{
				StopCoroutine(particlesCoroutine);
			}

			AOConnectManager.main.SendNotification("Pet Available", "Dumdum you have to Pet, to not loose your streak and get rekt");
		}

		gun.SetActive(!hasPet);
    }

    public void UpdateDumDum(List<int> choices)
    {
		dumdum.gameObject.SetActive(true);
		dumdumOutline.gameObject.SetActive(true);

        transform.position = Vector3.zero;

        if (choices[0] == 1)
        {
            hat.gameObject.SetActive(false);
        }
        else
        {
            hat.gameObject.SetActive(true);
            hat.material = hatMats[choices[0]-2];
        }

		if (choices[1] == 1)
		{
			glasses.gameObject.SetActive(false);
		}
		else
		{
			glasses.gameObject.SetActive(true);
			glasses.material = glassesMats[choices[1] - 2];
		}

		var coatMatsTmp = new List<Material>(coat.materials);
		coatMatsTmp[2] = coatMats[choices[2] - 1];
		coat.materials = coatMatsTmp.ToArray();

		if (choices[3] == 1)
		{
			balloon.gameObject.SetActive(false);
		}
		else
		{
			balloon.gameObject.SetActive(true);
			var balloonMatsTmp = new List<Material>(coat.materials);
			balloonMatsTmp[0] = balloonMats[choices[3] - 2];
			balloon.materials = balloonMatsTmp.ToArray();
		}
	}

	private IEnumerator Particles()
	{
		particles.Play();

		yield return new WaitForSeconds(8);

		particles.Stop();
	}
}

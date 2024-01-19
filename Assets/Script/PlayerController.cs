using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform thisTransform;
    [SerializeField] private Transform viewPoint;
    [SerializeField] private float mouseSensetivity = 1f;
    private float verticalRotationStore;
    private Vector2 mouseInput;

    [SerializeField] private float moveSpeed = 5f, runSpeed = 8f;
    [SerializeField] private float jumpForce = 12f, gravityMod = 2.5f;
    private float activeMoveSpeed;
    private Vector3 movementDirection;

    private Camera mainCamera;

    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    [SerializeField] private GameObject bulletImpact;
    [SerializeField] private float shotCounter = 0f;

    [SerializeField] private float maxHeat = 10f, coolRate = 4f, overHeatCoolRate = 5f;
    private float heatCounter;
    private bool overHeated;

    [SerializeField] private Gun[] allGuns;
    private int selectedGun = 0;
    private float muzzleDisplayTime = 0.016f;
    private float muzzleCounter;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;
    public GameObject playerModel;

    public Transform modelGunPoint;
    public Transform gunHolder;

    public Material[] allSkins;

    public AudioSource footStepSlow, footStepFast;

    private PlayerSpawner playerSpawner;
    private MatchManager matchManager;

    private void Start()
    {
        currentHealth = maxHealth;
        mainCamera = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        UIController.Instance.SetMaxWeaponTempSliderValue(maxHeat);

        //SwitchGun();
        photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);

        if(photonView.IsMine)
        {
            playerModel.SetActive(false);

            UIController.Instance.SetMaxHealthSliderValue(maxHealth);
            UIController.Instance.UpdateHealthSliderValue(currentHealth);
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        playerModel.GetComponent<Renderer>().material = allSkins[PhotonNetwork.LocalPlayer.ActorNumber % allSkins.Length];
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            Rotate();
            Move();
            HandleWeaponFire();
            HandleGunSwitch();
            HandleAnimationState();



            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0)
                    && matchManager.gameState != GameState.Pause)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (matchManager.gameState == GameState.Playing)
            {
                mainCamera.transform.SetPositionAndRotation(viewPoint.position, viewPoint.rotation);
            }
            else
            {
                Transform mapCameraPoint = matchManager.GetMapCameraPoint();
                mainCamera.transform.SetPositionAndRotation(mapCameraPoint.position, mapCameraPoint.rotation);
            }
        }
    }

    private void Rotate()
    {
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        Vector3 thisRotation = thisTransform.eulerAngles;
        thisRotation.y += mouseInput.x * mouseSensetivity;
        thisTransform.rotation = Quaternion.Euler(thisRotation);

        verticalRotationStore += mouseInput.y * mouseSensetivity;
        verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);

        viewPoint.rotation = Quaternion.Euler(-verticalRotationStore, viewPoint.eulerAngles.y, viewPoint.eulerAngles.z);
    }

    private void Move()
    {
        float yVel = movementDirection.y;

        Vector3 forward = thisTransform.forward * Input.GetAxisRaw("Vertical");
        Vector3 sideway = thisTransform.right * Input.GetAxisRaw("Horizontal");
        movementDirection = (forward + sideway).normalized;
        movementDirection.y = yVel;

        isGrounded = IsGrounded();

        if (isGrounded) { movementDirection.y = 0f; }


        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed = runSpeed;
            if (!footStepFast.isPlaying && movementDirection != Vector3.zero)
            {
                footStepFast.Play();
                footStepSlow.Stop();
            }
        }
        else
        {
            activeMoveSpeed = moveSpeed;
            if (!footStepSlow.isPlaying && movementDirection != Vector3.zero)
            {
                footStepFast.Stop();
                footStepSlow.Play();
            }
        }

        if (movementDirection == Vector3.zero || !isGrounded)
        {
            footStepFast.Stop();
            footStepSlow.Stop();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            movementDirection.y = jumpForce;
        }

        movementDirection.y += Physics.gravity.y * Time.deltaTime * gravityMod;

        characterController.Move(activeMoveSpeed * Time.deltaTime * movementDirection);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(groundCheckPoint.position, Vector3.down, 0.25f, groundLayer);
    }

    private void HandleGunSwitch()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            selectedGun++;
            if (selectedGun >= allGuns.Length)
            {
                selectedGun = 0;
            }
            photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            selectedGun--;
            if (selectedGun < 0)
            {
                selectedGun = allGuns.Length - 1;
            }
            photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
        }


        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun = i;
                photonView.RPC(nameof(SetGun), RpcTarget.All, selectedGun);
            }
        }
    }

    private void HandleWeaponFire()
    {
        if (allGuns[selectedGun].IsMuzzleFlashActive())
        {
            muzzleCounter -= Time.deltaTime;

            if (muzzleCounter <= 0)
            {
                allGuns[selectedGun].ToggleMuzzleFlash(false);
            }
        }


        if (!overHeated)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }

            if (Input.GetMouseButton(0) && allGuns[selectedGun].IsAutomatic())
            {
                shotCounter -= Time.deltaTime;

                if (shotCounter <= 0)
                {
                    Shoot();
                }
            }

            heatCounter -= coolRate * Time.deltaTime;
        }
        else
        {
            heatCounter -= overHeatCoolRate * Time.deltaTime;
            if (heatCounter <= 0)
            {
                overHeated = false;
                UIController.Instance.ToggleOverheatedText(false);
            }
        }

        if (heatCounter < 0)
        {
            heatCounter = 0;
        }

        UIController.Instance.UpdateWeaponHeatSliderValue(heatCounter);
    }

    private void HandleAnimationState()
    {
        anim.SetBool("grounded", isGrounded);

        float speed = 0f;

        if (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0 || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0)
        {
            speed = 1f;
        }

        anim.SetFloat("speed", speed);
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if(photonView.IsMine)
        {
            currentHealth -= damageAmount;

            if(currentHealth <= 0)
            {
                playerSpawner.Die(damager);
                currentHealth = 0;

                matchManager.UpdateStatsSendEvent(actor, 0, 1);
            }

            UIController.Instance.UpdateHealthSliderValue(currentHealth);
        }
    }

    private void Shoot()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        ray.origin = mainCamera.transform.position;

        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            PhotonView view = hit.collider.gameObject.GetPhotonView();
            if (view != null)
            {
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);
                view.RPC(nameof(DealDamage), RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].GetShotDamage(), PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject impact = Instantiate(bulletImpact, hit.point + (hit.normal * 0.02f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(impact, 7f);
            }
        }

        shotCounter = allGuns[selectedGun].GetTimeBetweenShot();

        heatCounter += allGuns[selectedGun].GetHeatPerShot();
        if(heatCounter >= maxHeat)
        {
            overHeated = true;
            heatCounter = maxHeat;
            UIController.Instance.ToggleOverheatedText(true);
        }

        muzzleCounter = muzzleDisplayTime;
        allGuns[selectedGun].ToggleMuzzleFlash(true);

        allGuns[selectedGun].StopSound();
        allGuns[selectedGun].PlaySound();
    }

    private void SwitchGun()
    {
        foreach(Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].ToggleMuzzleFlash(false);
    }

    [PunRPC]
    public void SetGun(int gunIndex)
    {
        if(gunIndex < allGuns.Length)
        {
            selectedGun = gunIndex;
            SwitchGun();
        }
    }

    public void SetReference(PlayerSpawner playerSpawner, MatchManager manager)
    {
        this.playerSpawner = null;
        this.playerSpawner = playerSpawner;
        matchManager = manager;
    }
}

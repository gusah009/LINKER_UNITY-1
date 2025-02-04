using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;
using Newtonsoft.Json.Linq;
using eggcation;

/// <summary>
/// Player manager.
/// Handles fire Input and Beams.
/// </summary>
public class PlayerManagerApp : MonoBehaviourPunCallbacks, IPunObservable
{
    #region IPunObservable implementation
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(IsEmotionsActive);
            stream.SendNext(emotionIsChange);
        }
        else
        {
            IsEmotionsActive = (bool[])stream.ReceiveNext();
            emotionIsChange = (bool)stream.ReceiveNext();
        }
    }

    #endregion


    #region Public Fields

    //[Tooltip("The current Health of our player")]
    //public float Health = 1f;
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance = null;

    public int CamMode;

    static public Camera MainCamera;

    static public Recorder VoiceRecorder;

    static public AudioSource VoiceAudioSource;

    static public AudioListener AudioListener;

    public bool emotionIsChange = false;

    public bool[] IsEmotionsActive;

    #endregion


    #region Private Serialize Field

    //[Tooltip("The Beams GameObject to control")]
    //[SerializeField]
    //private GameObject beams;

    ////True, when the user is firing

    [SerializeField]
    const int MaximumEmotionCount = 10;

    [SerializeField]
    private GameObject[] Emotions;

    [SerializeField]
    private KeyCode jumpKeyCode = KeyCode.Space;
    [SerializeField]
    private KeyCode CAMERA_KEY_CODE = KeyCode.Tab;
    [SerializeField]
    private KeyCode EMOTION1_KEYCODE = KeyCode.Alpha1;
    [SerializeField]
    private KeyCode EMOTION2_KEYCODE = KeyCode.Alpha2;
    [SerializeField]
    private KeyCode EMOTION3_KEYCODE = KeyCode.Alpha3;
    [SerializeField]
    private KeyCode NOTICE_KEYCODE = KeyCode.I;
    [SerializeField]
    private KeyCode ESC_KEYCODE = KeyCode.Escape;

    [SerializeField]
    private GameObject fpCamera;

    [SerializeField]
    private GameObject tpCamera;

    [SerializeField]
    private CameraController fpCameraController;
    
    [SerializeField]
    private CameraController tpCameraController;

    [SerializeField]
    private TextMeshProUGUI nameText;

    [SerializeField]
    private GameObject Sphere;

    [SerializeField]
    private GameObject Cloth;


    private float gravity = -9.81f;
    [SerializeField]
    private float moveSpeed = 5.0f;

    #endregion


    #region Private Field



    #endregion

    #region MonoBehaviour CallBacks


    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {
        // #Important
        // used in GameManagerApp.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
        if (photonView.IsMine)
        {
            PlayerManagerApp.LocalPlayerInstance = this.gameObject;
            JoyStick.characterController = LocalPlayerInstance.GetComponent<CharacterController>();
            JoyStick.fpCameraTransform = fpCamera.transform;
            JoyStick.tpCameraTransform = tpCamera.transform;
            JoyStickCamera.Player = LocalPlayerInstance;
            GameManagerApp.fpCamera = fpCamera;
            GameManagerApp.fpCameraController = fpCamera.GetComponent<CameraController>();
            CamMode = 1;
            fpCamera.SetActive(true);
            MainCamera = fpCamera.GetComponent<Camera>();
            tpCamera.SetActive(false);

            VoiceRecorder = LocalPlayerInstance.GetComponent<Recorder>();
            VoiceAudioSource = LocalPlayerInstance.GetComponent<AudioSource>();
            AudioListener = fpCamera.GetComponent<AudioListener>();
            VoiceRecorder.TransmitEnabled = true;
            VoiceAudioSource.mute = false;


            // 감정표현 개수가 MaximumEmotionCount 이상이면 MaximumEmotionCount를 수정해줘야합니다.
            IsEmotionsActive = new bool[MaximumEmotionCount] {false, false, false, false, false, false, false, false, false, false };

        }
        // #Critical
        // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
        DontDestroyOnLoad(this.gameObject);

        //if (beams == null)
        //{
        //    Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
        //}
        //else
        //{
        //    beams.SetActive(false);
        //}
    }
    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during initialization phase.
    /// </summary>
    void Start()
    {
        SetName();
        SetColorAndCloth();

        //if (playerUiPrefab != null)
        //{
        //    GameObject _uiGo = Instantiate(playerUiPrefab);
        //    _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
        //}
        //else
        //{
        //    Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
        //}

        //Photon.Pun.Demo.PunBasics.CameraWork _cameraWork = this.gameObject.GetComponent<Photon.Pun.Demo.PunBasics.CameraWork>();
        //if (_cameraWork != null)
        //{
        //    if (photonView.IsMine)
        //    {
        //        _cameraWork.OnStartFollowing();
        //    }
        //}
        //else
        //{
        //    Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
        //}
    }
    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity on every frame.
    /// </summary>
    void Update()
    {
        if (photonView.IsMine)
        {
            ProcessInputs();
            if (JoyStick.characterController.isGrounded == false)
            {
                JoyStick.moveDirection.y += gravity * Time.deltaTime;
            }
            JoyStick.characterController.Move(JoyStick.moveDirection * moveSpeed * Time.deltaTime);
        }
        //if (Health <= 0f)
        //{
        //    GameManagerApp.Instance.LeaveRoom();
        //}
        for (int i = 0; i < Emotions.Length; i++)
        {
            if (IsEmotionsActive != null && IsEmotionsActive[i] != Emotions[i].activeInHierarchy)
            {
                Emotions[i].SetActive(IsEmotionsActive[i]);
            }
        }
    }

    /// <summary>
    /// MonoBehaviour method called when the Collider 'other' enters the trigger.
    /// Affect Health of the Player if the collider is a beam
    /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
    /// One could move the collider further away to prevent this or check if the beam belongs to the player.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        //if (!photonView.IsMine)
        //{
        //    return;
        //}
        //// We are only interested in Beamers
        //// we should be using tags but for the sake of distribution, let's simply check by name.
        //if (!other.name.Contains("Beam"))
        //{
        //    return;
        //}
    }
    /// <summary>
    /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
    /// We're going to affect health while the beams are touching the player
    /// </summary>
    /// <param name="other">Other.</param>
    void OnTriggerStay(Collider other)
    {
        // we dont' do anything if we are not the local player.
        //if (!photonView.IsMine)
        //{
        //    return;
        //}
        //if (!other.name.Contains("Beam"))
        //{
        //    return;
        //}
    }

    void SetName()
    {
        nameText.text = photonView.Owner.NickName;
    }

    private void SetColorAndCloth()
    {
        var json = new JObject();
        string method = "member";
        json.Add("displayName", photonView.Owner.NickName);
        var user_info = JObject.Parse(Utility.request_server(json, method));
        Color myColor;
        ColorUtility.TryParseHtmlString("#" + user_info["user_skin_color"].ToString(), out myColor);
        Sphere.GetComponent<Renderer>().material.color = myColor;
        Material myMat;
        string cloth = user_info["user_skin_cloth"].ToString();
        myMat = Resources.Load(cloth, typeof(Material)) as Material;
        Cloth.GetComponent<Renderer>().material = myMat;
    }


    void ProcessInputs()
    {
        if(!GameManagerApp.isMouseMode)
        {
            if (Input.GetKeyDown(CAMERA_KEY_CODE))
            {
                StartCoroutine(CamChange());
                if (CamMode == 1)
                {
                    CamMode = 0;
                }
                else
                {
                    CamMode += 1;
                }
            }


        } 
       
        // ESC기능 부분입니다.
        if (Input.GetKeyDown(ESC_KEYCODE))
        {
            if (GameManagerApp.boardPanelObject.activeInHierarchy)
            {
                return;
            }
            else if (GameManagerApp.DeskModeObject.activeInHierarchy)
            {
                return;
            }
            else if (GameManagerApp.timerObject.activeInHierarchy)
            {
                return;
            }
            else if (GameManagerApp.ServerCanvasObject.activeInHierarchy ||
                    GameManagerApp.ClientCanvasObject.activeInHierarchy)
            {
                return;
            }

            bool isEscActive = GameManagerApp.escPanelObject.activeInHierarchy;
            GameManagerApp.DisplayCanvas(isEscActive, "esc");
            GameManagerApp.isMouseMode = !isEscActive;

            GameManagerApp.escPanelObject.SetActive(!isEscActive);
        }
    }
    IEnumerator CamChange()
    {
        yield return new WaitForSeconds(0.01f);
        if (CamMode == 1)
        {
            fpCamera.SetActive(false);
            tpCamera.SetActive(true);
            MainCamera = tpCamera.GetComponent<Camera>();
        }
        else
        {
            fpCamera.SetActive(true);
            tpCamera.SetActive(false);
            MainCamera = fpCamera.GetComponent<Camera>();
        }
    }
    #endregion

    public IEnumerator CoroutineEmotion(int i)
    {
        emotionIsChange = true;
        IsEmotionsActive[i] = true;

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(1f);

        emotionIsChange = false;
        IsEmotionsActive[i] = false;
    }

    public bool IsAllEmotionInactive()
    {
        foreach (var EmotionActive in IsEmotionsActive)
        {
            if (EmotionActive) return false;
        }
        return true;
    }
}
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Agora.Rtc;

using UnityEngine.Serialization;
using DG.Tweening;
using TMPro;


namespace Agora_RTC_Plugin.API_Example.Examples.Advanced.ScreenShare
{
    public class ScreenShare : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        private AppIdInput _appIdInput;
        //     [SerializeField]
        //  private Transform screenParent;
        //  public static Transform _screenParent;

        [Header("_____________Basic Configuration_____________")]
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        private string _appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        private string _token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        private string _channelName = "";

        public Text LogText;
        internal Logger Log;
        internal IRtcEngine RtcEngine = null;
        private ScreenCaptureSourceInfo[] _screenCaptureSourceInfos;

        public Dropdown WinIdSelect;
        public Button GetSourceBtn;
        public Button StartShareBtn;
        public Button StopShareBtn;
        public Button UpdateShareBtn;
        public Button PublishBtn;
        public Button UnpublishBtn;
        public Button ShowThumbBtn;
        public Button ShowIconBtn;
        public RawImage IconImage;
        public RawImage ThumbImage;

        private Rect _originThumRect = new Rect(0, 0, 500, 260);
        private Rect _originIconRect = new Rect(0, 0, 289, 280);



        [Header("_____________UI Configuration_____________")]
        public RectTransform screenSharingPanel;
        public TextMeshProUGUI screenSharingText;
        public GameObject joinLeaveParent;
        public GameObject joinButton;
        public GameObject leaveButton;
        public GameObject screensourcesPanel;
        public GameObject nonInteractablePublishButton;
        public GameObject publishButton;
        public GameObject stopPublishButton;
        public GameObject nonInteractableShareButton;
        public GameObject startSharingButton;
        public GameObject stopSharingButton;

        private bool isScreenSharingOpen = false;
        private bool canOpenScreenShare = true;
        private bool isSharing = false;
        public static bool isJoinedChannel = false;

        public static bool activateSharing = false;
        private void Awake()
        {
            // if (screenParent != null)
            //   _screenParent = screenParent;
            
        }

       
        // Use this for initialization
        private void Start()
        {
            LoadAssetData();
            if (CheckAppId())
            {
                InitEngine();
                SetBasicConfiguration();
#if UNITY_ANDROID || UNITY_IPHONE
                GetSourceBtn.gameObject.SetActive(false);
                WinIdSelect.gameObject.SetActive(false);
                UpdateShareBtn.gameObject.SetActive(true);
                IconImage.gameObject.SetActive(false);
                ThumbImage.gameObject.SetActive(false);
                ShowThumbBtn.gameObject.SetActive(false);
                ShowIconBtn.gameObject.SetActive(false);
#else
                UpdateShareBtn.gameObject.SetActive(false);
#endif
            }
        }

        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
        }

        //Show data in AgoraBasicProfile
        [ContextMenu("ShowAgoraBasicProfileData")]
        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        private void InitEngine()
        {
            RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
            UserEventHandler handler = new UserEventHandler(this);
            RtcEngineContext context = new RtcEngineContext(_appID, 0,
                                        CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                                        AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT);
            RtcEngine.Initialize(context);
            RtcEngine.InitEventHandler(handler);
        }

        private void SetBasicConfiguration()
        {
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            RtcEngine.EnableLocalVideo(false);
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        }

        private void Update()
        {
            if(activateSharing)
            {
                nonInteractableShareButton.SetActive(false);
                startSharingButton.SetActive(true);
                joinButton.SetActive(false);
                leaveButton.SetActive(true);
                activateSharing = false;
            }
        }

        #region -- Button Events ---

        public void JoinChannel()
        {
            var ret = RtcEngine.JoinChannel(_token, _channelName);
            Debug.Log("JoinChannel returns: " + ret);
        }

        public void LeaveChannel()
        {
            RtcEngine.LeaveChannel();
        }

        public void OnPublishButtonClick()
        {
            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false);
            options.publishScreenTrack.SetValue(true);

#if UNITY_ANDROID || UNITY_IPHONE
            options.publishScreenCaptureAudio.SetValue(true);
            options.publishScreenCaptureVideo.SetValue(true);
#endif
            var ret = RtcEngine.UpdateChannelMediaOptions(options);
            Debug.Log("UpdateChannelMediaOptions returns: " + ret);

            PublishBtn.gameObject.SetActive(false);
            UnpublishBtn.gameObject.SetActive(true);
        }

        public void OnUnplishButtonClick()
        {
            ChannelMediaOptions options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(true);
            options.publishScreenTrack.SetValue(false);

#if UNITY_ANDROID || UNITY_IPHONE
            options.publishScreenCaptureAudio.SetValue(false);
            options.publishScreenCaptureVideo.SetValue(false);
#endif
            var ret = RtcEngine.UpdateChannelMediaOptions(options);
            Debug.Log("UpdateChannelMediaOptions returns: " + ret);

            PublishBtn.gameObject.SetActive(true);
            UnpublishBtn.gameObject.SetActive(false);
        }

        public void PrepareScreenCapture()
        {
            if (WinIdSelect == null || RtcEngine == null) return;

            WinIdSelect.ClearOptions();

            SIZE t = new SIZE();
            t.width = 1280;
            t.height = 720;
            SIZE s = new SIZE();
            s.width = 640;
            s.height = 640;
            _screenCaptureSourceInfos = RtcEngine.GetScreenCaptureSources(t, s, true);

            WinIdSelect.AddOptions(_screenCaptureSourceInfos.Select(w =>
                    new Dropdown.OptionData(
                        string.Format("{0}: {1}-{2} | {3}", w.type, w.sourceName, w.sourceTitle, w.sourceId)))
                .ToList());
        }

        public void OnStartShareBtnClick()
        {
            if (RtcEngine == null)
            {
                Debug.LogError("RTC ENGINE IS NULL");
                return;

            }
                if (StartShareBtn != null) StartShareBtn.gameObject.SetActive(false);
            if (StopShareBtn != null) StopShareBtn.gameObject.SetActive(true);

#if UNITY_ANDROID || UNITY_IPHONE
            var parameters2 = new ScreenCaptureParameters2();
            parameters2.captureAudio = true;
            parameters2.captureVideo = true;
            var nRet = RtcEngine.StartScreenCapture(parameters2);
            this.Log.UpdateLog("StartScreenCapture :" + nRet);
#else
            RtcEngine.StopScreenCapture();
            if (WinIdSelect == null) return;
            var option = WinIdSelect.options[WinIdSelect.value].text;
            if (string.IsNullOrEmpty(option)) return;

            if (option.Contains("ScreenCaptureSourceType_Window"))
            {
                var windowId = option.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                Log.UpdateLog(string.Format(">>>>> Start sharing {0}", windowId));
                var nRet = RtcEngine.StartScreenCaptureByWindowId(long.Parse(windowId), default(Rectangle),
                        default(ScreenCaptureParameters));
                this.Log.UpdateLog("StartScreenCaptureByWindowId:" + nRet);
            }
            else
            {
                var dispId = uint.Parse(option.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]);
                Log.UpdateLog(string.Format(">>>>> Start sharing display {0}", dispId));
                var nRet = RtcEngine.StartScreenCaptureByDisplayId(dispId, default(Rectangle),
                    new ScreenCaptureParameters { captureMouseCursor = true, frameRate = 30 });
                this.Log.UpdateLog("StartScreenCaptureByDisplayId:" + nRet);
            }

#endif

            PublishBtn.gameObject.SetActive(true);
            UnpublishBtn.gameObject.SetActive(true);
            // OnPublishButtonClick();
            ScreenShare.MakeVideoView(0, "", VIDEO_SOURCE_TYPE.VIDEO_SOURCE_SCREEN);

        }

        public void OnStopShareBtnClick()
        {
            if (StartShareBtn != null) StartShareBtn.gameObject.SetActive(true);
            if (StopShareBtn != null) StopShareBtn.gameObject.SetActive(false);


            PublishBtn.gameObject.SetActive(false);
            UnpublishBtn.gameObject.SetActive(false);

            DestroyVideoView(0);
            RtcEngine.StopScreenCapture();
        }

        public void OnUpdateShareBtnClick()
        {
            //only work in ios or android
            var config = new ScreenCaptureParameters2();
            config.captureAudio = true;
            config.captureVideo = true;
            config.videoParams.dimensions.width = 960;
            config.videoParams.dimensions.height = 640;
            var nRet = RtcEngine.UpdateScreenCapture(config);
            this.Log.UpdateLog("UpdateScreenCapture: " + nRet);
        }

        public void OnShowThumbButtonClick()
        {
            if (ThumbImage.texture)
            {
                GameObject.Destroy(ThumbImage.texture);
                ThumbImage.texture = null;
            }
            ThumbImageBuffer thumbImageBuffer = _screenCaptureSourceInfos[WinIdSelect.value].thumbImage;
            if (thumbImageBuffer.buffer.Length == 0) return;
            Texture2D texture = null;
#if UNITY_STANDALONE_OSX
            texture = new Texture2D((int)thumbImageBuffer.width, (int)thumbImageBuffer.height, TextureFormat.RGBA32, false);
#elif UNITY_STANDALONE_WIN
            texture = new Texture2D((int)thumbImageBuffer.width, (int)thumbImageBuffer.height, TextureFormat.BGRA32, false);
#endif
            texture.LoadRawTextureData(thumbImageBuffer.buffer);
            texture.Apply();
            ThumbImage.texture = texture;

            float scale = Math.Min((float)_originThumRect.width / (float)thumbImageBuffer.width, (float)_originThumRect.height / (float)thumbImageBuffer.height);
            ThumbImage.rectTransform.sizeDelta = new Vector2(thumbImageBuffer.width * scale, thumbImageBuffer.height * scale);
        }

        public void OnShowIconButtonClick()
        {
            if (IconImage.texture)
            {
                GameObject.Destroy(IconImage.texture);
                IconImage.texture = null;
            }
            ThumbImageBuffer iconImageBuffer = _screenCaptureSourceInfos[WinIdSelect.value].iconImage;
            if (iconImageBuffer.buffer.Length == 0) return;
            Texture2D texture = null;
#if UNITY_STANDALONE_OSX
            texture = new Texture2D((int)iconImageBuffer.width, (int)iconImageBuffer.height, TextureFormat.RGBA32, false);
#elif UNITY_STANDALONE_WIN
            texture = new Texture2D((int)iconImageBuffer.width, (int)iconImageBuffer.height, TextureFormat.BGRA32, false);
#endif
            texture.LoadRawTextureData(iconImageBuffer.buffer);
            texture.Apply();
            IconImage.texture = texture;


            float scale = Math.Min((float)_originIconRect.width / (float)iconImageBuffer.width, (float)_originIconRect.height / (float)iconImageBuffer.height);
            IconImage.rectTransform.sizeDelta = new Vector2(iconImageBuffer.width * scale, iconImageBuffer.height * scale);

        }

        #endregion

        #region -- Custom Button Events -- 


        public void OnClick_JoinChannel()
        {
            JoinChannel();
            joinButton.SetActive(false);
            screensourcesPanel.transform.DOScale(1f, 0.5f).OnComplete
                (
                  () => PrepareScreenCapture()
                );
        }


        public void OnClick_LeaveChannel()
        {
            RtcEngine.LeaveChannel();
            joinButton.SetActive(true);
            leaveButton.SetActive(false);
            screensourcesPanel.transform.DOScale(0f, 0.2f);
            isJoinedChannel = false;
            nonInteractableShareButton.SetActive(true);
            startSharingButton.SetActive(false);
            stopSharingButton.SetActive(false);
            nonInteractablePublishButton.SetActive(true);
            publishButton.SetActive(false);
            stopPublishButton.SetActive(false);

        }

        public void OnClick_OpenScreenSharing()
        {
            if (canOpenScreenShare)
            {

                if (isScreenSharingOpen)
                {
                    canOpenScreenShare = false;
                    screenSharingText.transform.localScale = Vector3.zero;
                    screenSharingText.DOFade(0f, 0f);
                    float initialWidth = screenSharingPanel.sizeDelta.x;

                    // if(isJoinedChannel)
                    // {
                    screensourcesPanel.transform.DOScale(0f, 0.2f);
                    // }


                    DOTween.To(() => initialWidth, newWidth =>
                    {
                        Vector2 newSizeDelta = screenSharingPanel.sizeDelta;
                        newSizeDelta.x = newWidth;
                        screenSharingPanel.sizeDelta = newSizeDelta;
                    }, 50f, 0.2f).OnComplete
                    (
                       () =>
                       {
                           isScreenSharingOpen = false;
                           joinLeaveParent.transform.DOScale(0f, 0.2f);
                           canOpenScreenShare = true;
                       }
                    );
                }
                else
                {
                    screenSharingText.transform.localScale = Vector3.one;
                    canOpenScreenShare = false;
                    if (isJoinedChannel)
                    {
                        screensourcesPanel.transform.DOScale(1f, 0.2f);
                    }

                    // isScreenSharingOpen = true;
                    float initialWidth = screenSharingPanel.sizeDelta.x;
                    DOTween.To(() => initialWidth, newWidth =>
                    {
                        Vector2 newSizeDelta = screenSharingPanel.sizeDelta;
                        newSizeDelta.x = newWidth;
                        screenSharingPanel.sizeDelta = newSizeDelta;
                    }, 162f, 0.2f)
                    .OnComplete
                    (
                         () =>
                         {
                             screenSharingText.DOFade(1f, 0.4f);
                             joinLeaveParent.transform.DOScale(1f, 0.2f);
                             isScreenSharingOpen = true;
                             canOpenScreenShare = true;
                         }
                    );

                }
            }
        }


        public void OnClick_ShareButton()
        {
            OnStartShareBtnClick();
           

            isSharing = true;
            startSharingButton.gameObject.SetActive(false);
            stopSharingButton.gameObject.SetActive(true);
            nonInteractablePublishButton.SetActive(false);
            publishButton.SetActive(true);
        }

        public void OnClick_StopShareButton()
        {
            isSharing = false;
            startSharingButton.gameObject.SetActive(true);
            stopSharingButton.gameObject.SetActive(false);
            publishButton.SetActive(false);
            nonInteractablePublishButton.SetActive(true);
            OnStopShareBtnClick();
        }

        public void OnClick_PublishButton()
        {
            OnPublishButtonClick();
            publishButton.SetActive(false);
            stopPublishButton.SetActive(true);
        }

        public void OnClick_StopPublishButton()
        {
            OnUnplishButtonClick();
            if (isSharing)
            {
                stopPublishButton.SetActive(false);
                publishButton.SetActive(true);
                nonInteractablePublishButton.SetActive(false);
            }
            else
            {
                stopPublishButton.SetActive(false);
                publishButton.SetActive(false);
                nonInteractablePublishButton.SetActive(true);
            }

        }



        #endregion

        #region -- Custom UI Callbacks --

        public void JoinClickedJoinCallback()
        {
            joinButton.SetActive(false);
            leaveButton.SetActive(true);
            isJoinedChannel = true;
        }

        #endregion

        private void OnDestroy()
        {
            Debug.Log("OnDestroy");
            if (RtcEngine == null) return;
            RtcEngine.InitEventHandler(null);
            RtcEngine.LeaveChannel();
            RtcEngine.Dispose();
        }

        internal string GetChannelName()
        {
            return _channelName;
        }

        #region -- Video Render UI Logic ---

        internal static void MakeVideoView(uint uid, string channelId = "", VIDEO_SOURCE_TYPE videoSourceType = VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                return; // reuse
            }

            // create a GameObject and assign to this new user
            var videoSurface = MakeImageSurface(uid.ToString());

            //  var videoSurface = MakePlaneSurface(uid.ToString(), _screenParent);
            if (ReferenceEquals(videoSurface, null)) return;
            // configure videoSurface
            videoSurface.SetForUser(uid, channelId, videoSourceType);
            videoSurface.SetEnable(true);

            videoSurface.OnTextureSizeModify += (int width, int height) =>
            {
                float scale = (float)height / (float)width;
                //   videoSurface.transform.localScale = new Vector3(-5, 5 * scale, 1);
                Debug.Log("OnTextureSizeModify: " + width + "  " + height);
            };


        }

        // VIDEO TYPE 1: 3D Object
        private static VideoSurface MakePlaneSurface(string goName, Transform parentTransform)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh != null)
            {
                Debug.LogWarning("VideoSureface update shader");
                mesh.material = new Material(Shader.Find("Unlit/Texture"));
            }
            // set up transform
            // go.transform.Rotate(-90.0f, 0.0f, 0.0f);
            go.transform.position = Vector3.zero;
            //  go.transform.localScale = new Vector3(0.25f, 0.5f, .5f);

            if (parentTransform != null)
            {
                go.transform.parent = parentTransform;
                go.transform.localPosition = new Vector3(0, 0.9f, 0);
                go.transform.Rotate(-90.0f, 0.0f, 90f);
                go.transform.localScale = new Vector3(0.6589176f, 0.3431863f, 0.3431863f);
            }

            // configure videoSurface
            var videoSurface = go.AddComponent<VideoSurface>();


            return videoSurface;
        }

        // Video TYPE 2: RawImage
        private static VideoSurface MakeImageSurface(string goName)
        {



            var go = new GameObject();

            if (go == null)
            {
                return null;
            }

            go.name = goName;
            // to be renderered onto
            go.AddComponent<RawImage>();
            // make the object draggable
            go.AddComponent<UIElementDrag>();
            var canvas = GameObject.Find("VideoCanvas");
            if (canvas != null)
            {
                go.transform.parent = canvas.transform;
                Debug.Log("add video view");
            }
            else
            {
                Debug.Log("Canvas is null video view");
            }
          

            // set up transform
            //go.transform.Rotate(0f, 0.0f, 180.0f);
            //go.transform.localPosition = Vector3.zero;
            //go.transform.localScale = Vector3.one;

            GameObject parent = GameObject.Find("CanvasScreen");

            if (parent != null)
            {
                go.transform.parent = parent.transform;

                // set up transform
                go.transform.localEulerAngles = new Vector3 (0f, 180f, 180f);
                go.transform.localPosition = new Vector3(0f,0f,3.19f);
                go.transform.localScale = Vector3.one;

                // Get the current rectTransform size
                Vector2 size = go.GetComponent<RectTransform>().sizeDelta;
                // Set the new width and height
                size.x = 9.4f;
                size.y = 3.3f;
                // Apply the new size
                go.GetComponent<RectTransform>().sizeDelta = size;
             
            }


            // configure videoSurface
            var videoSurface = go.AddComponent<VideoSurface>();
            return videoSurface;
        }

        internal static void DestroyVideoView(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            if (!ReferenceEquals(go, null))
            {
                Destroy(go);
            }
        }

        #endregion
    }

    #region -- Agora Event ---

    internal class UserEventHandler : IRtcEngineEventHandler
    {
        private readonly ScreenShare _desktopScreenShare;

        internal UserEventHandler(ScreenShare desktopScreenShare)
        {
            _desktopScreenShare = desktopScreenShare;
        }

        public override void OnError(int err, string msg)
        {
            _desktopScreenShare.Log.UpdateLog(string.Format("OnError err: {0}, msg: {1}", err, msg));
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            int build = 0;
            _desktopScreenShare.Log.UpdateLog(string.Format("sdk version: ${0}",
                _desktopScreenShare.RtcEngine.GetVersion(ref build)));
            _desktopScreenShare.Log.UpdateLog(
                string.Format("OnJoinChannelSuccess channelName: {0}, uid: {1}, elapsed: {2}",
                                connection.channelId, connection.localUid, elapsed));

            ScreenShare.activateSharing = true;
            ScreenShare.isJoinedChannel = true;
        }

        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            _desktopScreenShare.Log.UpdateLog("OnRejoinChannelSuccess");
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
        {
            ScreenShare.isJoinedChannel = false;
            ScreenShare.activateSharing = false;
            _desktopScreenShare.Log.UpdateLog("OnLeaveChannel");
            ScreenShare.DestroyVideoView(connection.localUid);
        }

        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions)
        {
            _desktopScreenShare.Log.UpdateLog("OnClientRoleChanged");
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _desktopScreenShare.Log.UpdateLog(string.Format("OnUserJoined uid: ${0} elapsed: ${1}", uid, elapsed));
           
            ScreenShare.MakeVideoView(uid, _desktopScreenShare.GetChannelName(), VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _desktopScreenShare.Log.UpdateLog(string.Format("OnUserOffLine uid: ${0}, reason: ${1}", uid,
                (int)reason));
            ScreenShare.DestroyVideoView(uid);
        }
    }

    #endregion
}

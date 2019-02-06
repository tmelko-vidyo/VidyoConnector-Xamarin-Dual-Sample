using System;
using System.ComponentModel;
using VidyoClient;
using Xamarin.Forms;
using VidyoConnector.Controls;

#if __ANDROID__
using Android;
using Android.App;
using Android.OS;
#endif

namespace VidyoConnector
{
    public class VidyoController : IVidyoController, INotifyPropertyChanged, Connector.IConnect,
    Connector.IRegisterLogEventListener, Connector.IRegisterLocalCameraEventListener
    {
    
        /* Shared instance */
        private static readonly VidyoController _instance = new VidyoController();
        public static IVidyoController GetInstance() { return _instance; }

        private VidyoController() { }

        private Connector mConnector = null;

        /* Init Vidyo Client only once per app lifecycle */
        private bool mIsVidyoClientInitialized = false;

        private bool mIsDebugEnabled = false;
        private string mExperimentalOptions = null;
        private bool mCameraPrivacyState = false;

        private Logger mLogger = Logger.GetInstance();

        private VidyoConnectorState mState;

        private Controls.NativeView mVideoViewHolder;

        public event PropertyChangedEventHandler PropertyChanged;

        public VidyoConnectorState ConnectorState
        {
            get { return mState; }
            set
            {
                mState = value;
                // Raise PropertyChanged event
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ConnectorState"));
            }
        }

        /* Initialize Vidyo Client. Called only once */
        private bool Initialize()
        {
            if (mIsVidyoClientInitialized)
            {
                return true;
            }

#if __ANDROID__
            ConnectorPKG.SetApplicationUIContext(Forms.Context as Activity);
#endif
            // Initialize VidyoClient library.
            // This should be called only once throughout the lifetime of the app.
            mIsVidyoClientInitialized = ConnectorPKG.Initialize();
            return mIsVidyoClientInitialized;
        }

        public String Construct(NativeView videoView)
        {
            bool result = Initialize();
            if (!result)
            {
                throw new Exception("Client initialization error.");
            }

            string clientVersion = "Failed";

            // Remember the reference to video view
            this.mVideoViewHolder = videoView;

            mConnector = new Connector(IntPtr.Zero,
                                               Connector.ConnectorViewStyle.ConnectorviewstyleDefault,
                                               15,
                                               "info@VidyoClient info@VidyoConnector warning",
                                               "",
                                               0);
            // Get the version of VidyoClient
            clientVersion = mConnector.GetVersion();

            // If enableDebug is configured then enable debugging
            if (mIsDebugEnabled)
            {
                mConnector.EnableDebug(7776, "warning info@VidyoClient info@VidyoConnector");
            }

            // Set experimental options if any exist
            if (mExperimentalOptions != null)
            {
                ConnectorPKG.SetExperimentalOptions(mExperimentalOptions);
            }

            if (!mConnector.RegisterLocalCameraEventListener(this))
            {
                mLogger.Log("RegisterLocalCameraEventListener failed!");
            }

            // Register for log callbacks
            if (!mConnector.RegisterLogEventListener(this, "info@VidyoClient info@VidyoConnector warning"))
            {
                mLogger.Log("VidyoConnector RegisterLogEventListener failed");
            }

            mLogger.Log("Connector instance has been created.");
            return clientVersion;
        }

        /* App state changed to background mode */
        public void OnAppSleep()
        {
            if (mConnector != null)
            {
                mConnector.SetCameraPrivacy(true);
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeBackground);
            }
        }

        /* App state changed to foreground mode */
        public void OnAppResume()
        {
            if (mConnector != null)
            {
                mConnector.SetCameraPrivacy(this.mCameraPrivacyState);
                mConnector.SetMode(Connector.ConnectorMode.ConnectormodeForeground);
            }
        }

        public void Destruct()
        {
            mConnector.UnregisterLocalCameraEventListener();

            mConnector.SelectLocalCamera(null);
            mConnector.SelectLocalMicrophone(null);
            mConnector.SelectLocalSpeaker(null);

            mConnector.Disable();
            mConnector = null;

            mLogger.Log("Connector instance has been released.");
        }

        public bool Connect(string host, string token, string displayName, string resourceId)
        {
            return mConnector.Connect(host, token, displayName, resourceId, this);
        }

        public void Disconnect()
        {
            mConnector.Disconnect();
        }

        // Set the microphone privacy
        public void SetMicrophonePrivacy(bool privacy)
        {
            mConnector.SetMicrophonePrivacy(privacy);
        }

        // Set the camera privacy
        public void SetCameraPrivacy(bool privacy)
        {
            this.mCameraPrivacyState = privacy;
            mConnector.SetCameraPrivacy(privacy);
        }

        // Cycle the camera
        public void CycleCamera()
        {
            mConnector.CycleCamera();
        }

        /*
         * Private Utility Functions
         */

        // Refresh the UI
        public void RefreshUI()
        {
            // Refresh the rendering of the video
            if (mConnector != null)
            {
                uint w = mVideoViewHolder.NativeWidth;
                uint h = mVideoViewHolder.NativeHeight;

                mConnector.AssignViewToCompositeRenderer(mVideoViewHolder.Handle, Connector.ConnectorViewStyle.ConnectorviewstyleDefault, 15);
                mConnector.ShowViewAt(mVideoViewHolder.Handle, 0, 0, w, h);

                mLogger.Log("VidyoConnectorShowViewAt: x = 0, y = 0, w = " + w + ", h = " + h);
            }
        }

        public void OnSuccess()
        {
            mLogger.Log("OnSuccess");
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnected;
        }

        public void OnFailure(Connector.ConnectorFailReason reason)
        {
            mLogger.Log("OnFailure");
            ConnectorState = VidyoConnectorState.VidyoConnectorStateConnectionFailure;
        }

        public void OnDisconnected(Connector.ConnectorDisconnectReason reason)
        {
            mLogger.Log("OnDisconnected");
            ConnectorState = (reason == Connector.ConnectorDisconnectReason.ConnectordisconnectreasonDisconnected) ?
                VidyoConnectorState.VidyoConnectorStateDisconnected : VidyoConnectorState.VidyoConnectorStateDisconnectedUnexpected;
        }

        public void OnLog(LogRecord logRecord)
        {
            mLogger.LogClientLib(logRecord.message);
        }

        public void OnLocalCameraAdded(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraAdded");
        }

        public void OnLocalCameraRemoved(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraRemoved");
        }

        public void OnLocalCameraSelected(LocalCamera localCamera)
        {
            mLogger.Log("OnLocalCameraSelected");
        }

        public void OnLocalCameraStateUpdated(LocalCamera localCamera, VidyoClient.Device.DeviceState state)
        {
            mLogger.Log("OnLocalCameraStateUpdated");
        }

        public void EnableDebugging()
        {
            mConnector.EnableDebug(7776, "warning info@VidyoClient info@VidyoConnector");
        }

        public void DisableDebugging()
        {
            mConnector.DisableDebug();
        }
    }
}
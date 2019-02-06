using System;
using Xamarin.Forms;

namespace VidyoConnector
{
    public interface IVidyoController
    {

        // Create connector instance and return client version
        String Construct(Controls.NativeView videoView);

        // Release connector instance
        void Destruct();

        // Page high-level lifecycle events
        void OnAppResume();
        void OnAppSleep();

        // Fetch connector state
        VidyoConnectorState ConnectorState { get; set; }

        // Events triggered by button clicks from UI
        bool Connect(string host, string token, string displayName, string resourceId);
        void Disconnect();

        // Orientation has changed
        void RefreshUI();

        void SetCameraPrivacy(bool privacy);
        void SetMicrophonePrivacy(bool privacy);

        void CycleCamera();

        void EnableDebugging();
        void DisableDebugging();
    }
}
using PartyGame.UI.Framework;
using UnityEngine;

namespace PartyGame.Core.App
{
    /// <summary>
    /// The single entry point. Sits in the bootstrap scene and brings the app up; everything
    /// else is created in code and survives scene loads, so there are no managers to duplicate.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("Locks the app to portrait, which is what the UI is designed for.")]
        private bool forcePortrait = true;

        private void Awake()
        {
            if (forcePortrait)
            {
                Screen.orientation = ScreenOrientation.Portrait;
                Screen.autorotateToPortrait = true;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = false;
                Screen.autorotateToLandscapeRight = false;
            }

            AppController.Bootstrap();
        }
    }
}

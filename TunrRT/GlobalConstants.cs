using System;


namespace TunrRT
{
    /// <summary>
    /// Collection of string constants used in the entire solution. This file is shared for all projects
    /// </summary>
    class GlobalConstants
    {
        #region URLs and web locations
        public const string ServiceBaseUrl = "https://play.tunr.io";
        #endregion

        #region Foreground app constants
        public const string KeyLatestSyncId = "LatestSyncId";
        #endregion

        #region Background Task message keys
        public const string KeyCurrentPlaylistId = "CurrentPlaylistId";
        public const string KeyCurrentPlaylistItemId = "CurrentPlaylistItemId";
        public const string KeyCurrentStartPosition = "CurrentStartPosition";
        public const string KeyBackgroundTaskState = "BackgroundTaskState";
        public const string KeyBackgroundTaskStarted = "BackgroundTaskStarted";
        public const string KeyTrackChanged = "TrackChanged";

        public const string KeyStartPlayback = "StartPlayback";
        public const string KeyPausePlayback = "PausePlayback";
        public const string KeyPlayItem = "PlayItem";
        public const string KeySkipNextTrack = "SkipNextTrack";
        public const string KeySkipPreviousTrack = "SkipPreviousTrack";

        public const string AppState = "appstate";
        
        #endregion

        #region Background task message values
        public const string BackgroundTaskStateStarted = "BackgroundTaskStarted";
        public const string BackgroundTaskStateRunning = "BackgroundTaskRunning";
        public const string BackgroundTaskStateCancelled = "BackgroundTaskCancelled";

        public const string AppSuspended = "appsuspend";
        public const string AppResumed = "appresumed";
        public const string ForegroundAppActive = "Active";
        public const string ForegroundAppSuspended = "Suspended";
        #endregion
    }
}
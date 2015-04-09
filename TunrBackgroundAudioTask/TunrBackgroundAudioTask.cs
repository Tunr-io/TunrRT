using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TunrLibrary;
using TunrLibrary.Models;
using TunrRT;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;

namespace TunrBackgroundAudioTask
{
    /// <summary>
    /// Enum to identify foreground app state
    /// </summary>
    enum ForegroundAppStatus
    {
        Active,
        Suspended,
        Unknown
    }

    /// <summary>
    /// Impletements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class TunrBackgroundAudioTask : IBackgroundTask
    {

        #region Private fields, properties
        private static readonly string TUNRURL = "https://play.tunr.io";
        private SystemMediaTransportControls Smtc;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool backgroundtaskrunning = false;
        private PlaylistItem CurrentPlaylistItem;
        private Song CurrentSong;
        private TimeSpan CurrentStartPosition = TimeSpan.FromSeconds(0);
        private MediaPlayer _MediaPlayerInstance;
        private MediaPlayer MediaPlayerInstance
        {
            get
            {
                if (_MediaPlayerInstance == null)
                {
                    _MediaPlayerInstance = BackgroundMediaPlayer.Current;
                    _MediaPlayerInstance.MediaOpened += MediaPlayerInstance_MediaOpened;
                    _MediaPlayerInstance.MediaEnded += MediaPlayerInstance_MediaEnded;
                    _MediaPlayerInstance.CurrentStateChanged += MediaPlayerInstance_CurrentStateChanged;
                    _MediaPlayerInstance.MediaFailed += MediaPlayerInstance_MediaFailed;
                }
                return _MediaPlayerInstance;
            }
        }
        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance Interface Members and handlers
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");
            // Initialize SMTC object to talk with UVC. 
            //Note that, this is intended to run after app is paused and 
            //hence all the logic must be written to run in background process
            Smtc = SystemMediaTransportControls.GetForCurrentView();
            Smtc.ButtonPressed += systemmediatransportcontrol_ButtonPressed;
            Smtc.PropertyChanged += systemmediatransportcontrol_PropertyChanged;
            Smtc.IsEnabled = true;
            Smtc.IsPauseEnabled = true;
            Smtc.IsPlayEnabled = true;
            Smtc.IsNextEnabled = true;
            Smtc.IsPreviousEnabled = true;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += Taskcompleted;

            var value = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.AppState);
            if (value == null)
                foregroundAppState = ForegroundAppStatus.Unknown;
            else
                foregroundAppState = (ForegroundAppStatus)Enum.Parse(typeof(ForegroundAppStatus), value.ToString());

            //Add handlers for MediaPlayer
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

            //Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            //Send information to foreground that background task has been started if app is active
            if (foregroundAppState != ForegroundAppStatus.Suspended)
            {
                ValueSet message = new ValueSet();
                message.Add(Constants.BackgroundTaskStarted, "");
                BackgroundMediaPlayer.SendMessageToForeground(message);
            }
            BackgroundTaskStarted.Set();
            backgroundtaskrunning = true;

            ApplicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskRunning);
            deferral = taskInstance.GetDeferral();
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void Taskcompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
            deferral.Complete();
        }

        /// <summary>
        /// Handles background task cancellation. Task cancellation happens due to :
        /// 1. Another Media app comes into foreground and starts playing music 
        /// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
        /// In either case, save state so that if foreground app resumes it can know where to start.
        /// </summary>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // You get some time here to save your state before process and resources are reclaimed
            Debug.WriteLine("TunrBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested...");
            try
            {
                //save state
                ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentPlaylistItemId, CurrentPlaylistItem.PlaylistItemId);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentStartPosition, BackgroundMediaPlayer.Current.Position.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                backgroundtaskrunning = false;
                //unsubscribe event handlers
                Smtc.ButtonPressed -= systemmediatransportcontrol_ButtonPressed;
                Smtc.PropertyChanged -= systemmediatransportcontrol_PropertyChanged;

                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            deferral.Complete(); // signals task completion. 
            Debug.WriteLine("TunrBackgroundAudioTask Cancel complete...");
        }
        #endregion

        #region SysteMediaTransportControls related functions and handlers
        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private void UpdateUVCOnNewTrack()
        {
            Smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            Smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            Smtc.DisplayUpdater.MusicProperties.Title = CurrentSong.TagTitle;
            Smtc.DisplayUpdater.MusicProperties.Artist = CurrentSong.TagPerformers.FirstOrDefault();
            Smtc.DisplayUpdater.Update();
        }

        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void systemmediatransportcontrol_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            //TODO: If soundlevel turns to muted, app can choose to pause the music
        }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void systemmediatransportcontrol_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    // If music is in paused state, for a period of more than 5 minutes, 
                    //app will get task cancellation and it cannot run code. 
                    //However, user can still play music by pressing play via UVC unless a new app comes in clears UVC.
                    //When this happens, the task gets re-initialized and that is asynchronous and hence the wait
                    if (!backgroundtaskrunning)
                    {
                        bool result = BackgroundTaskStarted.WaitOne(2000);
                        if (!result)
                            throw new Exception("Background Task didnt initialize in time");
                    }
                    StartPlayback();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Debug.WriteLine("UVC pause button pressed");
                    try
                    {
                        BackgroundMediaPlayer.Current.Pause();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Debug.WriteLine("UVC next button pressed");
                    SkipToNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Debug.WriteLine("UVC previous button pressed");
                    SkipToPrevious();
                    break;
            }
        }



        #endregion

        #region Playlist management functions and handlers
        /// <summary>
        /// Start playlist and change UVC state
        /// </summary>
        private async void StartPlayback()
        {
            try
            {
                if (CurrentPlaylistItem == null)
                {
                    //If the task was cancelled we would have saved the current track and its position. We will try playback from there
                    PlaylistItem playlistItem = null;
                    var playlistItemId = (ApplicationSettingsHelper.ReadResetSettingsValue(Constants.CurrentPlaylistItemId));
                    if (playlistItemId != null)
                    {
                        playlistItem = await LibraryManager.FetchPlaylistItem((Guid)playlistItemId);
                    }
                    if (playlistItem != null)
                    {
                        CurrentPlaylistItem = playlistItem;
                        var currentPosition = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.CurrentStartPosition);
                        if (currentPosition == null)
                        {
                            // play from start if we dont have position
                            StartPlaylistItemAt(CurrentPlaylistItem);
                        }
                        else
                        {
                            // play from exact position otherwise
                            StartPlaylistItemAt(CurrentPlaylistItem, TimeSpan.Parse((string)currentPosition));
                        }
                    }
                    else
                    {
                        //If we dont have anything, play from beginning of playlist.
                        //Playlist.PlayAllTracks(); //start playback
                        var items = await LibraryManager.FetchPlaylistItems(Guid.Empty);
                        var firstItem = items.FirstOrDefault();
                        CurrentPlaylistItem = firstItem;
                        StartPlaylistItemAt(CurrentPlaylistItem);
                    }
                }
                else
                {
                    BackgroundMediaPlayer.Current.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private async void SkipToPrevious()
        {
            Smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            var list = await LibraryManager.FetchPlaylistItems(CurrentPlaylistItem.PlaylistFK);
            var index = list.FindIndex(p => p.PlaylistItemId == CurrentPlaylistItem.PlaylistItemId);
            var newIndex = ((index - 1) % list.Count + list.Count) % list.Count;
            var nextSong = list[newIndex];
            CurrentPlaylistItem = nextSong;
            StartPlaylistItemAt(nextSong);
            //Playlist.SkipToPrevious();
        }

        /// <summary>
        /// Skip track and update UVC via SMTC
        /// </summary>
        private async void SkipToNext()
        {
            Smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
            var list = await LibraryManager.FetchPlaylistItems(CurrentPlaylistItem.PlaylistFK);
            var index = list.FindIndex(p => p.PlaylistItemId == CurrentPlaylistItem.PlaylistItemId);
            if (list.Count <= 0 || index < 0)
            {
                Smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                Smtc.DisplayUpdater.Update();
            }
            else
            {
                var nextSong = list[(index + 1) % list.Count];
                CurrentPlaylistItem = nextSong;
                StartPlaylistItemAt(nextSong);
            }
        }

        #endregion

        #region MediaPlayer Event Handlers
        /// <summary>
        /// Handler for error event of Media Player
        /// </summary>
        void MediaPlayerInstance_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine("Failed with error code " + args.ExtendedErrorCode.ToString());
        }

        /// <summary>
        /// Handler for state changed event of Media Player
        /// </summary>
        void MediaPlayerInstance_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing && CurrentStartPosition != TimeSpan.FromSeconds(0))
            {
                // if the start position is other than 0, then set it now
                sender.Position = CurrentStartPosition;
                sender.Volume = 1.0;
                CurrentStartPosition = TimeSpan.FromSeconds(0);
                sender.PlaybackMediaMarkers.Clear();
            }
        }

        /// <summary>
        /// Handler for MediaPlayer Media Ended
        /// </summary>
        void MediaPlayerInstance_MediaEnded(MediaPlayer sender, object args)
        {
            // Skip to next...
            SkipToNext();
        }

        /// <summary>
        /// Fired when MediaPlayer is ready to play the track
        /// </summary>
        void MediaPlayerInstance_MediaOpened(MediaPlayer sender, object args)
        {
            // wait for media to be ready
            sender.Volume = 1;
            sender.Play();
            Debug.WriteLine("New Track: " + CurrentSong.TagTitle);
            UpdateUVCOnNewTrack();
            //if (foregroundAppState == ForegroundAppStatus.Active)
            //{
                //Message channel that can be used to send messages to foreground
                ValueSet message = new ValueSet();
                message.Add(Constants.Trackchanged, CurrentPlaylistItem.PlaylistItemId.ToString());
                BackgroundMediaPlayer.SendMessageToForeground(message);
            //}
        }
        #endregion

        #region Background Media Player Handlers
        void Current_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                Smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                Smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }


        /// <summary>
        /// Fires when a message is recieved from the foreground app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key.ToLower())
                {
                    case Constants.AppSuspended:
                        Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
                        foregroundAppState = ForegroundAppStatus.Suspended;
                        ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentPlaylistItemId, CurrentPlaylistItem.PlaylistItemId);
                        break;
                    case Constants.AppResumed:
                        Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
                        foregroundAppState = ForegroundAppStatus.Active;
                        break;
                    case Constants.StartPlayback: //Foreground App process has signalled that it is ready for playback
                        Debug.WriteLine("Starting Playback");
                        StartPlayback();
                        break;
                    case Constants.SkipNext: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to next");
                        SkipToNext();
                        break;
                    case Constants.SkipPrevious: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to previous");
                        SkipToPrevious();
                        break;
                }
            }
        }
        #endregion

        #region Playlist Commands

        private async void StartPlaylistItemAt(PlaylistItem playlistItem)
        {
            StartPlaylistItemAt(playlistItem, TimeSpan.Zero);
        }

        /// <summary>
        /// Starts a given track by finding its name and at desired position
        /// </summary>
        private async void StartPlaylistItemAt(PlaylistItem playlistItem, TimeSpan position)
        {
            var song = await LibraryManager.FetchPlaylistItemSong(playlistItem.PlaylistItemId);
            CurrentSong = song;

            MediaPlayerInstance.AutoPlay = false;

            ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentPlaylistItemId, playlistItem.PlaylistItemId);

            // Set the start position, we set the position once the state changes to playing, 
            // it can be possible for a fraction of second, playback can start before we are 
            // able to seek to new start position
            MediaPlayerInstance.Volume = 0;
            CurrentStartPosition = position;
            Debug.WriteLine(TUNRURL + "/stream/" + song.SongId);
            MediaPlayerInstance.SetUriSource(new Uri(TUNRURL + "/stream/" + song.SongId));
            
        }

        #endregion

    }
}

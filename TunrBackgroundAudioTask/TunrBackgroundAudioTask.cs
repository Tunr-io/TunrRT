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
using Windows.System;

namespace TunrBackgroundAudioTask
{
    /// <summary>
    /// Impletements IBackgroundTask to provide an entry point for app code to be run in background. 
    /// Also takes care of handling UVC and communication channel with foreground
    /// </summary>
    public sealed class TunrBackgroundAudioTask : IBackgroundTask
    {
        
        #region Private fields, properties
        private SystemMediaTransportControls Smtc;
        private BackgroundTaskDeferral deferral; // Used to keep task alive
        private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
        private bool IsBackgroundTaskRunning = false;

        /// <summary>
        /// The GUID of the currently selected playlist.
        /// Loaded from application settings if not present in memory.
        /// </summary>
        private Guid? CurrentPlaylistId {
            get
            {
                if (currentPlaylistId == null)
                {
                    currentPlaylistId = (ApplicationSettingsHelper.ReadResetSettingsValue(GlobalConstants.KeyCurrentPlaylistId)) as Guid?;
                }
                return currentPlaylistId;
            }
            set
            {
                if (value != currentPlaylistId)
                {
                    currentPlaylistId = value;
                    ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyCurrentPlaylistId, currentPlaylistId);
                }
            }
        }
        private Guid? currentPlaylistId;

        /// <summary>
        /// The currently playing playlist item.
        /// Loaded from application settings if not present in memory.
        /// </summary>
        private PlaylistItem CurrentPlaylistItem {
            get
            {
                if (currentPlaylistItem == null)
                {
                    var currentPlaylistItemId = (ApplicationSettingsHelper.ReadResetSettingsValue(GlobalConstants.KeyCurrentPlaylistItemId)) as Guid?;
                    if (currentPlaylistItemId != null)
                    {
                        currentPlaylistItem = LibraryManager.FetchPlaylistItem((Guid)currentPlaylistItemId);
                    }
                }
                return currentPlaylistItem;
            }
            set
            {
                if (value != currentPlaylistItem)
                {
                    currentPlaylistItem = value;
                    ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyCurrentPlaylistItemId, currentPlaylistItem.PlaylistItemId);
                    UpdateUvcMetadata(); // Update the UVC metadata when we switch songs
                    // Let the foreground app know we've changed tracks
                    ValueSet message = new ValueSet();
                    message.Add(GlobalConstants.KeyTrackChanged, CurrentPlaylistItem.PlaylistItemId.ToString());
                    BackgroundMediaPlayer.SendMessageToForeground(message);
                }
            }
        }
        private PlaylistItem currentPlaylistItem;

        /// <summary>
        /// The currently playing song from the current playlist item.
        /// </summary>
        private Song CurrentSong {
            get
            {
                return CurrentPlaylistItem == null ? null : CurrentPlaylistItem.Song;
            }
        }

        private TimeSpan CurrentStartPosition = TimeSpan.FromSeconds(0);
        
        /// <summary>
        /// The current instance of the media player
        /// Creates a new one if null
        /// </summary>
        private MediaPlayer MediaPlayerInstance
        {
            get
            {
                if (mediaPlayerInstance == null)
                {
                    mediaPlayerInstance = BackgroundMediaPlayer.Current;
                    mediaPlayerInstance.MediaOpened += MediaPlayerInstance_MediaOpened;
                    mediaPlayerInstance.MediaEnded += MediaPlayerInstance_MediaEnded;
                    mediaPlayerInstance.CurrentStateChanged += MediaPlayerInstance_CurrentStateChanged;
                    mediaPlayerInstance.MediaFailed += MediaPlayerInstance_MediaFailed;
                }
                return mediaPlayerInstance;
            }
        }
        private MediaPlayer mediaPlayerInstance;
        #endregion

        #region IBackgroundTask and IBackgroundTaskInstance members and handlers
        /// <summary>
        /// The Run method is the entry point of a background task. 
        /// </summary>
        /// <param name="taskInstance"></param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background Audio Task " + taskInstance.Task.Name + " starting...");
            Debug.WriteLine("Memory usage: " + (MemoryManager.AppMemoryUsage / 1024 / 1024) + "/" + (MemoryManager.AppMemoryUsageLimit / 1024 / 1024));

            // Initialize SMTC object to talk with UVC.
            Smtc = SystemMediaTransportControls.GetForCurrentView();
            Smtc.ButtonPressed += Smtc_ButtonPressed;
            Smtc.PropertyChanged += Smtc_PropertyChanged;
            Smtc.IsEnabled = true;
            Smtc.IsPauseEnabled = true;
            Smtc.IsPlayEnabled = true;
            Smtc.IsNextEnabled = true;
            Smtc.IsPreviousEnabled = true;

            // Associate a cancellation and completed handlers with the background task.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
            taskInstance.Task.Completed += TaskCompleted;

            // Add handlers for MediaPlayer
            BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;

            // Initialize message channel 
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

            // Mark background task as running
            BackgroundTaskStarted.Set();
            IsBackgroundTaskRunning = true;
            ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyBackgroundTaskState, GlobalConstants.BackgroundTaskStateRunning);
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet() { { "KeyBackgroundTaskStarted", "" } });

            deferral = taskInstance.GetDeferral();
        }

        /// <summary>
        /// Indicate that the background task is completed.
        /// </summary>       
        void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
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
            Debug.WriteLine("TunrBackgroundAudioTask " + sender.Task.TaskId + " task cancellation Requested...");
            try
            {
                //save state
                ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyCurrentStartPosition, BackgroundMediaPlayer.Current.Position.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(GlobalConstants.KeyBackgroundTaskState, GlobalConstants.BackgroundTaskStateCancelled);
                IsBackgroundTaskRunning = false;

                //unsubscribe event handlers
                Smtc.ButtonPressed -= Smtc_ButtonPressed;
                Smtc.PropertyChanged -= Smtc_PropertyChanged;

                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            deferral.Complete(); // signals task completion. 
            Debug.WriteLine("TunrBackgroundAudioTask cancellation complete...");
        }
        #endregion

        #region SystemMediaTransportControls related functions and handlers
        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args) { }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Debug.WriteLine("UVC play button pressed");
                    // Make sure our background task starts back up
                    // if we press play after the background task was cancelled
                    if (!IsBackgroundTaskRunning)
                    {
                        bool result = BackgroundTaskStarted.WaitOne(2000);
                        if (!result)
                        {
                            throw new Exception("Background Task didnt initialize in time");
                        }
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

        /// <summary>
        /// Update UVC using SystemMediaTransPortControl apis
        /// </summary>
        private void UpdateUvcMetadata()
        {
            Smtc.DisplayUpdater.Type = MediaPlaybackType.Music;
            Smtc.DisplayUpdater.MusicProperties.Title = CurrentSong.TagTitle;
            Smtc.DisplayUpdater.MusicProperties.Artist = CurrentSong.TagPerformers.FirstOrDefault();
            Smtc.DisplayUpdater.Update();
        }
        #endregion

        #region Playlist management functions and handlers
        /// <summary>
        /// Start playlist and change UVC state
        /// </summary>
        private void StartPlayback()
        {
            try
            {
                if (CurrentPlaylistItem == null)
                {
                    //If we dont have anything, play from beginning of playlist.
                    // HACK: Default playlist is an empty guid for now...
                    if (CurrentPlaylistId == null)
                    {
                        CurrentPlaylistId = Guid.Empty;
                    }
                    var items = LibraryManager.FetchPlaylistItems((Guid)CurrentPlaylistId);
                    var firstItem = items.FirstOrDefault();
                    CurrentPlaylistItem = firstItem;
                    StartPlaylistItemAt(CurrentPlaylistItem);
                }
                else
                {
                    if (MediaPlayerInstance.CurrentState == MediaPlayerState.Closed)
                    {
                        StartPlaylistItemAt(CurrentPlaylistItem);
                    }
                    else
                    {
                        BackgroundMediaPlayer.Current.Play();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Skip to previous track in the current playlist
        /// </summary>
        private void SkipToPrevious()
        {
            var list = LibraryManager.FetchPlaylistItems(CurrentPlaylistItem.PlaylistFK);
            var index = list.FindIndex(p => p.PlaylistItemId == CurrentPlaylistItem.PlaylistItemId);
            var newIndex = ((index - 1) % list.Count + list.Count) % list.Count;
            var nextSong = list[newIndex];
            StartPlaylistItemAt(nextSong);
        }

        /// <summary>
        /// Skip to next track in the current playlist
        /// </summary>
        private void SkipToNext()
        {
            var list = LibraryManager.FetchPlaylistItems(CurrentPlaylistItem.PlaylistFK);
            var index = list.FindIndex(p => p.PlaylistItemId == CurrentPlaylistItem.PlaylistItemId);
            if (list.Count <= 0 || index < 0)
            {
                Stop();
            }
            else
            {
                var nextSong = list[(index + 1) % list.Count];
                StartPlaylistItemAt(nextSong);
            }
        }

        /// <summary>
        /// Stops playback
        /// TODO: Make it actually unload the track info and such... right now it's just pause
        /// </summary>
        private void Stop()
        {
            MediaPlayerInstance.Pause();
            Smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            Smtc.DisplayUpdater.Update();
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
            System.Diagnostics.Debug.WriteLine("State changed:" + sender.CurrentState);
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
            System.Diagnostics.Debug.WriteLine("Media ended.");
            // Skip to next...
            SkipToNext();
        }

        /// <summary>
        /// Fired when MediaPlayer is ready to play the track
        /// </summary>
        void MediaPlayerInstance_MediaOpened(MediaPlayer sender, object args)
        {
            System.Diagnostics.Debug.WriteLine("media opened.");
            // wait for media to be ready
            Smtc.IsNextEnabled = true;
            Smtc.IsPreviousEnabled = true;
            sender.Volume = 1;
            sender.Play();
            Debug.WriteLine("New Track: " + CurrentSong.TagTitle);

            // TODO: Let the foreground app know that playback has started.
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
                switch (key)
                {
                    case GlobalConstants.KeyStartPlayback: //Foreground App process has signalled that it is ready for playback
                        Debug.WriteLine("Starting Playback");
                        StartPlayback();
                        break;
                    case GlobalConstants.KeyPausePlayback:
                        Debug.WriteLine("Pausing Playback");
                        try
                        {
                            BackgroundMediaPlayer.Current.Pause();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                        break;
                    case GlobalConstants.KeyPlayItem: // Foreground app has requested playback of a certain item
                        Debug.WriteLine("Starting Playback of playlist item");
                        StartPlaylistItemAt(LibraryManager.FetchPlaylistItem((Guid)(e.Data[key])));
                        break;
                    case GlobalConstants.KeySkipNextTrack: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to next");
                        SkipToNext();
                        break;
                    case GlobalConstants.KeySkipPreviousTrack: // User has chosen to skip track from app context.
                        Debug.WriteLine("Skipping to previous");
                        SkipToPrevious();
                        break;
                }
            }
        }
        #endregion

        #region Playlist Commands

        /// <summary>
        /// Starts a given track
        /// </summary>
        private void StartPlaylistItemAt(PlaylistItem playlistItem)
        {
            StartPlaylistItemAt(playlistItem, TimeSpan.Zero);
        }

        /// <summary>
        /// Starts a given track and start position
        /// </summary>
        private void StartPlaylistItemAt(PlaylistItem playlistItem, TimeSpan position)
        {
            CurrentPlaylistItem = playlistItem;
            CurrentPlaylistId = playlistItem.PlaylistFK;
            var song = CurrentSong;

            // Set the start position, we set the position once the state changes to playing, 
            // it can be possible for a fraction of second, playback can start before we are 
            // able to seek to new start position
            MediaPlayerInstance.AutoPlay = false;
            MediaPlayerInstance.Volume = 0;
            CurrentStartPosition = position;
            Debug.WriteLine(GlobalConstants.ServiceBaseUrl + "/stream/" + song.SongId);
            MediaPlayerInstance.SetUriSource(new Uri(GlobalConstants.ServiceBaseUrl + "/stream/" + song.SongId));
        }
        #endregion

    }
}

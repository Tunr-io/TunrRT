using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TunrLibrary.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.System.Threading;
using Windows.UI.Core;

namespace TunrRT
{
    public class BackgroundAudioHandler : INotifyPropertyChanged
    {
        private AutoResetEvent BackgroundTaskInitialized = new AutoResetEvent(false);
        
        public MediaPlayerState PlayerState {
            get
            {
                if (!BackgroundTaskRunning)
                {
                    return MediaPlayerState.Closed;
                }
                return BackgroundMediaPlayer.Current.CurrentState;
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event EventHandler TrackChanged;
        protected void OnTrackChanged()
        {
            EventHandler handler = TrackChanged;
            if (handler != null)
            {
                handler(this, null);
            }
        }

        /// <summary>
        /// Gets the information about background task is running or not by reading the setting saved by background task
        /// </summary>
        private bool BackgroundTaskRunning
        {
            get
            {
                if (!backgroundTaskRunning)
                {
                    object value = ApplicationSettingsHelper.ReadSettingsValue(GlobalConstants.KeyBackgroundTaskState);
                    if (value == null)
                    {
                        backgroundTaskRunning = false;
                    } else
                    {
                        backgroundTaskRunning = ((string)value).Equals(GlobalConstants.BackgroundTaskStateRunning);
                    }
                }

                return backgroundTaskRunning;
            }
        }
        private bool backgroundTaskRunning;

        private bool BackgroundMediaHandlersSubscribed = false;

        public BackgroundAudioHandler()
        {
            StartBackgroundAudioTask();
        }

        /// <summary>
        /// Run on resuming of app to re-bind event handlers to background task
        /// </summary>
        public void App_Resuming()
        {
            backgroundTaskRunning = false; // Assume it's not running and check.
            if (!BackgroundTaskRunning)
            {
                StartBackgroundAudioTask();
            }
            else
            {
                AddMediaPlayerEventHandlers();
            }
        }

        /// <summary>
        /// Called on app suspension to un-bind event handlers
        /// </summary>
        public void App_Suspending()
        {
            RemoveMediaPlayerEventHandlers();
        }


        /// <summary>
        /// Unsubscribes to MediaPlayer events, should run only on suspend
        /// </summary>
        private void RemoveMediaPlayerEventHandlers()
        {
            if (BackgroundMediaHandlersSubscribed)
            {
                BackgroundMediaPlayer.Current.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
                BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
                BackgroundMediaHandlersSubscribed = false;
            }
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            if (!BackgroundMediaHandlersSubscribed)
            {
                BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
                BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
                BackgroundMediaHandlersSubscribed = true;
            }
        }

        /// <summary>
        /// Fired when a message is received from the background task
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key)
                {
                    case GlobalConstants.KeyTrackChanged:
                        OnTrackChanged();
                        break;
                    case GlobalConstants.KeyBackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        Debug.WriteLine("Background Task started");
                        BackgroundTaskInitialized.Set();
                        break;
                }
            }
        }

        /// <summary>
        /// Fired when the state of the media player has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            OnPropertyChanged("PlayerState");
            switch (sender.CurrentState)
            {
                case MediaPlayerState.Playing:
                    // do something

                    break;
                case MediaPlayerState.Paused:
                    // do something

                    break;
            }
        }

        /// <summary>
        /// Makes sure background task is running and binds background media player handlers
        /// </summary>
        private void StartBackgroundAudioTask()
        {
            // Add media handlers
            AddMediaPlayerEventHandlers();
            if (BackgroundTaskRunning)
            {
                return;
            }
            var initResult = ThreadPool.RunAsync((source) =>
            {
                bool result = BackgroundTaskInitialized.WaitOne(3000);
                if (result != true)
                {
                    throw new Exception("Background Audio Task didn't start in expected time");
                }
            });
            initResult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
        }

        private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
        {
            if (status == AsyncStatus.Completed)
            {
                Debug.WriteLine("Background Audio Task initialized");
            }
            else if (status == AsyncStatus.Error)
            {
                Debug.WriteLine("Background Audio Task could not initialized due to an error :" + action.ErrorCode.ToString());
            }
        }

        #region App-accessible commands
        /// <summary>
        /// Play whatever track is queued, or whatever comes first in the selected playlist
        /// </summary>
        public void Play()
        {
            Debug.WriteLine("Play button pressed from App");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeyStartPlayback, "" } });
        }

        /// <summary>
        /// Pause the currently playing track
        /// </summary>
        public void Pause()
        {
            Debug.WriteLine("Pause button pressed from App");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeyPausePlayback, "" } });
        }

        /// <summary>
        /// Skips to the next track in the playlist
        /// </summary>
        public void Next()
        {
            Debug.WriteLine("Next button pressed from App");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeySkipNextTrack, "" } });
        }

        /// <summary>
        /// Skips to the previous track in the playlist
        /// </summary>
        public void Previous()
        {
            Debug.WriteLine("Previous button pressed from App");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeySkipPreviousTrack, "" } });
        }

        /// <summary>
        /// Play a specific item (also switches to playlist of given item)
        /// </summary>
        /// <param name="item"></param>
        public void PlayItem(PlaylistItem item)
        {
            Debug.WriteLine("Play item pressed from App");
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeyPlayItem, item.PlaylistItemId } });
        }
        #endregion
    }
}

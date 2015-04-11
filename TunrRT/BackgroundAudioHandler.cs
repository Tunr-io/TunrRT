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

        public BackgroundAudioHandler()
        {
            StartBackgroundAudioTask();
        }

        /// <summary>
        /// Run on resuming of app to re-bind event handlers to background task
        /// </summary>
        public void App_Resuming()
        {
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
            BackgroundMediaPlayer.Current.CurrentStateChanged -= BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

        /// <summary>
        /// Subscribes to MediaPlayer events
        /// </summary>
        private void AddMediaPlayerEventHandlers()
        {
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
        }

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
            var state = BackgroundMediaPlayer.Current.CurrentState; // HACK: get the state to jump-start the bg task...
            if (BackgroundTaskRunning)
            {
                return;
            }
            AddMediaPlayerEventHandlers();
            var initResult = ThreadPool.RunAsync((source) =>
            {
                bool result = BackgroundTaskInitialized.WaitOne(2000);
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
        public void Play()
        {
            Debug.WriteLine("Play button pressed from App");
            StartBackgroundAudioTask();
            if (BackgroundTaskRunning)
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeyStartPlayback, "" } });
                //BackgroundMediaPlayer.Current.Play();
                //switch (BackgroundMediaPlayer.Current.CurrentState)
                //{
                //    case MediaPlayerState.Closed:
                //        StartBackgroundAudioTask();
                //        break;
                //    case MediaPlayerState.Stopped:
                //    case MediaPlayerState.Paused:
                //        BackgroundMediaPlayer.Current.Play();
                //        break;
                //}
            }
            else
            {
                StartBackgroundAudioTask();
            }
        }

        public void PlayItem(PlaylistItem item)
        {
            StartBackgroundAudioTask();
            if (BackgroundTaskRunning)
            {
                BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { GlobalConstants.KeyPlayItem, item.PlaylistItemId } });
                //BackgroundMediaPlayer.Current.Play();
                //switch (BackgroundMediaPlayer.Current.CurrentState)
                //{
                //    case MediaPlayerState.Closed:
                //        StartBackgroundAudioTask();
                //        break;
                //    case MediaPlayerState.Stopped:
                //    case MediaPlayerState.Paused:
                //        BackgroundMediaPlayer.Current.Play();
                //        break;
                //}
            }
            else
            {
                StartBackgroundAudioTask();
            }
        }

        public void Stop()
        {
            Debug.WriteLine("Stopping...");
            BackgroundMediaPlayer.Shutdown();
        }
        #endregion
    }
}

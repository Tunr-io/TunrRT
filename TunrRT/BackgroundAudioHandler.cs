using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.System.Threading;
using Windows.UI.Core;

namespace TunrRT
{
	public class BackgroundAudioHandler
	{
		private AutoResetEvent BackgroundTaskInitialized = new AutoResetEvent(false);
		private bool _BackgroundTaskRunning = false;
		/// <summary>
		/// Gets the information about background task is running or not by reading the setting saved by background task
		/// </summary>
		private bool BackgroundTaskRunning
		{
			get
			{
				if (_BackgroundTaskRunning)
					return true;

				object value = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.BackgroundTaskState);
				if (value == null)
				{
					return false;
				}
				else
				{
					_BackgroundTaskRunning = ((String)value).Equals(Constants.BackgroundTaskRunning);
					return _BackgroundTaskRunning;
				}
			}
		}

		public BackgroundAudioHandler()
		{

		}

		/// <summary>
		/// Unsubscribes to MediaPlayer events. Should run only on suspend
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
                    case Constants.Trackchanged:
                        //When foreground app is active change track based on background message
                        break;
                    case Constants.BackgroundTaskStarted:
                        //Wait for Background Task to be initialized before starting playback
                        Debug.WriteLine("Background Task started");
                        BackgroundTaskInitialized.Set();
                        break;
                }
            }
		}

		private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
		{
			switch(sender.CurrentState)
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
		/// Initialize Background Media Player Handlers and starts playback
		/// </summary>
		private void StartBackgroundAudioTask()
		{
			AddMediaPlayerEventHandlers();
			var initResult = ThreadPool.RunAsync((source) =>
			{
				bool result = BackgroundTaskInitialized.WaitOne(2000);
				//Send message to initiate playback
				if (result == true)
				{
					var message = new ValueSet();
					message.Add(Constants.StartPlayback, "0");
					BackgroundMediaPlayer.SendMessageToBackground(message);
				}
				else
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
				Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
			}
		}

		#region App-accessible commands
		public void Play()
		{
			Debug.WriteLine("Play button pressed from App");
			if (BackgroundTaskRunning)
			{
				switch (BackgroundMediaPlayer.Current.CurrentState)
				{
					case MediaPlayerState.Closed:
						StartBackgroundAudioTask();
						break;
					case MediaPlayerState.Stopped:
					case MediaPlayerState.Paused:
						BackgroundMediaPlayer.Current.Play();
						break;
				}
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

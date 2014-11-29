﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
		private SystemMediaTransportControls Smtc;
		private BackgroundTaskDeferral deferral; // Used to keep task alive
		private ForegroundAppStatus foregroundAppState = ForegroundAppStatus.Unknown;
		private Song CurrentSong;
		private AutoResetEvent BackgroundTaskStarted = new AutoResetEvent(false);
		private bool backgroundtaskrunning = false;
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
			Debug.WriteLine("MyBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested...");
			try
			{
				//save state
				ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, Playlist.CurrentTrackName);
				ApplicationSettingsHelper.SaveSettingsValue(Constants.Position, BackgroundMediaPlayer.Current.Position.ToString());
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
			Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
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
			Smtc.DisplayUpdater.MusicProperties.Title = Playlist.CurrentTrackName;
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

		private void StartPlayback()
		{
			try
			{
				if (Playlist.CurrentTrackName == string.Empty)
				{
					//If the task was cancelled we would have saved the current track and its position. We will try playback from there
					var currenttrackname = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.CurrentTrack);
					var currenttrackposition = ApplicationSettingsHelper.ReadResetSettingsValue(Constants.Position);
					if (currenttrackname != null)
					{

						if (currenttrackposition == null)
						{
							// play from start if we dont have position
							Playlist.StartTrackAt((string)currenttrackname);
						}
						else
						{
							// play from exact position otherwise
							Playlist.StartTrackAt((string)currenttrackname, TimeSpan.Parse((string)currenttrackposition));
						}
					}
					else
					{
						//If we dont have anything, play from beginning of playlist.
						Playlist.PlayAllTracks(); //start playback
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
		private void SkipToPrevious()
		{
			Smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
			Playlist.SkipToPrevious();
		}

		/// <summary>
		/// Skip track and update UVC via SMTC
		/// </summary>
		private void SkipToNext()
		{
			Smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
			Playlist.SkipToNext();
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
						ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, Playlist.CurrentTrackName);
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

	}
}
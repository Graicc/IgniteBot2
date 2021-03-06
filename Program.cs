﻿#if INCLUDE_FIRESTORE
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IgniteBot.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static IgniteBot.g_Team;
using static Logger;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetMQ.Sockets;
using NetMQ;
using IgniteBot.Data_Containers.ZMQ_Messages;

namespace IgniteBot
{
	/// <summary>
	/// Main
	/// </summary>
	internal class Program
	{
		/// <summary>
		/// Set this to false to finish up and close
		/// </summary>
		public static bool running = true;

		public static bool inGame;

		/// <summary>
		/// Whether to continue reading input right now (for reading files)
		/// </summary>
		public static bool paused = false;

		// READ FROM FILE
		private const bool READ_FROM_FILE = false;

		/// <summary>
		/// whether to read slower when reading file 
		/// </summary>
		private const bool realtimeWhenReadingFile = false;

		// Should only use queue when reading from file
		private const bool readIntoQueue = true;
		private static bool fileFinishedReading = false;


		//private static string readFromFolder = "S:\\git_repo\\EchoVR-Session-Grabber\\bin\\Debug\\full_session_data\\example\\";
		private const string readFromFolder = "F:\\Documents\\EchoDataStorage\\TitanV Machine";
		private static List<string> filesInFolder;
		private static int readFromFolderIndex;

		public const bool uploadOnlyAtEndOfMatch = true;

		public static bool writeToOBSHTMLFile = false;

		public const string APIURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/";
		//public const string APIURL = "http://127.0.0.1:5005/";

		public const string UpdateURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/";

		public static bool enableStatsLogging = true;
		public static bool enableFullLogging = true;

		public static readonly HttpClient client = new HttpClient();

		public static string currentAccessCodeUsername = "";
		public static string InstalledSpeakerSystemVersion = "";
		public static bool IsSpeakerSystemUpdateAvailable = false;

		public static bool autoRestart;
		public static bool showDatabaseLog;

		// declarations.
		public static MatchData matchData;
		static MatchData lastMatchData;

		/// <summary>
		/// Contains the last state so that we can do a diff to determine state changes
		/// This acts like a set of flags.
		/// </summary>
		public static g_Instance lastFrame;

		public static g_Instance lastLastFrame;
		public static g_Instance lastLastLastFrame;

		private static int lastFrameSumOfStats;
		private static g_Instance lastValidStatsFrame;
		private static int lastValidSumOfStatsAge = 0;

		public static ConcurrentQueue<GoalData> lastGoals = new ConcurrentQueue<GoalData>();
		public static ConcurrentQueue<MatchData> lastMatches = new ConcurrentQueue<MatchData>();
		public static ConcurrentQueue<EventData> lastJousts = new ConcurrentQueue<EventData>();

		/// <summary>
		/// For replay file saving in batches
		/// </summary>
		private static List<string> dataCache = new List<string>();

		private class UserAtTime
		{
			public float gameClock;
			public g_Player player;
		}

		// { [stunner, stunnee], [stunner, stunnee] }
		static List<UserAtTime[]> stunningMatchedPairs = new List<UserAtTime[]>();
		private const float stunMatchingTimeout = 4f;

		public static string lastDateTimeString;
		public static string lastJSON;
		public static ConcurrentQueue<string> lastJSONQueue = new ConcurrentQueue<string>();
		public static ConcurrentQueue<string> lastDateTimeStringQueue = new ConcurrentQueue<string>();
		public static ConcurrentStack<g_Instance> milkFramesToSave = new ConcurrentStack<g_Instance>();
		public static Milk milkData;

		private static bool lastJSONUsed;

		private static readonly object lastJSONLock = new object();
		private static readonly object fileWritingLock = new object();

		public static readonly object logOutputWriteLock = new object();

		public static DateTime lastDataTime;
		static float minTillAutorestart = 3;

		static bool wasThrown;
		static int lastThrowPlayerId = -1;
		static bool inPostMatch = false;

		public static int deltaTimeIndexStats;
		public static int deltaTimeIndexFull = 1;

		public static int StatsHz {
			get => statsDeltaTimes[deltaTimeIndexStats];
		}

		public static List<int> statsDeltaTimes = new List<int> { 16, 100 };
		public static List<int> fullDeltaTimes = new List<int> { 16, 33, 100 };

		/// <summary>
		/// The folder to save all the full data logs to
		/// </summary>
		public static string saveFolder;

		public static string fileName;
		public static bool useCompression;
		public static bool batchWrites;

		public static LiveWindow liveWindow;
		public static SettingsWindow settingsWindow;
		public static Speedometer speedometerWindow;
		public static AtlasLinks atlasLinksWindow;
		public static Playspace playspaceWindow;
		public static TTSSettingsWindow ttsWindow;
		public static NVHighlightsSettingsWindow nvhWindow;
		public static LoginWindow loginWindow;
		public static FirstTimeSetupWindow firstTimeSetupWindow;
		public static ClosingDialog closingWindow;

		private static float smoothDeltaTime = -1;

		public static string customId;

		public static bool hostingLiveReplay = false;

#if INCLUDE_FIRESTORE
		public static FirestoreDb db;
#endif

		public static string echoVRIP = "";
		public static int echoVRPort = 6721;
		public static bool overrideEchoVRPort;

		public static bool Personal => currentAccessCodeUsername == "Personal" || string.IsNullOrEmpty(currentAccessCodeUsername);

		public static bool spectateMe;
		public static string lastSpectatedSessionId;

		public static SpeechSynthesizer synth;

		private static Thread statsThread;
		private static Thread fullLogThread;
		private static Thread autorestartThread;
		private static Thread fetchThread;
		private static Thread liveReplayThread;
		private static Thread milkThread;
		private static Thread IPSearchthread1;
		private static Thread IPSearchthread2;
		private static Thread thread3;
		private static Thread thread4;
		public static PublisherSocket pubSocket;

		private static App app;

		public static void Main(string[] args, App app)
		{
			AsyncIO.ForceDotNet.Force();
			NetMQConfig.Cleanup();
			pubSocket = new PublisherSocket();
			pubSocket.Options.SendHighWatermark = 1000;
			pubSocket.Bind("tcp://*:12345");
				Program.app = app;
			if (args.Contains("-port"))
			{
				int index = args.ToList().IndexOf("-port");
				if (index > -1)
				{
					if (int.TryParse(args[index + 1], out echoVRPort))
					{
						overrideEchoVRPort = true;
					}
				}
				else
				{
					LogRow(LogType.Error, "ERROR 3984. This shouldn't happen");
				}
			}


			if (CheckIfLaunchedWithCustomURLHandlerParam(args))
			{
				return; // wait for the dialog to quit the program
			}

			// allow multiple instances if the port is overriden
			if (IsIgniteBotOpen() && !overrideEchoVRPort)
			{
				new MessageBox("Instance already running", "Error").Show();
				//return; // wait for the dialog to quit the program
			}

			InstalledSpeakerSystemVersion = FindEchoSpeakerSystemInstallVersion();
			if(InstalledSpeakerSystemVersion.Length > 0)
            {
				string[] latestSpeakerSystemVer = GetLatestSpeakerSystemURLVer();
				IsSpeakerSystemUpdateAvailable = latestSpeakerSystemVer[1] != InstalledSpeakerSystemVersion;
			}

			// Reload old settings file
			if (Settings.Default.UpdateSettings)
			{
				Settings.Default.Upgrade();
				Settings.Default.UpdateSettings = false;
				Settings.Default.Save();
			}

			RegisterUriScheme("ignitebot", "IgniteBot Protocol");
			RegisterUriScheme("atlas", "ATLAS Protocol"); // TODO see how this would overwrite ATLAS URL opening

			// if logged in with discord
			if (!string.IsNullOrEmpty(Settings.Default.discordOAuthRefreshToken))
			{
				DiscordOAuth.OAuthLoginRefresh(Settings.Default.discordOAuthRefreshToken);
			}
			else
			{
				DiscordOAuth.RevertToPersonal();
			}

			liveWindow = new LiveWindow();
			liveWindow.Closed += (sender, args) => liveWindow = null;
			liveWindow.Show();

			if (!Settings.Default.firstTimeSetupShown)
			{
				firstTimeSetupWindow = new FirstTimeSetupWindow();
				firstTimeSetupWindow.Closed += (sender, args) => firstTimeSetupWindow = null;
				firstTimeSetupWindow.Show();
				Settings.Default.firstTimeSetupShown = true;
				Settings.Default.Save();
			}

			var argsList = new List<string>(args);

			// Check for command-line flags
			if (args.Contains("-slowmode"))
			{
				deltaTimeIndexStats = 1;
				Settings.Default.targetDeltaTimeIndexStats = deltaTimeIndexStats;
			}

			if (args.Contains("-autorestart"))
			{
				autoRestart = true;
				Settings.Default.autoRestart = true;
			}

			if (args.Contains("-showdatabaselog"))
			{
				showDatabaseLog = true;
				Settings.Default.showDatabaseLog = true;
			}


			// make an exception for certain users
			// Note that these usernames are not the access codes. Don't even try.
			if (currentAccessCodeUsername == "ignitevr")
			{
				ENABLE_LOGGER = false;
			}

			Settings.Default.Save();

			ReadSettings();

			client.DefaultRequestHeaders.Add("version", AppVersion());
			client.DefaultRequestHeaders.Add("User-Agent", "IgniteBot/" + AppVersion());

			client.BaseAddress = new Uri(APIURL);

			if (HighlightsHelper.isNVHighlightsEnabled)
			{
				HighlightsHelper.SetupNVHighlights();
			}
			else
			{
				HighlightsHelper.InitHighlightsSDK(true);
			}

			synth = new SpeechSynthesizer();
			// Initialize a new instance of the SpeechSynthesizer.
			DiscordOAuth.authenticated += () =>
			{
				synth = new SpeechSynthesizer();
				// Configure the audio output.
				synth.SetOutputToDefaultAudioDevice();
				synth.SetRate(Settings.Default.TTSSpeed);
			};


			// Server Score Tests - this works
			//float out1 = CalculateServerScore(new List<int> { 34, 78, 50, 53 }, new List<int> { 63, 562, 65, 81 });   // fail too high
			//float out2 = CalculateServerScore(new List<int> { 29, 60, 59, 30 }, new List<int> { 30, 70, 15, 26 });    // 90.54
			//float out3 = CalculateServerScore(new List<int> { 61, 59, 69, 67 }, new List<int> { 73, 57, 50, 51 });    // 92.33


			UpdateEchoExeLocation();

			DiscordRichPresence.Start();


			statsThread = new Thread(StatsThread);
			statsThread.IsBackground = true;
			statsThread.Start();

			fullLogThread = new Thread(FullLogThread);
			fullLogThread.IsBackground = true;
			fullLogThread.Start();

			autorestartThread = new Thread(AutorestartThread);
			autorestartThread.IsBackground = true;
			autorestartThread.Start();

			fetchThread = new Thread(FetchThread);
			fetchThread.IsBackground = true;
			fetchThread.Start();

			liveReplayThread = new Thread(LiveReplayHostingThread);
			liveReplayThread.IsBackground = true;
			liveReplayThread.Start();

			milkThread = new Thread(MilkThread);
			milkThread.IsBackground = true;
			//milkThread.Start();

			Logger.Init();

			//HighlightsHelper.CloseNVHighlights();
		}

		public static string AppVersion()
		{
			var version = Application.Current.GetType().Assembly.GetName().Version;
			return $"{version.Major}.{version.Minor}.{version.Build}";
		}


		/// <summary>
		/// This is just a failsafe so that the program doesn't leave a dangling thread.
		/// </summary>
		async static Task KillAll(HTTPServer httpServer = null)
		{
			if (liveWindow != null)
			{
				liveWindow.Close();
				liveWindow = null;
			}


			if (httpServer != null)
				httpServer.Stop();
		}

		async static Task GentleClose()
		{
			pubSocket.SendMoreFrame("CloseApp").SendFrame("");
			running = false;

			
			while (fullLogThread != null && fullLogThread.IsAlive)
			{
				closingWindow.label.Content = "Compressing Replay File...";
				await Task.Delay(10);
			}
			while (statsThread != null && statsThread.IsAlive)
			{
				closingWindow.label.Content = "Closing...";
				await Task.Delay(10);
			}
			HighlightsHelper.CloseNVHighlights();

			liveWindow.KillSpeakerSystem();
			AsyncIO.ForceDotNet.Force();
			NetMQConfig.Cleanup(false);
			app.ExitApplication();

			await Task.Delay(100);

			_ = KillAll();
		}

		public static bool IsIgniteBotOpen()
		{
			try
			{
				Process[] process = Process.GetProcessesByName("IgniteBot");
				return process?.Length > 1;
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, "Error getting other ignitebot windows\n" + e.ToString());
			}
			return false;
		}

		/// <summary>
		/// Thread that actually does the GET requests or reading from file. 
		/// Once a line has been used, this thread gets a new one.
		/// </summary>
		public static void FetchThread()
		{
			StreamReader fileReader = null;

			if (READ_FROM_FILE)
			{
				filesInFolder = Directory.GetFiles(readFromFolder, "*.zip").ToList();
				filesInFolder.Sort();
				fileReader = ExtractFile(fileReader, filesInFolder[readFromFolderIndex++]);
			}


			while (running && !fileFinishedReading)
			{
				while (paused && running)
				{
					Thread.Sleep(10);
				}

				if (READ_FROM_FILE)
				{
					if (fileReader != null)
					{
						string rawJSON = fileReader.ReadLine();
						if (rawJSON == null)
						{
							fileReader.Close();
							if (readFromFolderIndex >= filesInFolder.Count)
							{
								fileFinishedReading = true;
							}
							else
							{
								fileReader = ExtractFile(fileReader, filesInFolder[readFromFolderIndex++]);
							}
						}
						else
						{
							string[] splitJSON = rawJSON.Split('\t');
							string onlyJSON, onlyTime;
							if (splitJSON.Length > 1)
							{
								onlyJSON = splitJSON[1];
								onlyTime = splitJSON[0];
							}
							else
							{
								onlyJSON = splitJSON[0];
								string fileName = filesInFolder[readFromFolderIndex - 1].Split('\\').Last();
								onlyTime = fileName.Substring(4, fileName.Length - 8);
							}

							inGame = true;

							if (readIntoQueue)
							{
								lastJSONQueue.Enqueue(onlyJSON);
								lastDateTimeStringQueue.Enqueue(onlyTime);
							}
							else
							{
								lock (lastJSONLock)
								{
									lastDateTimeString = onlyTime;
									lastJSON = onlyJSON;
									lastJSONUsed = false;
								}
							}
						}
					}
					else
					{
						LogRow(LogType.Error, "File doesn't exist or something");
						return;
					}

					if (readIntoQueue)
					{
						if (lastJSONQueue.Count > 100000)
						{
							Console.WriteLine("Got 100k lines ahead");
							// sleep for 1 sec to let the other thread catch up
							Thread.Sleep(1000);
						}
					}
					else
					{
						// wait until we need to get another row
						while (!lastJSONUsed)
						{
							Thread.Sleep(1);
						}
					}
				}

				{
					WebResponse response;
					StreamReader sReader;


					// Do we get a response?
					try
					{
						// Create Session.
						WebRequest request = WebRequest.Create("http://" + echoVRIP + ":" + echoVRPort + "/session");
						response = request.GetResponse();
					}
					catch (Exception)
					{
						if (lastFrame != null && inGame)
						{
							MatchEventZMQMessage msg = new MatchEventZMQMessage("LeaveMatch","sessionid", lastFrame.sessionid);
							pubSocket.SendMoreFrame("MatchEvent").SendFrame(msg.ToJsonString());
						}
						// Don't update so quick if we aren't in a match anyway
						Thread.Sleep(2000);

						// split file between matches
						if (Settings.Default.whenToSplitReplays < 3)
						{
							NewFilename();
						}

						LogRow(LogType.Info, "Not in Match");
						inGame = false;


						lock (lastJSONLock)
						{
							lastJSON = null;
						}

						continue;
					}

					lastDataTime = DateTime.Now;
					inGame = true;

					Stream dataStream = response.GetResponseStream();
					sReader = new StreamReader(dataStream);

					// Session Contents
					string rawJSON = sReader.ReadToEnd();
					// pls close (;-;)
					if (sReader != null)
						sReader.Close();
					if (response != null)
						response.Close();

					lock (lastJSONLock)
					{
						lastJSON = rawJSON;
						lastJSONUsed = false;
					}
				}
			}
		}

		/// <summary>
		/// Thread for logging only stats
		/// </summary>
		public static void StatsThread()
		{
			// TODO these times aren't used, but we could do a difference on before and after times to 
			// calculate an accurate deltaTime. Right now the execution time isn't taken into account.
			var time = DateTime.Now;
			var deltaTimeSpan = new TimeSpan(0, 0, 0, 0, statsDeltaTimes[deltaTimeIndexStats]);

			Thread.Sleep(10);

			// Session pull loop.
			while (running)
			{
				if (enableStatsLogging && inGame)
				{
					try
					{
						string json, recordedTime;
						if (READ_FROM_FILE && readIntoQueue)
						{
							if (!lastJSONQueue.TryDequeue(out json))
							{
								if (fileFinishedReading)
								{
									running = false;
								}

								Thread.Sleep(1);
								continue;
							}

							lastDateTimeStringQueue.TryDequeue(out recordedTime);
						}

						lock (lastJSONLock)
						{
							// no new frame to read
							if (lastJSON == null || lastJSONUsed)
							{
								continue;
							}

							lastJSONUsed = true;
							json = lastJSON;
							recordedTime = lastDateTimeString;
						}

						// make sure there is a valid echovr path saved
						if (Settings.Default.echoVRPath == "")
						{
							UpdateEchoExeLocation();
						}


						// Convert session contents into game_instance class.
						g_Instance game_Instance = JsonConvert.DeserializeObject<g_Instance>(json);

						// add the recorded time
						if (recordedTime != string.Empty)
						{
							if (!DateTime.TryParse(recordedTime, out DateTime dateTime))
							{
								DateTime.TryParseExact(recordedTime, "yyyy-MM-dd_HH-mm-ss",
									CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
							}

							game_Instance.recorded_time = dateTime;
						}

						// prepare the raw api conversion for use
						game_Instance.teams[0].color = TeamColor.blue;
						game_Instance.teams[1].color = TeamColor.orange;
						game_Instance.teams[2].color = TeamColor.spectator;

						if (game_Instance.teams[0].players == null)
							game_Instance.teams[0].players = new List<g_Player>();
						if (game_Instance.teams[1].players == null)
							game_Instance.teams[1].players = new List<g_Player>();
						if (game_Instance.teams[2].players == null)
							game_Instance.teams[2].players = new List<g_Player>();

						// for the very first frame, duplicate it to the "previous" frame
						if (lastFrame == null)
						{
							lastFrame = game_Instance;
							DiscordRichPresence.lastDiscordPresenceTime = DateTime.Now;
						}

						if (matchData == null)
						{
							matchData = new MatchData(game_Instance);
							UpdateStatsIngame(game_Instance);
						}

						milkFramesToSave.Clear();
						milkFramesToSave.Push(game_Instance);

						ProcessFrame(game_Instance);


						//if (DateTime.Now - DiscordRichPresence.lastDiscordPresenceTime > TimeSpan.FromSeconds(1))
						//{
						//	DiscordRichPresence.ProcessDiscordPresence(game_Instance);
						//}
					}
					catch (Exception ex)
					{
						LogRow(LogType.Error, "Big oopsie. Please catch inside. " + ex);
					}

					if (READ_FROM_FILE && !realtimeWhenReadingFile)
					{
						while (running)
						{
							if (!lastJSONUsed)
							{
								break;
							}

							//Thread.Sleep(1);
						}
					}

					Thread.Sleep(statsDeltaTimes[deltaTimeIndexStats]);
				}
				else
				{
					Thread.Sleep(1000);
				}
			}
		}

		private static void MilkThread()
		{
			Thread.Sleep(2000);
			int frameCount = 0;
			// Session pull loop.
			while (running)
			{
				if (milkFramesToSave.TryPop(out g_Instance frame))
				{
					if (milkData == null)
					{
						milkData = new Milk(frame);
					}
					else
					{
						milkData.AddFrame(frame);
					}

					frameCount++;
				}

				// only save every once in a while
				if (frameCount > 200)
				{
					frameCount = 0;
					string filePath = Path.Combine(saveFolder, fileName + ".milk");
					File.WriteAllBytes(filePath, milkData.GetBytes());
				}

				Thread.Sleep(fullDeltaTimes[deltaTimeIndexFull]);
			}
		}

		/// <summary>
		/// Thread for logging all JSON data
		/// </summary>
		private static void FullLogThread()
		{
			Thread.Sleep(2000);
			lastDataTime = DateTime.Now;

			NewFilename();

			// Session pull loop.
			while (running)
			{
				if (enableFullLogging && inGame)
				{
					try
					{
						string json;
						lock (lastJSONLock)
						{
							if (lastJSON == null) continue;

							lastJSONUsed = true;
							json = lastJSON;
						}

						// if this is not a lobby api frame
						if (json.Length > 800)
						{
							bool log = false;
							if (Settings.Default.onlyRecordPrivateMatches)
							{
								g_InstanceSimple obj = JsonConvert.DeserializeObject<g_InstanceSimple>(json);
								if (obj.private_match)
								{
									log = true;
								}
							}
							else
							{
								log = true;
							}

							if (log)
							{
								WriteToFile(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "\t" + json);
							}
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Big oopsie. Please catch inside. " + ex);
					}

					Thread.Sleep(fullDeltaTimes[deltaTimeIndexFull]);
				}
				else
				{
					Thread.Sleep(100);
				}
			}

			// causes a final zip if that's needed
			NewFilename();
		}

		/// <summary>
		/// Thread to detect crashes and restart EchoVR
		/// </summary>
		public static void AutorestartThread()
		{
			lastDataTime = DateTime.Now;

			if (READ_FROM_FILE) autoRestart = false;

			// Session pull loop.
			while (running)
			{
				if (autoRestart)
				{
					// only start worrying once 15 seconds have passed
					if (DateTime.Compare(lastDataTime.AddMinutes(.25f), DateTime.Now) < 0)
					{
						LogRow(LogType.Info, "Time left until restart: " +
											 (lastDataTime.AddMinutes(minTillAutorestart) - DateTime.Now).Minutes +
											 " min " +
											 (lastDataTime.AddMinutes(minTillAutorestart) - DateTime.Now).Seconds +
											 " sec");

						// If `minTillAutorestart` minutes have passed, restart EchoVR
						if (DateTime.Compare(lastDataTime.AddMinutes(minTillAutorestart), DateTime.Now) < 0)
						{
							// Get process name
							Process[] process = GetEchoVRProcess();
							var echoPath = Settings.Default.echoVRPath;

							if (process.Length > 0)
							{
								var echo_ = process[0];
								// Get process path
								// close client
								echo_.Kill();
								// restart client
								Process.Start(echoPath, "-spectatorstream" + (Settings.Default.capturevp2 ? " -capturevp2" : ""));
							}
							else if (echoPath != null && echoPath != "")
							{
								// restart client
								Process.Start(echoPath, "-spectatorstream");
							}
							else
							{
								LogRow(LogType.Error, "Couldn't restart EchoVR because it isn't running");
							}

							// reset timer
							lastDataTime = DateTime.Now;
						}
					}
				}

				Thread.Sleep(1000);
			}
		}

		public static void LiveReplayHostingThread()
		{
			while (running)
			{
				if (hostingLiveReplay)
				{
					StringContent content = null;
					lock (lastJSONLock)
					{
						if (lastJSON != null)
							content = new StringContent(lastJSON, Encoding.UTF8, "application/json");
					}

					if (content != null)
					{
						_ = DoLiveReplayUpload(content);
					}
				}

				Thread.Sleep(1000);
			}
		}

		private static async Task DoLiveReplayUpload(StringContent content)
		{
			try
			{
				// client_name is just for visibility in the log
				var response = await client.PostAsync(
					"live_replay/" + lastFrame.sessionid + "?caprate=1&default=true&client_name=" +
					lastFrame.client_name, content);
			}
			catch
			{
				LogRow(LogType.Error, "Can't connect to the DB server");
			}
		}

		/// <summary>
		/// Saves the current process path
		/// </summary>
		/// <returns>The actual process</returns>
		public static Process[] GetEchoVRProcess()
		{
			try
			{
				var process = Process.GetProcessesByName("echovr");
				if (process.Length > 0)
				{
					// Get process path
					var newEchoPath = process[0].MainModule.FileName;
					if (newEchoPath != null && newEchoPath != "")
					{
						Settings.Default.echoVRPath = newEchoPath;
						Settings.Default.Save();
					}
				}

				return process;
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, $"Error getting process\n{e}");
				return null;
			}
		}

		public static void KillEchoVR()
		{
			Process[] process = Process.GetProcessesByName("echovr");
			if (process != null && process.Length > 0)
			{
				try
				{
					process[0].Kill();
				}
				catch (Exception ex)
				{
					LogRow(LogType.Error, "Failed to kill process\n" + ex);
				}
			}
		}

		public static void StartEchoVR(string joinType = "choose")
		{
			if (lastFrame != null)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "ignitebot://" + joinType + "/" + lastFrame.sessionid,
					UseShellExecute = true
				});
			}
			else
			{
				// No session id to launch
			}
		}

		// Functions required to force focus change, couldn't make them all local because GetWindowThreadProcessId would throw an error (though it likely shouldn't)
		[DllImport("User32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")]
		static extern bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);
		[DllImport("user32.dll")]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		public static void FocusEchoVR()
		{
			// Windows dosen't like programs stealing focus, so we have to hook into the current focused thred first
			IntPtr currentThread = new IntPtr(Thread.CurrentThread.ManagedThreadId);
			IntPtr foregroundThread = new IntPtr(GetWindowThreadProcessId(GetForegroundWindow(), out _));

			AttachThreadInput(currentThread, foregroundThread, true);

			Process[] echoProcesses = GetEchoVRProcess();
			if (echoProcesses.Length > 0)
			{
				SetForegroundWindow(echoProcesses[0].MainWindowHandle);
			}

			AttachThreadInput(currentThread, foregroundThread, false);
		}

		public static string FindEchoSpeakerSystemInstallVersion()
        {
			string ret = "";
			try
			{
				string[] subdirs = Directory.GetDirectories("C:\\Program Files (x86)\\Echo Speaker System");
				if (subdirs != null && subdirs.Length > 0)
				{
					ret = new DirectoryInfo(subdirs[0]).Name;
				}
			}
			catch { }
			return ret;
		}

		private static void UpdateEchoExeLocation()
		{
			// skip if we already have a valid path
			if (File.Exists(Settings.Default.echoVRPath)) return;

			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					const string key = "Software\\Oculus VR, LLC\\Oculus\\Libraries";
					RegistryKey oculusReg = Registry.CurrentUser.OpenSubKey(key);
					if (oculusReg == null)
					{
						// Oculus not installed
						return;
					}

					var paths = new List<string>();
					foreach (string subkey in oculusReg.GetSubKeyNames())
					{
						paths.Add((string)oculusReg.OpenSubKey(subkey).GetValue("OriginalPath"));
					}

					const string echoDir = "Software\\ready-at-dawn-echo-arena\\bin\\win7\\echovr.exe";
					foreach (var path in paths)
					{
						string file = Path.Combine(path, echoDir);
						if (File.Exists(file))
						{
							Settings.Default.echoVRPath = file;
							Settings.Default.Save();
							return;
						}
					}
				}
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, $"Can't get EchoVR path from registry\n{e}");
			}
		}

		private static StreamReader ExtractFile(StreamReader fileReader, string fileName)
		{
			string tempDir = Path.Combine(saveFolder, "temp_zip_read\\");

			if (Directory.Exists(tempDir))
			{
				while (running)
				{
					try
					{
						Directory.Delete(tempDir, true);
						break;
					}
					catch (IOException)
					{
						Thread.Sleep(10);
					}
				}
			}

			Directory.CreateDirectory(tempDir);

			using (ZipArchive archive = ZipFile.OpenRead(fileName))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					// Gets the full path to ensure that relative segments are removed.
					string destinationPath = Path.GetFullPath(Path.Combine(tempDir, entry.FullName));

					entry.ExtractToFile(destinationPath);

					fileReader = new StreamReader(destinationPath);
				}
			}

			return fileReader;
		}

		private static void ReadSettings()
		{
			showDatabaseLog = Settings.Default.showDatabaseLog;
			enableLoggingRemote = Settings.Default.logToServer;
			autoRestart = Settings.Default.autoRestart;
			deltaTimeIndexStats = Settings.Default.targetDeltaTimeIndexStats;
			useCompression = Settings.Default.useCompression;
			batchWrites = Settings.Default.batchWrites;
			saveFolder = Settings.Default.saveFolder;
			enableFullLogging = Settings.Default.enableFullLogging;
			enableStatsLogging = Settings.Default.enableStatsLogging;
			deltaTimeIndexFull = Settings.Default.targetDeltaTimeIndexFull;
			echoVRIP = Settings.Default.echoVRIP;
			HighlightsHelper.clearHighlightsOnExit = Settings.Default.clearHighlightsOnExit;
			HighlightsHelper.ClientHighlightScope = (HighlightLevel)Settings.Default.clientHighlightScope;
			HighlightsHelper.isNVHighlightsEnabled = Settings.Default.isNVHighlightsEnabled;
			if (!overrideEchoVRPort) echoVRPort = Settings.Default.echoVRPort;

			if (saveFolder == "none" || !Directory.Exists(saveFolder))
			{
				saveFolder = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal),
					"IgniteBot"), "replays");
				Directory.CreateDirectory(saveFolder);
				Settings.Default.saveFolder = saveFolder;
				Settings.Default.Save();
			}
		}

		/// <summary>
		/// Writes the data to the file
		/// </summary>
		/// <param name="data">The data to write</param>
		static void WriteToFile(string data)
		{
			if (batchWrites)
			{
				dataCache.Add(data);

				// if the time elapsed since last write is less than cutoff
				if (dataCache.Count * fullDeltaTimes[deltaTimeIndexFull] < 5000)
				{
					return;
				}
			}

			// Fail if the folder doesn't even exist
			if (!Directory.Exists(saveFolder))
			{
				return;
			}

			string filePath, directoryPath;

			// could combine with some other data path, such as AppData
			directoryPath = saveFolder;

			filePath = Path.Combine(directoryPath, fileName + ".echoreplay");

			lock (fileWritingLock)
			{
				StreamWriter streamWriter = new StreamWriter(filePath, true);

				if (batchWrites)
				{
					foreach (var row in dataCache)
					{
						streamWriter.WriteLine(row);
					}

					dataCache.Clear();
				}
				else
				{
					streamWriter.WriteLine(data);
				}

				streamWriter.Close();
			}
		}

		/// <summary>
		/// Goes through a "frame" (single JSON object) and generates the relevant events
		/// </summary>
		static void ProcessFrame(g_Instance frame)
		{
			// 'mpl_lobby_b2' may change in the future
			if (frame == null || string.IsNullOrWhiteSpace(frame.game_status)) return;
			pubSocket.SendMoreFrame("RawFrame").SendFrame(lastJSON);
			if (frame.inLobby) return;
			pubSocket.SendMoreFrame("TimeAndScore").SendFrame(String.Format("{0:0.00}", frame.game_clock) + " Orange: " + frame.orange_points.ToString() + " Blue: " + frame.blue_points.ToString());
			// if we entered a different match
			if (frame.sessionid != lastFrame.sessionid || lastFrame == null)
			{
				MatchEventZMQMessage msg = new MatchEventZMQMessage("NewMatch", "sessionid", frame.sessionid);
                pubSocket.SendMoreFrame("MatchEvent").SendFrame(msg.ToJsonString());
				// We just discard the old match and hope it was already submitted

				lastFrame = frame; // don't detect stats changes across matches
								   // TODO discard old players

				inPostMatch = false;
				matchData = new MatchData(frame);
				UpdateStatsIngame(frame);


				if (string.IsNullOrEmpty(Settings.Default.echoVRPath))
				{
					GetEchoVRProcess();
				}

				if (spectateMe)
				{
					try
					{
						KillEchoVR();
						StartEchoVR("spectate");
						lastSpectatedSessionId = lastFrame.sessionid;
					}
					catch (Exception e)
					{
						LogRow(LogType.Error, "Broke something in the spectator follow system.\n" + e.ToString());
					}
				}
			}

			// The time between the current frame and last frame in seconds based on the game clock
			float deltaTime = lastFrame.game_clock - frame.game_clock;
			if (deltaTime != 0)
			{
				if (smoothDeltaTime == -1) smoothDeltaTime = deltaTime;
				const float smoothingFactor = .99f;
				smoothDeltaTime = smoothDeltaTime * smoothingFactor + deltaTime * (1 - smoothingFactor);
			}


			// Did a player join or leave?

			// is a player from the current frame not in the last frame? (Player Join 🤝)
			// Loop through teams.
			foreach (g_Team team in frame.teams)
			{
				// Loop through players on team.
				foreach (g_Player player in team.players)
				{
					player.team = team;
					if (lastFrame.GetAllPlayers(true).Any(p => p.userid == player.userid))
					{
						continue;
					}

					// TODO find why this is crashing
					try
					{
						matchData.Events.Add(new EventData(matchData, EventData.EventType.player_joined,
							frame.game_clock, team, player, null, player.head.Position, Vector3.Zero));
						LogRow(LogType.File, frame.sessionid,
							frame.game_clock_display + " - Player Joined: " + player.name);

						if (team.color != TeamColor.spectator)
						{
							// cache this players stats so they aren't overridden if they join again
							MatchPlayer playerData = matchData.GetPlayerData(player);
							// if player was in this match before
							playerData?.CacheStats(player.stats);
						}

						UpdateStatsIngame(frame);

						if (Settings.Default.playerJoinTTS)
						{
							synth.SpeakAsync(player.name + " joined " + team.color);
						}
					}
					catch (Exception ex)
					{
						LogRow(LogType.Error, ex.ToString());
					}
				}
			}

			// Is a player from the last frame not in the current frame? (Player Leave 🚪)
			// Loop through teams.
			foreach (g_Team team in lastFrame.teams)
			{
				// Loop through players on team.
				foreach (g_Player player in team.players)
				{
					if (frame.GetAllPlayers(true).Any(p => p.userid == player.userid)) continue;

					matchData.Events.Add(new EventData(
						matchData,
						EventData.EventType.player_left,
						frame.game_clock,
						team,
						player,
						null,
						player.head.Position,
						player.velocity.ToVector3())
					);

					LogRow(LogType.File, frame.sessionid, frame.game_clock_display + " - Player Left: " + player.name);

					UpdateStatsIngame(frame);

					if (Settings.Default.playerLeaveTTS)
					{
						synth.SpeakAsync(player.name + " left " + team.color);
					}
				}
			}

			// Did a player switch teams? (Player Switch 🔁)
			// Loop through current frame teams.
			foreach (g_Team team in frame.teams)
			{
				// Loop through players on team.
				foreach (g_Player player in team.players)
				{
					TeamColor lastTeamColor = lastFrame.GetTeamColor(player.userid);
					if (lastTeamColor == team.color) continue;

					matchData.Events.Add(new EventData(
						matchData,
						EventData.EventType.player_switched_teams,
						frame.game_clock,
						team,
						player,
						null,
						player.head.Position,
						player.velocity.ToVector3())
					);

					LogRow(LogType.File, frame.sessionid, $"{frame.game_clock_display} - Player switched to {team.color} team: {player.name}");

					UpdateStatsIngame(frame);

					if (Settings.Default.playerSwitchTeamTTS)
					{
						synth.SpeakAsync($"{player.name} switched to {team.color}");
					}
				}
			}


			int currentFrameStats = 0;
			foreach (var team in frame.teams)
			{
				// Loop through players on team.
				foreach (var player in team.players)
				{
					currentFrameStats += player.stats.stuns + player.stats.points;
				}
			}

			if (currentFrameStats < lastFrameSumOfStats)
			{
				lastValidStatsFrame = lastFrame;
				lastValidSumOfStatsAge = 0;
			}

			lastValidSumOfStatsAge++;
			lastFrameSumOfStats = currentFrameStats;


			// Did the game state change?
			if (frame.game_status != lastFrame.game_status)
			{
				ProcessGameStateChange(frame, deltaTime);
			}


			// pause state changed
			try
			{
				if (frame.pause.paused_state != lastFrame.pause.paused_state)
				{
					if (frame.pause.paused_state == "paused")
					{
						LogRow(LogType.File, frame.sessionid, $"{frame.game_clock_display} - {frame.pause.paused_requested_team} team paused the game");
						if (Settings.Default.pausedTTS) synth.SpeakAsync($"{frame.pause.paused_requested_team} team paused the game");
						matchData.Events.Add(
							new EventData(
								matchData,
								EventData.EventType.pause_request,
								frame.game_clock,
								frame.teams[frame.pause.paused_requested_team == "blue" ? (int)TeamColor.blue : (int)TeamColor.orange],
								null,
								null,
								Vector3.Zero,
								Vector3.Zero)
							);
					}

					if (lastFrame.pause.paused_state == "unpaused" &&
						frame.pause.paused_state == "paused_requested")
					{
						LogRow(LogType.File, frame.sessionid, $"{frame.game_clock_display} - {frame.pause.paused_requested_team} team requested a pause");
						if (Settings.Default.pausedTTS) synth.SpeakAsync($"{frame.pause.paused_requested_team} team requested a pause");
					}

					if (lastFrame.pause.paused_state == "paused" &&
						frame.pause.paused_state == "unpausing")
					{
						LogRow(LogType.File, frame.sessionid, $"{frame.game_clock_display} - {frame.pause.paused_requested_team} team unpaused the game");
						if (Settings.Default.pausedTTS) synth.SpeakAsync($"{frame.pause.paused_requested_team} team unpaused the game");
					}
				}
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, "Error with pause request parsing\n" + e.ToString());
			}



			// while playing and frames aren't identical
			if (frame.game_status == "playing" && deltaTime != 0)
			{
				inPostMatch = false;


				matchData.currentDiskTrajectory.Add(frame.disc.position.ToVector3());

				if (frame.disc.velocity.ToVector3().Equals(Vector3.Zero))
				{
					wasThrown = false;
				}

				// Generate "playing" events
				foreach (g_Team team in frame.teams)
				{
					foreach (g_Player player in team.players)
					{
						var lastPlayer = lastFrame.GetPlayer(player.userid);
						if (lastPlayer == null) continue;

						MatchPlayer playerData = matchData.GetPlayerData(player);
						if (playerData != null)
						{
							// update player velocity
							//Vector3 playerSpeed = (player.head.Position - lastPlayer.head.Position) / deltaTime;
							//float speed = playerSpeed.Length();
							Vector3 playerSpeed = player.velocity.ToVector3();
							float speed = playerSpeed.Length();
							playerData.UpdateAverageSpeed(speed);
							playerData.AddRecentVelocity(speed);

							// starting a boost
							float smoothedVel = playerData.GetSmoothedVelocity();
							if (smoothedVel > MatchPlayer.boostVelCutoff)
							{
								// if not already boosting and there are enough values to get a stable reading
								if (!playerData.boosting && playerData.recentVelocities.Count > 10)
								{
									playerData.boosting = true;
								}
							}

							// finished a boost
							if (smoothedVel < MatchPlayer.boostVelStopCutoff && playerData.boosting)
							{
								playerData.boosting = false;

								(float, float) boost = playerData.GetMaxRecentVelocity(reset: true);
								float boostSpeed = boost.Item1;
								float howLongAgoBoost = boost.Item2;

								matchData.Events.Add(
									new EventData(
										matchData,
										EventData.EventType.big_boost,
										frame.game_clock + howLongAgoBoost,
										team,
										player,
										null,
										player.head.Position,
										new Vector3(boostSpeed, 0, 0)
									)
								);

								LogRow(LogType.File, frame.sessionid,
									frame.game_clock_display + " - " + player.name + " boosted to " +
									boostSpeed.ToString("N1") + " m/s");


								// TTS
								if (playerData.Name == frame.client_name)
								{
									if (Settings.Default.maxBoostSpeedTTS)
									{
										synth.SpeakAsync(boostSpeed.ToString("N0") + " meters per second");
									}
								}
							}

							// update hand velocities
							playerData.UpdateAverageSpeedLHand(
								((player.lhand.Position - lastPlayer.lhand.Position) - playerSpeed).Length() /
								deltaTime);
							playerData.UpdateAverageSpeedRHand(
								((player.rhand.Position - lastPlayer.rhand.Position) - playerSpeed).Length() /
								deltaTime);

							// update distance between hands
							//playerData.distanceBetweenHands.Add(Vector3.Distance(player.lhand.ToVector3(), player.rhand.ToVector3()));

							// update distance from hand to head
							float leftHandDistance = Vector3.Distance(player.head.Position, player.lhand.Position);
							float rightHandDistance = Vector3.Distance(player.head.Position, player.rhand.Position);
							playerData.distanceBetweenHands.Add(Math.Max(leftHandDistance, rightHandDistance));


							#region play space abuse

							if (Math.Abs(deltaTime) < .1f)
							{
								// move the playspace based on reported game velocity
								playerData.playspaceLocation += player.velocity.ToVector3() * deltaTime;
							}
							else
							{
								// reset playspace
								playerData.playspaceLocation = player.head.Position;
								LogRow(LogType.Error, "Reset playspace due to framerate");
							}
							// move the playspace towards the current player position
							Vector3 offset = (player.head.Position - playerData.playspaceLocation).Normalized() * .05f * deltaTime;
							// if there is no difference, so normalization doesn't work
							if (double.IsNaN(offset.X))
							{
								offset = Vector3.Zero;
							}
							playerData.playspaceLocation += offset;

							if (team.team != "SPECTATORS" && Math.Abs(smoothDeltaTime) < .1f &&
								Math.Abs(deltaTime) < .1f &&
								Vector3.Distance(player.head.Position, playerData.playspaceLocation) > 1.7f)
							{
								// playspace abuse happened
								matchData.Events.Add(
									new EventData(
										matchData,
										EventData.EventType.playspace_abuse,
										frame.game_clock,
										team,
										player,
										null,
										player.head.Position,
										player.head.Position - playerData.playspaceLocation));
								LogRow(LogType.File, frame.sessionid,
									frame.game_clock_display + " - " + player.name + " abused their playspace");
								playerData.PlayspaceAbuses++;

								// reset the playspace so we don't get extra events
								playerData.playspaceLocation = player.head.Position;
							}
							else if (Math.Abs(smoothDeltaTime) > .1f)
							{
								if (ENABLE_LOGGER)
								{
									//Console.WriteLine("Update rate too slow to calculate playspace abuses.");
								}
							}

							#endregion

							// add time if upside down
							if (Vector3.Dot(player.head.up.ToVector3(), Vector3.UnitY) < 0)
							{
								playerData.InvertedTime += deltaTime;
							}


							playerData.PlayTime += deltaTime;
						}
						else
						{
							LogRow(LogType.Error, "PlayerData is null");
						}


						// check saves 
						if (lastPlayer.stats.saves != player.stats.saves)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.save, frame.game_clock,
								team, player, null, player.head.Position, Vector3.Zero));
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " made a save");
							HighlightsHelper.SaveHighlightMaybe(player, frame, "SAVE");
						}

						// check steals 🕵️‍
						if (lastPlayer.stats.steals != player.stats.steals)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.steal, frame.game_clock,
								team, player, null, player.head.Position, Vector3.Zero));

							if (WasStealNearGoal(frame.disc.position.ToVector3(), team.color, frame))
							{
								HighlightsHelper.SaveHighlightMaybe(player, frame, "STEAL_SAVE");
								LogRow(LogType.File, frame.sessionid,
									frame.game_clock_display + " - " + player.name + " stole the disk near goal!");
							}
							else
							{
								LogRow(LogType.File, frame.sessionid,
									frame.game_clock_display + " - " + player.name + " stole the disk");
							}
						}

						// check stuns
						if (lastPlayer.stats.stuns != player.stats.stuns)
						{
							// try to match it to an existing stunnee

							// clean up the stun match list
							stunningMatchedPairs.RemoveAll(uat =>
							{
								if (uat[0] != null && uat[0].gameClock - frame.game_clock > stunMatchingTimeout)
									return true;
								else if (uat[1] != null && uat[1].gameClock - frame.game_clock > stunMatchingTimeout)
									return true;
								else return false;
							});

							bool added = false;
							foreach (var stunEvent in stunningMatchedPairs)
							{
								if (stunEvent[0] == null)
								{
									// if (stunEvent[1].player position is close to the stunner)
									if (stunEvent[1].player.name != player.name)
									{
										stunningMatchedPairs.Remove(stunEvent);

										var stunner = player;
										var stunnee = stunEvent[1].player;

										matchData.Events.Add(new EventData(matchData, EventData.EventType.stun,
											frame.game_clock, team, stunner, stunnee, stunnee.head.Position,
											Vector3.Zero));
										LogRow(LogType.File, frame.sessionid,
											frame.game_clock_display + " - " + stunner.name + " just stunned " +
											stunnee.name);
										added = true;
										break;
									}
								}
							}

							if (!added)
							{
								stunningMatchedPairs.Add(new UserAtTime[]
									{new UserAtTime {gameClock = frame.game_clock, player = player}, null});
							}
						}

						// check getting stunned 
						if (!lastPlayer.stunned && player.stunned)
						{
							// try to match it to an existing stun

							// clean up the stun match list
							stunningMatchedPairs.RemoveAll(uat =>
							{
								if (uat[0] != null && uat[0].gameClock - frame.game_clock > stunMatchingTimeout)
									return true;
								else if (uat[1] != null && uat[1].gameClock - frame.game_clock > stunMatchingTimeout)
									return true;
								else return false;
							});
							bool added = false;
							foreach (var stunEvent in stunningMatchedPairs)
							{
								if (stunEvent[1] == null)
								{
									// if (stunEvent[0].player position is close to the stunee)
									if (stunEvent[0].player.name != player.name)
									{
										stunningMatchedPairs.Remove(stunEvent);

										var stunner = stunEvent[0].player;
										var stunnee = player;

										matchData.Events.Add(new EventData(matchData, EventData.EventType.stun,
											frame.game_clock, team, stunner, stunnee, stunnee.head.Position,
											Vector3.Zero));
										LogRow(LogType.File, frame.sessionid,
											frame.game_clock_display + " - " + stunner.name + " just stunned " +
											stunnee.name);
										added = true;
										break;
									}
								}
							}

							if (!added)
							{
								stunningMatchedPairs.Add(new UserAtTime[]
									{null, new UserAtTime {gameClock = frame.game_clock, player = player}});
							}
						}

						// check disk was caught 🥊
						if (!lastPlayer.possession && player.possession)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.@catch, frame.game_clock,
								team, player, null, player.head.Position, Vector3.Zero));
							playerData.Catches++;
							bool caughtThrow = false;
							g_Team throwTeam = null;
							g_Player throwPlayer = null;
							bool wasTurnoverCatch = false;

							if (lastThrowPlayerId > 0)
							{
								g_Instance lframe = lastFrame;

								foreach (var lteam in lframe.teams)
								{
									foreach (var lplayer in lteam.players)
									{
										if (lplayer.playerid == lastThrowPlayerId && lplayer.possession == true)
										{
											caughtThrow = true;
											throwPlayer = lplayer;
											throwTeam = lteam;
											if (lteam.color != team.color)
											{
												wasTurnoverCatch = true;
											}
										}
									}
								}
							}

							if (caughtThrow)
							{
								if (wasTurnoverCatch && lastPlayer.stats.saves == player.stats.saves)
								{
									_ = DelayedCatchEvent(player, throwPlayer);
									LogRow(LogType.File, frame.sessionid,
										frame.game_clock_display + " - " + throwPlayer.name +
										" turned over the disk to " + player.name);
									// TODO enable once the db can handle it
									// matchData.Events.Add(new EventData(matchData, EventData.EventType.turnover, frame.game_clock, team, throwPlayer, player, throwPlayer.head.Position, player.head.Position));
									matchData.GetPlayerData(throwPlayer).Turnovers++;
								}
								else
								{
									LogRow(LogType.File, frame.sessionid,
										frame.game_clock_display + " - " + player.name + " received a pass from " +
										throwPlayer.name);
									matchData.Events.Add(new EventData(matchData, EventData.EventType.pass,
										frame.game_clock, team, throwPlayer, player, throwPlayer.head.Position,
										player.head.Position));
									matchData.GetPlayerData(throwPlayer).Passes++;
								}
							}
							else
							{
								LogRow(LogType.File, frame.sessionid,
									frame.game_clock_display + " - " + player.name + " made a catch");
							}
						}

						// check if the disk was caught using stats 🥊
						// This doesn't happen, because the API just reports 0
						if (lastPlayer.stats.catches != player.stats.catches)
						{
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " made a catch (stat)");
						}

						// check blocks 🧱
						// This doesn't happen, because the API just reports 0
						if (lastPlayer.stats.blocks != player.stats.blocks)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.block, frame.game_clock,
								team, player, null, player.head.Position, Vector3.Zero));
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " just blocked");
						}

						// check shots taken 🧺
						if (lastPlayer.stats.shots_taken != player.stats.shots_taken)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.shot_taken,
								frame.game_clock, team, player, null, player.head.Position, Vector3.Zero));
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " just took a shot");
							if (lastThrowPlayerId == player.playerid)
							{
								lastThrowPlayerId = -1;
							}
						}

						// check blocks 🧱
						if (lastPlayer.stats.passes != player.stats.passes)
						{
							//matchData.Events.Add(new EventData(matchData, EventData.EventType.block, frame.game_clock, team, player, null, player.head.Position, Vector3.Zero));
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " made a pass");
						}

						// check disk was thrown ⚾
						if (!wasThrown && player.possession &&
							!lastFrame.disc.velocity.ToVector3().Equals(Vector3.Zero) &&
							!frame.disc.velocity.ToVector3().Equals(Vector3.Zero) &&
							(frame.disc.velocity.ToVector3() - player.velocity.ToVector3()).Length() > 3)
						{
							wasThrown = true;
							lastThrowPlayerId = player.playerid;

							// find out which hand it was thrown by
							bool leftHanded = false;
							var leftHandVelocity = (lastPlayer.lhand.Position - player.lhand.Position) / deltaTime;
							var rightHandVelocity = (lastPlayer.rhand.Position - player.rhand.Position) / deltaTime;

							// based on position of hands
							if (Vector3.Distance(lastPlayer.lhand.Position, lastFrame.disc.position.ToVector3()) <
								Vector3.Distance(lastPlayer.rhand.Position, lastFrame.disc.position.ToVector3()))
							{
								leftHanded = true;
							}

							// find out underhandedness
							float underhandedness =
								Vector3.Distance(lastPlayer.lhand.Position, lastFrame.disc.position.ToVector3()) <
								Vector3.Distance(lastPlayer.rhand.Position, lastFrame.disc.position.ToVector3())
									? Vector3.Dot(lastPlayer.head.up.ToVector3(),
										lastPlayer.lhand.Position - lastPlayer.head.Position)
									: Vector3.Dot(lastPlayer.head.up.ToVector3(),
										lastPlayer.rhand.Position - lastPlayer.head.Position);

							// wait to actually log this throw to get more accurate velocity
							_ = DelayedThrowEvent(player, leftHanded, underhandedness,
								frame.disc.velocity.ToVector3().Length());
						}

						// TODO check if a pass was made
						if (false)
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.pass, frame.game_clock,
								team, player, null, player.head.Position, Vector3.Zero));
							LogRow(LogType.File, frame.sessionid,
								frame.game_clock_display + " - " + player.name + " made a pass");
						}
					}
				}


				// generate general playing events (not player-specific)

				try
				{
					// check blue restart request ↩
					if (!lastFrame.blue_team_restart_request && frame.blue_team_restart_request)
					{
						matchData.Events.Add(new EventData(matchData, EventData.EventType.restart_request,
							lastFrame.game_clock, frame.teams[(int)TeamColor.blue], null, null, Vector3.Zero,
							Vector3.Zero));
						LogRow(LogType.File, frame.sessionid,
							frame.game_clock_display + " - " + "blue team restart request");
					}

					// check orange restart request ↩
					if (!lastFrame.orange_team_restart_request && frame.orange_team_restart_request)
					{
						matchData.Events.Add(new EventData(matchData, EventData.EventType.restart_request,
							lastFrame.game_clock, frame.teams[(int)TeamColor.orange], null, null, Vector3.Zero,
							Vector3.Zero));
						LogRow(LogType.File, frame.sessionid,
							frame.game_clock_display + " - " + "orange team restart request");
					}
				}
				catch (Exception)
				{
					LogRow(LogType.Error, "Error with restart request parsing");
				}
			}

			lastLastLastFrame = lastLastFrame;
			lastLastFrame = lastFrame;
			lastFrame = frame;
		}


		// 💨
		private static async Task JoustDetection(g_Instance firstFrame, EventData.EventType eventType, TeamColor side)
		{
			float startGameClock = firstFrame.game_clock;
			float maxTubeExitSpeed = 0;
			float maxSpeed = 0;
			List<string> playersWhoExitedTube = new List<string>();

			int interval = 8; // time between checks - ms. This is faster than cap rate just to be sure.
			float maxJoustTimeTimeout = 10000; // time before giving up calculating joust time - ms
			for (int i = 0; i < maxJoustTimeTimeout / interval; i++)
			{
				g_Instance frame = lastFrame;
				var team = frame.teams[(int)side];
				foreach (g_Player player in team.players)
				{
					float speed = player.velocity.ToVector3().Length();
					// if the player exited the tube for the first time
					if (player.head.Position.Z * ((int)side * 2 - 1) < 40 && !playersWhoExitedTube.Contains(player.name))
					{
						playersWhoExitedTube.Add(player.name);
						if (speed > maxTubeExitSpeed)
						{
							maxTubeExitSpeed = speed;
						}
					}

					if (speed > maxSpeed)
					{
						maxSpeed = speed;
					}

					// if the player crossed the centerline - joust finished 🏁
					if (player.head.Position.Z * ((int)side * 2 - 1) < 0)
					{

						EventData joustEvent = new EventData(
							matchData,
							eventType,
							frame.game_clock,
							team,
							player,
							(long)((startGameClock - frame.game_clock) * 1000),
							Vector3.Zero,
							new Vector3(
								maxSpeed,
								maxTubeExitSpeed,
								startGameClock - frame.game_clock)
						);

						// only joust time
						if (Settings.Default.joustTimeTTS && !Settings.Default.joustSpeedTTS)
						{
							synth.SpeakAsync(team.color.ToString() + " " +
											 (startGameClock - frame.game_clock).ToString("N1"));
						}
						// only joust speed
						else if (!Settings.Default.joustTimeTTS && Settings.Default.joustSpeedTTS)
						{
							synth.SpeakAsync(team.color.ToString() + " " + maxSpeed.ToString("N0") +
											 " meters per second");
						}
						// both
						else if (Settings.Default.joustTimeTTS && Settings.Default.joustSpeedTTS)
						{
							synth.SpeakAsync(team.color.ToString() + " " +
											 (startGameClock - frame.game_clock).ToString("N1") + " " +
											 maxSpeed.ToString("N0") + " meters per second");
						}

						matchData.Events.Add(joustEvent);
						LogRow(LogType.File, frame.sessionid, frame.game_clock_display + " - " +
															  team.color.ToString() +
															  " team joust time" +
															  (eventType == EventData.EventType.defensive_joust ? " (defensive)" : "") +
															  ": " +
															  (startGameClock - frame.game_clock)
															  .ToString("N2") +
															  " s, Max speed: " +
															  maxSpeed.ToString("N2") +
															  " m/s, Tube Exit Speed: " +
															  maxTubeExitSpeed.ToString("N2") + " m/s");

						// Upload to Firebase 🔥
						_ = DoUploadEventFirebase(matchData, joustEvent);

						lastJousts.Enqueue(joustEvent);
						if (lastJousts.Count > 20)
						{
							lastJousts.TryDequeue(out var joust);
						}

						return;
					}
				}
				await Task.Delay(interval);
			}
		}


		private static async Task DelayedCatchEvent(g_Player originalPlayer, g_Player throwPlayer)
		{
			// TODO look through again
			// wait some time before checking if a save happened (then it wouldn't be an interception)
			await Task.Delay(2000);

			g_Instance frame = lastFrame;

			foreach (g_Team team in frame.teams)
			{
				foreach (g_Player player in team.players)
				{
					if (player.playerid != originalPlayer.playerid) continue;
					if (player.stats.saves != originalPlayer.stats.saves) continue;

					LogRow(LogType.File, frame.sessionid,
						frame.game_clock_display + " - " + player.name + " intercepted a throw from " +
						throwPlayer.name);
					// TODO enable this once the db supports it
					// matchData.Events.Add(new EventData(matchData, EventData.EventType.interception, frame.game_clock, team, player, null, player.head.Position, Vector3.Zero));
					MatchPlayer otherMatchPlayer = matchData.GetPlayerData(player);
					if (otherMatchPlayer != null) otherMatchPlayer.Interceptions++;
					else LogRow(LogType.Error, "Can't find player by name from other team: " + player.name);

					HighlightsHelper.SaveHighlightMaybe(player, frame, "INTERCEPTION");
				}
			}
		}

		private static async Task DelayedThrowEvent(g_Player originalPlayer, bool leftHanded, float underhandedness,
			float origSpeed)
		{
			// wait some time before re-checking the throw velocity
			await Task.Delay(100);

			g_Instance frame = lastFrame;

			foreach (var team in frame.teams)
			{
				foreach (var player in team.players)
				{
					if (player.playerid == originalPlayer.playerid)
					{
						if (player.possession && !frame.disc.velocity.ToVector3().Equals(Vector3.Zero))
						{
							matchData.Events.Add(new EventData(matchData, EventData.EventType.@throw, frame.game_clock,
								team, player, null, player.head.Position, frame.disc.velocity.ToVector3()));
							LogRow(LogType.File, frame.sessionid, frame.game_clock_display + " - " + player.name +
																  " threw the disk at " +
																  frame.disc.velocity.ToVector3().Length()
																	  .ToString("N2") + " m/s with their " +
																  (leftHanded ? "left" : "right") + " hand");
							matchData.currentDiskTrajectory.Clear();

							// add throw data type
							matchData.Throws.Add(new ThrowData(matchData, frame.game_clock, player,
								frame.disc.position.ToVector3(), frame.disc.velocity.ToVector3(), leftHanded,
								underhandedness));

							//if (Settings.Default.throwSpeedTTS)
							//{
							//	if (player.name == frame.client_name)
							//	{
							//		synth.SpeakAsync("" + player.velocity.ToVector3().Length().ToString("N1"));
							//	}
							//}
						}
						else
						{
							Console.WriteLine("Disc already caught");
						}
					}
				}
			}
		}

		/// <summary>
		/// Gets the server IP from the logs
		/// </summary>
		/// <returns></returns>
		private static string GetServerIP()
		{
			if (Settings.Default.echoVRPath != "")
			{
				try
				{
					string logPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Settings.Default.echoVRPath),
						"..", "..", "_local", "r14logs"));
					List<string> logs = Directory.GetFiles(logPath)
						.Where(f => !f.Contains("_json") && f.Contains(".log")).ToList();
					logs.Sort();
					string file = logs.Last();
					Stream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					StreamReader streamReader = new StreamReader(stream);
					string logData = streamReader.ReadToEnd();
					string searchStr = "[NSLOBBY] connected to host peer [";
					int loc = logData.LastIndexOf(searchStr);
					int endLoc = logData.IndexOf(":", loc);
					return logData.Substring(loc + searchStr.Length, endLoc - (loc + searchStr.Length));
				}
				catch (Exception e)
				{
					LogRow(LogType.Error, "Failed to read log file for server ip.\n" + e);
					return "";
				}
			}

			return "";
		}

		/// <summary>
		/// Function used to excute certain behavior based on frame given and previous frame(s).
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="deltaTime"></param>
		private static void ProcessGameStateChange(g_Instance frame, float deltaTime)
		{
			LogRow(LogType.File, frame.sessionid, frame.game_clock_display + " - Entered state: " + frame.game_status);

			switch (frame.game_status)
			{
				case "pre_match":
					// if we just came from a playing state, this was a reset - requires a high enough polling rate
					if (lastFrame.game_status == "playing" || lastFrame.game_status == "round_start")
					{
						g_Instance frameToUse = lastLastFrame;
						if (lastValidSumOfStatsAge < 30)
						{
							frameToUse = lastValidStatsFrame;
						}

						EventMatchFinished(frameToUse, MatchData.FinishReason.reset, lastFrame.game_clock);
					}

					break;

				// round began
				case "round_start":
					inPostMatch = false;

					// if we just started a new 'round' (so stats haven't been reset)
					if (lastFrame.game_status == "round_over")
					{
						if (!READ_FROM_FILE)
						{
							UpdateStatsIngame(frame, false, false);
						}

						foreach (MatchPlayer player in matchData.players.Values)
						{
							g_Player p = new g_Player
							{
								userid = player.Id
							};

							// TODO isn't this just a shallow copy anyway and won't do anything? How is this working?
							MatchPlayer lastPlayer = lastMatchData.GetPlayerData(p);

							if (lastPlayer != null)
							{
								player.StoreLastRoundStats(lastPlayer);
							}
							else
							{
								LogRow(LogType.Error, "Player exists in this round but not in last. Y");
							}
						}
					}

					if (!READ_FROM_FILE)
					{
						UpdateStatsIngame(frame);
					}

					break;

				// round really began
				case "playing":

					#region Started Playing

					if (!READ_FROM_FILE)
					{
						UpdateStatsIngame(frame);
					}

					// Autofocus
					if (Settings.Default.isAutofocusEnabled) {
						FocusEchoVR();
					}

					// Loop through teams.
					foreach (var team in frame.teams)
					{
						// Loop through players on team.
						foreach (var player in team.players)
						{
							// reset playspace
							MatchPlayer playerData = matchData.GetPlayerData(player);
							if (playerData != null)
							{
								playerData.playspaceLocation = player.head.Position;
							}
						}
					}

					// start a joust detection
					if (lastFrame.game_status == "round_start")
					{
						float zDiscPos = frame.disc.position.ToVector3().Z;
						// if the disc is in the center of the arena, neutral joust
						if (Math.Abs(zDiscPos) < .1f)
						{
							_ = JoustDetection(frame, EventData.EventType.joust_speed, TeamColor.blue);
							_ = JoustDetection(frame, EventData.EventType.joust_speed, TeamColor.orange);
						}

						// if the disc is on the orange nest
						else if (Math.Abs(zDiscPos + 27.5f) < 1)
						{
							_ = JoustDetection(frame, EventData.EventType.defensive_joust, TeamColor.orange);
						}


						// if the disc is on the blue nest
						else if (Math.Abs(zDiscPos - 27.5f) < 1)
						{
							_ = JoustDetection(frame, EventData.EventType.defensive_joust, TeamColor.blue);
						}
					}

					#endregion

					break;

				// just scored
				case "score":

					#region Process Score

					_ = ProcessScore(matchData);

					#endregion

					break;

				case "round_over":
					if (frame.blue_points == frame.orange_points)
					{
						// OVERTIME
						LogRow(LogType.Info, "overtime");
					}
					// mercy win
					else if (!frame.last_score.Equals(lastFrame.last_score))
					{
						// TODO check if the score actually changes the same frame the game ends
						_ = ProcessScore(matchData);

						EventMatchFinished(frame, MatchData.FinishReason.mercy, lastFrame.game_clock);
					}
					else if (frame.game_clock == 0 || lastFrame.game_clock < deltaTime * 10 || deltaTime < 0)
					{
						EventMatchFinished(frame, MatchData.FinishReason.game_time, 0);
					}
					else if (lastFrame.game_clock < deltaTime * 10 || lastFrame.game_status == "post_sudden_death" ||
							 deltaTime < 0)
					{
						// TODO add the score that ends an overtime
						// ProcessScore(frame); 

						// TODO find why finished and set reason
						EventMatchFinished(frame, MatchData.FinishReason.not_finished, lastFrame.game_clock);
					}
					else
					{
						EventMatchFinished(frame, MatchData.FinishReason.not_finished, lastFrame.game_clock);
					}

					break;

				// Game finished and showing scoreboard
				case "post_match":
					if (frame.private_match && Settings.Default.whenToSplitReplays == 1)
					{
						NewFilename();
					}

					//EventMatchFinished(frame, MatchData.FinishReason.not_finished);
					break;

				case "pre_sudden_death":
					LogRow(LogType.Error, "pre_sudden_death");
					break;
				case "sudden_death":
					// this happens right as the match finishes in a tie
					matchData.overtimeCount++;
					break;
				case "post_sudden_death":
					LogRow(LogType.Error, "post_sudden_death");
					break;
			}
		}

		private static bool WasStealNearGoal(Vector3 disckPos, TeamColor playerTeamColor, g_Instance frame)
		{
			float stealSaveRadius = 2.2f;

			float goalXPos = 0f;
			float goalYPos = 0f;
			float goalZPos = 36f;
			if (playerTeamColor == TeamColor.blue)
			{
				goalZPos *= -1f;
			}

			int x1 = (int)Math.Pow((disckPos.X - goalXPos), 2);
			int y1 = (int)Math.Pow((disckPos.Y - goalYPos), 2);
			int z1 = (int)Math.Pow((disckPos.Z - goalZPos), 2);

			// distance between the 
			// centre and given point 
			float distanceFromGoalCenter = x1 + y1 + z1;
			//LogRow(LogType.File, frame.sessionid, "Steal distance from goal: " + distanceFromGoalCenter + " X" + disckPos.X + " Y" + disckPos.Y + " Z" + disckPos.Z + " " + playerTeamColor);
			if (distanceFromGoalCenter > (stealSaveRadius * stealSaveRadius))
			{
				return false;
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// Function used to extracted data from frame given
		/// Delays a bit to avoid disk extrapolation
		/// </summary>
		private static async Task ProcessScore(MatchData matchData)
		{
			g_Instance initialFrame = lastLastFrame;

			// wait some time before re-checking the throw velocity
			await Task.Delay(100);

			g_Instance frame = lastFrame;

			Vector3 discVel = initialFrame.disc.velocity.ToVector3();
			Vector3 discPos = initialFrame.disc.position.ToVector3();
			Vector2 goalPos;
			bool backboard = false;
			float angleIntoGoal = 0;
			if (discVel != Vector3.Zero)
			{
				float angleIntoGoalRad =
					(float)(Math.Acos(Vector3.Dot(discVel, new Vector3(0, 0, 1) * (discPos.Z < 0 ? -1 : 1)) /
									   discVel.Length()));
				angleIntoGoal = (float)(angleIntoGoalRad * (180 / Math.PI));

				// make the angle negative if backboard
				if (angleIntoGoal > 90)
				{
					angleIntoGoal = 180 - angleIntoGoal;
					backboard = true;
				}
			}

			goalPos = new Vector2(initialFrame.disc.position.ToVector3().X, initialFrame.disc.position.ToVector3().Y);

			// nvidia highlights
			if (!HighlightsHelper.SaveHighlightMaybe(frame.last_score.person_scored, frame, "SCORE"))
			{
				HighlightsHelper.SaveHighlightMaybe(frame.last_score.assist_scored, frame, "ASSIST");
			}

			// Call the Score event
			LogRow(LogType.File, frame.sessionid,
				frame.game_clock_display + " - " + frame.last_score.person_scored + " scored at " +
				frame.last_score.disc_speed.ToString("N2") + " m/s from " +
				frame.last_score.distance_thrown.ToString("N2") + " m away" +
				(frame.last_score.assist_scored == "[INVALID]"
					? "!"
					: (", assisted by " + frame.last_score.assist_scored + "!")));
			LogRow(LogType.File, frame.sessionid,
				frame.game_clock_display + " - Goal angle: " + angleIntoGoal.ToString("N2") + "deg, from " +
				(backboard ? "behind" : "the front"));

			// show the scores in the log
			LogRow(LogType.File, frame.sessionid,
				frame.game_clock_display + " - ORANGE: " + frame.orange_points + "  BLUE: " + frame.blue_points);

			if (Settings.Default.goalDistanceTTS)
			{
				synth.SpeakAsync(frame.last_score.distance_thrown.ToString("N1") + " meters");
			}

			if (Settings.Default.goalSpeedTTS)
			{
				synth.SpeakAsync(frame.last_score.disc_speed.ToString("N1") + " meters per second");
			}

			g_Player scorer = frame.GetPlayer(frame.last_score.person_scored);
			var scorerPlayerData = matchData.GetPlayerData(scorer);
			if (scorerPlayerData != null)
			{
				if (frame.last_score.point_amount == 2)
				{
					scorerPlayerData.TwoPointers++;
				}
				else
				{
					scorerPlayerData.ThreePointers++;
				}

				scorerPlayerData.GoalsNum++;
			}

			// these are nullable types
			bool? leftHanded = null;
			float? underhandedness = null;
			if (matchData.Throws.Count > 0)
			{
				var lastThrow = matchData.Throws.Last();
				if (lastThrow != null)
				{
					leftHanded = lastThrow.isLeftHanded;
					underhandedness = lastThrow.underhandedness;
				}
			}

			GoalData goalEvent = new GoalData(
				matchData,
				scorer,
				frame.last_score,
				frame.game_clock,
				goalPos,
				angleIntoGoal,
				backboard,
				discPos.Z < 0 ? TeamColor.blue : TeamColor.orange,
				leftHanded,
				underhandedness,
				matchData.currentDiskTrajectory
			);
			matchData.Goals.Add(goalEvent);
			lastGoals.Enqueue(goalEvent);
			if (lastGoals.Count > 20)
			{
				lastGoals.TryDequeue(out var goal);
			}

			// Upload to Firebase 🔥
			_ = DoUploadEventFirebase(matchData, goalEvent);

			UpdateStatsIngame(frame);
		}

		/// <summary>
		/// Can be called often to update the ingame player stats
		/// </summary>
		/// <param name="frame">The current frame</param>
		public static void UpdateStatsIngame(g_Instance frame, bool endOfMatch = false, bool allowUpload = true,
			bool manual = false)
		{
			if (inPostMatch)
			{
				return;
			}

			// team names may have changed
			matchData.teams[TeamColor.blue].teamName = frame.teams[0].team;
			matchData.teams[TeamColor.orange].teamName = frame.teams[1].team;
			matchData.teams[TeamColor.spectator].teamName = frame.teams[2].team;

			if (frame.teams[0].stats != null)
			{
				matchData.teams[TeamColor.blue].points = frame.blue_points;
			}

			if (frame.teams[1].stats != null)
			{
				matchData.teams[TeamColor.orange].points = frame.orange_points;
			}

			UpdateAllPlayers(frame);

			// if end of match upload
			if (endOfMatch)
			{
				UploadMatchBatch(true);
			}
			// if during-match upload
			else if (manual || !Personal)
			{
				UploadMatchBatch(false);
			}
		}

		/// <summary>
		/// Function to wrap up the match once we've entered post_match, restarted, or left spectate unexpectedly (crash)
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="reason"></param>
		/// <param name="endTime"></param>
		private static void EventMatchFinished(g_Instance frame, MatchData.FinishReason reason, float endTime = 0)
		{
			matchData.endTime = endTime;
			matchData.finishReason = reason;

			if (frame == null)
			{
				// this happened on a restart right in the beginning once
				LogRow(LogType.Error, "frame is null on match finished event. INVESTIGATE");
				return;
			}

			LogRow(LogType.File, frame.sessionid, "Match Finished: " + reason);
			UpdateStatsIngame(frame, true);

			// if we here reset for public matches as well, then there would be super small files at the end of matches
			if (matchData.firstFrame.private_match && Settings.Default.whenToSplitReplays < 1)
			{
				// wait a little bit to actually split, so that the end of the match isn't cut off
				_ = DelayedNewFilename();
			}

			lastMatches.Enqueue(matchData);
			if (lastMatches.Count > 20)
			{
				lastMatches.TryDequeue(out var match);
			}

			lastMatchData = matchData;
			matchData = null;

			inPostMatch = true;

			// show the scores in the log
			LogRow(LogType.File, frame.sessionid,
				frame.game_clock_display + " - ORANGE: " + frame.orange_points + "  BLUE: " + frame.blue_points);
		}

		public static void UploadMatchBatch(bool final = false)
		{
			if (!Settings.Default.uploadToIgniteDB && !Settings.Default.uploadToFirestore)
			{
				Console.WriteLine("Won't upload right now.");
			}

			BatchOutputFormat data = new BatchOutputFormat();
			data.final = final;
			data.match_data = matchData.ToDict();
			matchData.players.Values.ToList().ForEach(e => data.match_players.Add(e.ToDict()));

			matchData.Events.ForEach(e =>
			{
				if (!e.inDB) data.events.Add(e.ToDict());
				e.inDB = true;
			});
			matchData.Goals.ForEach(e =>
			{
				if (!e.inDB) data.goals.Add(e.ToDict());
				e.inDB = true;
			});
			matchData.Throws.ForEach(e =>
			{
				if (!e.inDB) data.throws.Add(e.ToDict());
				e.inDB = true;
			});

			string dataString = JsonConvert.SerializeObject(data);
			string hash;
			using (SHA256 sha = SHA256.Create())
			{
				var rawHash = sha.ComputeHash(Encoding.ASCII.GetBytes(dataString + matchData.firstFrame.client_name));

				// Convert the byte array to hexadecimal string
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < rawHash.Length; i++)
				{
					sb.Append(rawHash[i].ToString("X2"));
				}

				hash = sb.ToString().ToLower();
			}

			if (Settings.Default.uploadToIgniteDB)
			{
				_ = DoUploadMatchBatchIgniteDB(dataString, hash, matchData.firstFrame.client_name);
			}
			// checks whether to upload or not are inside
			_ = DoUploadMatchBatchFirebase(data);
		}

		static async Task DoUploadMatchBatchIgniteDB(string data, string hash, string client_name)
		{
			client.DefaultRequestHeaders.Remove("x-api-key");
			client.DefaultRequestHeaders.Add("x-api-key", DiscordOAuth.igniteUploadKey);

			client.DefaultRequestHeaders.Remove("access-code");
			client.DefaultRequestHeaders.Add("access-code", DiscordOAuth.SeasonName);

			var content = new StringContent(data, Encoding.UTF8, "application/json");

			try
			{
				var response =
					await client.PostAsync("add_data?hashkey=" + hash + "&client_name=" + client_name, content);
				LogRow(LogType.Info, "[DB][Response] " + response.Content.ReadAsStringAsync().Result);
			}
			catch
			{
				LogRow(LogType.Error, "Can't connect to the DB server");
			}
		}

		static async Task DoUploadEventFirebase(MatchData matchData, GoalData goalData)
		{
#if INCLUDE_FIRESTORE
			if (Settings.Default.uploadToFirestore && !Personal)
			{
				if (!TryCreateFirebaseDB()) return;

				string season = DiscordOAuth.SeasonName;

				var match_data = matchData.ToDict();

				// update the match stats
				CollectionReference matchesRef = db.Collection("series/" + season + "/match_stats");
				DocumentReference matchDataRef =
					matchesRef.Document(match_data["match_time"] + "_" + match_data["session_id"]);
				CollectionReference eventsRef = matchDataRef.Collection("events");

				try
				{
					Dictionary<string, object> data = goalData.ToDict();
					// add the event type, since this isn't a normal type of event
					data["event_type"] = "goal";
					DocumentReference eventRef = await eventsRef.AddAsync(data);
					LogRow(LogType.File, lastFrame.sessionid, "-- Uploading Event Data --");
				}
				catch (Exception e)
				{
					LogRow(LogType.Error, "Error uploading to firebase.\n" + e.Message + "\n" + e.StackTrace);
					throw;
				}
			}
#endif
		}

		static async Task DoUploadEventFirebase(MatchData matchData, EventData eventData)
		{
#if INCLUDE_FIRESTORE
			if (Settings.Default.uploadToFirestore && !Personal)
			{
				if (!TryCreateFirebaseDB()) return;

				string season = DiscordOAuth.SeasonName;

				var match_data = matchData.ToDict();

				// update the match stats
				CollectionReference matchesRef = db.Collection("series/" + season + "/match_stats");
				DocumentReference matchDataRef =
					matchesRef.Document(match_data["match_time"] + "_" + match_data["session_id"]);
				CollectionReference eventsRef = matchDataRef.Collection("events");

				try
				{
					DocumentReference eventRef = await eventsRef.AddAsync(eventData.ToDict());
					LogRow(LogType.File, lastFrame.sessionid, "-- Uploading Event Data --");
				}
				catch (Exception e)
				{
					LogRow(LogType.Error, "Error uploading to firebase.\n" + e.Message + "\n" + e.StackTrace);
					throw;
				}
			}
#endif
		}


		static async Task DoUploadMatchBatchFirebase(BatchOutputFormat data)
		{
#if INCLUDE_FIRESTORE
			if (Settings.Default.uploadToFirestore && !Personal)
			{
				if (!TryCreateFirebaseDB()) return;

				WriteBatch batch = db.StartBatch();

				string season = DiscordOAuth.SeasonName;

				// update the cumulative player stats
				CollectionReference playersRef = db.Collection("series/" + season + "/player_stats");
				foreach (Dictionary<string, object> p in data.match_players)
				{
					DocumentReference playerRef = playersRef.Document(p["player_name"].ToString());

					batch.Set(playerRef, p, SetOptions.MergeAll);
				}

				// update the match stats
				CollectionReference matchesRef = db.Collection("series/" + season + "/match_stats");
				DocumentReference matchDataRef =
					matchesRef.Document(data.match_data["match_time"] + "_" + data.match_data["session_id"]);
				data.match_data["upload_timestamp"] = DateTime.UtcNow;
				batch.Set(matchDataRef, data.match_data, SetOptions.MergeAll);

				// update the match players
				foreach (var p in data.match_players)
				{
					DocumentReference matchPlayerRef =
						matchDataRef.Collection("players").Document(p["player_name"].ToString());
					batch.Set(matchPlayerRef, p, SetOptions.MergeAll);
				}

				try
				{
					await batch.CommitAsync();
					LogRow(LogType.File, lastFrame.sessionid, "-- Uploading Data --");
				}
				catch (Exception e)
				{
					LogRow(LogType.Error, "Error uploading to firebase.\n" + e.Message + "\n" + e.StackTrace);
					throw;
				}
			}
#endif
		}

		static bool TryCreateFirebaseDB()
		{
			if (db != null)
			{
				return true;
			}
			else if (DiscordOAuth.firebaseCred != null)
			{
				try
				{
					var builder = new FirestoreClientBuilder { JsonCredentials = DiscordOAuth.firebaseCred };
					db = FirestoreDb.Create("ignitevr-echostats", builder.Build());
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}

		// Update existing player with stats from game.
		private static void UpdateSinglePlayer(g_Instance frame, g_Team team, g_Player player, int won)
		{
			if (!matchData.teams.ContainsKey(team.color))
			{
				LogRow(LogType.Error, "No team. Wat.");
				return;
			}

			if (player.stats == null)
			{
				LogRow(LogType.Error, "Player stats are null. Maybe in lobby?");
				return;
			}

			TeamData teamData = matchData.teams[team.color];

			// add a new match player if they didn't exist before
			if (!matchData.players.ContainsKey(player.userid))
			{
				matchData.players.Add(player.userid, new MatchPlayer(matchData, teamData, player));
			}

			MatchPlayer playerData = matchData.players[player.userid];
			playerData.teamData = teamData;

			playerData.Level = player.level;
			playerData.Number = player.number;
			playerData.PossessionTime = player.stats.possession_time;
			playerData.Points = player.stats.points;
			playerData.ShotsTaken = player.stats.shots_taken;
			playerData.Saves = player.stats.saves;
			// playerData.GoalsNum = player.stats.goals;	// disabled in favor of manual increment because the api is broken here
			// playerData.Passes = player.stats.passes;		// api reports 0
			// playerData.Catches = player.stats.catches;  	// api reports 0
			playerData.Steals = player.stats.steals;
			playerData.Stuns = player.stats.stuns;
			playerData.Blocks = player.stats.blocks; // api reports 0
													 // playerData.Interceptions = player.stats.interceptions;	// api reports 0
			playerData.Assists = player.stats.assists;
			playerData.Won = won;
		}

		static void UpdateAllPlayers(g_Instance frame)
		{
			// Loop through teams.
			foreach (g_Team team in frame.teams)
			{
				// Loop through players on team.
				foreach (g_Player player in team.players)
				{
					TeamColor winningTeam = frame.blue_points > frame.orange_points ? TeamColor.blue : TeamColor.orange;
					int won = team.color == winningTeam ? 1 : 0;

					UpdateSinglePlayer(frame, team, player, won);
				}
			}
		}


		/// <summary>
		/// Generates a new filename from the current time.
		/// </summary>
		public static void NewFilename()
		{
			lock (fileWritingLock)
			{
				string lastFilename = fileName;
				fileName = DateTime.Now.ToString("rec_yyyy-MM-dd_HH-mm-ss");

				// compress the file
				if (useCompression)
				{
					if (File.Exists(Path.Combine(saveFolder, lastFilename + ".echoreplay")))
					{
						string tempDir = Path.Combine(saveFolder, "temp_zip");
						Directory.CreateDirectory(tempDir);
						File.Move(Path.Combine(saveFolder, lastFilename + ".echoreplay"),
							Path.Combine(saveFolder, "temp_zip",
								lastFilename + ".echoreplay")); // TODO can fail because in use
						ZipFile.CreateFromDirectory(tempDir, Path.Combine(saveFolder, lastFilename + ".echoreplay"));
						Directory.Delete(tempDir, true);
					}
				}
			}
		}

		private static async Task DelayedNewFilename()
		{
			// wait some time before calling
			await Task.Delay(5000);

			NewFilename();
		}

		public enum AuthCode
		{
			network_error,
			denied,
			approved
		}

		[Obsolete("delete soon")]
		public static AuthCode CheckAccessCode(string accessCode)
		{
			if (accessCode == "") return AuthCode.denied;

			try
			{
				WebRequest request = WebRequest.Create(APIURL + "ignitebot_auth?key=" + accessCode);

				// Get the response.
				WebResponse response = request.GetResponse();

				// Display the status.
				Console.WriteLine(((HttpWebResponse)response).StatusDescription);

				string responseFromServer;

				// Get the stream containing content returned by the server.
				// The using block ensures the stream is automatically closed.
				using (Stream dataStream = response.GetResponseStream())
				{
					// Open the stream using a StreamReader for easy access.
					StreamReader reader = new StreamReader(dataStream);
					// Read the content.
					responseFromServer = reader.ReadToEnd();
				}

				// Close the response.
				response.Close();

				// Display the content.
				Dictionary<string, string> respObj =
					JsonConvert.DeserializeObject<Dictionary<string, string>>(responseFromServer);

				if (respObj["auth"] != "true") return AuthCode.denied;

				currentAccessCodeUsername = respObj["username"];
				//currentSeasonName = respObj["season_name"];

				client.DefaultRequestHeaders.Remove("access-code");
				client.DefaultRequestHeaders.Add("access-code", respObj["season_name"]);

				return AuthCode.approved;
			}
			catch
			{
				LogRow(LogType.Error, "Can't connect to the DB server");
				return AuthCode.network_error;
			}
		}


		/// <summary>
		/// Generic method for getting data from a web url
		/// </summary>
		/// <param name="headers">Key-value pairs for headers. Leave null if none.</param>
		public static async Task GetAsync(string uri, Dictionary<string, string> headers, Action<string> callback)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
				if (headers != null)
				{
					foreach (var header in headers)
					{
						request.Headers[header.Key] = header.Value;
					}
				}
				using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
				using Stream stream = response.GetResponseStream();
				using StreamReader reader = new StreamReader(stream);

				callback(await reader.ReadToEndAsync());
			}
			catch (Exception e)
			{
				Console.WriteLine($"Can't get data\n{e}");
				callback("");
			}
		}

		/// <summary>
		/// Generic method for posting data to a web url
		/// </summary>
		/// <param name="headers">Key-value pairs for headers. Leave null if none.</param>
		public static async Task PostAsync(string uri, Dictionary<string, string> headers, string body, Action<string> callback)
		{
			try
			{
				//FormUrlEncodedContent content = new FormUrlEncodedContent(values);
				if (headers != null)
				{
					foreach (KeyValuePair<string, string> header in headers)
					{
						client.DefaultRequestHeaders.Remove(header.Key);
						client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}
				var content = new StringContent(body, Encoding.UTF8, "application/json");
				HttpResponseMessage response = await client.PostAsync(uri, content);
				string responseString = await response.Content.ReadAsStringAsync();

				callback(responseString);
			}
			catch (Exception e)
			{
				Console.WriteLine($"Can't post data\n{e}");
				callback("");
			}
		}


		public static JToken ReadEchoVRSettings()
		{
			try
			{
				string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rad",
				"loneecho", "settings_mp_v2.json");
				if (!File.Exists(file))
				{
					LogRow(LogType.Error, "Can't find the EchoVR settings file");
					return null;
				}

				return JsonConvert.DeserializeObject<JToken>(File.ReadAllText(file));
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, "Error when reading Arena settings.\n" + e.ToString());
			}
			return null;
		}

		public static void WriteEchoVRSettings(JToken settings)
		{
			try
			{
				string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "rad",
					"loneecho", "settings_mp_v2.json");
				if (!File.Exists(file))
				{
					throw new NullReferenceException("Can't find the EchoVR settings file");
				}

				var settingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);
				File.WriteAllText(file, settingsString);
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, "Error when writing Arena settings.\n" + e.ToString());
			}
		}


		public static void RegisterUriScheme(string UriScheme, string FriendlyName)
		{
			try
			{
				using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme);
				string applicationLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IgniteBot.exe");

				key.SetValue("", "URL:" + FriendlyName);
				key.SetValue("URL Protocol", "");

				using RegistryKey defaultIcon = key.CreateSubKey("DefaultIcon");
				defaultIcon.SetValue("", applicationLocation + ",1");

				using RegistryKey commandKey = key.CreateSubKey(@"shell\open\command");
				commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
			}
			catch (Exception e)
			{
				LogRow(LogType.Error, "Failed to set URI scheme");
			}
		}

		public static bool CheckIfLaunchedWithCustomURLHandlerParam(string[] args)
		{
			if (args.Length <= 0 || (!args[0].Contains("ignitebot://") && !args[0].Contains("atlas://"))) return false;

			// join a match directly	
			string[] parts = args[0].Split('/');
			if (parts.Length != 4)
			{
				LogRow(LogType.Error, "ERROR 3452. Incorrectly formatted IgniteBot or Atlas link");
				new MessageBox(
					$"Incorrectly formatted IgniteBot or Atlas link: wrong number of '/' characters for link:\n{args[0]}\n{parts.Length}",
					"Error", Quit).Show();
			}

			bool spectating = false;
			switch (parts[2])
			{
				case "spectate":
				case "spectator":
				case "s":
					spectating = true;
					break;
				case "join":
				case "player":
				case "j":
				case "p":
					spectating = false;
					break;
				case "choose":
				case "c":
					// hand the whole thing off to the popup window
					new ChooseJoinTypeDialog(parts[3]).Show();
					return true;
				default:
					LogRow(LogType.Error, "ERROR 8675. Incorrectly formatted IgniteBot or Atlas link");
					new MessageBox("Incorrectly formatted IgniteBot or Atlas link: Incorrect join type.", "Error", Quit)
						.Show();
					return true;
			}


			// start client
			string echoPath = Settings.Default.echoVRPath;
			if (!string.IsNullOrEmpty(echoPath))
			{
				Process.Start(echoPath, (Settings.Default.capturevp2 ? "-capturevp2 " : " ") + (spectating ? "-spectatorstream " : " ") + "-lobbyid " + parts[3]);
			}
			else
			{
				new MessageBox(
					"EchoVR exe path not set. Run the IgniteBot while in a lobby or game with the API enabled at least once first.",
					"Error", Quit).Show();
			}

			return true;
		}

		// The max number of physical addresses.
		const int MAXLEN_PHYSADDR = 8;

		// Define the MIB_IPNETROW structure.
		[StructLayout(LayoutKind.Sequential)]
		struct MIB_IPNETROW
		{
			[MarshalAs(UnmanagedType.U4)]
			public int dwIndex;
			[MarshalAs(UnmanagedType.U4)]
			public int dwPhysAddrLen;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac0;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac1;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac2;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac3;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac4;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac5;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac6;
			[MarshalAs(UnmanagedType.U1)]
			public byte mac7;
			[MarshalAs(UnmanagedType.U4)]
			public int dwAddr;
			[MarshalAs(UnmanagedType.U4)]
			public int dwType;
		}
		[DllImport("iphlpapi.dll", ExactSpelling = true)]
		public static extern int SendARP(int DestIP, int SrcIP, [Out] byte[] pMacAddr, ref int PhyAddrLen);

		public static IPAddress QuestIP = null;
		public static bool IPPingThread1Done = false;
		public static bool IPPingThread2Done = false;

		public static void GetCurrentIPAndPingNetwork()
		{
			foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces().Where(ni => ni.OperationalStatus == OperationalStatus.Up && (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)))
			{
				var addr = adapter.GetIPProperties().GatewayAddresses.FirstOrDefault();
				if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
				{
					foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
					{
						if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							Console.WriteLine("PC IP Address: " + unicastIPAddressInformation.Address);
							Console.Write("PC Subnet Mask: " + unicastIPAddressInformation.IPv4Mask + "\n Searching for Quest on network...");
							PingNetworkIPs(unicastIPAddressInformation.Address, unicastIPAddressInformation.IPv4Mask);
						}
					}
				}
			}
		}
		public static async void PingIPList(List<IPAddress> IPs, int threadID)
		{
			var tasks = IPs.Select(ip => new Ping().SendPingAsync(ip, 4000));
			var results = await Task.WhenAll(tasks);
			switch (threadID)
			{
				case 1:
					IPPingThread1Done = true;
					break;
				case 2:
					IPPingThread2Done = true;
					break;
				default:
					break;
			}
		}
		public static void PingNetworkIPs(IPAddress address, IPAddress mask)
		{
			uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
			uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
			uint broadCastIpAddress = ipAddress | ~ipMaskV4;

			IPAddress start = new IPAddress(BitConverter.GetBytes(broadCastIpAddress));

            var bytes = start.GetAddressBytes();
			var leastSigByte = address.GetAddressBytes().Last();
			var range = 255 - leastSigByte;

			var pingReplyTasks = Enumerable.Range(leastSigByte, range)
				.Select(x => {
					var bb = start.GetAddressBytes();
					bb[3] = (byte)x;
					var destIp = new IPAddress(bb);
					return destIp;
				})
				.ToList();
			var pingReplyTasks2 = Enumerable.Range(0, leastSigByte - 1)
				.Select(x => {

					var bb = start.GetAddressBytes();
					bb[3] = (byte)x;
					var destIp = new IPAddress(bb);
					return destIp;
				})
				.ToList();
			IPSearchthread1 = new Thread(new ThreadStart(() => PingIPList(pingReplyTasks, 1)));
			IPSearchthread2 = new Thread(new ThreadStart(() => PingIPList(pingReplyTasks2, 2)));
			IPPingThread1Done = false;
			IPPingThread2Done = false;
			IPSearchthread1.Start();
			IPSearchthread2.Start();
        }

		// Declare the GetIpNetTable function.
		[DllImport("IpHlpApi.dll")]
		[return: MarshalAs(UnmanagedType.U4)]
		static extern int GetIpNetTable(
		   IntPtr pIpNetTable,
		   [MarshalAs(UnmanagedType.U4)]
		 ref int pdwSize,
		   bool bOrder);

		[DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
		internal static extern int FreeMibTable(IntPtr plpNetTable);

		// The insufficient buffer error.
		const int ERROR_INSUFFICIENT_BUFFER = 122;
		static IntPtr buffer;

		static void CheckARPTable()
		{

			int bytesNeeded = 0;

			// The result from the API call.
			int result = GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

			// Call the function, expecting an insufficient buffer.
			if (result != ERROR_INSUFFICIENT_BUFFER)
			{
				// Throw an exception.
				throw new Exception();
			}

			// Allocate the memory, do it in a try/finally block, to ensure
			// that it is released.
			buffer = IntPtr.Zero;
			// Allocate the memory.
			buffer = Marshal.AllocCoTaskMem(bytesNeeded);

			// Make the call again. If it did not succeed, then
			// raise an error.
			result = GetIpNetTable(buffer, ref bytesNeeded, false);

			// If the result is not 0 (no error), then throw an exception.
			if (result != 0)
			{
				// Throw an exception.
				throw new Exception();
			}

			// Now we have the buffer, we have to marshal it. We can read
			// the first 4 bytes to get the length of the buffer.
			int entries = Marshal.ReadInt32(buffer);

			// Increment the memory pointer by the size of the int.
			IntPtr currentBuffer = new IntPtr(buffer.ToInt64() +
				Marshal.SizeOf(typeof(int)));

			// Allocate an array of entries.
			MIB_IPNETROW[] table = new MIB_IPNETROW[entries];

			// Cycle through the entries.
			for (int index = 0; index < entries; index++)
			{
				// Call PtrToStructure, getting the structure information.
				table[index] = (MIB_IPNETROW)Marshal.PtrToStructure(new
					IntPtr(currentBuffer.ToInt64() + (index *
					Marshal.SizeOf(typeof(MIB_IPNETROW)))), typeof(MIB_IPNETROW));
			}

			for (int index = 0; index < entries; index++)
			{
				MIB_IPNETROW row = table[index];

				if (row.mac0 == 0x2C && row.mac1 == 0x26 && row.mac2 == 0x17)
				{
					QuestIP = new IPAddress(BitConverter.GetBytes(row.dwAddr));
					break;
				}

			}
		}

        public static string[] GetLatestSpeakerSystemURLVer()
        {
            string[] ret = new string[2];
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(@"https://api.github.com/repos/iblowatsports/Echo-VR-Speaker-System/releases/latest");
				req.Accept = "application/json";
				req.UserAgent = "foo";

				var resp = req.GetResponse();
				Stream ds = resp.GetResponseStream();
				StreamReader sr = new StreamReader(ds);

				// Session Contents
				string textResp = sr.ReadToEnd();
				VersionJson verionJson = JsonConvert.DeserializeObject<VersionJson>(textResp);
				ret[0] = verionJson.assets.First(url => url.browser_download_url.EndsWith("zip")).browser_download_url;
				ret[1] = verionJson.tag_name;
			}
			catch(Exception e) {
				LogRow(LogType.Error, e.Message);
			}
            return ret;
        }

        public static void InstallSpeakerSystem(IProgress<string> progress)
        {
			try
			{
				string[] SpeakerSystemURLVer = GetLatestSpeakerSystemURLVer();
				System.Diagnostics.Process process = new System.Diagnostics.Process();
				System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
				startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				startInfo.FileName = "cmd.exe";
				startInfo.Arguments = "/C \"" + AppContext.BaseDirectory + "\\InstallEchoSpeakerSystem.bat\" " + SpeakerSystemURLVer[0] + " " + SpeakerSystemURLVer[1] + " 0";
				startInfo.Verb = "runas";
				startInfo.UseShellExecute = true;
				process.StartInfo = startInfo;
				process.Start();
				//process.WaitForExit(15000);
				//Thread.Sleep(20);
				int count = 0;
				string SpeakerSystemInstallLabel = "Installing Echo Speaker System";
				string statusDots = "";
				while (!process.HasExited && count < 6000) //Time out after 5 mins
				{
					if (count % 16 == 0)
					{
						statusDots = "";
					}
					else if (count % 4 == 0)
					{
						statusDots += ".";
					}
					count++;

					progress.Report(SpeakerSystemInstallLabel + statusDots);
					Thread.Sleep(50);
				}
				if (!process.HasExited)
				{
					process.Kill();
					progress.Report("Echo Speaker System install failed!");
				}
				else if (true)
				{
					progress.Report("Echo Speaker System installed successfully!");
				}
				int code = process.ExitCode;
				InstalledSpeakerSystemVersion = FindEchoSpeakerSystemInstallVersion();
				IsSpeakerSystemUpdateAvailable = false;
            }
            catch
            {
				InstalledSpeakerSystemVersion = FindEchoSpeakerSystemInstallVersion();
				IsSpeakerSystemUpdateAvailable = false;
				progress.Report("Echo Speaker System install failed!");
			}
		}

		public static void ClearARPCache()
        {
			try
			{
				System.Diagnostics.Process process = new System.Diagnostics.Process();
				System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
				startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				startInfo.FileName = "cmd.exe";
				startInfo.Arguments = "/C netsh interface ip delete arpcache";
				startInfo.Verb = "runas";
				startInfo.UseShellExecute = true;
				process.StartInfo = startInfo;
				process.Start();
				process.WaitForExit(500);
				Thread.Sleep(20);
            }
            catch { }
		}
		/// <summary>
		/// Finds a Quest local IP address on the same network
		/// </summary>
		/// <returns>The IP address</returns>
		public static string FindQuestIP(IProgress<string> progress)
		{
            try
            {
                string QuestStatusLabel = "Searching for Quest on network";
				QuestIP = null;
				ClearARPCache();
				CheckARPTable();
                int count = 0;
                string statusDots = "";
                if (QuestIP == null)
                {
                    GetCurrentIPAndPingNetwork();
                    while (QuestIP == null && (!IPPingThread1Done || !IPPingThread2Done))
                    {
                        if (count % 16 == 0)
						{
							statusDots = "";
						}
						else if (count % 4 == 0)
						{
							statusDots += ".";
						}
						count++;
                        progress.Report(QuestStatusLabel + statusDots);
                        Thread.Sleep(50);
                        CheckARPTable();
                    }
					IPSearchthread1 = null;
					IPSearchthread2 = null;
					if (QuestIP != null)
                    {
                        progress.Report("Found Quest on network!");
                    }
                    else
                    {
                        Thread.Sleep(1000);
                        CheckARPTable();
                        if (QuestIP != null)
                        {
                            progress.Report("Found Quest on network!");
                        }
                        else
                        {
                            progress.Report("Failed to find Quest on network!");
                        }
                    }
                }
                else
                {
                    progress.Report("Found Quest on network!");
                }

            }
            finally
            {
                // Release the memory.
                FreeMibTable(buffer);
            }
			Thread.Sleep(500);
            echoVRIP = QuestIP == null ? "127.0.0.1" : QuestIP.ToString();
			return echoVRIP;
		}

		/// <summary>
		/// This method is based on the python code that is used in the VRML Discord bot for calculating server score.
		/// </summary>
		/// <returns>The server score</returns>
		public static float CalculateServerScore(List<int> bluePings, List<int> orangePings)
		{
			// configurable parameters for tuning
			int min_ping = 10; // you don't lose points for being higher than this value
			int max_ping = 150; // won't compute if someone is over this number
			int ping_threshold = 100; // you lose extra points for being higher than this

			// points_distribution dictates how many points come from each area:
			//   0 - difference in sum of pings between teams
			//   1 - within-team variance
			//   2 - overall server variance
			//   3 - overall high/low pings for server
			int[] points_distribution = new int[] { 30, 30, 30, 10 };

			// determine max possible server/team variance and max possible sum diff,
			// given the min/max allowable ping
			float max_server_var = Variance(new float[]
				{min_ping, min_ping, min_ping, min_ping, max_ping, max_ping, max_ping, max_ping});
			float max_team_var = Variance(new float[] { min_ping, min_ping, max_ping, max_ping });
			float max_sum_diff = (4 * max_ping) - (4 * min_ping);

			// sanity check for ping values
			if (bluePings == null || bluePings.Count == 0 || orangePings == null || orangePings.Count == 0)
			{
				// Console.WriteLine("No player's ping can be over 150.");
				return -1;
			}
			if (bluePings.Max() > max_ping || orangePings.Max() > max_ping)
			{
				// Console.WriteLine("No player's ping can be over 150.");
				return -1;
			}



			// calculate points for sum diff
			float blueSum = bluePings.Sum();
			float orangeSum = orangePings.Sum();
			float sum_diff = MathF.Abs(blueSum - orangeSum);

			float sum_points = (1 - (sum_diff / max_sum_diff)) * points_distribution[0];

			// calculate points for team variances
			float blueVariance = Variance(bluePings);
			float orangeVariance = Variance(orangePings);

			float mean_var = new[] { blueVariance, orangeVariance }.Average();
			float team_points = (1 - (mean_var / max_team_var)) * points_distribution[1];

			// calculate points for server variance
			List<int> bothPings = new(bluePings);
			bothPings.AddRange(orangePings);

			float server_var = Variance(bothPings);

			float server_points = (1 - (server_var / max_server_var)) * points_distribution[2];

			// calculate points for high/low ping across server
			float hilo = ((blueSum + orangeSum) - (min_ping * 8)) / ((ping_threshold * 8) - (min_ping * 8));

			float hilo_points = (1 - hilo) * points_distribution[3];

			// add up points
			float final = sum_points + team_points + server_points + hilo_points;

			return final;
		}


		public static float Variance(IEnumerable<float> values)
		{
			float avg = values.Average();
			return values.Average(v => MathF.Pow(v - avg, 2));
		}

		public static float Variance(IEnumerable<int> values)
		{
			float avg = (float)values.Average();
			return values.Average(v => MathF.Pow(v - avg, 2));
		}

		// TODO
		public void ShowToast(string text, float duration = 3)
		{

		}

		internal static void Quit()
		{
			running = false;
			if (closingWindow != null)
			{
				// already trying to close
				return;
			}

			closingWindow = new ClosingDialog();
			closingWindow.Show();

			_ = GentleClose();
		}
	}

	/// <summary>
	/// Custom Vector3 class used to keep track of 3D coordinates.
	/// Works more like the Vector3 included with Unity now.
	/// </summary>
	public static class Vector3Extensions
	{
		public static Vector3 ToVector3(this List<float> input)
		{
			if (input.Count != 3)
			{
				throw new Exception("Can't convert List to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}

		public static Vector3 ToVector3(this float[] input)
		{
			if (input.Length != 3)
			{
				throw new Exception("Can't convert array to Vector3");
			}

			return new Vector3(input[0], input[1], input[2]);
		}

		public static float DistanceTo(this Vector3 v1, Vector3 v2)
		{
			return (float)Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
		}

		public static Vector3 Normalized(this Vector3 v1)
		{
			return v1 / v1.Length();
		}
	}
}
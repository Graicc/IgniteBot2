﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Windows;
using IgniteBot.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IgniteBot
{
	/// <summary>
	/// 🔑
	/// </summary>
	public class DiscordOAuth
	{
		/// <summary>
		/// Called when the program get's authenticated with Discord
		/// </summary>
		public static Action authenticated;


		const string REDIRECT_URI = "http://localhost:6722/oauth_login";

		static WebServer2 webServer;

		static HttpClient client = new HttpClient();

		public static string oauthToken = "";

		public static Dictionary<string, string> discordUserData;
		public static string DiscordUsername => discordUserData?["username"];
		public static string DiscordUserID => discordUserData?["id"];
		public static string DiscordPFPURL => $"https://cdn.discordapp.com/avatars/{discordUserData["id"]}/{discordUserData["avatar"]}";

		public static string igniteUploadKey = string.Empty;
		public static string firebaseCred;

		public static List<Dictionary<string, string>> availableAccessCodes = new List<Dictionary<string, string>>();
		private static readonly Dictionary<string, string> personalAccessCode = new Dictionary<string, string>()
		{
			{ "username","Personal" },
			{ "pw","personal" },
			{ "season_name","personal" }
		};
		public static Dictionary<string, string> CurrentKeys {
			get {
				foreach (var key in availableAccessCodes)
				{
					if (key["username"] == Program.currentAccessCodeUsername)
					{
						return key;
					}
				}
				return personalAccessCode;
			}
		}

		public static string AccessCode => CurrentKeys?["pw"];
		public static string SeasonName => CurrentKeys?["season_name"];

		public static bool IsLoggedIn { get => oauthToken != string.Empty; }

		public static int GetAccessCodeIndex(string hash)
		{
			for (int i = 0; i < availableAccessCodes.Count; i++)
			{
				if (SecretKeys.Hash(availableAccessCodes[i]["pw"]) == hash)
				{
					return i;
				}
			}
			return 0;
		}
		public static string GetAccessCodeUsername(string hash)
		{
			foreach (var key in availableAccessCodes)
			{
				if (SecretKeys.Hash(key["pw"]) == hash)
				{
					return key["username"];
				}
			}
			return personalAccessCode["username"];
		}

		public static void OAuthLogin(bool force = false)
		{
			string token = Settings.Default.discordOAuthRefreshToken;
			if (string.IsNullOrEmpty(token) || force)
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = SecretKeys.OAuthURL,
					UseShellExecute = true
				});

				webServer = new WebServer2(OAuthResponse, "http://localhost:6722/");
				webServer.Run();
			}
			else
			{
				OAuthLoginRefresh(token);
			}
		}

		internal static void Unlink()
		{
			oauthToken = string.Empty;
			discordUserData = null;
			availableAccessCodes.Clear();
			Settings.Default.discordOAuthRefreshToken = string.Empty;
			Settings.Default.Save();
		}

		private static string OAuthResponse(HttpListenerRequest request)
		{
			NameValueCollection queryStrings = HttpUtility.ParseQueryString(request.Url.Query);
			if (queryStrings["code"] != null)
			{
				OAuthLoginResponse(queryStrings["code"]);
				return "<html><body onload=\"javascript: close(); \">You can close this window</body></html>";
			}
			else
			{
				return "<html><body onload=\"javascript: close(); \">There was an error. Close this window and try again.</body></html>";
			}
		}

		public static async void OAuthLoginRefresh(string refresh_token)
		{
			Dictionary<string, string> postDataDict = new Dictionary<string, string>
			{
				{ "client_id", SecretKeys.CLIENT_ID },
				{ "client_secret", SecretKeys.CLIENT_SECRET },
				{ "grant_type", "refresh_token" },
				{ "refresh_token", refresh_token },
				{ "redirect_uri", REDIRECT_URI },
				{ "scope", "identify" }
			};

			HttpResponseMessage response = await client.PostAsync("https://discord.com/api/v6/oauth2/token", new FormUrlEncodedContent(postDataDict));

			if (!response.IsSuccessStatusCode)
			{
				RevertToPersonal();
			}
			else
			{
				if (webServer == null)
				{
					webServer = new WebServer2(OAuthResponse, "http://localhost:6722/");
					webServer.Run();
				}

				string responseString = await response.Content.ReadAsStringAsync();
				Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
				ProcessResponse(data);
			}

		}

		public static async void OAuthLoginResponse(string code)
		{

			Dictionary<string, string> postDataDict = new Dictionary<string, string>
			{
				{ "client_id", SecretKeys.CLIENT_ID },
				{ "client_secret", SecretKeys.CLIENT_SECRET },
				{ "grant_type", "authorization_code" },
				{ "code", code },
				{ "redirect_uri", REDIRECT_URI },
				{ "scope", "identify" }
			};

			HttpResponseMessage response = await client.PostAsync("https://discord.com/api/v6/oauth2/token", new FormUrlEncodedContent(postDataDict));
			string responseString = await response.Content.ReadAsStringAsync();
			Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
			ProcessResponse(data);
		}

		public static async void ProcessResponse(Dictionary<string, string> response)
		{
			if (response.ContainsKey("error"))
			{
				RevertToPersonal();
				Settings.Default.discordOAuthRefreshToken = string.Empty;
				oauthToken = string.Empty;
			}

			Settings.Default.discordOAuthRefreshToken = response["refresh_token"];
			Settings.Default.Save();
			oauthToken = response["access_token"];

			webServer.Stop();

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);
			HttpResponseMessage userResponse = await client.GetAsync("https://discord.com/api/v6/users/@me");

			string responseString = await userResponse.Content.ReadAsStringAsync();

			Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
			if (data.ContainsKey("username"))
			{
				discordUserData = data;
			}
			else
			{
				Console.WriteLine("Not Authorized");
			}

			// get the access codes for this user
			HttpResponseMessage accessCodesResponse = await client.GetAsync(SecretKeys.accessCodesURL + oauthToken);
			string accessCodesResponseString = await accessCodesResponse.Content.ReadAsStringAsync();

			Dictionary<string, JToken> accessCodesResponseData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(accessCodesResponseString);
			availableAccessCodes = accessCodesResponseData["keys"].ToObject<List<Dictionary<string, string>>>();
			if (accessCodesResponseData.ContainsKey("write"))
			{
				igniteUploadKey = accessCodesResponseData["write"].ToObject<string>();
			}
			if (accessCodesResponseData.ContainsKey("firebase_cred"))
			{
				firebaseCred = accessCodesResponseData["firebase_cred"].ToObject<string>();
			}

			availableAccessCodes.Insert(0, new Dictionary<string, string>
			{
				{ "pw", "personal" },
				{ "season_name", "personal" },
				{ "username", "Personal" }
			});

			Program.currentAccessCodeUsername = GetAccessCodeUsername(Settings.Default.accessCode);


			authenticated?.Invoke();
		}

		public static void RevertToPersonal()
		{
			// revert to personal
			Program.currentAccessCodeUsername = personalAccessCode["username"];
			Settings.Default.accessCode = SecretKeys.Hash(personalAccessCode["pw"]);
			Settings.Default.Save();
		}
	}
}

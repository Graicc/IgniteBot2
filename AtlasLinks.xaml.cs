﻿using IgniteBot.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace IgniteBot
{
	/// <summary>
	/// Interaction logic for AtlasLinks.xaml
	/// </summary>
	public partial class AtlasLinks : Window
	{
		private bool initialized = false;
		private bool onlyCasters = false;

		private readonly Timer outputUpdateTimer = new Timer();

		public AtlasLinks()
		{
			InitializeComponent();


			alternateIPTextBox.Text = Settings.Default.echoVRIP;
			ipSourceDropdown.SelectedIndex = Settings.Default.echoVRIP == "127.0.0.1" ? 0 : 1;

			surroundWithAngleBracketsCheckbox.IsChecked = Settings.Default.atlasLinkUseAngleBrackets;
			linkTypeComboBox.SelectedIndex = Settings.Default.atlasLinkStyle;

			RefreshCurrentLink();


			GetAtlasMatches();


			outputUpdateTimer.Interval = 100;
			outputUpdateTimer.Elapsed += Update;
			outputUpdateTimer.Enabled = true;

			initialized = true;
		}

		private void Update(object source, ElapsedEventArgs e)
		{
			if (Program.running)
			{
				Dispatcher.Invoke(() =>
				{
					hostMatchButton.IsEnabled = Program.lastFrame != null && Program.lastFrame.private_match == true;
				});
			}
		}

		private string CurrentLink(string sessionid)
		{
			if (Settings.Default.atlasLinkUseAngleBrackets)
			{
				switch (Settings.Default.atlasLinkStyle)
				{
					case 0:
						return "<ignitebot://choose/" + sessionid + ">";
					case 1:
						return "<atlas://j/" + sessionid + ">";
					case 2:
						return "<atlas://s/" + sessionid + ">";
				}
			}
			else
			{
				switch (Settings.Default.atlasLinkStyle)
				{
					case 0:
						return "ignitebot://choose/" + sessionid;
					case 1:
						return "atlas://j/" + sessionid;
					case 2:
						return "atlas://s/" + sessionid;
				}
			}
			return "";
		}

		public void GetLinks(object sender, RoutedEventArgs e)
		{
			string ip = alternateIPTextBox.Text;
			Task.Run(() => Program.GetAsync($"http://{ip}:6721/session", null, (responseJSON) =>
			{
				try
				{
					g_InstanceSimple obj = JsonConvert.DeserializeObject<g_InstanceSimple>(responseJSON);

					if (obj != null && !string.IsNullOrEmpty(obj.sessionid))
					{
						Dispatcher.Invoke(() =>
						{
							joinLink.Text = CurrentLink(obj.sessionid);

							Settings.Default.alternateEchoVRIP = alternateIPTextBox.Text;
							Settings.Default.Save();
						});
					}

				}
				catch (Exception e)
				{
					Logger.LogRow(Logger.LogType.Error, $"Can't parse response\n{e}");
				}
			}));
		}

		private void closeButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void HostMatchClicked(object sender, RoutedEventArgs e)
		{
			HostAtlasMatch();
		}


		public class AtlasMatchResponse
		{
			public AtlasMatch[] matches;
		}
		public class AtlasMatch
		{
			public class AtlasTeamInfo
			{
				public int count;
				public float percentage;
				public string team_logo;
				public string team_name;
			}
			public string session_id;
			public AtlasTeamInfo blue_team_info;
			public AtlasTeamInfo orange_team_info;
			public string[] blue_team;
			public string[] orange_team;
			public bool is_protected;
			public string server_location;
			public float server_score;
			public string username;

			public Dictionary<string, object> ToDict()
			{

				try
				{
					var values = new Dictionary<string, object>
					{
						{"session_id", session_id },
						{"blue_team_members", blue_team },
						{"orange_team_members", orange_team },
						{"is_protected", is_protected },
						{"server_location", server_location},
						{"server_score", server_score},
						{"username", username },
					};
					return values;
				}
				catch (Exception e)
				{
					Logger.LogRow(Logger.LogType.Error, "Can't serialize atlas match data.\n" + e.Message + "\n" + e.StackTrace);
					return new Dictionary<string, object>
					{
						{"none", 0 }
					};
				}
			}
		}

		public void UpdateUIWithAtlasMatches(AtlasMatch[] matches)
		{
			try
			{
				Dispatcher.Invoke(() =>
				{
					// remove all the old children
					MatchesBox.Children.RemoveRange(0, MatchesBox.Children.Count);

					foreach (var match in matches)
					{
						var content = new Grid();
						var header = new StackPanel
						{
							Orientation = Orientation.Horizontal,
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalAlignment = HorizontalAlignment.Right,
							Margin = new Thickness(0, 0, 10, 0)
						};
						header.Children.Add(new Label
						{
							Content = match.is_protected ? "Casters Only" : "Public"
						});
						if (!match.is_protected || !Program.Personal)
						{
							var copyLinkButton = new Button
							{
								Content = "Copy Atlas Link",
								Margin = new Thickness(100, 0, 0, 0),
								Padding = new Thickness(10, 0, 10, 0),

							};
							copyLinkButton.Click += (s, e) =>
							{
								Clipboard.SetText(CurrentLink(match.session_id));
							};
							header.Children.Add(copyLinkButton);
							var joinButton = new Button
							{
								Content = "Join",
								Margin = new Thickness(20, 0, 0, 0),
								Padding = new Thickness(10, 0, 10, 0)
							};
							joinButton.Click += (s, e) =>
							{
								Process.Start(new ProcessStartInfo
								{
									FileName = "ignitebot://choose/" + match.session_id,
									UseShellExecute = true
								});
							};
							header.Children.Add(joinButton);
						}

						content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
						content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
						content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
						content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

						content.ShowGridLines = true;

						var blueLogo = new Image
						{
							Width = 100,
							Height = 100
						};
						if (match.blue_team_info.team_logo != string.Empty)
						{
							blueLogo.Source = new BitmapImage(new Uri(match.blue_team_info.team_logo));
						}
						var blueLogoBox = new StackPanel
						{
							Orientation = Orientation.Vertical,
							Margin = new Thickness(10, 10, 10, 10)
						};
						blueLogoBox.SetValue(Grid.ColumnProperty, 0);
						blueLogoBox.Children.Add(blueLogo);
						blueLogoBox.Children.Add(new Label
						{
							Content = match.blue_team_info.team_name,
							HorizontalAlignment = HorizontalAlignment.Center

						});


						var orangeLogo = new Image
						{
							Width = 100,
							Height = 100
						};
						if (match.orange_team_info.team_logo != string.Empty)
						{
							orangeLogo.Source = new BitmapImage(new Uri(match.orange_team_info.team_logo));
						}
						var orangeLogoBox = new StackPanel
						{
							Orientation = Orientation.Vertical,
							Margin = new Thickness(10, 10, 10, 10)
						};
						orangeLogoBox.SetValue(Grid.ColumnProperty, 3);
						orangeLogoBox.Children.Add(orangeLogo);
						orangeLogoBox.Children.Add(new Label
						{
							Content = match.orange_team_info.team_name,
							HorizontalAlignment = HorizontalAlignment.Center
						});

						var bluePlayers = new TextBlock
						{
							Text = string.Join('\n', match.blue_team),
							Margin = new Thickness(10, 10, 10, 10),
							TextAlignment = TextAlignment.Right
						};
						bluePlayers.SetValue(Grid.ColumnProperty, 1);
						var orangePlayers = new TextBlock
						{
							Text = string.Join('\n', match.orange_team),
							Margin = new Thickness(10, 10, 10, 10)
						};
						orangePlayers.SetValue(Grid.ColumnProperty, 2);
						var sessionIdTextBox = new Label
						{
							Content = match.session_id
						};
						//content.Children.Add(sessionIdTextBox);
						content.Children.Add(blueLogoBox);
						content.Children.Add(orangeLogoBox);
						content.Children.Add(bluePlayers);
						content.Children.Add(orangePlayers);
						MatchesBox.Children.Add(new GroupBox
						{
							Content = content,
							Margin = new Thickness(10, 10, 10, 10),
							Header = header
						});
					}

				});
			}
			catch (Exception e)
			{
				Logger.LogRow(Logger.LogType.Error, $"Error showing matches in UI\n{e}");
			}
		}

		public void HostAtlasMatch()
		{
			if (Program.lastFrame == null || Program.lastFrame.teams == null) return;

			string matchesAPIURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/host_atlas_match";

			AtlasMatch match = new AtlasMatch
			{
				session_id = Program.lastFrame.sessionid,
				blue_team = Program.lastFrame.teams[0].player_names.ToArray(),
				orange_team = Program.lastFrame.teams[1].player_names.ToArray(),
				is_protected = onlyCasters,
				server_location = Program.matchData.ServerLocation,
				server_score = Program.matchData.ServerScore,
				username = Program.lastFrame.client_name
			};
			string data = JsonConvert.SerializeObject(match.ToDict());
			Task.Run(() => Program.PostAsync(matchesAPIURL, new Dictionary<string, string>() { { "x-api-key", DiscordOAuth.igniteUploadKey } }, data, (responseJSON) =>
			{
				GetAtlasMatches();
			}));
		}

		public void GetAtlasMatches()
		{
			string matchesAPIURL = "https://ignitevr.gg/cgi-bin/EchoStats.cgi/atlas_matches";
			Task.Run(() => Program.GetAsync(matchesAPIURL, new Dictionary<string, string>() { { "x-api-key", DiscordOAuth.igniteUploadKey } }, (responseJSON) =>
			{
				try
				{
					AtlasMatchResponse obj = JsonConvert.DeserializeObject<AtlasMatchResponse>(responseJSON);

					if (obj != null && obj.matches != null)
					{
						UpdateUIWithAtlasMatches(obj.matches);
					}
					else
					{
						UpdateUIWithAtlasMatches(Array.Empty<AtlasMatch>());
					}

				}
				catch (Exception e)
				{
					Logger.LogRow(Logger.LogType.Error, $"Can't parse Atlas matches response\n{e}");
				}
			}));
		}

		private void RefreshMatchesClicked(object sender, RoutedEventArgs e)
		{
			GetAtlasMatches();
		}

		private void SurroundWithAngleBracketsChecked(object sender, RoutedEventArgs e)
		{
			if (!initialized) return;

			Settings.Default.atlasLinkUseAngleBrackets = ((CheckBox)sender).IsChecked == true;
			Settings.Default.Save();
			RefreshCurrentLink();
		}

		private void RefreshCurrentLink()
		{
			if (Program.lastFrame != null)
			{
				joinLink.Text = CurrentLink(Program.lastFrame.sessionid);
			}
		}

		private void CopyMainLinkToClipboard(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(joinLink.Text);
		}

		private void FollowMainLink(object sender, RoutedEventArgs e)
		{
			if (joinLink.Text.Length > 10)
			{
				string text = joinLink.Text;
				if (joinLink.Text.StartsWith('<'))
				{
					text = text.Substring(1, text.Length - 2);
				}
				Process.Start(new ProcessStartInfo
				{
					FileName = text,
					UseShellExecute = true
				});
			}
		}

		private void IPSourceDropdownChanged(object sender, SelectionChangedEventArgs e)
		{
			// TODO
		}

		private void LinkTypeChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!initialized) return;

			Settings.Default.atlasLinkStyle = ((ComboBox)sender).SelectedIndex;
			Settings.Default.Save();
			RefreshCurrentLink();
		}

		private async void FindQuestIP(object sender, RoutedEventArgs e)
		{
			findQuestStatusLabel.Content = "Searching for Quest on network";
			findQuestStatusLabel.Visibility = Visibility.Visible;
			alternateIPTextBox.IsEnabled = false;
			findQuest.IsEnabled = false;
			resetIP.IsEnabled = false;
			var progress = new Progress<string>(s => findQuestStatusLabel.Content = s);
			await Task.Factory.StartNew(() => Program.echoVRIP = Program.FindQuestIP(progress),
										TaskCreationOptions.None);
			alternateIPTextBox.IsEnabled = true;
			findQuest.IsEnabled = true;
			resetIP.IsEnabled = true;
			if (!Program.overrideEchoVRPort) Program.echoVRPort = 6721;
			alternateIPTextBox.Text = Program.echoVRIP;
			Settings.Default.echoVRIP = Program.echoVRIP;
			if (!Program.overrideEchoVRPort) Settings.Default.echoVRPort = Program.echoVRPort;
			Settings.Default.Save();
		}

		private void SetToLocalIP(object sender, RoutedEventArgs e)
		{
			Program.echoVRIP = "127.0.0.1";
			alternateIPTextBox.Text = Program.echoVRIP;
			Settings.Default.echoVRIP = Program.echoVRIP;
			Settings.Default.Save();
		}

		private void PublicToggled(object sender, RoutedEventArgs e)
		{
			if (!initialized) return;

			onlyCasters = ((CheckBox)sender).IsChecked == true;
		}
	}
}

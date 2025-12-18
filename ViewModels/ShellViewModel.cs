using MahApps.Metro.Controls.Dialogs;
using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SPP_LegionV2_Management
{
	public class ShellViewModel : BaseViewModel, INotifyPropertyChanged
	{
		private DateTime lastUpdate = DateTime.Now;

		public bool updateAvailable { get; set; }

		// Setup our public variables and such, many are saved within the general settings class, so we'll get/set from those
		public string AppTitle { get; set; } = $"SPP LegionV2 Management v{Assembly.GetExecutingAssembly().GetName().Version}";

		// declare individual VMs, lets us always show the same one as we switch tabs
		public ConfigGeneratorViewModel ConfigGeneratorVM = new ConfigGeneratorViewModel(DialogCoordinator.Instance);
		public AccountManagerViewModel AccountManagerVM = new AccountManagerViewModel(DialogCoordinator.Instance);
		public SettingsViewModel SettingsVM = new SettingsViewModel(DialogCoordinator.Instance);

		// This holds the values for the window position/size to be pulled from saved settings
		public double WindowTop
		{ get { return GeneralSettingsManager.GeneralSettings.WindowTop; } set { GeneralSettingsManager.GeneralSettings.WindowTop = value; } }
		public double WindowLeft
		{ get { return GeneralSettingsManager.GeneralSettings.WindowLeft; } set { GeneralSettingsManager.GeneralSettings.WindowLeft = value; } }
		public double WindowHeight
		{ get { return GeneralSettingsManager.GeneralSettings.WindowHeight; } set { GeneralSettingsManager.GeneralSettings.WindowHeight = value; } }
		public double WindowWidth
		{ get { return GeneralSettingsManager.GeneralSettings.WindowWidth; } set { GeneralSettingsManager.GeneralSettings.WindowWidth = value; } }

		// Status display at the top section of the app
		public string ServerConfigStatus { get; set; } = "⚠";
		public string ClientConfigStatus { get; set; } = "⚠";
		public string SQLConnectionStatus { get; set; } = "⚠";

		public ShellViewModel()
		{
			updateAvailable = false;
			GeneralSettingsManager.MoveIntoView();
			UpdateStatus();
			CheckForUpdates();
		}

		private async System.Threading.Tasks.Task CheckForUpdates()
		{
			//Get all releases from GitHub
			//Source: https://octokitnet.readthedocs.io/en/latest/getting-started/
			GitHubClient client = new GitHubClient(new ProductHeaderValue("Skeezerbean-SPP-LegionV2-Management"));
			IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("Skeezerbean", "SPP-LegionV2-Management");

			//Setup the versions
			Version latestGitHubVersion = new Version(releases[0].TagName);
			Version localVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString()); //Replace this with your local version. 

			//Compare the Versions
			//Source: https://stackoverflow.com/questions/7568147/compare-version-numbers-without-using-split-function
			int versionComparison = localVersion.CompareTo(latestGitHubVersion);
			if (versionComparison < 0)
				updateAvailable = true;
		}

		public bool ShowUpdateButton
		{
			get { return updateAvailable; }
		}

		// This will constantly run to update the status
		private async void UpdateStatus()
		{
			// Keep this running always
			while (1 == 1)
			{
				// Every 2 seconds we want to check for updates in the even the Database Server status or file locations change
				if (lastUpdate.AddSeconds(2) < DateTime.Now)
				{
					if ((File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\worldserver.conf") && File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\bnetserver.conf"))
						|| (File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\Servers\\worldserver.conf") && File.Exists($"{ GeneralSettingsManager.GeneralSettings.SPPFolderLocation}\\Servers\\bnetserver.conf")))
						ServerConfigStatus = "✓";
					else
						ServerConfigStatus = "⚠";

					if (GeneralSettingsManager.GeneralSettings.WOWConfigLocation.EndsWith(".wtf"))
					{
						if (File.Exists(GeneralSettingsManager.GeneralSettings.WOWConfigLocation))
							ClientConfigStatus = "✓";
					}
					else
					{
						try
						{
							// only need the first match
							string[] files = Directory.GetFiles(GeneralSettingsManager.GeneralSettings.WOWConfigLocation, "*.wtf");
							if (files.Any() && File.Exists(files[0]))
									ClientConfigStatus = "✓";
								else
								{
									files = Directory.GetFiles(GeneralSettingsManager.GeneralSettings.WOWConfigLocation + "\\WTF", "*.wtf");
									if (files.Any() && File.Exists(files[0]))
										ClientConfigStatus = "✓";
									else
										ClientConfigStatus = "⚠";
								}
						}
						catch { ClientConfigStatus = "⚠"; }
					}

					if (await CheckSQLStatus())
					{
						SQLConnectionStatus = "✓";
						GeneralSettingsManager.IsMySQLRunning = true;
					}
					else
					{
						SQLConnectionStatus = "⚠";
						GeneralSettingsManager.IsMySQLRunning = false;
					}

					lastUpdate = DateTime.Now;
				}

				await Task.Delay(1);
			}
		}

		private async Task<bool> CheckSQLStatus()
		{
			string result = string.Empty;

			await Task.Run(() =>
			{
				result = MySqlManager.MySQLQueryToString(@"SELECT '1'");
			});

			return result == "1";
		}

		public void LoadPageConfigGenerator()
		{
			ActivateItemAsync(ConfigGeneratorVM);
		}

		public void LoadPageAccountManager()
		{
			ActivateItemAsync(AccountManagerVM);
		}

		public void LoadPageSettings()
		{
			ActivateItemAsync(SettingsVM);
		}
	}
}
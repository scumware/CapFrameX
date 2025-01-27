﻿using CapFrameX.Contracts.Cloud;
using CapFrameX.Contracts.Configuration;
using CapFrameX.Contracts.Data;
using CapFrameX.Data;
using CapFrameX.Data.Session.Contracts;
using CapFrameX.EventAggregation.Messages;
using CapFrameX.Extensions;
using CapFrameX.Webservice.Data.DTO;
using GongSolutions.Wpf.DragDrop;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CapFrameX.Extensions.NetStandard;
using CapFrameX.Statistics.NetStandard.Contracts;

namespace CapFrameX.ViewModel
{
    public class CloudViewModel : BindableBase, INavigationAware, IDropTarget
	{
		private const int CLOUDUPLOAD_MAX_LENGTH = 32000000;
		private readonly IStatisticProvider _statisticProvider;
		private readonly IRecordManager _recordManager;
		private readonly IEventAggregator _eventAggregator;
		private readonly IAppConfiguration _appConfiguration;
		private readonly ILogger<CloudViewModel> _logger;
		private readonly IAppVersionProvider _appVersionProvider;
		private readonly LoginManager _loginManager;
		private PubSubEvent<AppMessages.CloudFolderChanged> _cloudFolderChanged;
		private PubSubEvent<AppMessages.SelectCloudFolder> _selectCloudFolder;
		private bool _useUpdateSession;
		private int _selectedCloudEntryIndex = -1;
		private bool _showHelpText = true;
		private bool _enableClearAndUploadButton;
		private List<IFileRecordInfo> _fileRecordInfoList = new List<IFileRecordInfo>();
		private string _downloadIdString = string.Empty;
		private string _shareUrl;
		private bool _showUploadInfo = false;
		private bool _showDownloadInfo = false;
		private bool _showUploadDescriptionTextBox = false;
		private string _uploadDescription = string.Empty;


		public string UploadDescription
		{
			get
			{
				return _uploadDescription;
			}
			set
			{
				_uploadDescription = value;
				RaisePropertyChanged();
			}
		}

		public string ShareUrl
		{
			get
			{
				return _shareUrl;
			}
			set
			{
				_shareUrl = value;
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(ShareUrlVisible));
			}
		}

		public bool ShareUrlVisible
		{
			get
			{
				return !string.IsNullOrWhiteSpace(ShareUrl);
			}
			set
			{
				ShareUrlVisible = value;
				RaisePropertyChanged();
			}
		}

		public int SelectedCloudEntryIndex
		{
			get
			{ return _selectedCloudEntryIndex; }
			set
			{
				_selectedCloudEntryIndex = value;
				RaisePropertyChanged();
			}
		}

		public bool ShowHelpText
		{
			get
			{ return _showHelpText; }
			set
			{
				_showHelpText = value;
				RaisePropertyChanged();
			}
		}

		public bool ShowUploadInfo
		{
			get
			{ return _showUploadInfo; }
			set
			{
				_showUploadInfo = value;
				RaisePropertyChanged();
			}
		}

		public bool ShowDownloadInfo
		{
			get
			{ return _showDownloadInfo; }
			set
			{
				_showDownloadInfo = value;
				RaisePropertyChanged();
			}
		}

		public bool ShowUploadDescriptionTextBox
		{

			get
			{ return _showUploadDescriptionTextBox; }
			set
			{
				_showUploadDescriptionTextBox = value;
				RaisePropertyChanged();
			}
		}

		public bool EnableClearAndUploadButton
		{
			get
			{ return _enableClearAndUploadButton; }
			set
			{
				_enableClearAndUploadButton = value;
				RaisePropertyChanged();
			}
		}

		public bool EnableDownloadButton { get; set; }

		public string CloudDownloadDirectory
		{
			get { return _appConfiguration.CloudDownloadDirectory; }
			set
			{
				_appConfiguration.CloudDownloadDirectory = value;
				RaisePropertyChanged();
			}
		}
		public bool ShareProcessListEntries
		{
			get
			{ return _appConfiguration.ShareProcessListEntries; }
			set
			{
				_appConfiguration.ShareProcessListEntries = value;
				RaisePropertyChanged();
			}
		}
		public bool AutoUpdateProcessList
		{
			get
			{ return _appConfiguration.AutoUpdateProcessList; }
			set
			{
				_appConfiguration.AutoUpdateProcessList = value;
				RaisePropertyChanged();
			}
		}

		public string DownloadIDString
		{
			get
			{
				return _downloadIdString;
			}
			set
			{
				var regex = new Regex(@"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}");
				var match = regex.Match(value);
				if (match.Success)
				{
					_downloadIdString = match.Value;
				}
				else
				{
					_downloadIdString = value;
				}
				EnableDownloadButton = match.Success;
				RaisePropertyChanged(nameof(EnableDownloadButton));
			}
		}

		public ICommand ClearTableCommand { get; }

		public ICommand UploadRecordsCommand { get; }

		public ICommand DownloadRecordsCommand { get; }

		public ICommand CopyURLCommand { get; }

		public ICommand SwitchToDownloadDirectoryCommand { get; }

		public ISubject<Unit> DownloadCompleteStream = new Subject<Unit>();

		public ObservableCollection<ICloudEntry> CloudEntries { get; private set; }
			= new ObservableCollection<ICloudEntry>();
		public bool IsLoggedIn { get; private set; }

		public CloudViewModel(IStatisticProvider statisticProvider, IRecordManager recordManager,
			IEventAggregator eventAggregator, IAppConfiguration appConfiguration, ILogger<CloudViewModel> logger, IAppVersionProvider appVersionProvider, LoginManager loginManager)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			_statisticProvider = statisticProvider;
			_recordManager = recordManager;
			_eventAggregator = eventAggregator;
			_appConfiguration = appConfiguration;
			_logger = logger;
			_appVersionProvider = appVersionProvider;
			_loginManager = loginManager;
			ClearTableCommand = new DelegateCommand(OnClearTable);
			CopyURLCommand = new DelegateCommand(() => Clipboard.SetText(_shareUrl));
			SwitchToDownloadDirectoryCommand = new DelegateCommand(OnSwitchToDownloadDirectory);
			_cloudFolderChanged = eventAggregator.GetEvent<PubSubEvent<AppMessages.CloudFolderChanged>>();
			_selectCloudFolder = eventAggregator.GetEvent<PubSubEvent<AppMessages.SelectCloudFolder>>();
			UploadRecordsCommand = new DelegateCommand(async () =>
			{
				await UploadRecords();
				OnClearTable();
			});

			DownloadRecordsCommand = new DelegateCommand(async () =>
			{
				await DownloadCaptureCollection(DownloadIDString);
			});


			SubscribeToUpdateSession();

			CloudEntries.CollectionChanged += new NotifyCollectionChangedEventHandler
				((sender, eventArg) => OnCloudEntriesChanged());

			IsLoggedIn = loginManager.State.Token != null;

			_eventAggregator.GetEvent<PubSubEvent<AppMessages.LoginState>>().Subscribe(state =>
			{
				IsLoggedIn = state.IsLoggedIn;
				RaisePropertyChanged(nameof(IsLoggedIn));
			});

			stopwatch.Stop();
			_logger.LogInformation(this.GetType().Name + " {initializationTime}s initialization time", Math.Round(stopwatch.ElapsedMilliseconds * 1E-03, 1));
		}

		public void RemoveCloudEntry(ICloudEntry entry)
		{
			_fileRecordInfoList.Remove(entry.FileRecordInfo);
			CloudEntries.Remove(entry);
		}

		private void OnCloudEntriesChanged()
		{
			ShowHelpText = !CloudEntries.Any();
			ShowUploadDescriptionTextBox = CloudEntries.Any();
			EnableClearAndUploadButton = CloudEntries.Any();
		}

		public void OnSwitchToDownloadDirectory()
		{
			string downloadDirectory = CloudDownloadDirectory;

			if (downloadDirectory.Contains(@"MyDocuments\CapFrameX\Captures\Cloud"))
				downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"CapFrameX\Captures\Cloud");

			_appConfiguration.ObservedDirectory = downloadDirectory;
			_selectCloudFolder.Publish(new AppMessages.SelectCloudFolder());
		}

		private void SubscribeToUpdateSession()
		{
			_eventAggregator.GetEvent<PubSubEvent<ViewMessages.SelectSession>>()
							.Subscribe(msg =>
							{
								if (_useUpdateSession)
								{
									AddCloudEntry(msg.RecordInfo, msg.CurrentSession);
								}
							});
		}

		private void AddCloudEntry(IFileRecordInfo recordInfo, ISession session)
		{
			if (recordInfo != null)
			{
				_fileRecordInfoList.Add(recordInfo);
			}
			else
				return;

			ShareUrl = string.Empty;
			CloudEntries.Add(new CloudEntry()
			{
				GameName = recordInfo.GameName,
				CreationDate = recordInfo.CreationDate,
				CreationTime = recordInfo.CreationTime,
				Comment = recordInfo.Comment,
				FileRecordInfo = recordInfo
			});
		}

		private void OnClearTable()
		{
			CloudEntries.Clear();
			_fileRecordInfoList.Clear();
		}

		public void OnSelectDownloadFolder()
		{
			var dialog = new CommonOpenFileDialog
			{
				IsFolderPicker = true
			};

			CommonFileDialogResult result = dialog.ShowDialog();

			if (result == CommonFileDialogResult.Ok)
			{
				_appConfiguration.CloudDownloadDirectory = dialog.FileName;
				CloudDownloadDirectory = dialog.FileName;
				_cloudFolderChanged.Publish(new AppMessages.CloudFolderChanged());
			}
		}

		public void OnNavigatedTo(NavigationContext navigationContext)
		{
			_useUpdateSession = true;
		}

		public bool IsNavigationTarget(NavigationContext navigationContext)
		{
			return true;
		}

		public void OnNavigatedFrom(NavigationContext navigationContext)
		{
			_useUpdateSession = false;
		}

		void IDropTarget.Drop(IDropInfo dropInfo)
		{
			if (dropInfo != null)
			{
				if (dropInfo.VisualTarget is FrameworkElement frameworkElement)
				{
					if (frameworkElement.Name == "CloudItemDataGrid"
						|| frameworkElement.Name == "DragAndDropInfoTextTextBlock")
					{
						if (dropInfo.Data is IFileRecordInfo recordInfo)
						{
							AddCloudEntry(recordInfo, null);
						}
						else if (dropInfo.Data is IEnumerable<IFileRecordInfo> recordInfos)
						{
							recordInfos.ForEach(info => AddCloudEntry(info, null));
						}
					}
				}
			}
		}

		void IDropTarget.DragOver(IDropInfo dropInfo)
		{
			if (dropInfo != null)
			{
				dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
				dropInfo.Effects = DragDropEffects.Move;
			}
		}

		private async Task UploadRecords()
		{
			ShowUploadDescriptionTextBox = false;
			ShowUploadInfo = true;
			ShareUrl = string.Empty;
			EnableClearAndUploadButton = false;

			var sessions = CloudEntries.Select(ce => _recordManager.LoadData(ce.FileRecordInfo.FullPath));

			var contentAsJson = JsonConvert.SerializeObject(sessions);
			if(System.Text.Encoding.UTF8.GetByteCount(contentAsJson) > CLOUDUPLOAD_MAX_LENGTH)
            {
				MessageBox.Show($"Size of selected sessions exceed limit of {CLOUDUPLOAD_MAX_LENGTH} bytes per upload");
				return;
            }
			using (var client = new HttpClient() {
				BaseAddress = new Uri(ConfigurationManager.AppSettings["WebserviceUri"])
			})
			{
				client.DefaultRequestHeaders.AddCXClientUserAgent();
				if (_loginManager.State?.Token != null)
				{
					try
					{
						await _loginManager.RefreshTokenIfNeeded();
						if (_loginManager.State.IsSigned)
						{
							client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _loginManager.State.Token.AccessToken);
						}
					} catch(Exception e)
					{
						_logger.LogWarning(e, "Something went wrong while Refreshing the Accesstoken. Using Guest Mode");
					}
				}
				var content = new StringContent(contentAsJson);
				content.Headers.ContentType.MediaType = "application/json";
				var response = await client.PostAsync($@"SessionCollections?description={UploadDescription}", content);

				if (response.IsSuccessStatusCode)
				{
					ShareUrl = response.Headers.Location.ToString();
					ShowUploadInfo = false;
					UploadDescription = string.Empty;
					_logger.LogInformation("Successfully uploaded Captures. ShareUrl is {shareUrl}", response.Headers.Location);
				}
				else
				{
					var responseBody = await response.Content.ReadAsStringAsync();
					_logger.LogError("Upload of Captures failed. {error}", responseBody);
					try
                    {
						var responseJson = JToken.Parse(responseBody);
						MessageBox.Show($"Upload failed: {responseJson.GetValue<string>("message") ?? responseJson.GetValue<string>("title")}", "Error");
					} catch { }
				}
			}
		}

		private async Task DownloadCaptureCollection(string id)
		{
			ShowDownloadInfo = true;
			EnableDownloadButton = false;
			RaisePropertyChanged(nameof(EnableDownloadButton));
			var url = $@"SessionCollections/{id}";
			using (var client = new HttpClient() { 
				BaseAddress = new Uri(ConfigurationManager.AppSettings["WebserviceUri"]),
			})
			{
				client.DefaultRequestHeaders.AddCXClientUserAgent();
				var response = await client.GetAsync(url);

				if (response.IsSuccessStatusCode)
				{
					var content = JsonConvert.DeserializeObject<SessionCollectionDTO>(await response.Content.ReadAsStringAsync());

					var downloadDirectory = _appConfiguration.CloudDownloadDirectory;

					if (downloadDirectory.Contains(@"MyDocuments\CapFrameX\Captures\Cloud"))
					{
						downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"CapFrameX\Captures\Cloud");
					}
					if (!Directory.Exists(downloadDirectory))
					{
						Directory.CreateDirectory(downloadDirectory);
						_cloudFolderChanged.Publish(new AppMessages.CloudFolderChanged());

					}
					foreach (var session in content.Sessions)
					{
						string dateTimeFormat = 
						$"{session.Info.CreationDate.Year}-" +
						$"{session.Info.CreationDate.Month.ToString("d2")}-" +
						$"{session.Info.CreationDate.Day.ToString("d2")}" +
						$"T{session.Info.CreationDate.Hour}" +
						$"{session.Info.CreationDate.Minute}{session.Info.CreationDate.Second}";

						var fileInfo = new FileInfo(Path.Combine(downloadDirectory, $"CapFrameX-{session.Info.ProcessName}-{dateTimeFormat}.json"));
						File.WriteAllText(fileInfo.FullName, JsonConvert.SerializeObject(session));
					}
					ShowDownloadInfo = false;
					DownloadIDString = string.Empty;
					DownloadCompleteStream.OnNext(default);
				}
				else
				{
					var content = await response.Content.ReadAsStringAsync();
					_logger.LogError("Download of CaptureCollection failed. {error}", content);
					EnableDownloadButton = true;
					ShowDownloadInfo = false;
					RaisePropertyChanged(nameof(EnableDownloadButton));
					try
					{
						var responseJson = JToken.Parse(content);
						MessageBox.Show($"Download failed: {responseJson.GetValue<string>("message") ?? responseJson.GetValue<string>("title")}", "Error");
					}
					catch { }
				}
			}
		}
	}
}
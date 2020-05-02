using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using MovieHasher;
using OSAPIcalls;

namespace OSBDgui
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public bool bgIsReady { get; set; }
		public string currentPath { get; set; }
		public MainWindow()
		{
			InitializeComponent();
		}

		private void openFileFolder(object sender, RoutedEventArgs e)
		{
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
			{
				dialog.RootFolder = Environment.SpecialFolder.MyComputer;
				System.Windows.Forms.DialogResult result = dialog.ShowDialog();
				txtEditor.Text = dialog.SelectedPath;
			}					
		}

		private void searchSubtitles(object sender, RoutedEventArgs e)
		{
			if (Directory.Exists(currentPath)) {
				var movieFiles = Directory.GetFiles(currentPath, "*.*", SearchOption.AllDirectories)
								.Where(s => s.EndsWith(".mp4") || s.EndsWith(".mkv"));

				foreach (var movieFileFullName in movieFiles)
				{
					BackgroundWorker searchMovieBG = new BackgroundWorker();
					searchMovieBG.WorkerReportsProgress = true;
					searchMovieBG.DoWork += searchMovieBG_DoWork;
					searchMovieBG.ProgressChanged += SearchMovieBG_ProgressChanged;
					searchMovieBG.RunWorkerAsync(movieFileFullName);
				}
			}
			
			else {
				logBox.Items.Add(new ListBoxItem
				{
					Content = "Path not valid.",
					Foreground = Brushes.PaleVioletRed

				});
				logBox.SelectedIndex = logBox.Items.Count - 1;
				logBox.ScrollIntoView(logBox.SelectedItem);
			}
		}


		private void SearchMovieBG_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			logBox.Items.Add(new ListBoxItem { Content = e.UserState });
			logBox.SelectedIndex = logBox.Items.Count - 1;
			logBox.ScrollIntoView(logBox.SelectedItem);
		}

		public static void Decompress(FileInfo fileToDecompress)
		{
			using (FileStream originalFileStream = fileToDecompress.OpenRead())
			{
				string currentFileName = fileToDecompress.FullName;
				string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length) + ".srt";

				using (FileStream decompressedFileStream = File.Create(newFileName))
				{
					using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
					{
						decompressionStream.CopyTo(decompressedFileStream);
						//logBox.Items.Add(new ListBoxItem() { Content = String.Format("Decompressed: {0}", newFileName) });
					}
				}
			}
		}

		private void updateCurrentPath(object sender, TextChangedEventArgs e)
		{
			currentPath = txtEditor.Text;
		}

		private void searchMovieBG_DoWork(object sender, DoWorkEventArgs e) 
		{
			string movieFileFullName = (string)e.Argument;
			string movieFileName = System.IO.Path.GetFileName(movieFileFullName);
			byte[] movieHashByte = Hasher.ComputeMovieHash(movieFileFullName);
			string movieHashString = Hasher.ToHexadecimal(movieHashByte);
			FileInfo movieFileInfo = new FileInfo(movieFileFullName);
			string movieFileSize = movieFileInfo.Length.ToString();
			var worker = sender as BackgroundWorker;
			worker.ReportProgress(20, String.Format("File: {0}\nSearching subtitle...", movieFileName));

			OSAPICalls osAPI = new OSAPICalls(movieHashString, movieFileSize, "TemporaryUserAgent");

			MatchResult searchResult = osAPI.searchMovie();
			if (!searchResult.IsEmpty)
			{
				worker.ReportProgress(40, String.Format("Found subtitle: {0}\nDownloading...", searchResult.SubFileName));
				WebClient webClient = new WebClient();
				string zipSubtitle = movieFileFullName.Remove(movieFileFullName.Length - movieFileInfo.Extension.Length) + ".gz";
				webClient.DownloadFile(searchResult.SubDownloadLink, zipSubtitle);
				worker.ReportProgress(80, String.Format("Downloaded {0}\nDecompressing...", zipSubtitle));
				FileInfo gzFileInfo = new FileInfo(zipSubtitle);
				Decompress(gzFileInfo);
				File.Delete(zipSubtitle);
			}
			else
			{
				worker.ReportProgress(90, String.Format("No subtitle found for {0}.", movieFileName));
			}

			worker.ReportProgress(100);
		}
	}
}

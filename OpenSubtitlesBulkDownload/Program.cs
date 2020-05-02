using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSAPIcalls;
using System.IO;
using MovieHasher;
using System.Windows.Forms;
using System.Net;
using System.IO.Compression;

namespace OpenSubtitlesBulkDownload
{
	class Program
	{
		static void Main(string[] args)
		{
			string assemblyFullName = Application.ExecutablePath;
			string currentPath;
			if (args.Length == 0)
			{
				currentPath = Path.GetDirectoryName(assemblyFullName);
			}
			else
			{
				currentPath = args[0];
			}
			var movieFiles = Directory.GetFiles(currentPath, "*.*", SearchOption.AllDirectories)
								.Where(s => s.EndsWith(".mp4") || s.EndsWith(".mkv"));

			foreach (var movieFileFullName in movieFiles)
			{
				string movieFileName = Path.GetFileName(movieFileFullName);
				byte[] movieHashByte = Hasher.ComputeMovieHash(movieFileFullName);
				string movieHashString = Hasher.ToHexadecimal(movieHashByte);
				FileInfo movieFileInfo = new FileInfo(movieFileFullName);
				string movieFileSize = movieFileInfo.Length.ToString();

				Console.WriteLine("File: {0}", movieFileName);
				Console.WriteLine("Searching subtitle...");

				OSAPICalls osAPI = new OSAPICalls(movieHashString, movieFileSize, "TemporaryUserAgent");
				MatchResult searchResult = osAPI.searchMovie();

				if (!searchResult.IsEmpty)
				{
					Console.WriteLine("Found subtitle: {0}", searchResult.SubFileName);

					Console.WriteLine("Downloading...");
					WebClient webClient = new WebClient();
					//string zipSubtitle = Path.GetFileNameWithoutExtension(movieFileFullName) + ".gz";
					string zipSubtitle = movieFileFullName.Remove(movieFileFullName.Length - movieFileInfo.Extension.Length) + ".gz";
					webClient.DownloadFile(searchResult.SubDownloadLink, zipSubtitle);
					Console.WriteLine("Downloaded {0}", zipSubtitle);
					Console.WriteLine("Decompressing...");
					FileInfo gzFileInfo = new FileInfo(zipSubtitle);
					Decompress(gzFileInfo);
					File.Delete(zipSubtitle);
				}
				else
				{
					Console.WriteLine("No subtitle found for {0}.", movieFileName);
				}

				Console.WriteLine("-------");
			}
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
						Console.WriteLine("Decompressed: {0}", newFileName);
					}
				}
			}
		}
	}
}

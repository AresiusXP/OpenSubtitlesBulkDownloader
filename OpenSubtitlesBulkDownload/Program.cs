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
			string currentPath = Path.GetDirectoryName(assemblyFullName);
			Console.WriteLine(currentPath);
			var movieFiles = Directory.GetFiles(currentPath, "*.mp4");

			foreach (var movieFileFullName in movieFiles)
			{
				string movieFileName = Path.GetFileName(movieFileFullName);
				byte[] movieHashByte = Hasher.ComputeMovieHash(movieFileFullName);
				string movieHashString = Hasher.ToHexadecimal(movieHashByte);
				FileInfo movieFileInfo = new FileInfo(movieFileName);
				string movieFileSize = movieFileInfo.Length.ToString();

				Console.WriteLine("File: {0} - Hash: {1} - Size (bytes): {2}", movieFileName, movieHashString, movieFileSize);
				Console.WriteLine("Searching subtitle...");

				OSAPICalls osAPI = new OSAPICalls(movieHashString, movieFileSize, "TemporaryUserAgent");
				MatchResult searchResult = osAPI.searchMovie();
				Console.WriteLine("Found subtitle: {0}", searchResult.SubFileName);
				
				Console.WriteLine("Downloading...");
				WebClient webClient = new WebClient();
				string zipSubtitle = Path.GetFileNameWithoutExtension(movieFileFullName) + ".gz";
				webClient.DownloadFile(searchResult.SubDownloadLink, zipSubtitle);
				Console.WriteLine("Downloaded {0}", zipSubtitle);
				
				Console.WriteLine("Decompressing...");
				FileInfo gzFileInfo = new FileInfo(Path.GetFullPath(currentPath + "\\" + zipSubtitle));
				Decompress(gzFileInfo);

				File.Delete(zipSubtitle);
				Console.WriteLine("Finished. \n-------");
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

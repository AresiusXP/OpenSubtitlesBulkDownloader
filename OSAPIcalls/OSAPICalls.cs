using System;
using System.Collections.Generic;
using RestSharp;

namespace OSAPIcalls
{
	public class OSAPICalls
	{
		const string openSubtitlesEndpoint = "https://rest.opensubtitles.org/search";
		public string userAgent { get; set; }
		public string movieHash { get; set; }
		public string movieByteSize { get; set; }

		public OSAPICalls(string movieHash, string movieByteSize, string userAgent)
		{
			this.movieHash = movieHash;
			this.movieByteSize = movieByteSize;
			this.userAgent = userAgent;
		}

		public MatchResult searchMovie()
		{
			string calculatedEndpoint = String.Format("/moviebytesize-{1}/moviehash-{2}/sublanguageid-eng", openSubtitlesEndpoint, movieByteSize, movieHash);
			var restClient = new RestClient(openSubtitlesEndpoint);
			restClient.UserAgent = userAgent;
			//var restResponse = restClient.Get(new RestRequest());
			var restResponse = restClient.Execute<List<MatchResult>>(new RestRequest(calculatedEndpoint));

			return restResponse.Data[0];
		}
	}

	public class MatchResult
	{
		public string SubFileName { get; set; }
		public string SubDownloadLink { get; set; }
	}
}

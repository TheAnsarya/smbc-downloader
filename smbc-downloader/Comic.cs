using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smbc_downloader {
	class Comic {
		// rough versioning of this class structure
		public string schema_version = "0.1.1";

		// fetch info date, when comic page was scraped
		public DateTime fetch_date;

		// If there is an error during construction and logging in or json constuction is available (TODO: what does this mean?)
		public string error_message;

		// Which comic is this, the first comic is ID == 1, 
		public int ID;

		// URL of this comic
		public string URL;

		// Date comic was published, pulled from the topmost blog post on the page
		public DateTime PublishedDate;

		// Used for days with multiple comics, 1 == first, 2 == second, etc.
		// They are in posted order
		// So 1 if the only comic of the day. If you have two bonus comics then:
		// 1 == bonus comic #1, 2 == bonus comic #2, 3 == main comic
		// Test by starting at https://www.smbc-comics.com/comic/drugs as this is 2017-08-06 and 2017-08-09 has 3 comics
		public int ForThisDate;

		// The unique bit of the url at the end, also works as a title sort of
		public string ShortURL;

		// The actual title
		public string Title;

		// Filename of the comic
		public string ComicFilename;

		// URL of the comic image
		public string ComicURL;

		// Filename of the votey (the red button at the end of the comic)
		public string VoteyFilename;

		// URL of the votey image
		public string VoteyURL;

		// The hover-text/title-text
		public string TitleText;

		// URL clicking comic goes to (used for Soonish and stuff)
		public string ClickURL;

		// URL of the next comic
		public string NextURL;

		// Url to the "Buy a print" website/shop
		public string BuyAPrintURL;

		// If there is a news post, this is the title
		public string TodaysNewsTitle;

		// Content of the news print
		public string TodaysNewsContent;
		
		public Comic() {
		}

		public Comic(
				DateTime fetch_date,
				string error_message,
				int ID,
				string URL,
				DateTime PublishedDate,
				int ForThisDate,
				string ShortURL,
				string ComicFilename,
				string ComicURL,
				string VoteyFilename,
				string VoteyURL,
				string TitleText,
				string ClickURL,
				string NextURL,
				string BuyAPrintURL,
				string TodaysNewsTitle,
				string TodaysNewsContent) {
			this.fetch_date = fetch_date;
			this.error_message = error_message;
			this.ID = ID;
			this.URL = URL;
			this.PublishedDate = PublishedDate;
			this.ForThisDate = ForThisDate;
			this.ShortURL = ShortURL;
			this.ComicFilename = ComicFilename;
			this.ComicURL = ComicURL;
			this.VoteyFilename = VoteyFilename;
			this.VoteyURL = VoteyURL;
			this.TitleText = TitleText;
			this.ClickURL = ClickURL;
			this.NextURL = NextURL;
			this.BuyAPrintURL = BuyAPrintURL;
			this.TodaysNewsTitle = TodaysNewsTitle;
			this.TodaysNewsContent = TodaysNewsContent;
		}

		public string ToJSON() {
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		public Comic FromJSON(string text) {
			return JsonConvert.DeserializeObject<Comic>(text);
		}
	}
}

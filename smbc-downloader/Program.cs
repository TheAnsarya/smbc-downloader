using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// Note: this is probably a terrible example of how to program, but it gets the job done
// If you're looking at this because I applied for a job, yes I understand OO and design patterns and all that
// Sometimes they're not necessary, not everything needs to be abstracted into a framework
// Should really add some error handling though, even if it is a one-off
namespace smbc_downloader {
	class Program {

		/**************************************************************
		/* File names
		/**************************************************************/

		
		// Everything on the console screen is also piped here
		static string LogFileName = @"c:\working\smbc\{now}\log.txt";

		// Errors get logged here as well so if 0 bytes then no errors
		// TODO: Doesn't look like we're using this yet. Should really add some error handling
		static string ErrorFileName = @"c:\working\smbc\{now}\error.txt";

		// The response content from the page download
		static string PageName = @"c:\working\smbc\{now}\pages\{id}.txt";

		// The filename for the comic image
		static string ComicName = @"c:\working\smbc\{now}\comics\{name}";

		// The filename for the votey image
		static string VoteyName = @"c:\working\smbc\{now}\votey\{name}";

		// The filename for the json information file
		static string JsonName = @"c:\working\smbc\{now}\info\{id}_{url}.json";


		/**************************************************************
		/* URLs
		/**************************************************************/

		
		// First page downloaded everything parses and progresses from here
		//static string StartURL = @"https://www.smbc-comics.com/comic/2002-09-05";
		static string StartURL = @"https://www.smbc-comics.com/comic/2013-04-24";
		//static string StartURL = @"https://www.smbc-comics.com/comic/drugs";


		/**************************************************************
		/* Other
		/**************************************************************/

		
		// Log Level
		// 1 == only top messages
		// 2 == also, values of properties
		// 3 == also, match counts
		// 4 == also, debug messages
		static int LogLevel = 1;

		// ID, starts at 0, which comic is this
		// 0 means nothing has been downloaded, ID++; first comic has ID == 1
		// If you're starting part way through and not at the beginning set it to the ID of the comic you want minus 1
		//static int ID = 0;
		static int ID = 2898;

		// Program start time in yyyy-MM-dd_HH-mm-ss-ffff format
		static string NowString = Now();

		// Sometimes you need to fake the date
		static string DiagnosticNow = @"2018-07-26_21-40-28-4545";

		// Null
		// Fun note: this is Caché's $ZD(0)
		static DateTime VoidDate = new DateTime(1840, 12, 31);

		// Used to fetch everything
		static WebClient web;


		/**************************************************************
		/* Regular Expressions
		 * The heart of this whole thing
		/**************************************************************/


		// Will be two, should be the same, check and error if not but continue
		// Group 1: url of next comic page
		static Regex nextURLRegex = new Regex(@"class=""cc-next"" rel=""next"" href=""(.*?)"">");

		// Used to grab the short redirect url, also acts as kind of a title
		// Group 1: unique short redirect url piece
		static Regex shortURLRegex = new Regex(@"https:\/\/www.smbc-comics.com\/comic\/(.+)");

		// The actual title
		// Group 1: comic title
		static Regex titleRegex = new Regex(@"<title>Saturday Morning Breakfast Cereal - (.*?)</title>");

		// Group 1: url clicking comic goes to (used for Soonish and stuff)
		// Group 2: title text
		// Group 3: url of comic image
		// The (?:\t)* in src is because id=2899 2013-04-24 has it
		// The (?:\r\n)* in src is because id=1234 2008-09-04 has it
		static Regex comicRegex = new Regex(@"<div id=""cc-comicbody""><a href=""(.*?)""><img title=""(.*?)"" src=""(.*?)(?:\t)*(?:\r\n)*"" id=""cc-comic"" \/>");

		// TODO: huh, looks the same as the short url regex
		// Group 1: comic/votey filename
		static Regex comicNameRegex = new Regex(@"https:\/\/www.smbc-comics.com\/comics\/(.*)");

		// Group 1: url of the votey
		static Regex voteyRegex = new Regex(@"<div id=""aftercomic"" onclick='toggleBlock\(""aftercomic""\)' style=""display:none;"" class=""mobilehide""><img src='(.*?)'></div>");

		// Group 1: url to the "Buy a print" website/shop
		static Regex buyAPrintRegex = new Regex(@"<a href=""(.*?)""><img id=""buythisimg"" src=""https://www.smbc-comics.com/images/printme.png"" \/><\/a>");

		// This is actually the published time of the top blog post, so it should be the date of comic but you know
		// Use the first one as that's the most recent comment at time of posting the comment.
		// Ignore everything > 1(first) for now, incorporate the blog posts later
		// Group 1: date published
		// Group 2: time published
		static Regex publishedDateRegex = new Regex(@"<div class=""cc-publishtime"">Posted (.*?) at (.*?)<\/div>");

		/*
		// Group 1: published month, in longform, ex "September"
		// Group 2: published day of month, ex "5"
		// Group 3: published year, ex. "2002"
		// Group 4: published time, ex. "12:00"
		// Group 5: published am/pm: ex. "am"
		static Regex publishedDateRegex = new Regex(@"<div class=""cc-publishtime"">Posted ([A-Za-z]+) (\d+), (\d{4}) at (\d+:\d\d) (am|pm)<\/div>");
		*/

		// Group 1: title of the news post
		static Regex todaysNewsTitleRegex = new Regex(@"<div class=""cc-newsarea""><div class=""cc-newsheader"">(.*?)</div>");

		// Group 1: content of the news post
		static Regex todaysNewsContentRegex = new Regex(@"<div class=""cc-newsbody"">(.*?)<div style=""padding:10px;clear:both;""><a href=""http:\/\/www.smbc-comics.com\/smbcforum\/");



		// Characters not allowed in windows file names so you can replace them with another character
		// We should also check for CON, PRN, etc... so this of limitied usefulness, not fully robust
		// Don't think I need it for this project but it's available if I do
		static Regex filenameRegex = new Regex(@"[\*\.""\/\?\\:;|=,]");


		/**************************************************************
		/* Methods
		/**************************************************************/


		// Entry point
		// Can be used to run test methods instead of the full program if needed
		static void Main(string[] args) {
			DownloadSMBC();
			//DownloadTypes();
		}

		// Here we go, the actual program
		static void DownloadSMBC() {
			FixFileNames();
			Directories();

			Log("START");

			// Initialize
			DateTime lastDate = VoidDate.Date;
			int forThisDate = 1;
			web = new WebClient();
			string comicURL = StartURL;

			while ((comicURL != null) && (comicURL != "")) {
				ID++;
				string pageName = PageName.Replace("{id}", ID.ToString());
				Log("-------------------------------------------------");
				Log("page file - " + pageName);
				web.DownloadFile(comicURL, pageName);

				string text = File.ReadAllText(pageName);
				Comic one = ParseComicPage(ID, text, comicURL, lastDate, forThisDate);
				SaveAssets(one);

				// Update last date and forthisdate
				Log("b: " + lastDate.ToString(), 4);
				Log("b: " + one.PublishedDate.Date.ToString(), 4);
				Log("b: " + (lastDate != one.PublishedDate.Date).ToString(), 4);
				if (lastDate != one.PublishedDate.Date) {
					forThisDate = 1;
					Log("FTD = 1, " + forThisDate.ToString(), 4);
				} else {
					forThisDate++;
					Log("FTD other, " + forThisDate.ToString(), 4);
				}
				lastDate = one.PublishedDate.Date;

				comicURL = one.NextURL;
			}

			Wait();

			web.Dispose();

			End();
		}

		// Convert the page to a Comic object
		static Comic ParseComicPage(int id, string text, string url, DateTime lastDate, int forThisDate) {
			Comic one = new Comic();
			Log("f: " + forThisDate.ToString(), 4);
			// Comic ID
			one.ID = id;
			Log("ID: " + id);

			// Fetch date
			one.fetch_date = DateTime.Now;

			// Page URL
			one.URL = url;
			Log("URL: " + url);

			// Published date
			MatchCollection matches = publishedDateRegex.Matches(text);
			Log("PublishedDate match count - " + matches.Count, 3);

			one.PublishedDate = VoidDate;
			if (matches.Count == 0) {
				Error("No PublishedDate");
			} else {
				one.PublishedDate = DateTime.Parse(matches[0].Groups[1].Value + " " + matches[0].Groups[2].Value);
				Log("PublishedDate: " + one.PublishedDate.ToString("yyyy-MM-dd_HH-mm"));
			}

			// For This Date
			Log("a " + lastDate.ToString(), 4);
			Log("a " + one.PublishedDate.Date.ToString(), 4);
			Log("a " + (lastDate != one.PublishedDate.Date).ToString(), 4);
			Log("a " + forThisDate, 4);
			if (lastDate != one.PublishedDate.Date) {
				one.ForThisDate = 1;
			} else {
				// TODO: review, I don't track the logic but +1 is necessary
				one.ForThisDate = forThisDate + 1;
			}
			Log("ForThisDate: " + one.ForThisDate, 1);

			//Console.ReadKey();

			// Next URL
			matches = nextURLRegex.Matches(text);
			Log("NextURL match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No NextURL");
			} else {
				one.NextURL = matches[0].Groups[1].Value;
				Log("NextURL - " + one.NextURL, 2);
			}

			// Short URL
			matches = shortURLRegex.Matches(url);
			Log("ShortURL match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No ShortURL");
			} else {
				one.ShortURL = matches[0].Groups[1].Value;
				Log("ShortURL - " + one.ShortURL, 2);
			}

			// Title
			matches = titleRegex.Matches(text);
			Log("Title match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No Title");
			} else {
				one.Title = matches[0].Groups[1].Value;
				Log("Title - " + one.Title, 2);
			}

			// CLick URL, Title Text, and Comic URL
			matches = comicRegex.Matches(text);
			Log("Comic match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No Comic");
			} else {
				// Click URL
				one.ClickURL = matches[0].Groups[1].Value;
				Log("ClickURL - " + one.ClickURL, 2);

				// Title Text
				one.TitleText = matches[0].Groups[2].Value;
				Log("TitleText - " + one.TitleText, 2);

				// Comic URL
				one.ComicURL = matches[0].Groups[3].Value;
				Log("ComicURL - " + one.ComicURL, 2);
			}

			// Comic Filename
			matches = comicNameRegex.Matches(one.ComicURL);
			Log("ComicFilename match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No ComicFilename");
			} else {
				one.ComicFilename = matches[0].Groups[1].Value;
				Log("ComicFilename - " + one.ComicFilename, 2);
			}

			// Votey
			matches = voteyRegex.Matches(text);
			Log("Votey match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No Votey");
			} else {
				one.VoteyURL = matches[0].Groups[1].Value;
				Log("VoteyURL - " + one.VoteyURL, 2);
			}

			// Votey Filename
			matches = comicNameRegex.Matches(one.VoteyURL);
			Log("VoteyFilename match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No VoteyFilename");
			} else {
				one.VoteyFilename = matches[0].Groups[1].Value;
				Log("VoteyFilename - " + one.VoteyFilename, 2);
			}

			// Buy A Print URL
			matches = buyAPrintRegex.Matches(text);
			Log("BuyAPrintURL match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No BuyAPrintURL");
			} else {
				one.BuyAPrintURL = matches[0].Groups[1].Value;
				Log("BuyAPrintURL - " + one.BuyAPrintURL, 2);
			}

			// Today's News Title
			matches = todaysNewsTitleRegex.Matches(text);
			Log("TodaysNewsTitle match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No TodaysNewsTitle");
			} else {
				one.TodaysNewsTitle = matches[0].Groups[1].Value;
				Log("TodaysNewsTitle - " + one.TodaysNewsTitle, 2);
			}

			// Today's News Content
			matches = todaysNewsContentRegex.Matches(text);
			Log("TodaysNewsContent match count - " + matches.Count, 3);

			if (matches.Count == 0) {
				Error("No TodaysNewsContent");
			} else {
				one.TodaysNewsContent = matches[0].Groups[1].Value;
				Log("TodaysNewsContent - " + one.TodaysNewsContent, 2);
			}

			return one;
		}

		// Download comic, votey, etc
		static void SaveAssets(Comic one) {
			// Comic
			string comicFilename = ComicName.Replace("{name}", one.ComicFilename);
			web.DownloadFile(one.ComicURL, comicFilename);
			Log("Downloaded comic");

			// Votey
			string voteyFilename = VoteyName.Replace("{name}", one.VoteyFilename);
			web.DownloadFile(one.VoteyURL, voteyFilename);
			Log("Downloaded votey");

			// Info
			string jsonFilename = JsonName.Replace("{id}", one.ID.ToString()).Replace("{url}", one.ShortURL);
			File.WriteAllText(jsonFilename, one.ToJSON());
			Log("Saved JSON info");
		}


		// Alter/preface?? (find right word) all the file paths with the DateTime the program was started
		static void FixFileNames() {
			LogFileName = LogFileName.Replace("{now}", NowString);
			ErrorFileName = ErrorFileName.Replace("{now}", NowString);
			PageName = PageName.Replace("{now}", NowString);
			ErrorFileName = ErrorFileName.Replace("{now}", NowString);
			ComicName = ComicName.Replace("{now}", NowString);
			VoteyName = VoteyName.Replace("{now}", NowString);
			JsonName = JsonName.Replace("{now}", NowString);
		}

		// Create directories for the files
		static void Directories() {
			mkdir(LogFileName);
			mkdir(ErrorFileName);
			mkdir(PageName);
			mkdir(ErrorFileName);
			mkdir(ComicName);
			mkdir(VoteyName);
			mkdir(JsonName);
		}

		// Utility method to make a directory
		static void mkdir(string path) {
			string dirName = Path.GetDirectoryName(path);
			if (!Directory.Exists(dirName)) {
				Directory.CreateDirectory(dirName);
			}
		}

		// Log file stream that records eveything that appears on the console
		static StreamWriter _logFile;
		static StreamWriter LogFile {
			get {
				if (_logFile == null) {
					_logFile = File.CreateText(String.Format(LogFileName, Now()));
				}
				return _logFile;
			}
		}

		// Log file stream that records all the errors encountered
		// TODO: Not using this in this project yet. Add error handling
		static StreamWriter _errorFile;
		static StreamWriter ErrorFile {
			get {
				if (_errorFile == null) {
					_errorFile = File.CreateText(String.Format(ErrorFileName, Now()));
				}
				return _errorFile;
			}
		}

		static Stopwatch _clock;
		static Stopwatch Clock {
			get {
				if (_clock == null) {
					_clock = new Stopwatch();
					_clock.Start();
				}
				return _clock;
			}
		}

		// RNG who art in heaven, hallowed be thy name
		static Random _rng;
		static Random RNG {
			get {
				if (_rng == null) {
					_rng = new Random();
				}
				return _rng;
			}
		}

		static string ClockStamp() {
			TimeSpan delta = Clock.Elapsed;
			string stamp = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", delta.Hours, delta.Minutes, delta.Seconds, delta.Milliseconds / 10);
			return stamp;
		}

		static string Now() {
			return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-ffff");
		}

		static string LogStamp() {
			return Now() + " " + ClockStamp() + " --- ";
		}

		static void Log(string message) {
			Log(message, 1);
		}

		static void Log(string message, int level) {
			if (level <= LogLevel) {
				message = LogStamp() + ID + ": " + message;
				Console.WriteLine(message);
				LogFile.WriteLine(message);
				LogFile.Flush();
			}
		}

		static void Error(string message) {
			Log(message);
			message = LogStamp() + ID + ": " + message;
			ErrorFile.WriteLine(message);
			ErrorFile.Flush();
		}

		static void Wait() {
			//int time = 500 + RNG.Next(1000);
			//Thread.Sleep(time);
		}

		static void End() {
			Clock.Stop();
			Log("END");

			ErrorFile.Flush();
			ErrorFile.Close();

			LogFile.Flush();
			LogFile.Close();

			Console.ReadKey();
		}
	}
}

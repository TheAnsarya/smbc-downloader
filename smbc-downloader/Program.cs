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

		// First page downloaded everything parses and progresses from here
		// As a default, is the downloaded version of https://www.smbc-comics.com/comic/2002-09-05
		// TODO: Probably gonna remove this and process StartURL to treat it like every other page
		static string StartFileName = @"c:\working\smbc\{now}\start.txt";

		// TODO: Gonna have to go through these and make sure they suit our purposes
		static string PageName = @"c:\working\smbc\{now}\pages\{i}_{date}_{forthisdate}_{title}.txt";
		static string ComicName = @"c:\working\smbc\{now}\comics\{name}";
		static string Votey = @"c:\working\smbc\{now}\votey\{name}";
		static string ByDateComicName = @"c:\working\smbc\{now}\comics-bydate\{date}\{name}";
		static string ByDateVotey = @"c:\working\smbc\{now}\comics-bydate\votey\{name}";


		/**************************************************************
		/* URLs
		/**************************************************************/

		
		// First page downloaded everything parses and progresses from here
		//static string StartURL = @"https://www.smbc-comics.com/comic/2002-09-05";
		static string StartURL = @"https://www.smbc-comics.com/comic/bar-joke-2";


		/**************************************************************
		/* Other
		/**************************************************************/


		// ID, starts at 0, which comic is this
		// 0 means nothing has been downloaded, ID++; first comic has ID == 1
		static int ID = 0;

		// Program start time in yyyy-MM-dd_HH-mm-ss-ffff format
		static string NowString = Now();

		// Sometimes you need to fake the date
		static string DiagnosticNow = @"2018-07-26_21-40-28-4545";

		// Null
		static DateTime VoidDate = new DateTime(1840, 1, 1);

		/**************************************************************
		/* Regular Expressions
		 * The heart of this whole thing
		/**************************************************************/


		// Will be two, should be the same, check and error if not but continue
		// Group 1: url of next comic page
		static Regex nextUrlRegex = new Regex(@"class=""cc-next"" rel=""next"" href=""(.*?)"">");

		// Used to grab the short redirect url, also acts as kind of a title
		// Group 1: unique short redirect url piece
		static Regex shorUrlRegex = new Regex(@"https:\/\/www.smbc-comics.com\/comic\/(.+)");

		// The actual title
		// Group 1: comic title
		static Regex titleRegex = new Regex(@"<title>Saturday Morning Breakfast Cereal - (.*?)</title>");

		// Group 1: url clicking comic goes to (used for Soonish and stuff)
		// Group 2: title text
		// Group 3: url of comic image
		static Regex comicRegex = new Regex(@"<div id=""cc-comicbody""><a href=""(.*?)""><img title=""(.*?)"" src=""(.*?)"" id=""cc-comic"" \/>");

		// Group 1: url of the aftercomic/votey
		static Regex afterComicRegex = new Regex(@"<div id=""aftercomic"" onclick='toggleBlock\(""aftercomic""\)' style=""display:none;"" class=""mobilehide""><img src='(.*?)'></div>");

		// Group 1: url to the "Buy a print" website/shop
		static Regex buyAPrintRegex = new Regex(@"<a href=""(.*?)""><img id=""buythisimg"" src=""https://www.smbc-comics.com/images/printme.png"" \/><\/a>");

		// This is actually the published time of the top blog post, so it should be the date of comic but you know
		// Use the first one as that's the most recent comment at time of posting the comment.
		// Ignore everything > 1(first) for now, incorporate the blog posts later
		static Regex publishedDateRegex = new Regex(@"<div class=""cc-publishtime"">Posted (.*?) at (.*?)<\/div>");

		/*
		// Group 1: published month, in longform, ex "September"
		// Group 2: published day of month, ex "5"
		// Group 3: published year, ex. "2002"
		// Group 4: published time, ex. "12:00"
		// Group 5: published am/pm: ex. "am"
		static Regex publishedDateRegex = new Regex(@"<div class=""cc-publishtime"">Posted ([A-Za-z]+) (\d+), (\d{4}) at (\d+:\d\d) (am|pm)<\/div>");
		*/

		static Regex TodaysNewsTitle = new Regex(@"<div class=""cc-newsarea""><div class=""cc-newsheader"">(.*?)</div>");

		static Regex TodaysNewsContent = new Regex(@"<div class=""cc-newsbody""><p>(.*?)</p><div style=""padding:10px;clear:both;"">");



		// Characters not allowed in windows file names so you can replace them with another character
		// We should also check for CON, PRN, etc... so this of limitied usefulness, not fully robust
		// Don't think I need it for this project but it's available if I do
		static Regex filenameRegex = new Regex(@"[\*\.""\/\?\\:;|=,]");


		/**************************************************************
		/* Regular Expressions
		 * The heart of this whole thing
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

			WebClient web = new WebClient();

			string startName = String.Format(StartFileName, Now());
			Log("start file - " + startName);
			web.DownloadFile(StartURL, startName);

			string text = File.ReadAllText(startName);
			MatchCollection startMatches = publishedDateRegex.Matches(text);
			Log("start match count - " + startMatches.Count);

			DateTime publishedDate = VoidDate;
			if (startMatches.Count == 0) {
				Error("No published date");
			} else {
				publishedDate = DateTime.Parse(startMatches[0].Groups[1].Value + " " + startMatches[0].Groups[2].Value);
				Log("published date - " + publishedDate.ToString("yyyy-MM-dd_HH-mm-ss-ffff"));
			}





			Wait();

			web.Dispose();

			End();
		}

		// Alter/preface?? (find right word) all the file paths with the DateTime the program was started
		static void FixFileNames() {
			LogFileName = LogFileName.Replace("{now}", NowString);
			ErrorFileName = ErrorFileName.Replace("{now}", NowString);
			StartFileName = StartFileName.Replace("{now}", NowString);
			PageName = PageName.Replace("{now}", NowString);
			ErrorFileName = ErrorFileName.Replace("{now}", NowString);
			ComicName = ComicName.Replace("{now}", NowString);
			Votey = Votey.Replace("{now}", NowString);
			ByDateComicName = ByDateComicName.Replace("{now}", NowString);
			ByDateVotey = ByDateVotey.Replace("{now}", NowString);
		}

		// Create directories for the files
		static void Directories() {
			mkdir(LogFileName);
			mkdir(ErrorFileName);
			mkdir(StartFileName);
			mkdir(PageName);
			mkdir(ErrorFileName);
			mkdir(ComicName);
			mkdir(Votey);
			mkdir(ByDateVotey);
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
			message = LogStamp() + message;
			Console.WriteLine(message);
			LogFile.WriteLine(message);
			LogFile.Flush();
		}

		static void Error(string message) {
			Log(message);
			message = LogStamp() + message;
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

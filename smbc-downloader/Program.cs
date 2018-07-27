using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace smbc_downloader {
	class Program {
		static string LogFileName = @"c:\working\smbc\{now}\log.txt";
		static string ErrorFileName = @"c:\working\smbc\{now}\error.txt";
		static string StartURL = @"https://www.smbc-comics.com/comic/2002-09-05"; // @"https://www.smbc-comics.com/comic/literary-analysis";
		static string StartFileName = @"c:\working\smbc\{now}\start.txt";
		static string PageName = @"c:\working\smbc\{now}\pages\{i}_{date}_{title}.txt";
		static string ComicName = @"c:\working\smbc\{now}\comics\{name}";
		static string Votey = @"c:\working\smbc\{now}\votey\{name}";
		static string ByDateComicName = @"c:\working\smbc\{now}\comics-bydate\{date}\{name}";
		static string ByDateVotey = @"c:\working\smbc\{now}\comics-bydate\votey\{name}";


		static string DiagnosticNow = @"2018-07-26_21-40-28-4545";

		static string NowString = Now();

		// Will be two, should be the same, check and error if not but continue
		// Group 1: url of next comic page
		static Regex nextUrlRegex = new Regex(@"class=""cc-next"" rel=""next"" href=""(.*?)"">");

		// Group 1: url clicking comic goes to (use for Soonish and stuff)
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
		// Group 1: published month, in longform, ex "September"
		// Group 2: published day of month, ex "5"
		// Group 3: published year, ex. "2002"
		// Group 4: published time, ex. "12:00"
		// Group 5: published am/pm: ex. "am"
		static Regex publishedDateRegex = new Regex(@"<div class=""cc-publishtime"">Posted ([A-Za-z]+) (\d+), (\d{4}) at (\d+:\d\d) (am|pm)<\/div>");

		static Regex filenameRegex = new Regex(@"[\*\.""\/\?\\:;|=,]");

		static void Main(string[] args) {
			DownloadYTS();
			//DownloadTypes();
		}

		static void DownloadYTS() {
			FixFileNames();
			Directories();

			Log("START");

			WebClient web = new WebClient();

			string startName = String.Format(StartFileName, Now());
			Log("start file - " + startName);
			web.DownloadFile(StartURL, startName);
			Wait();





			End();
		}

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

		static void mkdir(string path) {
			string dirName = Path.GetDirectoryName(path);
			if (!Directory.Exists(dirName)) {
				Directory.CreateDirectory(dirName);
			}
		}

		static StreamWriter _logFile;
		static StreamWriter LogFile {
			get {
				if (_logFile == null) {
					_logFile = File.CreateText(String.Format(LogFileName, Now()));
				}
				return _logFile;
			}
		}

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

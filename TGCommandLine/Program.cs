﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using TGServiceInterface;
using TGServiceInterface.Components;

namespace TGCommandLine
{

	class Program
	{
		static Command.ExitCode RunCommandLine(IList<string> argsAsList)
		{
			//first lookup the connection string
			bool badConnectionString = false;
			for (var I = 0; I < argsAsList.Count - 1; ++I) {
				var lowerarg = argsAsList[I].ToLower();
				if (lowerarg == "-c" || lowerarg == "--connect")
				{
					var connectionString = argsAsList[I + 1];
					var splits = connectionString.Split('@');
					var userpass = splits[0].Split(':');
					if (splits.Length != 2 || userpass.Length != 2)
					{
						badConnectionString = true;
						break;
					}
					var addrport = splits[1].Split(':');
					if (addrport.Length != 2)
					{
						badConnectionString = true;
						break;
					}
					var username = userpass[0];
					var password = userpass[1];
					var address = addrport[0];
					ushort port;
					try
					{
						port = Convert.ToUInt16(addrport[1]);
					}
					catch
					{
						badConnectionString = true;
						break;
					}
					if(String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password) || String.IsNullOrWhiteSpace(address))
					{
						badConnectionString = true;
						break;
					}
					argsAsList.RemoveAt(I);
					argsAsList.RemoveAt(I);
					Interface.SetRemoteLoginInformation(address, port, username, password);
					break;
				}
			}

			if (badConnectionString)
			{
				Console.WriteLine("Remote connection usage: <-c/--connect> username:password@address:port");
				return Command.ExitCode.BadCommand;
			}

			var res = Interface.VerifyConnection();
			if (res != null)
			{
				Console.WriteLine("Unable to connect to service: " + res);
				Console.WriteLine("Remote connection usage: <-c/--connect> username:password@address:port");
				return Command.ExitCode.ConnectionError;
			}

			if (!Interface.Authenticate())
			{
				Console.WriteLine("Authentication error: Username/password/windows identity is not authorized!");
				return Command.ExitCode.ConnectionError;
			}

			if (!SentVMMWarning && Interface.VersionMismatch(out string error))
			{
				SentVMMWarning = true;
				Console.WriteLine(error);
			}

			try
			{
				return new CLICommand().DoRun(argsAsList);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error: " + e.ToString());
				return Command.ExitCode.ConnectionError;
			};
		}
		public static string ReadLineSecure()
		{
			string result = "";
			while (true)
			{
				ConsoleKeyInfo i = Console.ReadKey(true);
				if (i.Key == ConsoleKey.Enter)
				{
					break;
				}
				else if (i.Key == ConsoleKey.Backspace)
				{
					if (result.Length > 0)
					{
						result = result.Substring(0, result.Length - 1);
						Console.Write("\b \b");
					}
				}
				else
				{
					result += i.KeyChar;
					Console.Write("*");
				}
			}
			Console.WriteLine();
			return result;
		}
		static bool SentVMMWarning = false;
		static string AcceptedBadCert;
		static bool BadCertificateInteractive(string message)
		{
			if (AcceptedBadCert == message)
				return true;
			Console.WriteLine(message);
			Console.Write("Do you wish to continue? NOT RECCOMENDED! (y/N): ");
			var result = Console.ReadLine().Trim().ToLower();
			if (result == "y" || result == "yes")
			{
				AcceptedBadCert = message;
				return true;
			}
			return false;
		}

		static int Main(string[] args)
		{
			Command.OutputProcVar.Value = Console.WriteLine;
			if (args.Length != 0)
			{
				Interface.SetBadCertificateHandler((message) => {
					foreach (var I in args)
						if (I.ToLower() == "--disable-ssl-verification")    //im just not even going to document this because i hate it so much
							return true;
					return false;
				});
				//allow self signed certs in debug mode
				return (int)RunCommandLine(new List<string>(args));
			}

			Interface.SetBadCertificateHandler(BadCertificateInteractive);

			Console.WriteLine("Type 'remote' to connect to a remote service");
			//interactive mode
			while (true)
			{
				Console.Write("Enter command: ");
				var NextCommand = Console.ReadLine();
				switch (NextCommand.ToLower())
				{
					case "remote":
						SentVMMWarning = false;
						Console.Write("Enter server address: ");
						var address = Console.ReadLine();
						Console.Write("Enter server port: ");
						ushort port;
						try{
							port = Convert.ToUInt16(Console.ReadLine());
						}
						catch
						{
							Console.WriteLine("Error: Bad port!");
							break;
						}
						Console.Write("Enter username: ");
						var username = Console.ReadLine();
						Console.Write("Enter password: ");
						var password = ReadLineSecure();
						Interface.SetRemoteLoginInformation(address, port, username, password);
						var res = Interface.VerifyConnection();
						if (res != null)
						{
							Console.WriteLine("Unable to connect: " + res);
							Interface.MakeLocalConnection();
						}
						else if (!Interface.Authenticate())
						{
							Console.WriteLine("Authentication error: Username/password/windows identity is not authorized! Returning to local mode...");
							Interface.MakeLocalConnection();
						}
						else
						{
							Console.WriteLine("Connected remotely");
							if (Interface.VersionMismatch(out res))
							{
								SentVMMWarning = true;
								Console.WriteLine(res);
							}
							Console.WriteLine("Type 'disconnect' to return to local mode");
						}
						break;
					case "disconnect":
						SentVMMWarning = false;
						Interface.MakeLocalConnection();
						Console.WriteLine("Switch to local mode");
						break;
					case "quit":
					case "exit":
						return (int)Command.ExitCode.Normal;
#if DEBUG
					case "debug-upgrade":
						Interface.GetComponent<ITGSService>().PrepareForUpdate();
						return (int)Command.ExitCode.Normal;
#endif
					default:
						//linq voodoo to get quoted strings
						var formattedCommand = NextCommand.Split('"')
										   .Select((element, index) => index % 2 == 0  // If even index
										   ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
										   : new string[] { element })  // Keep the entire item
										   .SelectMany(element => element).ToList();

						formattedCommand = formattedCommand.Select(x => x.Trim()).ToList();
						formattedCommand.Remove("");
						RunCommandLine(formattedCommand);
						break;
				}
			}
		}
	}
}
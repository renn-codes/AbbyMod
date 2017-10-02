using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace AbbyMod
{
    internal class IRCConfig
    {
        public bool joined;
        public string server;
        public int port;
        public string nick;
        public string name;
        public string pass;
        public List<string> regularsList;
        public string channel;

        //wordfilter
        public List<string> wordList;
        public List<string> warnList;
        public string suspend;
        public int infractions;

        private StreamReader sr = null;
        private StreamWriter sw = null;

        public IRCConfig()
        {
            wordList = new List<string>();
            warnList = new List<string>();
            regularsList = new List<string>();
        }

        public void loadRegulars()
        {
            string line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sr = new StreamReader("regulars.txt");

                //Read the first line of text
                line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null)
                {
                    if (!line.Contains("//"))
                    {
                        //write the lie to console window
                        this.regularsList.Add(line);
                        Console.WriteLine("Added user '" + regularsList.Last() + "' to the regulars database from config");
                        //Read the next line
                        line = sr.ReadLine();
                    }
                    else
                    {
                        line = sr.ReadLine();
                    }
                }

                //close the file
                sr.Close();
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot load regulars database: regulars.txt missing, please recreate.\n\nExample file:\n--------------------------------\n//Do not remove this, this will track regular vewers to your channel\n***\n//you can manually enter regular below");
                throw;
            }
        }

        public void loadWords()
        {
            string line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sr = new StreamReader("banwords.txt");

                //Read the first line of text
                line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null)
                {
                    if (!line.Contains("//"))
                    {
                        //write the lie to console window
                        this.wordList.Add(line);
                        Console.WriteLine("Added word '" + wordList.Last() + "' to the banword database from config");
                        //Read the next line
                        line = sr.ReadLine();
                    }
                    else
                    {
                        line = sr.ReadLine();
                    }
                }

                //close the file
                sr.Close();
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot load ban word database: banwords.txt missing, please recreate.\n\nExample file:\n--------------------------------\n//Do not remove this, this will catch words banned by Twitch's ban filter\n***\n//Enter banned words below");
                throw;
            }
        }

        public void loadPrefs()
        {
            string line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sr = new StreamReader("config.txt");

                //Read the first line of text
                line = sr.ReadLine();

                int i = 0;
                string[] prefTemp = new string[5];

                //Continue to read until you reach end of file
                while (line != null)
                {
                    if (!line.Contains("//"))
                    {
                        //write the lie to console window
                        prefTemp[i] = line;
                        //Read the next line
                        line = sr.ReadLine();
                        i++;
                    }
                    else
                    {
                        line = sr.ReadLine();
                    }
                }

                //close the file
                sr.Close();
                this.name = prefTemp[0];
                Console.WriteLine("Sucessfully set IRC name to " + this.name);
                this.nick = prefTemp[0];
                Console.WriteLine("Sucessfully set IRC nick to " + this.nick);
                this.channel = prefTemp[1];
                Console.WriteLine("Sucessfully set IRC channel to #" + this.channel);
                this.pass = prefTemp[2];
                Console.WriteLine("Sucessfully set oauth password to " + this.pass);
                this.suspend = prefTemp[3];
                Console.WriteLine("Sucessfully set chat suspend time to " + this.suspend);
                int numVal;
                try
                {
                    numVal = Convert.ToInt32(prefTemp[4]);
                }
                catch
                {
                    Console.WriteLine("Cannot load the server config: the infractions value in config.txt is not a number and/or has whitespace");
                    throw;
                }
                this.infractions = numVal;
                Console.WriteLine("Sucessfully set chat infractions before ban to " + this.infractions.ToString());
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot load the server config: config.txt missing, please recreate.\n\nExample file:\n--------------------------------\n//ATTENTION: ANY WHITESPACE/INCORRECT ENTRIES WILL BREAK YOUR CONFIG (I'll add a config checker at some point)\n//Bot Twitch account name--Twitch account name for the bot's account\n//Broadcaster Twitch username--Twitch account username for the channel that this bot is moderating for (usually yours)\n//Twitch OAuth password--Your Twitch OAuth password (without whitspace). If you need a new one, get it from http://www.twitchapps.com/tmi\n//Banned word chat suspend--Amount of time to ban users from chat when they use a banned word, default is 10\n//Infractions before chat ban--Amount of temporary bans before a user is perma-banned, default is 3\nbotusername\nyourusername\noauth\n10\n3");
                throw;
            }
        }

        public void loadWarns()
        {
            string line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sr = new StreamReader("warnlist.txt");

                //Read the first line of text
                line = sr.ReadLine();

                //Continue to read until you reach end of file
                while (line != null)
                {
                    //write the line to console window
                    this.warnList.Add(line);
                    Console.WriteLine("Added user[warnings] '" + warnList.Last() + "' to the user warning list from config");
                    //Read the next line
                    line = sr.ReadLine();
                }

                //close the file
                sr.Close();
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot load the user warning database: warnlist.txt missing, please recreate.");
                throw;
            }
        }

        public void saveWarns()
        {
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sw = new StreamWriter("warnlist.txt");

                //Write the user warn database to file line by line
                foreach (string item in warnList)
                {
                    //write the line to console window
                    sw.WriteLine(item.ToString());
                    Console.WriteLine("Updated user instance '" + item + "' to the user warning database from this session");
                }

                //close the file
                sw.Close();
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot save the user warning database.");
                throw;
            }
        }

        public void saveWords()
        {
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sw = new StreamWriter("banwords.txt");

                //Write the user warn database to file line by line
                foreach (string item in wordList)
                {
                    //write the line to console window
                    sw.WriteLine(item.ToString());
                    Console.WriteLine("Updated word '" + item + "' to the banword database from this session");
                }

                //close the file
                sw.Close();
                Console.WriteLine("\n");
            }
            catch
            {
                Console.WriteLine("Cannot save the user warning database.");
                throw;
            }
        }
    }
}

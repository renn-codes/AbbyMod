using System;
using System.Collections.Generic;
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
        public string[] regulars;
        public string channel;

        //wordfilter
        public List<string> wordList;
        public int suspend;

        private StreamReader sr = null;
        private StreamWriter sw = null;

        public IRCConfig()
        {
            wordList = new List<string>();
        }

        public void loadWords()
        {
            string line;
            try
            {
                //Pass the file path and file name to the StreamReader constructor
                sr = new StreamReader("banWords.txt");

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
                Console.ReadLine();
            }
            catch
            {
                Console.WriteLine("Cannot load ban word database: wordlist.txt missing, please recreate.");
                throw;
            }
        }
    }

    internal class IRCBot : IDisposable
    {
        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;
        public IRCBot(IRCConfig config)
        {
            this.config = config;
        }

        public void Connect()
        {
            try
            {
                IRCConnection = new TcpClient(config.server, config.port);
            }
            catch
            {
                Console.WriteLine("Connection Error");
                throw;
            }

            try
            {
                ns = IRCConnection.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sendData("PASS", config.pass);
                //sendData("USER", config.nick + " 0 * " + config.name, false);
                sendData("NICK", config.nick);
            }
            catch
            {
                Console.WriteLine("Communication error");
                throw;
            }
        }

        public void sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
                
                Console.WriteLine(cmd);
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();

                Console.WriteLine(cmd + " " + param);
            }
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == ' ')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void IRCWork()
        {
            StreamWriter sw2 = new StreamWriter(ns);
            string[] usrScan;
            string usr;
            string user;
            string[] ex;
            string data;
            string line;
            bool shouldRun = true;
            while (shouldRun)
            {
                data = sr.ReadLine();
                Console.WriteLine(data); //Used for debugging
                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 4); //Split the data into 5 parts
                if (!config.joined) //if we are not yet in the assigned channel
                {
                    //if (ex[1] == "MODE") //Normally one of the last things to be sent (usually follows motd)
                    //{
                        sendData("JOIN", config.channel); //join assigned channel

                        config.joined = true;
                    //}
                }

                if (ex[0] == "PING")  //respond to pings
                {
                    sendData("PONG", ex[1]);
                }

                
                //**WordFilter
                if (ex.Length > 3) //is the command received long enough to be a bot command?
                {
                    if(ex[1] == "PRIVMSG")
                	{
                        string message;
                           
                        //Grab the message for scanning
                        message = ex[3];
                              
                        //cehck for termination
                        if (message == ":!quit")
                        {
                            sendData("QUIT", "");
                            //if the command is quit, send the QUIT command to the server with a quit message
                            shouldRun = false;
                        }
                        else if (message == ":hello bot")
                        {
                            line = "meows warmly at you";
                            sendData("PRIVMSG", ex[2] + " :" + line);
                            Console.WriteLine(line);
                            line = null;
                        }
                        else
                        {
                            usr = ex[0];
                            message = RemoveSpecialCharacters(message);
                            string[] msgScan = message.Split(' ');
                            usrScan = usr.Split('@');
                            user = usrScan[1].Remove(usrScan[1].IndexOf('.'));
                            foreach (string word in msgScan)
                            {
                                foreach (string badword in config.wordList)
                                {
                                    if (word.IndexOf(badword, 0, StringComparison.CurrentCultureIgnoreCase) != -1)
                                    {
                                        // The string exists in the original
                                        line = "it works!";
                                        //line = ".timeout" + " " + user + " " + config.suspend.ToString();
                                        sendData("PRIVMSG", ex[2] + " :" + line);
                                        Console.WriteLine(line);
                                        line = null;
                                    }
                                }
                            }
                        } 
                    }
                    else if(ex[1] == "JOIN")
                    {
                        usr = ex[0];
                        usrScan = usr.Split('@');
                        user = usrScan[1].Remove(usrScan[1].IndexOf('.'));
                        line = "Welcome to her stream, " + user;
                        sendData("PRIVMSG", ex[2] + " :" + line);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();
        }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
            IRCConfig conf = new IRCConfig();
            conf.name = "BOTNAME";
            conf.nick = "cthulhucultist_bot";
            conf.port = 6667;
            conf.channel = "#comrade_nekobear";
            conf.server = "irc.twitch.tv";
            conf.suspend = 10; //default time 10
            conf.pass = "oauth:av4xp12aq4hy7by6z144tzspl9rzv0";
            using (var bot = new IRCBot(conf))
            {
                conf.loadWords();
                conf.joined = false;
                bot.Connect();
                bot.IRCWork();
            }
            Console.WriteLine("Bot quit/crashed");
            Console.ReadLine();
        }
    }
}

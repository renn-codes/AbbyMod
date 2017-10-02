using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace AbbyMod
{
    internal class IRCBot : IDisposable
    {
        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;
        //private int greetWait = 0;
        List<string> outputUser = new List<string>();
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
                sendData("CAP REQ :twitch.tv/membership");
            }
            catch
            {
                Console.WriteLine("Communication error");
                throw;
            }
        }

        public void sendData(string cmd, string param = null)
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

        public static string RemoveSpaces(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c != ' ')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void IRCWork()
        {
            //StreamWriter sw2 = new StreamWriter(ns);
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
                    line = "#" + config.channel;
                        sendData("JOIN", line); //join assigned channel

                        config.joined = true;
                    //}
                }

                if (ex[0] == "PING")  //respond to pings
                {
                    sendData("PONG", ex[1]);
                }

                
                //**WordFilter
                if (ex.Length > 2) //is the command received long enough to be a bot command?
                {
                    if (ex[1] == "PRIVMSG")
                    {
                        string message;

                        //Grab the message for scanning
                        message = ex[3];
                        string[] botCmd = ex[3].Split(' ');
                        //check for termination
                        if (message == ":hello bot")
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
                                        if (user != config.channel)
                                        {
                                            config.warnList.Add(user);
                                            int wrn = config.warnList.Where(x => x.Equals(user)).Count();
                                            //Console.WriteLine("DEBUG" + wrn + " " + user);
                                            if (wrn > config.infractions - 1)
                                            {
                                                line = ".ban" + " " + user;
                                                sendData("PRIVMSG", ex[2] + " :" + line);
                                                Console.WriteLine(user + " has been banned (3/3 chat infractions)");
                                                line = null;
                                                config.warnList.RemoveAll(item => item == user);
                                                //Console.WriteLine("DEBUG: User has been removed from warnList Object");
                                            }
                                            else
                                            {
                                                line = ".timeout" + " " + user + " " + config.suspend;
                                                sendData("PRIVMSG", ex[2] + " :" + line);
                                                Console.WriteLine(user + " has been suspended from chat for " + config.suspend + " minutes and given an infraction (" + wrn.ToString() + "/3 chat infractions)");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        switch (botCmd[0])
                        {
                            case ":!quit":
                                sendData("QUIT", "");
                                //if the command is quit, send the QUIT command to the server with a quit message
                                shouldRun = false;
                                break;
                            case ":!addw":
                                try
                                {
                                    string[] firstWord = ex[3].Split(' ');
                                    config.wordList.Add(firstWord[1]);
                                    Console.WriteLine("Added word to the banned words database. It will be saved on a clean disconnect.");
                                }
                                catch
                                {
                                    Console.WriteLine("Error: No word given to add to banword database.");
                                }
                                break;
                            case ":!infr":
                                try
                                {
                                    foreach (string outUser in config.warnList)
                                    {
                                        if (!outputUser.Contains(outUser))
                                        {
                                            outputUser.Add(outUser);
                                            int outWrn = config.warnList.Where(x => x.Equals(outUser)).Count();
                                            Console.WriteLine("USER:  " + outUser + "  WARN:  " + outWrn.ToString());
                                        }
                                    }
                                    outputUser.Clear();
                                }
                                catch
                                {

                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else if (ex[1] == "JOIN")
                    {
                        Console.WriteLine(ex[0].ToString());
                        usr = ex[0];
                        usrScan = usr.Split('@');
                        user = usrScan[1].Remove(usrScan[1].IndexOf('.'));
                        delayedGreet(ex[2], user);
                        //else
                        //{
                        //    line = "Welcome to the stream, " + user + "  ^.^";
                        //    delayedGreet(ex[2], line);
                        //}
                        //sendData("PRIVMSG", ex[2] + " :" + line);
                    }
                }
            }
        }

        private async void delayedGreet(string channel, string user)
        {
            await Task.Delay(TimeSpan.FromSeconds(0));
            int code = 0;
            string line = "";
            foreach(string item in config.regularsList)
            {
                if (user == item) { code = 1; }
            }
            if (user == config.nick) { code = 2; } 
            switch (code)
            {
                case 0:
                    line = "Welcome to the stream, " + user + "!";
                    break;
                case 1:
                    line = "You's a special mothafucka, " + user + "!";
                    break;
                case 2:
                    line = "Meow :3";
                    break;
            }
            
            sendData("PRIVMSG", channel + " :" + line);
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
            conf.port = 6667;
            conf.server = "irc.twitch.tv";
            using (var bot = new IRCBot(conf))
            {
                conf.loadWords();
                conf.loadPrefs();
                conf.loadWarns();
                conf.loadRegulars();
                conf.joined = false;
                bot.Connect();
                bot.IRCWork();
            }
            conf.saveWarns();
            conf.saveWords();
            Console.WriteLine("Bot quit/crashed");
            Console.ReadLine();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;


namespace PongServer
{
    class Program
    {

        public static bool running = true;
        public static bool gameEnabled = true;
        public static int rightPaddleHeight = 20;
        public static int leftPaddleHeight = 20;

        public static float hitMultiply = 1.3f;

        public static float balldx = 0.1f;
        public static float balldy = 0.3f;

        public const float balldxOriginal = 0.1f;
        public const float balldyOriginal = 0.3f;

        public static int totalPointsWon = 0;

        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            SimpleListenerExample(args);
            handleQueue();
            simulateBall();
            string consoleValue = Console.ReadLine();
            while (consoleValue != "quit")
            {

                if (consoleValue.StartsWith("BallSpeedMultiply="))
                {
                    float mult = System.Convert.ToSingle(consoleValue.Replace("BallSpeedMultiply=", ""));
                    balldx = mult * balldxOriginal;
                    balldy = mult * balldyOriginal;
                }else if (consoleValue.StartsWith("HitMultiply="))
                {
                    float mult = System.Convert.ToSingle(consoleValue.Replace("HitMultiply=", ""));
                    hitMultiply = mult;
                }
                else if (consoleValue.StartsWith("PaddleHeight="))
                {
                    int mult = int.Parse(consoleValue.Replace("PaddleHeight=", ""));
                    leftPaddleHeight = mult;
                    rightPaddleHeight = mult;
                }
                else if (consoleValue.StartsWith("PointsWon"))
                {
                    Console.WriteLine(totalPointsWon);
                }
                else if (consoleValue.StartsWith("UniquePlayers"))
                {
                    Console.WriteLine(totalUnique);
                }

                consoleValue = Console.ReadLine();
            }
        }

        public static void simulateBall()
        {
            new Thread(async () =>
            {

                while (true)
                {
                    while (gameEnabled)
                    {
                        Thread.Sleep(16);
                        game.ballPos.x += game.ballPos.dx;
                        game.ballPos.y += game.ballPos.dy;
                        if (game.ballPos.y > 100 || game.ballPos.y < 0)
                        {
                            game.ballPos.dy *= -1;
                        }

                        //hitting right paddle
                        if (game.ballPos.x > 97)
                        {
                            if (game.rightPlayer != null && Math.Abs(game.ballPos.y - game.rightPlayer.y) < rightPaddleHeight)
                            {
                                game.ballPos.dx *= -(hitMultiply);
                                game.ballPos.dy += game.rightPlayer.dy / 60;
                                game.ballPos.x = 96.9f;
                            }
                            else
                            {
                                //rightPlayerLost
                                game.ballPos.dx *= -1;
                                if (game.leftPlayer != null)
                                {
                                    game.leftPlayer.score += 1;
                                    totalPointsWon += 1;
                                }
                                
                                if (queue.Count > 1)
                                {
                                    queue.Remove(queue[1]);
                                    gameEnabled = false;
                                    game.ballPos = new ballPosition();
                                }
                                
                            }
                        }else if (game.ballPos.x < 3)
                        {
                            if (game.leftPlayer != null && Math.Abs(game.ballPos.y - game.leftPlayer.y) < leftPaddleHeight)
                            {
                                game.ballPos.dx *= -(hitMultiply);
                                game.ballPos.dy += game.leftPlayer.dy / 60;
                                game.ballPos.x = 3.1f;
                            }
                            else
                            {
                                //leftPlayerLost
                                game.ballPos.dx *= -1;
                                if (game.rightPlayer != null && game.leftPlayer != null)
                                {
                                    //game.rightPlayer.score += 1 + game.leftPlayer.score;
                                }else if (game.rightPlayer != null)
                                {
                                    game.rightPlayer.score += 1;
                                    totalPointsWon += 1;
                                }
                                
                                if (queue.Count > 1)
                                {
                                    queue.Remove(queue[0]);
                                    gameEnabled = false;
                                    game.ballPos = new ballPosition();
                                }

                            }
                        }
                    }
                }

            }).Start();
        }

        public static void handleQueue()
        {
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (running) { 
                    Thread.Sleep(50);
                    List<PlayerDetails> nukelist = new List<PlayerDetails>();
                    for (var i = 0; i < queue.Count; i++)
                    {
                        if ((DateTime.Now - queue[i].time).TotalMinutes > 2 || (queue[i].isUpNow && ((DateTime.Now - queue[i].time).TotalSeconds > 10)) || queue[i].hasLost)
                        {
                            nukelist.Add(queue[i]);
                        }
                    }

                    foreach (var nukeItem in nukelist)
                    {
                        Console.WriteLine("Had to nuke a player with GUID: " + nukeItem.uid.ToString());
                        Console.WriteLine(totalPointsWon.ToString() + " total points awarded");
                        queue.Remove(nukeItem);
                    }

                    if (queue.Count >= 2)
                    {
                        var player1 = queue[0];
                        player1.isUpNow = true;
                        //player1.time = DateTime.Now;
                        game.leftPlayer = player1;
                        queue[0] = player1;

                        var player2 = queue[1];
                        player2.isUpNow = true;
                        //player2.time = DateTime.Now;
                        game.rightPlayer = player2;
                        queue[1] = player2;

                        gameEnabled = true;

                    }else if (queue.Count == 0)
                    {
                        game.leftPlayer = null;
                        game.rightPlayer = null;
                    }else if (queue.Count == 1)
                    {
                        if (game.rightPlayer != null && queue[0].uid == game.rightPlayer.uid)
                        {
                            game.leftPlayer = game.rightPlayer;
                            game.rightPlayer = null;
                        }else if (game.leftPlayer != null && queue[0].uid == game.leftPlayer.uid)
                        {
                            //do nothing, everything is good
                            game.rightPlayer = null;
                        }
                        else
                        {
                            game.leftPlayer = queue[0];
                        }
                    }
                }

            }).Start();
        }

        public static void SimpleListenerExample(string[] prefixes)
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            Console.WriteLine("Starting server...");

            new Thread(async () =>
            {

                Console.WriteLine("Server running in background thread...");

                Thread.CurrentThread.IsBackground = true;

                // Create a listener.
                HttpListener listener = new HttpListener();
                // Add the prefixes.
                foreach (string s in prefixes)
                {
                    listener.Prefixes.Add(s);
                }
                listener.Start();
                while (running)
                {
                    // Note: The GetContext method blocks while waiting for a request. 
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    //Console.WriteLine("Request received at endpoint: " + request.RawUrl);
                    // Obtain a response object.
                    HttpListenerResponse response = context.Response;
                    // Construct a response.
                    string responseString = "";//"<HTML><BODY> Hello world!" + request.RawUrl + "</BODY></HTML>";
                    try
                    {
                        responseString = generateResponseForRequest(request);
                    }
                    catch (Exception e)
                    {
                        responseString = e.Message;
                    }

                    response.Headers.Add("Access-Control-Allow-Origin:*");

                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    // Get a response stream and write the response to it.
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    // You must close the output stream.
                    output.Close();
                    //Console.WriteLine("Responded to requester...");
                }
                listener.Stop();
            }).Start();
        }

        public static PongGame game = new PongGame();

        public static int totalUnique = 0;

        public static string generateResponseForRequest(HttpListenerRequest request)
        {
            string req = request.Url.Query.Remove(0,1);
            if (req == "getUUID")
            {
                totalUnique += 1;
                var newPlayer = new PlayerDetails();
                queue.Add(newPlayer);
                Console.WriteLine(totalUnique.ToString() + " unique players");
                return newPlayer.uid.ToString();
            }else if (req.StartsWith("getQueuePosition&guid="))
            {
                req = req.Replace("getQueuePosition&guid=", "");
                Guid lookupGuid = new Guid(req);
                for (var i = 0; i < queue.Count; i++)
                {
                    if (queue[i].uid == lookupGuid)
                    {
                        var player = queue[i];
                        player.time = DateTime.Now;
                        queue[i] = player;
                        return i.ToString();
                    }
                }
                return "-1";
            }else if (req.StartsWith("getGameState"))
            {
                req = req.Replace("getGameState&guid=", "");
                if (req != "")
                {
                    Guid lookupGuid = new Guid(req);
                    for (var i = 0; i < queue.Count; i++)
                    {
                        if (queue[i].uid == lookupGuid)
                        {
                            var player = queue[i];
                            player.time = DateTime.Now;
                            queue[i] = player;
                            break;
                        }
                    }
                }
                return JsonConvert.SerializeObject(game);
            }else if (req.StartsWith("setGameState&guid="))
            {
                req = req.Replace("setGameState&guid=", "");
                Guid lookupGuid = new Guid(req.Split("&"[0])[0]);
                var splitReq = req.Split("&");
                float y = System.Convert.ToSingle(splitReq[1].Replace("y=", ""));
                float dy = System.Convert.ToSingle(splitReq[2].Replace("dy=", ""));

                for (var i = 0; i < queue.Count; i++)
                {
                    if (queue[i].uid == lookupGuid)
                    {
                        var player = queue[i];
                        player.time = DateTime.Now;
                        player.y = y;
                        player.dy = dy;
                        queue[i] = player;
                        return "";
                    }
                }

            }
            else if (req.StartsWith("getSetGameState&guid="))
            {
                req = req.Replace("getSetGameState&guid=", "");
                Guid lookupGuid = new Guid(req.Split("&"[0])[0]);
                var splitReq = req.Split("&");
                float y = System.Convert.ToSingle(splitReq[1].Replace("y=", ""));
                float dy = System.Convert.ToSingle(splitReq[2].Replace("dy=", ""));

                for (var i = 0; i < queue.Count; i++)
                {
                    if (queue[i].uid == lookupGuid)
                    {
                        var player = queue[i];
                        player.time = DateTime.Now;
                        player.y = y;
                        player.dy = dy;
                        queue[i] = player;
                        return JsonConvert.SerializeObject(game);
                    }
                }

            }
            return JsonConvert.SerializeObject(game);
        }

        public static List<PlayerDetails> queue = new List<PlayerDetails>();

    }

    class PlayerDetails
    {
        public Guid uid;
        public DateTime time;
        public bool isUpNow = false;
        public bool hasLost = false;

        public int score = 0;
        public float y = 50;
        public float dy = 0;

        public PlayerDetails()
        {
            uid = Guid.NewGuid();
            time = DateTime.Now;
        }
    }

    //left Paddle at 3
    //right Paddle at 97

    class ballPosition
    {
        public float x;
        public float y;
        public float dx;
        public float dy;
        public ballPosition()
        {
            x = 50;
            y = 50;
            dx = Program.balldx;
            dy = Program.balldy;
        }
    }

    class PongGame
    {
        public PlayerDetails leftPlayer;
        public PlayerDetails rightPlayer;
        public ballPosition ballPos;
        public bool gameRunning = false;

        public PongGame()
        {
            ballPos = new ballPosition();
        }

    }

}

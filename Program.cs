//===----------------------------------------------------------------------===//
//
//                               Violet Styler
//
//===----------------------------------------------------------------------===//
//
//  Copyright (C) 2021. violet-team. All Rights Reserved.
//
//===----------------------------------------------------------------------===//

using System;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace violet_styler
{

    public class Options : IConsoleOption
    {
        [CommandLine("--help", CommandType.OPTION)]
        public bool Help;
        [CommandLine("--version", CommandType.OPTION, ShortOption = "-v", Info = "Show version information.")]
        public bool Version;

        [CommandLine("--syncronize-chunk", CommandType.OPTION, ShortOption = "-s", Info = "Syncronize chunk datas")]
        public bool SyncronizeChunk;
        [CommandLine("--export-rel-list", CommandType.OPTION, ShortOption = "-e", Info = "Export relative list")]
        public bool ExportRelativeList;
        [CommandLine("--export-simprel-list", CommandType.OPTION, ShortOption = "-m", Info = "Export simple relative list")]
        public bool ExportSimpleRelativeList;

        [CommandLine("--test", CommandType.ARGUMENTS, ArgumentsCount = 1, Info = "Test")]
        public string[] Test;

    }


    public class Command
    {
        public static void Start(string[] arguments)
        {
            arguments = CommandLineUtil.SplitCombinedOptions(arguments);
            var option = CommandLineParser.Parse<Options>(arguments);

            //
            //  Single Commands
            //
            if (option.Help)
            {
                PrintHelp();
            }
            else if (option.Version)
            {
                // PrintVersion();
            }
            else if (option.SyncronizeChunk)
            {
                ProcessSyncronizeChunk();
            }
            else if (option.ExportRelativeList)
            {
                ProcessExportRelativeList();
            }
            else if (option.ExportSimpleRelativeList)
            {
                ProcessExportSimpleRelativeList();
            }
            else if (option.Test != null)
            {
                ProcessTest(option.Test);
            }
            else if (option.Error)
            {
                Console.WriteLine(option.ErrorMessage);
                if (option.HelpMessage != null)
                    Console.WriteLine(option.HelpMessage);
                return;
            }
            else
            {
                Console.WriteLine("Nothing to work on.");
                Console.WriteLine("Enter './violet-styler --help' to get more information");
            }

            return;
        }

        static void PrintHelp()
        {
            // PrintVersion();
            Console.WriteLine($"Copyright (C) 2021. project violet-styler.");
            Console.WriteLine("Usage: ./violet-styler [OPTIONS...]");

            var builder = new StringBuilder();
            CommandLineParser.GetFields(typeof(Options)).ToList().ForEach(
                x =>
                {
                    var key = x.Key;
                    if (!key.StartsWith("--"))
                        return;
                    if (!string.IsNullOrEmpty(x.Value.Item2.ShortOption))
                        key = $"{x.Value.Item2.ShortOption}, " + key;
                    var help = "";
                    if (!string.IsNullOrEmpty(x.Value.Item2.Help))
                        help = $"[{x.Value.Item2.Help}]";
                    if (!string.IsNullOrEmpty(x.Value.Item2.Info))
                        builder.Append($"   {key}".PadRight(30) + $" {x.Value.Item2.Info} {help}\r\n");
                    else
                        builder.Append($"   {key}".PadRight(30) + $" {help}\r\n");
                });
            Console.Write(builder.ToString());
        }

        // public static void PrintVersion()
        // {
        //     Console.WriteLine($"{Version.Name} {Version.Text}");
        //     Console.WriteLine($"Build Date: " + Internals.GetBuildDate().ToLongDateString());
        // }

        static void ProcessSyncronizeChunk()
        {
            var db = new Database(File.ReadAllText("connection-string.txt").Trim());

            Console.WriteLine(db.Count());

            var cnt = db.Count();

            for (var i = 0; i <= 10; i++)
            {
                var data = db.LoadData(i * cnt / 10, Math.Min(cnt / 10, cnt - i * cnt / 10 - 1));
                Console.WriteLine($"Load Data ... [{i * cnt / 10}/{Math.Min(cnt / 10, cnt - i * cnt / 10 - 1)}]");
                File.WriteAllText($"chunk-{i}.json", JsonConvert.SerializeObject(data));
            }
        }

        static List<UserArticle> loadChunks()
        {
            var vv = new List<UserArticle>();

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"Load Data ... [chunk-{i}.json]");
                vv.AddRange(JsonConvert.DeserializeObject<List<UserArticle>>(File.ReadAllText($"chunk-{i}.json")));
            }

            return vv;
        }

        static Dictionary<string, User> buildUser(List<UserArticle> userArticles)
        {
            var userDict = new Dictionary<string, User>();

            Console.Write($"Build User ... ");
            userArticles.ForEach(x =>
            {
                if (!x.IsValid() || x.ValidSeconds < 4 || x.MPP.VCount() < 2) return;
                if (!userDict.ContainsKey(x.UserAppId))
                    userDict.Add(x.UserAppId, new User(x.UserAppId));
                userDict[x.UserAppId].UserArticles.Add(x);
            });
            Console.WriteLine("Complete");

            return userDict;
        }

        static List<User> buildSimpleUser(List<UserArticle> userArticles)
        {
            var userDict = new Dictionary<string, User>();

            Console.Write($"Build Simple User ... ");
            userArticles.ForEach(x =>
            {
                if (!x.IsValid() || x.ValidSeconds < 2) return;
                if (!userDict.ContainsKey(x.UserAppId))
                    userDict.Add(x.UserAppId, new User(x.UserAppId));
                userDict[x.UserAppId].UserArticles.Add(x);
            });
            Console.WriteLine("Complete");

            return userDict.Select(x => x.Value).ToList();
        }

        static List<User> organizeUser(Dictionary<string, User> userDict)
        {
            Console.Write($"Organize User ... ");
            var users = userDict.Select(x => x.Value).ToList();

            using (var pb = new ExtractingProgressBar())
            {
                var count = 0;
                users.ForEach(x =>
                {
                    pb.Report(users.Count, count++);
                    x.Merge();
                    x.Organize();
                });
            }
            Console.WriteLine("Complete");
            return users;
        }

        static Dictionary<string, double> buildUDI(Dictionary<string, User> userDict)
        {
            Console.Write($"Build User Confidence Level (UDI) ... ");
            var udi = User.UDI(userDict.Select(x => x.Value).ToList());
            Console.WriteLine("Complete");
            return udi;
        }

        static Dictionary<int, Article> buildArticles(List<User> users, Dictionary<string, double> udi)
        {
            var articles = new Dictionary<int, Article>();
            Console.Write($"Build Article Concentration ... ");
            using (var pb = new ExtractingProgressBar())
            {
                var count = 0;
                users.ForEach(x =>
                {
                    x.Concentration().ToList().ForEach(y =>
                    {
                        if (!articles.ContainsKey(y.Key))
                            articles.Add(y.Key, new Article(y.Key));

                        x.Concentration().ToList().ForEach(z =>
                        {
                            if (y.Key == z.Key) return;
                            if (!articles.ContainsKey(z.Key))
                                articles.Add(z.Key, new Article(z.Key));

                            // y ---  user --- z
                            //  ytoz      ztoy

                            articles[y.Key].PushAssocication(x.UserAppId, z.Key, y.Value, z.Value, udi[x.UserAppId]);
                        });
                    });
                    pb.Report(users.Count, count++);
                });
            }
            Console.WriteLine("Complete");
            return articles;
        }

        static Dictionary<int, double> buildLDI(List<User> users)
        {
            Console.Write($"Build Likes and Dislikes Index (LDI) ... ");
            var ldiSource = new Dictionary<int, List<double>>();

            users.ForEach(x =>
                x.Concentration().ToList().ForEach(y =>
                {
                    if (!ldiSource.ContainsKey(y.Key))
                        ldiSource.Add(y.Key, new List<double>());
                    ldiSource[y.Key].Add(y.Value);
                })
            );

            var ldiPreStd = ldiSource.Select(x => new Tuple<int, double>(x.Key, NormalDist.Std(x.Value))).ToList();

            var ldiAvg = ldiPreStd.Select(x => x.Item2).Average();
            var ldiStd = NormalDist.Std(ldiPreStd.Select(x => x.Item2).ToList());

            var ldi = new Dictionary<int, double>();

            ldiPreStd.ForEach(x =>
            {
                var percent = NormalDist.Phi((x.Item2 - ldiAvg) / ldiStd);
                ldi.Add(x.Item1, percent * 5);
            });
            Console.WriteLine("Complete");

            return ldi;
        }

        static void ProcessExportRelativeList()
        {
            var chunks = loadChunks();
            var userDict = buildUser(chunks);
            var users = organizeUser(userDict);
            var udi = buildUDI(userDict);
            var articles = buildArticles(users, udi);
            var ldi = buildLDI(users);

            Console.Write($"Build Article Assocications ... ");
            var articlesList = articles.ToList();
            articlesList.Sort((x, y) => x.Value.Association.Count.CompareTo(y.Value.Association.Count));

            var articleRelList = articlesList.Select(x =>
            {
                var e = x.Value.Evaluate(ldi).ToList();
                e.Sort((x, y) => y.Value.CompareTo(x.Value));
                return new Tuple<int, List<KeyValuePair<int, double>>>(x.Key, e);
            }).Where(x => x.Item2.Count > 20).Select(x =>
                new KeyValuePair<int, List<int>>(x.Item1, x.Item2.Select(x => x.Key).Take(100).ToList()));
            Console.WriteLine("Complete");

            Console.Write($"Writing Article Relation List ... ");
            File.WriteAllText("article-rel-list.json", JsonConvert.SerializeObject(new Dictionary<int, List<int>>(articleRelList)));
            Console.WriteLine("Complete");
        }

        static void ProcessExportSimpleRelativeList()
        {
            var chunks = loadChunks();
            var users = buildSimpleUser(chunks);

            const int negativeThreshold = 24;

            var articles = new Dictionary<int, Article>();
            Console.Write($"Build Article Relationship ... ");
            using (var pb = new ExtractingProgressBar())
            {
                var count = 0;

                users.ForEach(x =>
                {
                    x.UserArticles.ForEach(y =>
                    {
                        if (!articles.ContainsKey(y.ArticleId))
                            articles.Add(y.ArticleId, new Article(y.ArticleId));

                        x.UserArticles.ForEach(z =>
                        {
                            if (!articles.ContainsKey(z.ArticleId))
                                articles.Add(z.ArticleId, new Article(z.ArticleId));

                            var yn = y.ValidSeconds < negativeThreshold;
                            var zn = z.ValidSeconds < negativeThreshold;

                            if (yn && zn) return;

                            if (!yn)
                                articles[y.ArticleId].PushSimpleAssocication(z.ArticleId, !zn);
                            if (!zn)
                                articles[y.ArticleId].PushSimpleAssocication(z.ArticleId, !yn);
                        });
                    });

                    pb.Report(users.Count, count++);
                });
            }
            Console.WriteLine("Complete");

            var results = articles.ToList().Select(x =>
                new KeyValuePair<int, Dictionary<int, double>>(x.Key, x.Value.EvaluateSimple()));

            Console.Write($"Writing Simple Article Relation List ... ");
            File.WriteAllText("article-simprel-list.json", JsonConvert.SerializeObject(new Dictionary<int, Dictionary<int, double>>(results)));
            Console.WriteLine("Complete");
        }

        static void ProcessTest(string[] args)
        {
            var chunks = loadChunks();

            // Read Time Per Page
            // (articleId, readTime)
            var rtpp = new Dictionary<int, List<double>>();

            chunks.ForEach(x =>
            {
                if (!rtpp.ContainsKey(x.ArticleId))
                    rtpp.Add(x.ArticleId, new List<double>());

                rtpp[x.ArticleId].Add(x.ValidSeconds / (double)x.Pages);
            });

            var ll = rtpp.ToList();

            ll.Sort((x, y) => x.Value.Count.CompareTo(y.Value.Count));

            // Average Read Time per Page
            var artpp = new List<List<double>>();

            ll.ForEach(x =>
            {
                var avg = x.Value.Average();
                var std = Math.Sqrt(x.Value.Sum(x => (x - avg) * (x - avg)));
                artpp.Add(new List<double> { x.Value.Count, std });
            });

            File.WriteAllText("artpp.json", JsonConvert.SerializeObject(artpp));
            Console.WriteLine("Complete");
        }
    }

    class Program
    {
        static void savedb(string connstr)
        {
            var db = new Database(connstr);

            Console.WriteLine(db.Count());

            var cnt = db.Count();

            for (var i = 0; i <= 10; i++)
            {
                var data = db.LoadData(i * cnt / 10, Math.Min(cnt / 10, cnt - i * cnt / 10 - 1));
                Console.WriteLine($"Load Data ... [{i * cnt / 10}/{Math.Min(cnt / 10, cnt - i * cnt / 10 - 1)}]");
                File.WriteAllText($"chunk-{i}.json", JsonConvert.SerializeObject(data));
            }
        }

        public static string DatabaseConnectionString;

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Logs.Instance.AddLogNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("info: ");
                Console.ResetColor();
                Console.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logs.Instance.AddLogErrorNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("error: ");
                Console.ResetColor();
                Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            Logs.Instance.AddLogWarningNotify((s, e) =>
            {
                var tuple = s as Tuple<DateTime, string, bool>;
                CultureInfo en = new CultureInfo("en-US");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.Write("warning: ");
                Console.ResetColor();
                Console.Error.WriteLine($"[{tuple.Item1.ToString(en)}] {tuple.Item2}");
            });

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logs.Instance.PushError("unhandled: " + (e.ExceptionObject as Exception).ToString());
            };


            try
            {
                Command.Start(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured! " + e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine("Please, check log.txt file.");
            }

            Environment.Exit(0);

            return;

            Logs.Instance.Push("Start");

            DatabaseConnectionString = args[0];

            //savedb(args[0]);

            //
            //  Load User Articles
            //

            var vv = new List<UserArticle>();

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"Load Data ... [chunk-{i}.json]");
                vv.AddRange(JsonConvert.DeserializeObject<List<UserArticle>>(File.ReadAllText($"chunk-{i}.json")));
            }

            //
            //  Build User
            //

            var userDict = new Dictionary<string, User>();

            vv.ForEach(x =>
            {
                if (!x.IsValid() || x.ValidSeconds < 4 || x.MPP.VCount() < 2) return;
                if (!userDict.ContainsKey(x.UserAppId))
                    userDict.Add(x.UserAppId, new User(x.UserAppId));
                userDict[x.UserAppId].UserArticles.Add(x);
            });

            // Console.WriteLine(userDict.Count);
            var users = userDict.Select(x => x.Value).ToList();
            users.ForEach(x => x.Merge());
            users.ForEach(x => x.Organize());
            users.Sort((x, y) => x.UserArticles.Count.CompareTo(y.UserArticles.Count));
            var udi = User.UDI(userDict.Select(x => x.Value).ToList());
            // foreach (var kv in ll)
            // {
            //     if (kv.Value.UserArticles.Count == 0) continue;
            //     Console.WriteLine(kv.Value.ToString());
            //     Console.WriteLine(udi[kv.Value.UserAppId]);
            //     //kv.Value.Print();
            //     //Console.WriteLine(Logs.SerializeObject(kv.Value.Concentration()));
            // }

            var articles = new Dictionary<int, Article>();
            var ldiSource = new Dictionary<int, List<double>>();

            users.ForEach(x => x.Concentration().ToList().ForEach(y =>
            {
                if (!articles.ContainsKey(y.Key))
                    articles.Add(y.Key, new Article(y.Key));

                if (!ldiSource.ContainsKey(y.Key))
                    ldiSource.Add(y.Key, new List<double>());

                ldiSource[y.Key].Add(y.Value);

                x.Concentration().ToList().ForEach(z =>
                {
                    if (y.Key == z.Key) return;
                    if (!articles.ContainsKey(z.Key))
                        articles.Add(z.Key, new Article(z.Key));

                    // y ---  user --- z
                    //  ytoz      ztoy

                    articles[y.Key].PushAssocication(x.UserAppId, z.Key, y.Value, z.Value, udi[x.UserAppId]);
                });
            }));

            //
            // Calculation LDI
            //

            var ldiPreStd = ldiSource.Select(x => new Tuple<int, double>(x.Key, NormalDist.Std(x.Value))).ToList();
            //Console.WriteLine(Logs.SerializeObject(ldiPreStd.Select(x => x.Item2)));

            var ldiAvg = ldiPreStd.Select(x => x.Item2).Average();
            var ldiStd = NormalDist.Std(ldiPreStd.Select(x => x.Item2).ToList());

            var ldi = new Dictionary<int, double>();

            Console.WriteLine($"{ldiPreStd.Select(x => x.Item2).Max()}");
            Console.WriteLine($"{ldiAvg}, {ldiStd}");

            ldiPreStd.ForEach(x =>
            {
                var percent = NormalDist.Phi((x.Item2 - ldiAvg) / ldiStd);
                ldi.Add(x.Item1, percent * 5);
            });

            var ldiLL = ldi.ToList();
            ldiLL.Sort((x, y) => x.Value.CompareTo(y.Value));
            //Console.WriteLine(Logs.SerializeObject(new Dictionary<int, Tuple<double, int>>(
            //    ldiLL.Where(x=>ldiSource[x.Key].Count > 50).Select(x => new KeyValuePair<int, Tuple<double, int>>(x.Key, new Tuple<double, int>(x.Value, ldiSource[x.Key].Count))))));

            //Console.WriteLine(Logs.SerializeObject(ldiSource[1954415]));

            //
            // Print Relative Article
            //

            // while (true)
            // {
            //     try
            //     {
            //         var article = int.Parse(Console.ReadLine().Trim());

            //         var articlesList = articles.ToList().Where(x => x.Key == article).ToList();
            //         articlesList.Sort((x, y) => x.Value.Association.Count.CompareTo(y.Value.Association.Count));

            //         //Console.WriteLine(Logs.SerializeObject(articlesList.Last().Value.Association));
            //         //Console.WriteLine(articlesList.Last().Value.Association.Count);

            //         var e = articlesList.Last().Value.Evaluate(ldi).ToList();
            //         e.Sort((x, y) => x.Value.CompareTo(y.Value));

            //         Console.WriteLine(Logs.SerializeObject(new Dictionary<int, double>(e)));
            //         Console.WriteLine(articlesList.Last().Value.ArticleId);

            //         //articlesList.ForEach(x => Console.WriteLine(x.Value.Association.Count));
            //     }
            //     catch (Exception e)
            //     {
            //         Console.WriteLine(e.ToString());
            //     }
            // }

            var articlesList = articles.ToList();
            articlesList.Sort((x, y) => x.Value.Association.Count.CompareTo(y.Value.Association.Count));

            var articleRelList = articlesList.Select(x =>
            {
                var e = x.Value.Evaluate(ldi).ToList();
                e.Sort((x, y) => y.Value.CompareTo(x.Value));
                return new Tuple<int, List<KeyValuePair<int, double>>>(x.Key, e);
            }).Where(x => x.Item2.Count > 20).Select(x =>
                new KeyValuePair<int, List<int>>(x.Item1, x.Item2.Select(x => x.Key).Take(100).ToList()));

            File.WriteAllText("article-rel-list.json", JsonConvert.SerializeObject(new Dictionary<int, List<int>>(articleRelList)));

            return;

            //////////////////////////////////////////////////////////////

            /*
            //var vv = db.LoadData(900000, 10000).Where(x => x.IsValid() && x.ValidSeconds > 24).ToList();
            //Console.WriteLine(Logs.SerializeObject(vv));
            vv = vv.Where(x => x.MPP.VStd() < 2000 && x.MPP.VCount() > 1).ToList();
            //vv.Sort((x, y) => x.MPP.VAvg().CompareTo(y.MPP.VAvg()));
            vv.Sort((x, y) => x.Score().CompareTo(y.Score()));
            vv.ForEach(
                (x) =>
                {
                    Console.WriteLine("----------------------------");
                    Console.WriteLine($"{x.ArticleId}, {x.ValidSeconds}, {x.MPP.value.Sum()}ms, " +
                        $"{x.MPP.VAvg().ToString("#.0")}ms, {x.MPP.VStd().ToString("#.0")}, {x.MPP.VCount()}/{x.Pages}");
                    //x.MPP.Print();
                    //x.VVMPP(50).Print();
                    Console.WriteLine($"{x.OrganizedMPP().value.Sum()}ms, {x.OrganizedMPP().VAvg().ToString("#.0")}ms, " +
                                      $"{x.OrganizedMPP().VStd().ToString("#.0")}, {x.OrganizedMPP().VCount()}/{x.Pages}, {x.Score()}");
                    //x.OrganizedVMPP(50).Print();
                });
            */
        }
    }
}

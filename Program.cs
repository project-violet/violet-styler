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

        static void Main(string[] args)
        {
            Logs.Instance.Push("Start");

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

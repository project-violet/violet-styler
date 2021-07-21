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

            var vv = new List<UserArticle>();

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine($"Load Data ... [chunk-{i}.json]");
                vv.AddRange(JsonConvert.DeserializeObject<List<UserArticle>>(File.ReadAllText($"chunk-{i}.json")));
            }

            var userDict = new Dictionary<string, User>();

            vv.ForEach(x =>
            {
                if (!x.IsValid() || x.ValidSeconds < 24 || x.MPP.VCount() < 2) return;
                if (!userDict.ContainsKey(x.UserAppId))
                    userDict.Add(x.UserAppId, new User(x.UserAppId));
                userDict[x.UserAppId].UserArticles.Add(x);
            });

            // Console.WriteLine(userDict.Count);
            var ll = userDict.ToList();
            ll.ForEach(x => x.Value.Merge());
            ll.ForEach(x => x.Value.Organize());
            ll.Sort((x, y) => x.Value.UserArticles.Count.CompareTo(y.Value.UserArticles.Count));
            var udi = User.UDI(userDict.Select(x => x.Value).ToList());
            foreach (var kv in ll)
            {
                if (kv.Value.UserArticles.Count == 0) continue;
                Console.WriteLine(kv.Value.ToString());
                Console.WriteLine(udi[kv.Value.UserAppId]);
                //kv.Value.Print();
                //Console.WriteLine(Logs.SerializeObject(kv.Value.Concentration()));
            }

            return;

            //////////////////////////////////////////////////////////////

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
        }
    }
}

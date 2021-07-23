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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace violet_styler
{
    class Article
    {
        public int ArticleId { get; private set; }

        // User Name, ArticleId, Conecntration Rate To, Conecntration Rate From, UDI
        public List<Tuple<string, int, double, double, double>> Association =
                new List<Tuple<string, int, double, double, double>>();

        public Article(int articleId)
        {
            this.ArticleId = articleId;
        }

        public void PushAssocication(string userName, int articleId,
                                     double crto, double crfrom, double udi)
        {
            Association.Add(
                    new Tuple<string, int, double, double, double>
                        (userName, articleId, crto, crfrom, udi));
        }

        // Article Id, Conecntration
        public Dictionary<int, double> Evaluate(Dictionary<int, double> ldi)
        {
            var articleGroup = new Dictionary<int, List<Tuple<double, double>>>();

            Association.ForEach(x =>
            {
                if (!articleGroup.ContainsKey(x.Item2))
                    articleGroup.Add(x.Item2, new List<Tuple<double, double>>());
                articleGroup[x.Item2].Add(new Tuple<double, double>(x.Item3, x.Item4));
            });

            var calVec = new Dictionary<int, double>();

            var cAvg = articleGroup.ToList().Select(x => x.Value.Count).Average();
            var cStd = NormalDist.Std(articleGroup.ToList().Select(x => x.Value.Count * 1.0).ToList());

            articleGroup.ToList().ForEach(x =>
            {
                // Console.WriteLine(Logs.SerializeObject(x));
                // var xx = Math.Sqrt(x.Value.Sum(x => x.Item1 * x.Item1));
                // var yy = Math.Sqrt(x.Value.Sum(y => y.Item2 * y.Item2));
                // var zz = x.Value.Select(x => x.Item1 * x.Item2).Sum();
                var pureScore = x.Value.Select(x => Math.Sqrt(x.Item1 * x.Item2)).Sum() / x.Value.Count;

                calVec.Add(x.Key, pureScore * 0.5 + ldi[x.Key] * 0.3 + (x.Value.Count - cAvg) / cStd * 5 * 0.2);
                // calVec.Add(x.Key, Math.Sqrt(x.Value.Select(x => x.Item1 * x.Item2));
            });

            return calVec;
        }
    }
}
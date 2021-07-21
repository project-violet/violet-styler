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
    class User
    {
        public List<UserArticle> UserArticles = new List<UserArticle>();
        public string UserAppId;

        public User(string userAppId)
        {
            this.UserAppId = userAppId;
        }

        public double Avg() => UserArticles.Sum(x => x.Score()) / UserArticles.Count;

        public double Min() => UserArticles.Min(x => x.Score());
        public double Max() => UserArticles.Max(x => x.Score());

        public double Var()
        {
            var avg = Avg();
            var dd = UserArticles.Select(x => (x.Score() - avg) * (x.Score() - avg));
            return dd.Sum() / UserArticles.Count;
        }

        public double Std() => Math.Sqrt(Var());

        public override string ToString()
        {
            return $"{UserArticles.Count}, {Avg()}, {Std()}\n"; //+ string.Join("\n", UserArticles.Select(x => x.Score()));
        }

        public void Print()
        {
            var x = new int[Convert.ToInt32(Max() / 500) + 1];
            UserArticles.ForEach(y => x[Convert.ToInt32(y.Score() / 500)]++);
            for (var i = 0; i < x.Length; i++)
            {
                Console.WriteLine(new string('*', x[i]));
            }
        }

        public void Organize()
        {
            var p = 1.96; // 95%
            var avg = Avg();
            var std = Std();
            var zs = avg - p * std;
            var ze = avg + p * std;

            UserArticles = UserArticles.Where(x => x.Score() > zs && x.Score() < ze).ToList();
        }

        // Merge MPP of Same Articles
        public void Merge()
        {
            var dict = new Dictionary<int, UserArticle>();

            UserArticles.ForEach(x =>
            {
                if (!dict.ContainsKey(x.ArticleId))
                    dict.Add(x.ArticleId, x);
                else
                {
                    for (var i = 0; i < x.MPP.value.Count; i++)
                        dict[x.ArticleId].MPP.value[i] += x.MPP.value[i];
                }
            });

            UserArticles = dict.ToList().Select(x => x.Value).ToList();
        }

        public Dictionary<int, double> Concentration()
        {
            var dict = new Dictionary<int, double>();

            //  0 ~  20%: 0~1
            // 20 ~  40%: 1~2
            // 40 ~  60%: 2~3
            // 60 ~  80%: 3~4
            // 80 ~ 100%: 4~5

            // var z = new double[] { -0.842, -0.253, 0.253, 0.842 };

            var avg = Avg();
            var std = Std();

            UserArticles.ForEach(x => {
                var percent = NormalDist.Phi((x.Score() - avg) / std);
                dict.Add(x.ArticleId, percent * 5);
            });

            return dict;
        }

        // User Confidence Level
        public static Dictionary<string, double> UDI(List<User> users) {
            var dict = new Dictionary<string, double>();

            users = users.Where(x => !double.IsNaN(x.Std())).ToList();

            var avg = users.Sum(x => x.Std()) / users.Count;
            var std = Math.Sqrt(users.Select(x => (x.Std() - avg) * (x.Std() - avg)).Sum() / users.Count);

            Console.WriteLine(avg);
            Console.WriteLine(std);

            users.ForEach(x => {
                var percent = NormalDist.Phi((x.Std() - avg) / std);
                dict.Add(x.UserAppId, percent * 5);
            });

            return dict;
        }
    }
}
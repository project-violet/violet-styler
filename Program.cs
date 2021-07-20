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
        static void Main(string[] args)
        {
            Logs.Instance.Push("Start");

            var db = new Database(args[0]);

            Console.WriteLine(db.Count());

            var vv = db.LoadData(900000, 10000).Where(x => x.IsValid() && x.ValidSeconds > 24).ToList();
            //Console.WriteLine(Logs.SerializeObject(vv));
            vv = vv.Where(x => x.MPP.VStd() < 1000 && x.MPP.VCount() > 1).ToList();
            vv.Sort((x, y) => x.MPP.VAvg().CompareTo(y.MPP.VAvg()));
            vv.ForEach(
                (x) =>
                {
                    Console.WriteLine("----------------------------");
                    Console.WriteLine($"{x.ArticleId}, {x.ValidSeconds}, {x.MPP.value.Sum()}ms, " +
                        $"{x.MPP.VAvg().ToString("#.0")}ms, {x.MPP.VStd().ToString("#.0")}, {x.MPP.VCount()}/{x.Pages}");
                    //x.MPP.Print();
                    x.VVMPP(50).Print();
                    Console.WriteLine($"{x.OrganizedMPP().value.Sum()}ms, {x.OrganizedMPP().VAvg().ToString("#.0")}ms, "+
                                      $"{x.OrganizedMPP().VStd().ToString("#.0")}, {x.OrganizedMPP().VCount()}/{x.Pages}");
                    x.OrganizedVMPP(50).Print();
                });
        }
    }
}

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
    class MPP
    {
        public List<int> value { get; set; }
        public MPP(List<int> mpp) => this.value = mpp;
        public virtual double Avg() => (double)value.Sum() / value.Count;
        public virtual int Min() => value.Min();
        public virtual int Max() => value.Max();
        public virtual double Var()
        {
            var avg = Avg();
            var dd = value.Select(x => (x - avg) * (x - avg));
            return dd.Sum() / value.Count;
        }
        public virtual double Std() => Math.Sqrt(Var());

        public virtual void Print()
        {
            foreach (var v in value)
            {
                for (var i = 0; i < v / 50; i++)
                {
                    Console.Write('*');
                }
                Console.WriteLine();
            }
        }

        public virtual int VCount() => value.Count(x => x != 0);
        public virtual double VAvg() => (double)value.Sum() / VCount();
        public virtual int VMin() => value.Where(x => x != 0).Min();
        public virtual double VVar()
        {
            var cnt = VCount();
            var avg = (double)value.Sum() / cnt;
            var dd = value.Where(x => x != 0).Select(x => (x - avg) * (x - avg));
            return dd.Sum() / cnt;
        }
        public virtual double VStd() => Math.Sqrt(VVar());
    }

    class VMPP : MPP
    {
        int cutoffMS;

        public VMPP(List<int> mpp, int cutoffMS) : base(mpp)
        {
            this.cutoffMS = cutoffMS;
        }

        public override double Avg() => throw new AccessViolationException();
        public override int Min() => throw new AccessViolationException();
        public override int Max() => throw new AccessViolationException();
        public override double Var() => throw new AccessViolationException();
        public override double Std() => throw new AccessViolationException();
        public override double VAvg() => throw new AccessViolationException();
        public override int VMin() => throw new AccessViolationException();
        public override double VVar() => throw new AccessViolationException();
        public override double VStd() => throw new AccessViolationException();

        public override void Print()
        {
            int c = 0;
            foreach (var v in value)
            {
                if (v == 0) { c++; continue; }

                Console.Write($"{(c++ * cutoffMS).ToString("#,0").PadLeft(5)}ms: ");
                for (var i = 0; i < v; i++)
                {
                    Console.Write('*');
                }
                Console.WriteLine();
            }
        }
    }

    class UserArticle
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public int Pages { get; set; }
        public int LastPage { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime StartsTime { get; set; }
        public DateTime EndsTime { get; set; }
        public int ValidSeconds { get; set; }
        // Time to Read Per Page (milli seconds)
        public MPP MPP { get; set; }
        public string UserAppId { get; set; }

        public UserArticle() { }

        public UserArticle(Object[] source)
        {
            Id = (source[0] as int?).Value;
            ArticleId = (source[1] as int?).Value;
            Pages = (source[2] as int?).Value;
            LastPage = (source[3] as int?).Value;
            TimeStamp = (source[4] as DateTime?).Value;
            StartsTime = (source[5] as DateTime?).Value;
            EndsTime = (source[6] as DateTime?).Value;
            ValidSeconds = (source[7] as int?).Value;
            var mpp = JsonConvert.DeserializeObject<List<int>>((source[8] as string));
            MPP = new MPP(mpp.Select(x => x * 100).ToList());
            UserAppId = (source[9] as string);
        }

        public bool IsValid()
        {
            return MPP.value.Count == Pages && !MPP.value.All((x) => x == 0);
        }

        // VMPP = VT x COUNT(P)
        public VMPP VMPP(int cutoffMS = 500)
        {
            var max = MPP.Max();
            var vmpp = new int[max / cutoffMS + 1];
            MPP.value.ForEach(x => vmpp[x / cutoffMS]++);
            return new VMPP(vmpp.ToList(), cutoffMS);
        }

        public VMPP VVMPP(int cutoffMS = 500)
        {
            var max = MPP.Max();
            var vmpp = new int[max / cutoffMS + 1];
            MPP.value.Where(x => x != 0).ToList().ForEach(x => vmpp[x / cutoffMS]++);
            return new VMPP(vmpp.ToList(), cutoffMS);
        }

        // MPP Average (ms)
        public double Avg() => MPP.Avg();
        // MPP Variable (ms)
        public double Var() => MPP.Var();

        // Valid Read Time (ms)
        public double VRT() => ValidSeconds * 1000;
        // Valid Read Pages
        public double VRP() => MPP.value.Count(x => x > 0);
        // Valid Read Time per Page (ms/p)
        public double VRTP() => VRT() / VRP();

        public MPP OrganizedMPP()
        {
            var p = 1.96; // 95%
            var avg = MPP.VAvg();
            var std = MPP.VStd();
            var zs = avg - p * std;
            var ze = avg + p * std;

            return new MPP(MPP.value.Where(x => x > zs && x < ze).ToList());
        }

        public VMPP OrganizedVMPP(int cutoffMS = 500)
        {
            var mpp = OrganizedMPP();
            var max = mpp.Max();
            var vmpp = new int[max / cutoffMS + 1];
            mpp.value.Where(x => x != 0).ToList().ForEach(x => vmpp[x / cutoffMS]++);
            return new VMPP(vmpp.ToList(), cutoffMS);
        }

        // Reproduced Valid Read Time per Pages
        public double RVRTP() => OrganizedMPP().VAvg();

        // VRP with Concentration Weight
        public double FVRP()
        {
            var p = 0.842; // 80%
            var avg = MPP.VAvg();
            var std = MPP.VStd();
            var zs = avg - p * std;
            var ze = avg + p * std;

            return MPP.value.Where(x => x > 0).Sum(x =>
            {
                if (x < zs)
                {
                    var v = NormalDist.Phi((x - avg) / std);
                    return 25 * v * v;
                }
                else if (x > ze)
                {
                    var v = NormalDist.Phi((x - avg) / std);
                    return v + 0.2;
                }

                return 1.0;
            });
        }

        // w := [0, 1]
        private double NoiseFilterModel(double w)
        {
            var ww = (w - 0.2) * 5;
            return 1 / (1 + Math.Exp(-ww));
        }

        // User Article Score
        double scoreCache = 0;
        public double Score()
        {
            if (scoreCache != 0) return scoreCache;
            var vavg = MPP.VAvg();
            var fvrp = FVRP();
            return scoreCache = NoiseFilterModel(fvrp / Pages) * vavg;
        }
    }
}

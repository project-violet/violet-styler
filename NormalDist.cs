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
    class NormalDist : ILazy<NormalDist>
    {
        // Normal Distribution Z-Score
        public static double Phi(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x) / Math.Sqrt(2.0);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return 0.5 * (1.0 + sign * y);
        }


        public static double Std(List<double> ld)
        {
            var avg = ld.Average();
            return Math.Sqrt(ld.Select(x => x - avg).Sum(x => x * x) / ld.Count);
        }

    }
}
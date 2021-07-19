//===----------------------------------------------------------------------===//
//
//                               Violet Styler
//
//===----------------------------------------------------------------------===//
//
//  Copyright (C) 2021. violet-team. All Rights Reserved.
//
//===----------------------------------------------------------------------===//

const a_syncdatabase = require('./api/syncdatabase');

const path = require('path');
const fs = require('fs');
const {Console} = require('console');

function download_report_data() {
  const conn = a_syncdatabase();
  const data = conn.query('select * from viewreport');
  const dataPath = path.resolve(__dirname, 'viewreport.json');

  console.log(data.length);

  fs.writeFileSync(dataPath, JSON.stringify(data, null, 4), function(err) {
    console.log(err);
  });
}

function load_cache_data() {
  const dataPath = path.resolve(__dirname, 'viewreport.json');
  const data = fs.readFileSync(dataPath);
  return JSON.parse(data);
}

function load_report_data() {
  const dataPath = path.resolve(__dirname, 'viewreport.json');
  if (!fs.existsSync(dataPath)) download_report_data();
  return load_cache_data();
}

var data = load_report_data();

function hide_userappid_for_dist() {
  var userSet = {};
  data.forEach(function(report) {
    if (!(report.UserAppId in userSet)) {
      userSet[report.UserAppId] = 1;
    }
  });

  var idCount = 0;
  Object.keys(userSet).forEach(function(key) {
    userSet[key] = idCount++;
  });

  data.forEach(function(report) {
    report.UserAppId = userSet[report.UserAppId];
  });

  const dataPath = path.resolve(__dirname, 'viewreport-dist.json');
  fs.writeFileSync(dataPath, JSON.stringify(data, null, 4), function(err) {
    console.log(err);
  });
}

/*
 "Id": 14307,
 "ArticleId": 1518690,
 "Pages": 34,
 "LastPage": 31,
 "TimeStamp": "2021-06-27T07:59:04.000Z",
 "StartsTime": "2021-06-27T07:57:15.000Z",
 "EndsTime": "2021-06-27T07:59:02.000Z",
 "ValidSeconds": 31,
 "MsPerPages": "[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]",
 "UserAppId": "asdf123456789"
*/

console.log('raw-data-size: ' + data.length);

function _filter_irregular() {
  var ndata = [];

  data.forEach(function(report) {
    var msPerPages = JSON.parse(report.MsPerPages);
    if (report.Pages != msPerPages.length) return;
    if (msPerPages.every(x => x == 0)) return;
    ndata.push(report);
  });

  return ndata;
}

data = _filter_irregular();

console.log('filtered-data-size: ' + data.length);

// var count  = 0;
// data.forEach(function (report) {
//     if (report.ValidSeconds > 24)  {
//         if (count > 10) return;
//         console.log(report);
//         count++;
//     }
// });
// console.log(count);

function _merge_msperpages_by_articleid() {
  var merged = {};
  var userCount = {};
  data.forEach(function(report) {
    const ms = JSON.parse(report.MsPerPages);
    if (!(report.ArticleId in merged)) {
      merged[report.ArticleId] = ms;
    } else {
      for (var i = 0; i < ms.length; i++) merged[report.ArticleId][i] += ms[i];
    }
  });
  return merged;
}

function _get_user_article_count() {
  var userCount = {};
  data.forEach(function(report) {
    if (!(report.ArticleId in userCount)) {
      userCount[report.ArticleId] = 1;
    } else {
      userCount[report.ArticleId] += 1;
    }
  });
  return userCount;
}

function _calculate_burst_time() {
  var merged = _merge_msperpages_by_articleid();
  var userCount = _get_user_article_count();

  var kv = [];
  Object.keys(userCount).forEach(function(key) {
    kv.push([key, userCount[key]]);
  });

  kv.sort((x, y) => y[1] - x[1]);

  var bt = [];
  for (var i = 0; i < kv.length; i++) {
    // console.log(kv[i][0]);
    // console.log(merged[kv[i][0]].toString());

    var sum =
        merged[kv[i][0]].map((x) => x / kv[i][1]).reduce((a, cv) => a + cv);
    var avg = sum / merged[kv[i][0]].length;
    // var avg = kv[i][1];
    var ud = merged[kv[i][0]].map((x) => x / kv[i][1]).map((x) => {
      if (x >= avg) return x / avg;
      return x / avg;
    });

    // function _show_burst_time_to_graph() {
    //   var min = 99999;
    //   var max = 0;

    //   ud.forEach(function (x) {
    //     if (x < min) min = x;
    //     if (x > max) max = x;
    //   });

    //   for (var i = 0; i < ud.length; i++) {
    //     const v = (ud[i] - min) * 10;
    //     var pp = '';
    //     for (var j = 0; j < v; j++) {
    //       pp += '*';
    //     }
    //     console.log(pp);
    //   }
    // }

    // console.log(kv[i][1]);
    // console.log(ud.map((x) => ' ' + x.toFixed(1)).toString());
    // _show_burst_time_to_graph();

    const bt_cnt_threshold = 15;

    if (kv[i][1] < bt_cnt_threshold) continue;

    bt.push({
      id: kv[i][0],
      bt: ud,
      ct: kv[i][1],
    });
  }

  const dataPath = path.resolve(__dirname, 'burst-time.json');
  fs.writeFileSync(dataPath, JSON.stringify(bt, null, 4), function(err) {
    console.log(err);
  });
}

function _show_burst_time_to_graph(id) {
  var merged = _merge_msperpages_by_articleid();
  var userCount = _get_user_article_count();

  var min = 99999;
  var max = 0;

  var sum = merged[id].map((x) => x / userCount[id]).reduce((a, cv) => a + cv);
  var avg = sum / merged[id].length;
  var ud = merged[id].map((x) => x / userCount[id]).map((x) => {
    if (x >= avg) return x / avg;
    return x / avg;
  });

  ud.forEach(function(x) {
    if (x < min) min = x;
    if (x > max) max = x;
  });

  for (var i = 0; i < ud.length; i++) {
    const v = (ud[i] - min) * 10;
    var pp = '';
    for (var j = 0; j < v; j++) {
      pp += '*';
    }
    console.log(pp);
  }
}

// _calculate_burst_time();

function _calculate_smoothly() {
  var merged = _merge_msperpages_by_articleid();
  var userCount = _get_user_article_count();

  var kv = [];
  Object.keys(userCount).forEach(function(key) {
    kv.push([key, userCount[key]]);
  });

  kv.sort((x, y) => y[1] - x[1]);

  var bt = [];
  for (var i = 0; i < kv.length; i++) {
    var sum =
        merged[kv[i][0]].map((x) => x / kv[i][1]).reduce((a, cv) => a + cv);
    var avg = sum / merged[kv[i][0]].length;
    var ud = merged[kv[i][0]].map((x) => x / kv[i][1]).map((x) => {
      if (x >= avg) return x / avg;
      return x / avg;
    });
    var usum = ud.reduce((a, cv) => a + cv);
    var uavg = usum / ud.length;
    var uva = ud.map((x) => (x - uavg) * (x - uavg)).reduce((a, cv) => a + cv) /
        ud.length;
        
    const bt_cnt_threshold = 15;

    if (kv[i][1] < bt_cnt_threshold) continue;

    bt.push({
      id: kv[i][0],
      ct: kv[i][1],
      va: uva,
      st: Math.sqrt(uva),
    });
  }

  
  bt.sort((x, y) => y.st - x.st);

  const dataPath = path.resolve(__dirname, 'smoothly.json');
  fs.writeFileSync(dataPath, JSON.stringify(bt, null, 4), function(err) {
    console.log(err);
  });
}

// _calculate_smoothly();

function _show_vmpp(id) {
  var merged = _merge_msperpages_by_articleid();

  var min = 99999999999;
  var max = 0;

  merged[id].forEach(function(x) {
    if (x < min) min = x;
    if (x > max) max = x;
  });

  var vmpp = Array(((max / 10000) >> 0) + 1).fill(0);

  merged[id].forEach(function(x) {
    vmpp[(x / 10000) >> 0] += 1;
  });

  for (var i = 0; i < vmpp.length; i++) {
    var pp = '';
    for (var j = 0; j < vmpp[i]; j++)
      pp += '*';
    console.log(pp);
  }

  console.log(vmpp.length);
}

// _show_burst_time_to_graph(1951387);
_show_vmpp(1946094);

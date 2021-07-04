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

  fs.writeFile(dataPath, JSON.stringify(data, null, 4), function(err) {
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
  var map = {};
  data.forEach(function(report) {
    const ms = JSON.parse(report.MsPerPages);
    if (!(report.ArticleId in map))
      map[report.ArticleId] = ms;
    else {
      for (var i = 0; i < ms.length; i++) map[report.ArticleId][i] += ms[i];
    }
  });
  Object.keys(map).forEach(function(key) {
      console.log(key + ': ' + map[key]);
  });
  console.log(Object.keys(map).length);
}

_merge_msperpages_by_articleid();